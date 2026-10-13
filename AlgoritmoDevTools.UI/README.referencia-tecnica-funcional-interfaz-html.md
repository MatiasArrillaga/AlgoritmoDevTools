# Referencia técnica y funcional — Interfaz HTML (Blazor Hybrid)

Documento de la interfaz en HTML de Algoritmo DevTools: cómo está armada, qué hace cada pieza y qué cambia respecto de la ventana clásica de Windows Forms.

Implementado el 09/10/2026.

---

## 1. Objetivo funcional

Que la herramienta se vea y se comporte como una aplicación moderna, con la estética plana de los cuatro temas, sin dejar de ser un único `.exe` de escritorio y sin reescribir la lógica que ya funciona.

## 2. Qué es Blazor Hybrid acá

Un control `BlazorWebView` dentro de un formulario de Windows Forms. El contenido lo dibuja el motor de Edge —vía WebView2— a partir de componentes Razor, pero el proceso sigue siendo la misma aplicación de escritorio de siempre: mismo publish de un solo archivo, mismo widget de bandeja, mismo acceso al disco, al registro y a SQLite.

**No hay servidor web.** Los componentes corren en el proceso y llaman a los servicios directamente, como lo hacía la vista de Windows Forms.

### Requisito de la máquina

Hace falta el **WebView2 Runtime**. En Windows 11 viene con el sistema; en Windows 10 llega por Windows Update junto con Edge (verificado el 09/10/2026 en una máquina con build 19045: runtime 154 instalado sin intervención). Para confirmarlo en otra máquina:

```
reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv
```

Si faltara, Microsoft distribuye un instalador de 2 MB, o se puede empaquetar la versión fija dentro del `.exe`.

## 3. Cómo arranca

`Program.Main` abre `MainFormBlazor` cuando no hay argumentos. La ventana clásica quedó detrás del verbo `--clasico`, y `--html` sigue funcionando para los accesos directos que ya lo usaban.

| Pieza | Archivo | Rol |
|---|---|---|
| Ventana host | `AlgoritmoDevTools.Shell/MainFormBlazor.cs` | Crea el `BlazorWebView`, arma el contenedor de servicios y monta `Raiz` |
| Página host | `AlgoritmoDevTools.Shell/wwwroot/index.html` | El HTML donde se inyecta la aplicación |
| Componente raíz | `AlgoritmoDevTools.UI/Raiz.razor` | Sólo cuelga el armazón |
| Armazón | `AlgoritmoDevTools.UI/Layout/Shell.razor` | Descripción, panel lateral, contenido y barra de estado |
| Catálogo | `AlgoritmoDevTools.UI/CatalogoDeHerramientas.cs` | Qué herramientas hay y qué componente dibuja cada una |
| Estilos | `AlgoritmoDevTools.UI/wwwroot/app.css` | Los cuatro temas y todos los componentes visuales |
| Visor | `AlgoritmoDevTools.Shell/VisorEmbebido.cs` | El `BlazorWebView`, leyendo el contenido de adentro del ejecutable |
| Contenido web | `AlgoritmoDevTools.Shell/ContenidoWebEmbebido.cs` | Le contesta cada archivo por su ruta, desde los recursos del ensamblado |

El tema se aplica con `data-tema` en el nodo raíz del armazón: todas las variables de color cuelgan de ahí, así que cambiar ese atributo repinta la aplicación entera. Lee y escribe el mismo `ServicioDeTemas` que la ventana clásica, de modo que **el tema elegido en una aparece en la otra**.

## 4. Trampas que ya costaron una tarde

### El proyecto host necesita `Microsoft.NET.Sdk.Razor`

Con `Microsoft.NET.Sdk` a secas el proyecto no genera su manifiesto de recursos estáticos, `_framework/blazor.webview.js` devuelve 404 y **la ventana abre mostrando el texto inicial del `index.html` sin ningún error**. Ni excepción, ni log, ni proceso caído.

Para verificarlo sin abrir la ventana: en la carpeta de salida tiene que existir el manifiesto con el nombre del ensamblado del host (`AlgoritmoDevTools.staticwebassets.runtime.json`), no sólo el del proyecto de componentes.

