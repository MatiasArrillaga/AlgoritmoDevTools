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

El tema se aplica con `data-tema` en el nodo raíz del armazón: todas las variables de color cuelgan de ahí, así que cambiar ese atributo repinta la aplicación entera. Lee y escribe el mismo `ServicioDeTemas` que la ventana clásica, de modo que **el tema elegido en una aparece en la otra**.

## 4. Trampas que ya costaron una tarde

### El proyecto host necesita `Microsoft.NET.Sdk.Razor`

Con `Microsoft.NET.Sdk` a secas el proyecto no genera su manifiesto de recursos estáticos, `_framework/blazor.webview.js` devuelve 404 y **la ventana abre mostrando el texto inicial del `index.html` sin ningún error**. Ni excepción, ni log, ni proceso caído.

Para verificarlo sin abrir la ventana: en la carpeta de salida tiene que existir el manifiesto con el nombre del ensamblado del host (`AlgoritmoDevTools.staticwebassets.runtime.json`), no sólo el del proyecto de componentes.

Efecto secundario: el SDK de Razor incluye `wwwroot/**` por su cuenta, así que un `<Content Include="wwwroot\**">` escrito a mano rompe la compilación con `NETSDK1022`.

### El script va **sin** `autostart="false"`

```html
<script src="_framework/blazor.webview.js"></script>
```

El atributo `autostart="false"` pertenece al patrón de MAUI, donde el host llama a `Blazor.start()` por su cuenta. En Windows Forms nadie lo llama, así que el framework nunca arranca y **el síntoma es idéntico al del SDK equivocado**: la página se queda con su texto inicial, sin error.

### Visual Studio no toma el cambio de SDK en caliente

Después de cambiar el `Sdk=` del `.csproj` hay que recargar el proyecto o la solución. Mientras tanto Visual Studio sigue evaluando con el SDK anterior y reproduce el mismo síntoma aunque el archivo ya esté bien.

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
