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

Queda **un solo archivo**, `DevTools.exe`, en `AlgoritmoDevTools.Shell/bin/Release/net10.0-windows/win-x64/publish/`: autocontenido, con el runtime de .NET 10 adentro y sin nada al lado. Se copia a donde sea y corre; no necesita instalador ni permisos de administrador.

> El nombre sale del `<AssemblyName>` del csproj, y es el que el actualizador busca entre los archivos del release. Cambiarlo obliga a cambiar también `NombreDelArchivo` en `Actualizador` y la ruta del `files:` en el workflow.

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
| **Disk Cleaner** | Qué ocupa el disco, con qué riesgo tiene borrar cada cosa | [referencia](AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md) |

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

### `ArranqueDelWidget`

Si el widget de bandeja arranca con la sesión de Windows. Se prende desde Configuraciones, que es donde está todo lo demás que la aplicación deja puesto en el sistema; el menú del widget no lo repite.

Acá viven el nombre de la entrada, el verbo y el nombre del mutex, porque los usan los dos extremos: esta pantalla para saber si hay un widget y para levantarlo, y el widget al nacer para asegurarse de ser uno solo.

**Lo que queda residente es el widget, no la ventana**: al iniciar sesión deja el ícono en la bandeja, sin abrir nada.

El tilde aparece apagado cuando la entrada existe pero apunta a otro ejecutable —publicaste el exe en otra carpeta—, para que al prenderlo se regenere contra el que está corriendo.

La sección también dice si hay un widget corriendo y lo levanta si no, para no tener que cerrar sesión y volver a entrar. Se pregunta por el **mutex** y no por los procesos: el mismo ejecutable corre la ventana y los verbos del menú contextual, así que encontrar un proceso con ese nombre no dice nada.

**Los íconos.** El del ejecutable y la ventana es el engranaje a color. El de la bandeja no: a 16 píxeles un engranaje con degradado es una mancha, así que hay dos versiones de un solo trazo en `AlgoritmoDevTools.Shell/Assets/`. **El nombre dice sobre qué fondo van, no de qué color son**: `bandeja-clara.ico` lleva el trazo oscuro y `bandeja-oscura.ico` el blanco.

Cuál se usa lo decide `TemaDeWindows.BarraDeTareasOscura()`, y es **el único lugar donde manda el tema de Windows y no el de DevTools**: el ícono vive en la barra de tareas, así que lo que tiene que contrastar es ese fondo. Un ícono blanco sobre una barra clara no se ve, aunque la ventana esté en modo oscuro. El widget escucha `SystemEvents.UserPreferenceChanged` y lo cambia en caliente; sin eso quedaría del color anterior hasta que alguien lo reinicie.

### `MenusContextuales`

Qué herramientas dejan sus acciones en el clic derecho de Windows. Vive en Configuraciones, con una casilla por herramienta.

Antes cada pantalla tenía sus botones de agregar y quitar, y para saber qué había puesto en el explorador había que recorrerlas una por una. El menú es uno solo —todas cuelgan de la misma raíz `DevTools` y comparten su presupuesto— así que decidirlo repartido no tenía mucho sentido.

**Prender y apagar es todo lo que hay.** Qué muestra cada grupo lo sigue armando la herramienta con lo que tenga guardado en ese momento, y lo reescribe sola cuando eso cambia: agregar una conexión la hace aparecer en el menú sin pasar por Configuraciones. Por eso acá no se guarda nada — el registro de Windows ya dice qué está instalado, y preguntárselo es más barato que mantener una copia que se puede desincronizar.

**El presupuesto es de 16 ítems en todo el árbol**, submenús incluidos y contando lo que hay adentro, y es compartido entre las herramientas que cuelgan de `DevTools`. Pasarse no da ningún error: el explorador dibuja los primeros 16 y descarta el resto en silencio, así que el síntoma es "me faltan opciones" sin nada que lo explique. La sección muestra cuántos van usados y avisa al llegar al tope. El Markdown Converter no cuenta: su verbo cuelga de la extensión del archivo, no de la raíz.

