# AlgoritmoDevTools

Suite de utilidades para el desarrollo diario sobre **SoftCerealCore** / **AlgoritmoCore**. Shell WinForms que hospeda varias tools como pestañas, comparte servicios comunes (SQLite local, user-secrets, conexiones guardadas) y se distribuye como un único `.exe`.

## Requisitos

- Windows 10/11.
- .NET 8 SDK (para compilar) — no hace falta runtime instalado para ejecutar el exe final, va todo bundleado.
- `git` en PATH (lo usa el Schema Change Detector).
- Repo `AlgoritmoCore` clonado en `~/source/repos/AlgoritmoCore`. Las tools asumen esa ruta hardcodeada.

## Cómo correrlo

**Debug (durante desarrollo):**

```bash
dotnet run --project AlgoritmoDevTools.Shell
```

**Generar el ejecutable único (Release):**

```bash
dotnet publish AlgoritmoDevTools.Shell -c Release
```

Salida: `AlgoritmoDevTools.Shell/bin/Release/net8.0-windows/win-x64/publish/AlgoritmoDevTools.Shell.exe` (~75 MB, self-contained, incluye runtime .NET 8).

## Estructura de la solución

```
AlgoritmoDevTools/
├── AlgoritmoDevTools.Core/                         Abstracciones comunes
│   ├── Abstractions/ITool.cs                       Contrato que implementa cada tool
│   ├── Abstractions/IToolStorage.cs                Storage SQLite aislado por tool
│   ├── Infrastructure/ToolStorage.cs               Implementación (%LOCALAPPDATA%/AlgoritmoDevTools/{id}/data.db)
│   ├── Infrastructure/ProcessRunner.cs             Wrapper de git/dotnet/powershell con UTF-8 y cancelación
│   ├── UI/InputDialog.cs                           Diálogo genérico para pedir texto
│   └── UI/IconLoader.cs                            Carga iconos embebidos por assembly
│
├── AlgoritmoDevTools.Integrations.SoftCerealCore/  Servicios específicos del stack AlgoritmoCore
│   ├── Constantes.cs                               Constantes (nombres de secret, project API, etc.)
│   ├── SecretService.cs                            Wrapper de `dotnet user-secrets` (shared singleton)
│   ├── SavedConnection.cs                          Modelo de conexión guardada
│   ├── SavedConnectionsRepository.cs               CRUD SQLite de conexiones
│   ├── DomainRepository.cs                         CRUD de dominios (compartido entre tools)
│   ├── SQLService.cs                               TryTestConnection + TryGetDatabases
│   └── ConnectionStringParser.cs                   Parseo simple de connection strings
│
├── AlgoritmoDevTools.Shell/                        Host WinForms + MainForm
│   ├── MainForm.cs                                 Nav lateral (ListView con iconos) + ContentPanel + StatusStrip
│   ├── Program.cs                                  Registra los ITool disponibles
│   └── Assets/app.ico                              Icono del ejecutable
│
├── AlgoritmoDevTools.Tools.CommandsMaker/
├── AlgoritmoDevTools.Tools.SecretsManager/
├── AlgoritmoDevTools.Tools.ModelDriftChecker/      Schema Change Detector
└── AlgoritmoDevTools.Tools.TyeServiceSelector/     Selector de Servicios (Tye)
```

Cada **Tool** es un `classlib` con:
- Una clase que implementa `ITool` (`Id`, `DisplayName`, `Description`, `Icon`, `CreateView()`).
- Un `UserControl` que hace de vista principal.
- `Assets/icon.ico` embebido como recurso.
- Servicios propios en `Services/`.

## Tools incluidas

### 🔵 Commands Maker

**Qué hace**: genera los comandos `Add-Migration`, `Remove-Migration` y `Update-Database` listos para pegar en la **Package Manager Console** de Visual Studio.

- Lee `Server` y `Database` del user-secret `SoftCerealCore.Development.ConnectionString`.
- Reemplaza las credenciales por `Integrated Security = true; MultipleActiveResultSets=True` (equivalente a lo que usan los scripts PowerShell del backend — corre como tu user de Windows con SA).
- Mantiene una lista local de dominios (Cereales, Contabilidad, etc.) que se comparte con el Schema Change Detector.
- Al dar click en Add/Remove/UpdateD, el comando se copia al clipboard.

**Storage**: `%LOCALAPPDATA%/AlgoritmoDevTools/CommandsMaker/data.db` — tabla `DOMINIOS`.

### 🔒 Secrets Manager

**Qué hace**: gestiona los `dotnet user-secrets` del proyecto `Algoritmo.Microservices.Shared.API` y un CRUD de **conexiones guardadas**.

