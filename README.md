# AlgoritmoDevTools

Suite de utilidades para el desarrollo diario sobre **SoftCerealCore** / **AlgoritmoCore**. Un único `.exe` que hospeda varias herramientas, comparte servicios comunes —SQLite local, user-secrets, conexiones guardadas— y se distribuye sin instalador.

La interfaz se dibuja en HTML con **Blazor Hybrid**, dentro de la misma aplicación de escritorio.

## Requisitos

- Windows 10 o 11.
- **WebView2 Runtime**. Viene con Windows 11 y llega a Windows 10 por Windows Update junto con Edge; en la práctica ya está. Para confirmarlo:
  ```bash
  reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv
  ```
- .NET 10 SDK para compilar. Para ejecutar el `.exe` final no hace falta nada: va todo incluido.
- `git` en el PATH, que lo usa el Schema Change Detector.
- El repositorio `AlgoritmoCore` clonado. La carpeta se elige en **Configuraciones**; por defecto es `%USERPROFILE%\source\repos`.
- `pandoc` sólo para el Convertidor a Markdown. Sin él, el resto funciona igual.

## Cómo correrlo

```bash
dotnet run --project AlgoritmoDevTools.Shell
```

Generar el ejecutable único:

```bash
dotnet publish AlgoritmoDevTools.Shell -c Release
```

Salida en `AlgoritmoDevTools.Shell/bin/Release/net10.0-windows/win-x64/publish/`, autocontenido, con el runtime de .NET 10 adentro.

### Modos de arranque

| Comando | Qué abre |
|---|---|
| sin argumentos | La ventana con la interfaz HTML |
| `--clasico` | La ventana de Windows Forms, mientras dure la transición |
| `--widget` | El widget de bandeja, sin ventana principal |
| verbos del menú contextual | Ejecuta la acción y sale, sin abrir nada |

## Las herramientas

Cada una tiene su documento de referencia, con sus reglas, sus decisiones y sus trampas:

| Herramienta | Qué hace | Documento |
|---|---|---|
| **Commands Maker** | Comandos de migraciones de EF Core por dominio | [referencia](AlgoritmoDevTools.Tools.CommandsMaker/README.referencia-tecnica-funcional-commands-maker.md) |
| **Secrets Manager** | User-secrets y conexiones guardadas | [referencia](AlgoritmoDevTools.Tools.SecretsManager/README.referencia-tecnica-funcional-secrets-manager.md) |
| **Schema Change Detector** | Si los commits traídos exigen migraciones | [referencia](AlgoritmoDevTools.Tools.ModelDriftChecker/README.referencia-tecnica-funcional-schema-change-detector.md) |
| **Selector de Servicios (Tye)** | Qué microservicios levanta el entorno local | [referencia](AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md) |
| **Convertidor a Markdown** | Documentos a Markdown para pasarle a un asistente | [referencia](AlgoritmoDevTools.Tools.MarkdownConverter/README.referencia-tecnica-funcional-convertidor-markdown.md) |

Transversales:

- [Interfaz HTML](AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md) — cómo está armada, qué puede y qué no.
- [Temas](AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md) — las cuatro paletas y cómo se aplican.

## Estructura

```
AlgoritmoDevTools/
├── AlgoritmoDevTools.Core/                     Abstracciones y servicios comunes
│   ├── Abstractions/                           ITool, IToolStorage
│   ├── Infrastructure/                         ToolStorage, ProcessRunner, Configuracion, RutasDeTrabajo
│   ├── Theming/                                Los cuatro temas y su aplicador
│   └── UI/                                     InputDialog, IconLoader
│
├── AlgoritmoDevTools.Integrations.SoftCerealCore/   Lo propio del stack
│   ├── SecretService.cs                        user-secrets (singleton compartido)
│   ├── SavedConnectionsRepository.cs           Conexiones guardadas
│   ├── DomainRepository.cs                     Dominios, compartidos entre herramientas
│   └── SQLService.cs                           Probar conexión y listar bases
│
├── AlgoritmoDevTools.UI/                       La interfaz en HTML
│   ├── Layout/Shell.razor                      Armazón: lateral, contenido, barra de estado
│   ├── Paginas/                                Una por herramienta, más Inicio y Configuraciones
│   ├── Componentes/                            Piezas compartidas
│   ├── Servicios/                              Lo que la interfaz le pide a la ventana
│   ├── CatalogoDeHerramientas.cs               Qué herramientas hay y qué componente las dibuja
│   └── wwwroot/app.css                         Los temas y todos los estilos
│
├── AlgoritmoDevTools.Shell/                    La aplicación de escritorio
│   ├── MainFormBlazor.cs                       Ventana que hospeda la interfaz HTML
│   ├── MainForm.cs                             Ventana clásica, bajo --clasico
│   ├── ZonaDeArrastre.cs                       Franja para soltar archivos
│   ├── DialogosDelSistema.cs                   Diálogos de Windows
│   └── Program.cs                              Arranque y verbos
│
└── AlgoritmoDevTools.Tools.*/                  Una por herramienta: servicios y vista clásica
```