Para sumarle una herramienta: una entrada más en `MenusContextuales.Todos`, con cómo se pregunta, se instala y se quita su menú.

### `TransferenciaDeConfiguracion`

Arma y lee el archivo con el que un usuario le pasa su configuración a otro. Está en Configuraciones, y existe porque alguien que recién entra arranca con todo vacío: los trece dominios, las conexiones a cada servidor y los perfiles de servicios, todo a mano.

**Se elige qué llevar.** Al exportar aparece un árbol con una rama por herramienta: la herramienta, sus opciones y sus elementos uno por uno. Arranca todo marcado —lo normal es querer pasar todo, y desmarcar lo que sobra es menos trabajo que marcar lo que falta— menos las contraseñas. Desde ahí se puede dejar afuera la conexión a producción o un perfil que se armó para una prueba puntual.

Para sumarle una herramienta al árbol: su lista en `SeleccionDeExportacion`, llenarla en `PrepararSeleccion`, leerla en `Armar` y colgar un `<RamaDeExportacion>` más en la pantalla.

**Qué puede viajar y qué no.** Van las tres cosas que sirven igual en cualquier máquina:

| Va | De dónde sale |
|---|---|
| Conexiones guardadas | `Shared` → `SavedConnections` |
| Dominios | `CommandsMaker` → `DOMINIOS` |
| Perfiles del selector de servicios | `TyeServiceSelector` → `Profiles` |

Queda afuera, a propósito, todo lo que es de la máquina o del avance personal de cada uno: los dominios usados recientemente, el baseline de migraciones —dice hasta qué commit migró uno—, el tema y la carpeta de repositorios. Importar cualquiera de esos le cambiaría al que recibe algo que no es suyo.

**Las contraseñas sólo van si se piden.** Están guardadas en texto plano, así que un archivo con contraseñas adentro es una credencial y hay que tratarlo como tal. El tilde arranca apagado en cada exportación —no se recuerda a propósito— y la pantalla avisa cuando se enciende. Las conexiones con autenticación de Windows nunca la llevan, aunque tengan una guardada: cada uno entra con su cuenta.

La contraseña se lee del repositorio recién al escribir el archivo, y sobre **copias** de lo que muestra el árbol. Escribirla sobre el árbol la dejaría puesta: alcanzaría con marcar el tilde, exportar, desmarcarlo y volver a exportar para que el segundo archivo las llevara igual, sin que nada en pantalla lo dijera.

**Importar agrega, nunca pisa.** El que importa puede tener la misma conexión con otra contraseña, dominios propios o un perfil con el mismo nombre y otros servicios adentro; sobrescribirlos sería romperle lo que ya tenía andando. Lo repetido se cuenta y se informa, así el número que ve no tiene por qué dar con el del archivo. Importar dos veces el mismo archivo no duplica nada.

El archivo es un JSON con un número de `Formato`: uno más nuevo que el que entiende la aplicación se rechaza con un mensaje en vez de leerse a medias.

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
- **Cada sección se encabeza con `<TituloDeGrupo>`**, que trae el mismo signo de pregunta. Ninguna sección lleva arriba un párrafo que la explique: ese texto ocupa lugar todas las veces para algo que se lee una sola, así que va adentro del signo, donde aparece sólo cuando alguien lo busca.
- **Los avisos van a `BarraDeEstado`**, no a un renglón propio. Limpiala en `Dispose`. Vale para los dos finales de una operación: "exportado" y "no se pudo exportar" van los dos a la barra.
- **Las confirmaciones son bloques dentro de la página.** El visor no muestra `alert`, `confirm` ni `prompt`: devuelven falso o nulo al instante.
- **El botón que despliega un bloque también lo cierra**, y abrir uno cierra el otro.
- **Ningún color escrito a mano.** Todo sale de las variables del tema; un literal queda igual en las cuatro paletas.
- **Clases ya disponibles**: `btn btn-primario`, `btn btn-secundario`, `campo`, `fila`, `chk`, `lista-chk`, `salida`, `confirmacion`, `metadatos`, `tarjeta`, `sub`, `estado-exito` / `estado-error` / `estado-advertencia` / `estado-neutro`. La clase `grupo` la pone `<TituloDeGrupo>`: no se escribe a mano.
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