Efecto secundario: el SDK de Razor incluye `wwwroot/**` por su cuenta, así que un `<Content Include="wwwroot\**">` escrito a mano rompe la compilación con `NETSDK1022`.

### El tema cuelga de `.shell`, no de `:root`

`data-tema` se aplica en el `<div class="shell">`, así que **todas las variables de color viven dentro de ese nodo**. El `<body>` queda afuera: su `color` y su `background` se resuelven siempre con `:root`, que es el tema claro.

No se nota hasta que alguien elige un tema oscuro. Ahí, todo elemento que no fije su propio `color` hereda del `<body>` —casi negro— y desaparece sobre el fondo oscuro: pasó con `h2` (el título de cada pantalla), `.saludo`, las etiquetas de los tildes y el texto de las confirmaciones, mientras el resto se veía bien porque sí declaraba `color: var(--texto-2)`.

Por eso `.shell` declara `background` y `color` con sus propias variables: es el nodo que cierra el tema, y lo que herede de él ya sale del tema elegido. Una regla nueva **no** necesita repetir `color`; lo que hay que recordar es que fuera de `.shell` el tema no llega.

Caso parecido, por si aparece de nuevo: el signo de pregunta del título dibujaba su círculo con `--borde-campo`. Ese borde está pensado para apoyarse sobre el fondo de un campo y sobre el fondo de la página se pierde, así que en los temas oscuros quedaba el signo flotando solo. Ahora el círculo sale de `currentColor`, que es el mismo color del signo.

### El publish de un solo archivo deja el contenido web afuera

`PublishSingleFile` no incluye los recursos estáticos. El SDK los trata como si atrás hubiera un servidor web que los va a leer del disco: los copia a una carpeta `wwwroot` al lado del ejecutable y les pone `ExcludeFromSingleFile`, **sin ninguna propiedad para pedirle lo contrario** —el metadato está escrito a mano en los targets del SDK, y en .NET 10 esos archivos ni siquiera pasan por la lista que arma el archivo único: se copian con una tarea aparte, `_PublishCopyStaticWebAssetsPreserveNewest`, bastante después de que el bundle ya está cerrado.

El `.exe` repartido sin esa carpeta abre una ventana en blanco que dice *There is no content at*, sin más explicación.

La salida es incrustarlos en el ensamblado y servirlos desde ahí:

| Pieza | Qué hace |
|---|---|
| Target `EmbeberElContenidoWeb` (csproj del Shell) | Toma `@(StaticWebAsset)` y agrega cada uno como `EmbeddedResource` con `LogicalName` igual a su ruta web, bajo el prefijo `web/` |
| `ContenidoWebEmbebido` | `IFileProvider` que traduce la ruta que pide el visor al nombre del recurso |
| `VisorEmbebido` | `BlazorWebView` que devuelve ese proveedor en `CreateFileProvider` |
| Target `CleanupPublishOutputs` | Borra la carpeta `wwwroot` y el `.staticwebassets.endpoints.json` que el publish deja igual |

Dos detalles del target que no se adivinan:

- **Va en `BeforeTargets="AssignTargetPaths"`, no en `PrepareResources`.** Resolver los recursos estáticos arrastra una cadena larga de targets, y con `BeforeTargets="PrepareResources"` esa cadena termina corriendo *después* de `PrepareResourceNames`: los `EmbeddedResource` se agregan, el log los muestra, y aun así nunca llegan al compilador. El síntoma es un ensamblado que compila sin un error y no tiene ningún recurso adentro.
- **Se filtra por `AssetRole == 'Primary'`.** Los `Alternative` son las copias comprimidas (`.gz`, `.br`), que existen para ahorrar ancho de banda en una descarga y acá sólo agrandarían el ejecutable.

Para verificarlo sin abrir la ventana, el ensamblado del Shell tiene que pesar cientos de KB y no decenas:

```bash
dotnet build -c Release -t:Rebuild
grep -a -c "web/_framework/blazor.webview.js" AlgoritmoDevTools.Shell/bin/Release/net10.0-windows/win-x64/AlgoritmoDevTools.dll
```

### El script va **sin** `autostart="false"`

```html
<script src="_framework/blazor.webview.js"></script>
```

El atributo `autostart="false"` pertenece al patrón de MAUI, donde el host llama a `Blazor.start()` por su cuenta. En Windows Forms nadie lo llama, así que el framework nunca arranca y **el síntoma es idéntico al del SDK equivocado**: la página se queda con su texto inicial, sin error.

### Visual Studio no toma el cambio de SDK en caliente

Después de cambiar el `Sdk=` del `.csproj` hay que recargar el proyecto o la solución. Mientras tanto Visual Studio sigue evaluando con el SDK anterior y reproduce el mismo síntoma aunque el archivo ya esté bien.

### Un diálogo de Windows abierto en el acto cierra la aplicación

Es la trampa más cara de todas, porque **no deja rastro en ningún lado al que uno mire**: no hay excepción, no hay log, no hay pantalla de error. La ventana desaparece a mitad de un clic.

Un diálogo modal de Windows Forms —`OpenFileDialog`, `SaveFileDialog`, `FolderBrowserDialog`— corre su propio bucle de mensajes. Si se lo abre desde el manejador de un `@onclick`, ese bucle arranca **adentro** del aviso con el que WebView2 está entregando el clic. El navegador lo detecta como reentrada y aborta el proceso.

Lo único que queda es el registro de eventos de Windows:

```text
Nombre del módulo con errores: EmbeddedBrowserWebView.dll
Código de excepción: 0x80000003
```

`0x80000003` es un breakpoint, no una violación de acceso: el navegador se detuvo a propósito. Para confirmar que fue el diálogo y no otra cosa, en el `Report.wer` del informe archivado —en `C:\ProgramData\Microsoft\Windows\WER\ReportArchive`, en UTF-16— tienen que aparecer entre los módulos cargados `comdlg32.dll` y `explorerframe.dll`: el diálogo alcanzó a empezar a abrirse.

**La salida** es `DialogosDelSistema.Diferir`, que abre el diálogo con `BeginInvoke` —o sea, en el siguiente ciclo del bucle de mensajes, ya fuera del aviso— y devuelve una `Task` que se completa cuando el usuario lo cierra. Por eso los tres métodos de `IDialogosDelSistema` son asincrónicos y hay que esperarlos con `await`: **no hay forma de ofrecerlos sincrónicos sin cerrar la aplicación**.

```csharp
// Mal: cierra la aplicación.
var ruta = Dialogos.ElegirCarpeta("...");

// Bien.
var ruta = await Dialogos.ElegirCarpeta("...");
```

Vale para cualquier ventana modal que se abra desde un evento de la página, no sólo para estos tres diálogos.

### El visor no muestra los diálogos del navegador

`alert`, `confirm` y `prompt` no aparecen. Toda confirmación —borrar un perfil, borrar una conexión, mover el baseline— es un bloque dentro de la propia página, con la clase `confirmacion`.

## 5. Lo que la interfaz HTML no puede hacer sola

El motor de Edge **no entrega la ruta en disco** de un archivo elegido o arrastrado: por seguridad sólo da su contenido. Como las herramientas trabajan con rutas reales —pandoc deja el `.md` al lado del original—, elegir archivos pasa por el diálogo de Windows.

Eso resuelve `IDialogosDelSistema` (`AlgoritmoDevTools.UI/Servicios/`), implementado por `DialogosDelSistema` en el Shell. El diálogo se abre sobre la ventana principal y se marshalea al hilo de la interfaz, porque el llamador es un componente que no necesariamente está en él.

## 5.1. La barra lateral

Es una franja de 60px pintada con el **acento del tema**: la única superficie de la aplicación que lo usa como fondo, y por eso funciona como identidad sin competir con el contenido. Los íconos van en blanco al 82% y el elegido queda sobre un bloque claro, que en una franja de un solo color es lo que mejor se lee.