## Servicios compartidos

### `SecretService.Shared`

Singleton. Lee los secretos **una sola vez al arrancar** y avisa por el evento `SecretsChanged`; quien los necesite escucha ese evento en lugar de volver a pedirlos.

La lectura tarda unos 2,2 segundos porque levanta el CLI de `dotnet`. **No la dispares de nuevo**: dos lecturas simultáneas contra el mismo archivo se bloquean entre sí.

### `ToolStorage(toolId)`

Resuelve a `%LOCALAPPDATA%/AlgoritmoDevTools/{toolId}/data.db`. Cada herramienta tiene su SQLite aislado, o comparte uno:

| Clave | Quién la usa |
|---|---|
| `CommandsMaker` | Commands Maker y Schema Change Detector (dominios) |
| `Shared` | Secrets Manager (conexiones), también el menú contextual |
| `ModelDriftChecker` | Sólo el detector (baselines) |
| `TyeServiceSelector` | Sólo el selector (perfiles) |

**La clave es lo que aísla los datos: cambiarla deja a la herramienta sin su historial.**

### `RutasDeTrabajo`

Dónde están los repositorios. Sale de Configuraciones y cae en `%USERPROFILE%\source\repos` si no hay preferencia. **Ninguna herramienta debe volver a armar esa ruta por su cuenta.**

Se resuelve en cada consulta y no se guarda en un campo, para que al cambiarla las herramientas abiertas tomen la nueva sin reiniciar.

### `ConfiguracionDevTools.Compartida`

Las preferencias que valen para toda la aplicación. Una sola instancia: dos leyendo el mismo archivo se pisarían al guardar.

### `BarraDeEstado`

El canal por el que una pantalla escribe en la barra de abajo. **Los avisos van ahí, no a un renglón dentro de la vista**: son estado de la aplicación, no contenido.

### `ProcessRunner`

`RunDotnet` y `RunPowerShell`, con stdout y stderr en UTF-8, `DOTNET_CLI_UI_LANGUAGE=en` y `Kill(entireProcessTree)` al cancelar.

**Si vas a llamar a `dotnet`, usá `RunDotnet` y no `RunPowerShell` con un `dotnet` adentro**: son dos procesos para el mismo comando, y se nota.

---

# Agregar una herramienta nueva

## 1. El proyecto

```bash
dotnet new classlib -n AlgoritmoDevTools.Tools.MiTool
```

En el `.csproj`:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

Referenciar `AlgoritmoDevTools.Core`, y `Integrations.SoftCerealCore` si necesita secretos o conexiones.

## 2. Los servicios

Van en `Services/`, **sin nada de interfaz adentro**. Es lo que permitió migrar las cinco herramientas a HTML sin tocarlos.

Reglas:

- **Todo I/O fuera del hilo de interfaz.** Git, `dotnet`, SQL y archivos van en `Task.Run`, con `CancellationToken` donde haya un botón de detener.
- **Patrón `TryXxx`.** Las operaciones de red y de disco devuelven un resultado con `Error` en lugar de tirar. Evita que el depurador rompa en "user-unhandled" al cruzar un `await`.
- **Las rutas de repositorio salen de `RutasDeTrabajo`.** Nunca `%USERPROFILE%\source\repos` escrito a mano.
- **El almacenamiento, por `ToolStorage`** con una clave propia, o una existente si los datos se comparten de verdad.

## 3. La pantalla

Un componente en `AlgoritmoDevTools.UI/Paginas/MiToolPagina.razor`, con **el código en su `.razor.cs`**: en el `.razor` sólo markup y sus `@using`.

```csharp
public partial class MiToolPagina : IDisposable
{
    [Inject] private MiServicio Servicio { get; set; } = default!;
    [Inject] private BarraDeEstado Barra { get; set; } = default!;
}
```

Estándares de la pantalla:

- **Título con `<TituloDePagina>`**, que trae el signo de pregunta con la explicación. Sin subtítulo debajo: lo que haya que aclarar va en ese tooltip.
- **Los avisos van a `BarraDeEstado`**, no a un renglón propio. Limpiala en `Dispose`.
- **Las confirmaciones son bloques dentro de la página.** El visor no muestra `alert`, `confirm` ni `prompt`: devuelven falso o nulo al instante.
- **El botón que despliega un bloque también lo cierra**, y abrir uno cierra el otro.
- **Ningún color escrito a mano.** Todo sale de las variables del tema; un literal queda igual en las cuatro paletas.
- **Clases ya disponibles**: `btn btn-primario`, `btn btn-secundario`, `campo`, `grupo`, `fila`, `chk`, `lista-chk`, `salida`, `confirmacion`, `metadatos`, `tarjeta`, `estado-exito` / `estado-error` / `estado-advertencia` / `estado-neutro`.
- **Una sola acción primaria por pantalla.** Si todo es primario, nada lo es.