Si la herramienta necesita algo configurable, una entrada más en `Configuraciones.Secciones` y su bloque en el markup. La pantalla es un menú a la izquierda con el contenido al lado, y las secciones de un mismo grupo tienen que ir seguidas en la lista: el agrupado sale de ahí.

Si la herramienta deja algo en el menú contextual del explorador, una entrada más en `MenusContextuales.Todos`. **La pantalla de la herramienta no lleva botones para eso**: prender y apagar los menús se hace desde Configuraciones, en un solo lugar. Lo que sí queda en la pantalla es volver a escribir el menú cuando cambia el dato que lo alimenta —un `Sincronizar` después de cada alta o baja—, porque el menú es estático y si no sigue mostrando lo viejo.

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

> **Un diálogo de Windows no se abre en el acto: hay que esperarlo.** Los tres métodos de `IDialogosDelSistema` devuelven `Task` y van con `await`. No es por rendimiento. Un diálogo modal corre su propio bucle de mensajes, y abrirlo mientras el visor está entregando el clic es volver a entrar en el navegador mientras trabaja: WebView2 **aborta el proceso entero**, sin excepción, sin mensaje y sin nada que se pueda atrapar. La ventana desaparece y listo. `DialogosDelSistema.Diferir` lo deja para el siguiente ciclo del bucle, ya fuera del aviso.

## 7. Documentarla

Una herramienta nueva deja su documento en su propia carpeta:

```
AlgoritmoDevTools.Tools.MiTool/README.referencia-tecnica-funcional-mi-tool.md
```

Y una fila en la tabla de este README. Lo que tiene que decir: qué hace, las reglas en vigor, dónde guarda, las decisiones que no conviene revisitar, qué deberían cubrir las pruebas y la deuda conocida. **Las trampas verificadas son lo más valioso**: un síntoma que no se parece a su causa ahorra una tarde entera.

## 8. La lista completa

Los siete pasos de arriba son el camino mínimo. Esta es la lista entera de lugares que tocar, para que no quede ninguno a medias: **lo que falte no rompe la compilación**, simplemente la herramienta no aparece donde debería y nadie se entera hasta que alguien la busca.

| Hay que | Dónde | ¿Siempre? |
|---|---|---|
| Crear el proyecto | `AlgoritmoDevTools.Tools.MiTool` | Sí |
| Referenciarlo | csproj del Shell y de `AlgoritmoDevTools.UI` | Sí |
| Darle su almacenamiento | `new ToolStorage("MiTool")`, con una clave que no cambie nunca | Si guarda algo |
| Registrar sus servicios | `MainFormBlazor.RegistrarServiciosDeHerramientas` | Si guarda algo |
| Escribir la pantalla | `AlgoritmoDevTools.UI/Paginas/MiToolPagina.razor` + `.razor.cs` | Sí |
| Anotarla en el catálogo | `CatalogoDeHerramientas.Todas`, en el orden en que va en el menú | Sí |
| Dibujar su ícono | `IconosSvg`: SVG en línea 16×16, trazo 1,6, sin relleno | Sí |
| Sumar su menú del explorador | `MenusContextuales.Todos`, y un `Sincronizar` en la pantalla tras cada alta o baja | Si deja algo en el clic derecho |
| Sumar lo suyo al archivo que se exporta | `SeleccionDeExportacion`, `PrepararSeleccion`, `Armar` y una `<RamaDeExportacion>` más | Si guarda algo que le sirva a otro |
| Sumar su sección de ajustes | `Configuraciones.Secciones` y su bloque en el markup | Si tiene algo configurable |
| Escribir su documento | `README.referencia-tecnica-funcional-mi-tool.md`, en su carpeta | Sí |
| Anotarla en la tabla de herramientas | Este README | Sí |