- **Conexiones guardadas**: CRUD con validación — `Nuevo` / `Modificar` / `Eliminar`. Cada conexión tiene Server, User, Password (o Integrated Security) y se valida abriendo una conexión real antes de guardar.
- **DataBase** (combo externo al CRUD): cuando elegís una conexión, lista las BDs del server (`SELECT name FROM sys.databases`) para que puedas apuntar a una sin tener que crear otra conexión.
- **Listar Secretos**: re-ejecuta `dotnet user-secrets list --project Algoritmo.Microservices.Shared.API`. Los valores se muestran en **negrita** en el visor.
- **Modificar Secreto**: toma la conexión + BD seleccionadas y reescribe los secretos `SoftCerealCore.Development.ConnectionString` y `SoftCerealCore.DAPR.ConnectionString`.
- **Restaurar Secretos**: carga desde `secrets/SoftCerealCore.ConnectionString.json` (si existe en el repo).

**Storage**: `%LOCALAPPDATA%/AlgoritmoDevTools/Shared/data.db` — tabla `SavedConnections` con constraint único `(Server, DataBase, UserName)` y columna `UseIntegratedSecurity`. Lo consume también el Schema Change Detector.

**Status bar**: siempre muestra `Server: X | Base: Y` del secreto Development actual; se actualiza automáticamente cuando modificás un secreto.

### 🔄 Schema Change Detector (Model Drift Checker)

**Qué hace**: contesta la pregunta *"¿los commits que acabo de traer de master requieren correr migraciones?"*.

- Mantiene un **baseline** (SHA de git) por repo — el último commit donde confirmaste que tu BD local estaba migrada.
- Al verificar: corre `git diff --name-only <baseline> HEAD` y clasifica los archivos tocados en tres niveles:

  | Severidad | Regla | Qué significa |
  |---|---|---|
  | 🔴 Definitiva | `*/Algoritmo.*.Infrastructure/**/*DbContext*.cs` | Seguro hay que migrar |
  | 🔴 Probable | `*/Algoritmo.*.Infrastructure/**/*Configuration*.cs` | Muy probable que haya migración |
  | ⚠️ Posible | `*/Algoritmo.*.Domain/**/*.cs` | Archivo de dominio tocado — puede ser propiedad, lógica o comentario |

- **Filtros del nivel Possible** (se excluyen por falsos positivos conocidos):
  - Filename que termine en `Event`, `Rule`, `Repository`, `RepositoryAsync`, `Error`, `Errors`, `Request`, `Requests`, `Command`, `Commands`, `Handler`, `Handlers`, `Response`, `Responses`, `Query`, `Queries`.
  - Interfaces de repositorio (`I*Repository*`).
  - Archivos con `public enum`.
  - Archivos con `[NotMapped]`.
- **Deduplicación por nombre de clase**: si `Usuario.cs` aparece en 8 dominios diferentes, se muestra una sola vez con la lista de paths debajo.
- **Workflow típico**:
  1. Primera vez: click `Usar HEAD como baseline` (asume que tu BD está en ese estado).
  2. Hacés `git pull` → volvés a la tool → auto-verifica.
  3. Si dice 🔴: corré las migraciones → `Ya migré (mover baseline a HEAD)`.
  4. Si ✅: arrancá el backend tranquilo.

**Por qué no usamos `dotnet ef`**: porque el `AlgoritmoDbContextFactory` de AlgoritmoCore corre `CheckTablesInventoryResources` al construir el DbContext (requiere la función SQL `GetTableColumnsInfo` y permisos que no todos los usuarios tienen). El approach por git diff evita ese problema por completo — no toca la BD, no compila, no carga assemblies. Limitación: compara **código vs archivos**, no contra el esquema real de la BD. En la práctica cubre el 95% de los casos.

**Storage**: `%LOCALAPPDATA%/AlgoritmoDevTools/ModelDriftChecker/data.db` — tabla `SchemaBaseline` indexada por ruta del repo.

### 🚀 Selector de Servicios (Tye)

**Qué hace**: permite elegir, con checkboxes, **qué microservicios levantar** sin tener que tocar a mano el `tye.yaml`.