El tooltip es propio y no el del sistema: el del `title` tiene la demora y el aspecto de Windows, que en esta pantalla desentona. Sale de `data-tooltip` con un `::after`, y por eso **la franja no puede recortar su contenido** — lleva `overflow: visible` a propósito. Con siete ítems entra sin scroll; si algún día no entraran, el tooltip habría que sacarlo del flujo antes de poner scroll.

Cada tema define su `--tooltip-fondo`: el acento llevado casi al negro, para que el globo se lea como su sombra.

## 6. Diferencias con la ventana clásica

| Qué | Clásica | HTML | Por qué |
|---|---|---|---|
| Arrastrar y soltar documentos | Sí | Sí | Lo resuelve la ventana, no el contenido web (sección 5) |
| Elegir archivos | Diálogo de Windows | Diálogo de Windows | Vía `IDialogosDelSistema` |
| Editor de conexión | Ventana modal aparte | Bloque dentro de la página | El visor no muestra diálogos del navegador |
| Confirmaciones | `MessageBox` | Bloque dentro de la página | Ídem |
| Avisos de error | `MessageBox` | Línea de estado | Menos interrupción para el mismo aviso |
| Visor de secretos | Texto con claves en negrita | Texto plano | El resaltado venía del `RichTextBox` |
| Panel lateral | Lista con nombres | Franja de íconos del color del acento | Pedido del usuario, al modo de TeamViewer |

### El arrastre lo maneja la ventana

`MainFormBlazor` pone `WebView.AllowExternalDrop = false`. Con el arrastre externo habilitado, el motor de Edge se queda con el evento y al componente sólo le llegan los bytes; apagándolo, el drop pasa de largo hasta el formulario, que sí recibe la ruta real.

De ahí viaja a los componentes por `IArchivosArrastrados`, que avisa dos cosas: qué se soltó, con sus rutas, y si hay un arrastre en curso —eso último es lo que enciende la zona de soltar—. El componente del convertidor se suscribe al montarse y se da de baja al destruirse, así que sólo reacciona mientras está en pantalla.

## 7. Cómo migrar una herramienta

1. Componente en `AlgoritmoDevTools.UI/Paginas/<Herramienta>Pagina.razor`.
2. Inyectar sus servicios con `@inject`. **No se tocan los servicios**: son los mismos que usa la ventana clásica.
3. Registrar el servicio en `MainFormBlazor.RegistrarServiciosDeHerramientas`, con **la misma clave de `ToolStorage`** que usa su `ITool`. Cambiarla deja a la herramienta sin su historial.
4. Apuntar la entrada del catálogo al componente con `typeof(...)`.

El armazón lo dibuja con `DynamicComponent`. Una entrada sin componente muestra el marcador de pendiente.

## 8. Estado de la migración

| Herramienta | Componente | Estado |
|---|---|---|
| Secrets Manager | `SecretsManagerPagina` | Migrada |
| Tye Service Selector | `TyeServiceSelectorPagina` | Migrada |
| Commands Maker | `CommandsMakerPagina` | Migrada |
| Markdown Converter | `MarkdownConverterPagina` | Migrada, sin arrastrar y soltar |
| Model Drift Checker | `ModelDriftCheckerPagina` | Migrada |

Las vistas de Windows Forms **siguen en el repositorio** y funcionando bajo `--clasico`. Conviene dejarlas hasta que las pantallas nuevas tengan uso real encima.

## 9. Deuda conocida

| Qué | Por qué está así |
|---|---|
| Claves resaltadas en el visor de secretos | Era formato del `RichTextBox`; se puede rehacer con marcado |
| Warning `MSB3277` de `WindowsBase` | El paquete de WebView2 trae también la variante WPF, que no se usa |
| Dos interfaces en paralelo | Deliberado mientras dura la transición |
| Los textos de la interfaz están en el código | Es la convención vigente del repositorio, que no tiene archivo de recursos |

---

**Este documento debe actualizarse** cuando se migre o se dé de baja una herramienta, cuando cambie el contrato de `IDialogosDelSistema`, o cuando se salde alguna de las deudas de la sección 9.