**Lo que no hay que hacer**: poner botones de "agregar al menú contextual" en la pantalla de la herramienta —eso vive en Configuraciones—, escribir colores a mano, ni dejar los avisos en un renglón de la vista en lugar de la barra de estado.

## Convenciones

- **Idioma**: interfaz y mensajes en español rioplatense (voseo). Código y comentarios en español cuando es contexto de negocio.
- **Comentarios**: sólo cuando aportan. Explican el *por qué*, no el *qué*.
- **Archivos nuevos**: UTF-8 **con BOM**. Sin él, Visual Studio lee con la codificación ANSI y los acentos de los comentarios se ven rotos; el compilador no avisa.
- **Miembros de una línea** con `=>`; con más de una sentencia, llaves. El cuerpo de un `if` va con llaves salvo una guarda de salida temprana.

## Widget de bandeja

El **mismo ejecutable** con `--widget`, sin ventana principal. Deja en la bandeja contra qué servidor y base apunta el proyecto, y a mano lo más usado: restaurar secretos, elegir base, los tres comandos de migración del dominio elegido.

No es un proyecto aparte: es una rama del arranque, así que comparte binario y código. Instancia única por `Mutex`. El estado se relee al abrir el menú y tras cada acción, **nunca por sondeo**.

> **Al desarrollar, corré el widget desde el publish y no desde `bin\Debug`.** Un widget residente mantiene los DLL tomados y cualquier compilación falla con `MSB3027`. Por eso el menú tiene **Salir** a mano.

## Actualizaciones

La aplicación se actualiza sola desde los releases de GitHub. El repositorio es público, así que la consulta no lleva credenciales: nada que embeber en el ejecutable ni que repartirle al equipo.

**Sacar una versión** son tres pasos:

```bash
# 1. subir el <Version> de AlgoritmoDevTools.Shell.csproj, commitear y pushear a master
# 2. etiquetar con el mismo numero, con la v adelante
git tag -a v1.2.0 -m "Titulo corto de la version

- Que cambio, contado para quien lo va a usar
- Una linea por cosa" && git push origin v1.2.0
```

**El mensaje de la etiqueta es lo que lee el usuario.** El workflow lo copia tal cual al cuerpo del release, y eso es lo que la pantalla de Actualizaciones muestra **antes** de bajar nada: es la única oportunidad de contarle qué va a cambiar. Tiene que entenderse sin el código al lado, y se lee en texto plano —la pantalla no interpreta markdown—, así que viñetas con guión y poco más. Anotada con `-a` y con mensaje: una etiqueta liviana deja el release en blanco.

`generate_release_notes` por su cuenta no alcanzaba, y por eso las primeras versiones salieron mudas: arma la lista a partir de los pull requests, y acá se pushea derecho a master, así que dejaba el release con el enlace de comparación y nada más. Sigue puesto, debajo del mensaje, porque el enlace es útil.

Y verificar que haya salido, porque si el workflow falla nadie se entera: no hay release y las instalaciones siguen viendo la anterior.

```bash
gh run list --limit 1
gh release view v1.2.0
```

El workflow `.github/workflows/publicar.yml` compila, publica el `.exe` y crea el release. **La etiqueta tiene que coincidir con el `<Version>`**: es el número que queda adentro del ejecutable y con el que cada instalación decide si la de GitHub es más nueva que la suya. Si no coinciden, el workflow falla antes de publicar nada, en vez de dejar un release que nadie va a poder usar para actualizar.