- **No modifica el `tye.yaml` original** (queda limpio en git). Lee la lista `services:` del master y genera un archivo derivado `tye.devtools.yaml` en la raíz de AlgoritmoCore (tiene que estar ahí para que las rutas relativas `project:` resuelvan).
- El toggle es **por comentarios de línea** (`# `): los servicios destildados se comentan en vez de borrarse, así re-tildarlos los descomenta sin perder su definición. Es reversible y preserva todo el formato (no se usa un parser YAML que reformatearía). Se togglea tanto el bloque en `services:` como la entrada del servicio en la extensión `dapr`, para que el archivo quede consistente.
- Al reabrir, el estado tildado/destildado se lee desde el `tye.devtools.yaml` generado; los servicios nuevos que aparezcan en el master se asumen activos.
- **Perfiles**: podés guardar la selección actual con un nombre (ej: *"Logística mínima"*, *"Solo Cereales+Stock"*) y volver a aplicarla desde el combo. CRUD completo: `Guardar perfil` / `Eliminar perfil`.
- **Copiar comando run**: copia al clipboard `dotnet tye run tye.devtools.yaml --watch`.
- **Workflow típico**: tildás los servicios (o elegís un perfil) → `Generar y guardar` → corrés `dotnet tye run tye.devtools.yaml --watch`.

**Storage**: `%LOCALAPPDATA%/AlgoritmoDevTools/TyeServiceSelector/data.db` — tabla `Profiles` (`Name` PK, `Services` CSV).

> El `tye.devtools.yaml` generado aparece como archivo sin trackear en AlgoritmoCore — conviene agregarlo al `.gitignore` del repo.

## Servicios compartidos

### `SecretService.Shared`
Singleton Lazy. Ejecuta `dotnet user-secrets list` una vez al inicio del Shell, cachea el resultado, y notifica cambios vía evento `SecretsChanged` (las vistas que lo escuchan — p.ej. el status bar — se actualizan solas).

### `ToolStorage(toolId)`
Resuelve a `%LOCALAPPDATA%/AlgoritmoDevTools/{toolId}/data.db`. Cada tool (o área) puede tener su propio SQLite aislado, o usar un ID compartido:
- `"CommandsMaker"` → compartido entre Commands Maker y Schema Change Detector (dominios).
- `"Shared"` → usado por Secrets Manager (conexiones).
- `"ModelDriftChecker"` → sólo el detector (baselines).

### `ProcessRunner`
- `RunDotnet(args, workingDirectory, cancellationToken)`
- `RunPowerShell(command, workingDirectory, cancellationToken)`
- Stdout/stderr en UTF-8, env var `DOTNET_CLI_UI_LANGUAGE=en`, `process.Kill(entireProcessTree)` al cancelar.

## Agregar una tool nueva

1. `dotnet new classlib -n AlgoritmoDevTools.Tools.MiTool -f net8.0`
2. Editar el `.csproj`: `<TargetFramework>net8.0-windows</TargetFramework>` + `<UseWindowsForms>true</UseWindowsForms>`.
3. Referenciar `AlgoritmoDevTools.Core` (y `Integrations.SoftCerealCore` si necesita secrets/conexiones).
4. Implementar `ITool`:
   ```csharp
   public sealed class MiTool : ITool
   {
       private static readonly Image? _icon = IconLoader.LoadEmbedded(typeof(MiTool).Assembly, "icon.ico");
       public string Id => "MiTool";
       public string DisplayName => "Mi Tool";
       public string Description => "Qué hace.";
       public Image? Icon => _icon;
       public UserControl CreateView() => new MiToolView(new ToolStorage(Id));
   }
   ```
5. Agregar `Assets/icon.ico` embebido con `<LogicalName>icon.ico</LogicalName>` en el csproj.
6. Registrar en `Shell.csproj` (ProjectReference) y en `Shell/Program.cs` (nueva instancia en el array `tools`).
7. Recompilar.

## Convenciones

- **Idioma**: UI y mensajes al usuario en español (voseo). Código, comentarios y commits pueden ser bilingües pero se prefiere español cuando es contexto de negocio (ej: `Dominio` vs `Domain`).
- **Tooltips**: cada botón principal tiene tooltip en español describiendo su efecto. Se setean en el constructor después de `InitializeComponent()` (no en el Designer, para que VS no los pise al regenerar).
- **Async UI**: todo I/O (git, dotnet, SQL) corre en `Task.Run`. Cancelación vía `CancellationTokenSource` disparada por botón "Detener" donde aplique.
- **Errors pattern**: operaciones de red/IO exponen `TryXxx` que devuelve `null` / result con `Error` en vez de tirar. Evita que VS rompa el debugger en "user-unhandled" al cruzar `await`.

## Configuración del publish

En `AlgoritmoDevTools.Shell.csproj`:

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
<DebugType>embedded</DebugType>
```

Más un target `AfterTargets="Publish"` que borra `.pdb`/`.xml` residuales dejando sólo el `.exe` en el folder de publish.