## 4. Registrarla

En `MainFormBlazor.RegistrarServiciosDeHerramientas`:

```csharp
servicios.AddSingleton(new MiServicio(new ToolStorage("MiTool")));
```

En `CatalogoDeHerramientas.Todas`, respetando el orden en que se muestran:

```csharp
new HerramientaUI(
    "MiTool",                        // Id: el mismo que usa ToolStorage
    "Mi Herramienta",                // Nombre en el panel lateral
    "Qué hace, en una línea.",       // Va al signo de pregunta del título
    IconosSvg.MiIcono,
    typeof(Paginas.MiToolPagina)),
```

Y el ícono en `IconosSvg`: **SVG en línea de 16×16, trazo 1,6, sin relleno**, en la misma grilla que los demás. Es lo que hace que se lean como un juego y no como íconos sueltos; además toman el color del tema solos.

## 5. Si además va en la ventana clásica

Mientras exista `--clasico`, una herramienta puede tener su vista de Windows Forms: una clase que implemente `ITool` y un `UserControl`. En ese caso:

- Los colores **no se escriben**: el tema los aplica solo. Lo único que se declara es el rol de los controles que se salen de lo común, con la propiedad `Tag` del diseñador: `primario`, `lateral`, `sutil`, `separador`.
- Los colores de estado salen de `ColoresDeEstado`, nunca de `Color.Firebrick` y compañía.
- Registrarla en `Program.CrearHerramientas()` y en el `.csproj` del Shell.

Para una herramienta nueva conviene hacer sólo la pantalla HTML.

## 6. Lo que no puede hacer la interfaz HTML

Vale la pena saberlo antes de diseñar la pantalla:

| No puede | Cómo se resuelve |
|---|---|
| Saber la ruta en disco de un archivo elegido o soltado | `IDialogosDelSistema` abre el diálogo de Windows; para arrastrar, la franja de `ZonaDeArrastre` |
| Mostrar `alert`, `confirm`, `prompt` | Bloques dentro de la página |
| Abrir una ventana de Windows | Lo hace la ventana, por un servicio del espacio `UI/Servicios` |

El patrón es siempre el mismo: una interfaz en `AlgoritmoDevTools.UI/Servicios/`, su implementación en el Shell, y registro en `MainFormBlazor`.

## 7. Documentarla

Una herramienta nueva deja su documento en su propia carpeta:

```
AlgoritmoDevTools.Tools.MiTool/README.referencia-tecnica-funcional-mi-tool.md
```

Y una fila en la tabla de este README. Lo que tiene que decir: qué hace, las reglas en vigor, dónde guarda, las decisiones que no conviene revisitar, qué deberían cubrir las pruebas y la deuda conocida. **Las trampas verificadas son lo más valioso**: un síntoma que no se parece a su causa ahorra una tarde entera.

## Convenciones

- **Idioma**: interfaz y mensajes en español rioplatense (voseo). Código y comentarios en español cuando es contexto de negocio.
- **Comentarios**: sólo cuando aportan. Explican el *por qué*, no el *qué*.
- **Archivos nuevos**: UTF-8 **con BOM**. Sin él, Visual Studio lee con la codificación ANSI y los acentos de los comentarios se ven rotos; el compilador no avisa.
- **Miembros de una línea** con `=>`; con más de una sentencia, llaves. El cuerpo de un `if` va con llaves salvo una guarda de salida temprana.

## Widget de bandeja

El **mismo ejecutable** con `--widget`, sin ventana principal. Deja en la bandeja contra qué servidor y base apunta el proyecto, y a mano lo más usado: restaurar secretos, elegir base, los tres comandos de migración del dominio elegido.

No es un proyecto aparte: es una rama del arranque, así que comparte binario y código. Instancia única por `Mutex`. El estado se relee al abrir el menú y tras cada acción, **nunca por sondeo**.

> **Al desarrollar, corré el widget desde el publish y no desde `bin\Debug`.** Un widget residente mantiene los DLL tomados y cualquier compilación falla con `MSB3027`. Por eso el menú tiene **Salir** a mano.

## Publish

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
<DebugType>embedded</DebugType>
```

`IncludeAllContentForSelfExtract` no es opcional: es lo que WebView2 necesita para convivir con el archivo único. Hay además un target que borra los `.pdb` y `.xml` residuales del publish.

El proyecto del Shell usa **`Microsoft.NET.Sdk.Razor`** aunque sea una aplicación de escritorio. Con el SDK común no genera su manifiesto de recursos estáticos y la ventana abre en blanco, sin ningún error.