**Del lado de la aplicación**, el armazón pregunta al arrancar —después de los secretos y sin esperar la respuesta— y el inicio lo avisa si hay algo nuevo. En Configuraciones está la versión instalada y el botón para forzar la revisión y para instalar. Mientras no haya nada más nuevo no se muestra nada: no hay por qué anunciar que uno está al día.

Una vez bajada, el aviso cambia: ya no queda nada por descargar y lo único que falta es reiniciar, así que el botón pasa a ser **Reiniciar ahora**, en el inicio y en Configuraciones. Cierra la ventana y vuelve a abrir el ejecutable, sin que nadie tenga que hacerlo a mano. Quien tenga algo a medio hacer en otra pantalla simplemente no lo toca: la versión nueva ya está puesta y empieza a correr la próxima vez que se abra.

El cierre no es inmediato sino diferido con `BeginInvoke`, por el mismo motivo que los diálogos de Windows: se llama desde un clic que el visor todavía está entregando, y destruir el navegador adentro de su propio aviso cierra el proceso de mala manera. Está contado en `DialogosDelSistema`, en el Shell.

**Cómo se reemplaza a sí mismo.** Un ejecutable en uso no se puede sobrescribir, pero Windows sí deja **renombrarlo**, y esa es toda la maniobra: el actual se corre a `DevTools.exe.anterior` y el nuevo ocupa su lugar. Por eso no hace falta cerrar el widget, que corre del mismo archivo y sigue andando desde el renombrado hasta que alguien lo cierre. El `.anterior` se borra en el siguiente arranque, que es cuando ya nadie lo está ejecutando.

**El reinicio espera al proceso que actualizó.** El ejecutable nuevo se arranca con `--reinicio <numero-de-proceso>` y lo primero que hace es esperar a que ése termine —hasta diez segundos— antes de tocar el `.anterior`. Sin esa espera los dos conviven un instante: el nuevo va a borrar el viejo cuando el viejo todavía se está ejecutando, no puede, y queda en la carpeta hasta el arranque siguiente. Eso es lo que hacía parecer que habían quedado dos DevTools instalados.

Mientras no se pueda borrar —el widget abierto, sin ir más lejos, que corre del mismo archivo— el `.anterior` queda con el atributo de oculto. Sigue funcionando igual para quien lo está ejecutando y desaparece de la carpeta, que es lo único que importaba: a la vista era un segundo ejecutable de 55 MB con el nombre casi igual al bueno.

**Un ejecutable en uso tampoco se deja borrar, y Windows no lo avisa como tal.** Si el widget quedó abierto de una actualización anterior está corriendo del `.anterior`, y el intento de borrarlo para reutilizar el nombre falla con un **acceso denegado**, no con un error de archivo en uso, que es lo que uno esperaría. La actualización entera se caía ahí diciendo que no había permiso para escribir en la carpeta, con la carpeta perfecta. Por eso al viejo se le busca un nombre libre —`.anterior`, y si está tomado `.anterior2`— en vez de insistir con uno solo, y el arranque borra todos los que pueda.

El orden importa y está probado: se baja a un temporal primero, y si el segundo paso falla el viejo vuelve a su nombre. Una descarga cortada o una carpeta sin permiso de escritura dejan todo como estaba. Cuando Windows rechaza la escritura, el mensaje dice **en cuál de los dos pasos** fue —crear el archivo de la descarga o reemplazar el ejecutable— y en qué carpeta: son permisos distintos, y saber cuál falló es la diferencia entre poder arreglarlo y volver a probar a ciegas.

**Para probarlo sin publicar**, la variable de entorno `DEVTOOLS_RELEASES` reemplaza la dirección que se consulta. Sin ella puesta, que es lo normal, se consulta GitHub.

Apuntándola a un servidor local que devuelva este JSON se recorre el camino entero —el aviso en el inicio, la descarga y el reemplazo— sin publicar nada:

```json
{
  "tag_name": "v9.9.9",
  "body": "Lo que se muestra en la pantalla.",
  "assets": [
    { "name": "DevTools.exe", "browser_download_url": "http://localhost:51888/DevTools.exe" }
  ]
}
```

Hay que compilar dos binarios con versiones distintas: uno viejo que hace de instalado y uno nuevo que el servidor entrega como release.

```bash
dotnet publish AlgoritmoDevTools.Shell -c Release -p:Version=0.9.0 -o <carpeta-vieja>
dotnet publish AlgoritmoDevTools.Shell -c Release -o <carpeta-release>
```

Así se verificó esta función: el ejecutable de la carpeta vieja termina con el mismo hash que el del release y, al tocar **Reiniciar ahora**, la ventana vuelve sola corriendo la versión nueva. La carpeta queda con un único ejecutable: el arranque esperó a que terminara el proceso anterior y recién entonces borró el `.anterior`.

El caso del widget se prueba aparte, porque es el que fallaba: se abre el widget, se corre el ejecutable a `.anterior` con el widget andando —que es el estado en que lo deja una actualización— y recién entonces se actualiza. Tiene que completarse igual, usando `.anterior2`, y la carpeta queda con el ejecutable nuevo y el `.anterior` que el widget sigue ejecutando.

## Publish

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
<DebugType>embedded</DebugType>
```

`IncludeAllContentForSelfExtract` no es opcional: es lo que WebView2 necesita para convivir con el archivo único.

**El contenido web no entra solo en el `.exe`.** `PublishSingleFile` deja afuera los recursos estáticos —el `index.html`, `app.css`, `blazor.webview.js`—: el SDK da por sentado que atrás hay un servidor web que los va a leer del disco, así que los copia a una carpeta `wwwroot` al lado del ejecutable y los marca como excluidos del archivo único, sin propiedad que lo evite. Un `.exe` repartido sin esa carpeta abre una ventana en blanco que dice *There is no content at*.

Lo resuelve el target `EmbeberElContenidoWeb`, que los incrusta como recursos del ensamblado antes de compilar, y `ContenidoWebEmbebido`, que se los sirve al visor por la misma ruta que pediría un navegador. El target de limpieza borra después los `.pdb`, los `.xml`, el `.staticwebassets.endpoints.json` y la carpeta `wwwroot` que el publish deja igual, de manera que **lo único que queda es el ejecutable**.

El proyecto del Shell usa **`Microsoft.NET.Sdk.Razor`** aunque sea una aplicación de escritorio. Con el SDK común no genera su manifiesto de recursos estáticos y la ventana abre en blanco, sin ningún error.

## Deuda conocida

| Qué | Estado |
|---|---|
| `System.Security.Cryptography.Pkcs` 9.0.4 con vulnerabilidad **alta** ([GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx)) y otra moderada | Llega como transitiva de `Microsoft.Data.SqlClient` 6.1.3, así que se arregla subiendo esa, y ese salto de major **merece su propio cambio**: la librería ya cambió defaults de cifrado entre versiones y tanto el Secrets Manager como el Commands Maker dependen de ese comportamiento |

**El detalle que importa de esta deuda no es el paquete, es cómo apareció.** Estuvo invisible todo el tiempo porque el feed interno `packages.algoritmo.com.ar` no respondía, y con el feed caído el SDK **no audita nada**: en lugar de avisar, emite un `NU1900` diciendo que no pudo consultar los datos de vulnerabilidad.

O sea que un build sin advertencias de seguridad puede significar dos cosas muy distintas:

```bash
# Esto NO prueba que no haya paquetes vulnerables si el feed no contesta.
dotnet build -c Release

# Esto sí: lo pregunta y falla si no puede.
dotnet list package --vulnerable --include-transitive
```

Conviene correr el segundo antes de dar por buena una verificación de seguridad, y desconfiar de cualquier `NU1900` en la salida: mientras esté, la auditoría no está corriendo.
