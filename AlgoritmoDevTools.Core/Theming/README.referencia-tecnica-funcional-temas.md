# Referencia técnica y funcional — Temas de la interfaz

Documento del sistema de temas de Algoritmo DevTools: los cuatro temas que ofrece Configuraciones, cómo se aplican a toda la aplicación y qué hay que saber para extenderlo.

Implementado el 09/10/2026. Reemplaza el aspecto por defecto de WinForms —relieves 3D, grises de sistema, azul de selección de Windows— por una estética plana con un color de acento elegible.

---

## 1. Objetivo funcional

Que el usuario pueda elegir cómo se ve la herramienta, con cuatro opciones cerradas y nombradas, y que esa elección valga para **toda** la aplicación: el shell, las cinco herramientas y los diálogos que abren.

No es un selector de colores libre. Son cuatro paletas completas y calibradas; el usuario elige una, no arma la suya.

## 2. Alcance

- **Cuatro temas**: dos familias (verde y azul), cada una con variante clara y oscura.
- **Cambio en caliente**: se aplica al instante, sin reiniciar, incluso a las vistas que ya estaban abiertas.
- **Preferencia por usuario**, guardada en disco.
- **Una sola fuente de color**: ninguna vista fija colores propios.

### Fuera de alcance

- La **barra de título** de la ventana. La pinta Windows, no la aplicación: en los temas oscuros queda clara. Oscurecerla requiere `DwmSetWindowAttribute`, que es una llamada a la API de Windows por cada ventana.
- Los **íconos de las herramientas**. Vienen de los `.ico` de cada tool y no se recolorean: en los temas oscuros se ven igual que en los claros.
- El **cursor de texto** de los campos. Windows lo dibuja negro; sobre un campo oscuro casi no se ve.
- **Temas definidos por el usuario.** El catálogo es fijo y está en código.

## 3. Modelo principal

| Pieza | Archivo | Qué es |
|---|---|---|
| `Tema` | `Tema.cs` | La paleta: 14 colores + `Id`, `Nombre` y `EsOscuro` |
| `Temas` | `Temas.cs` | El catálogo de los cuatro, y `Resolver(id)` |
| `ServicioDeTemas` | `ServicioDeTemas.cs:23` | Singleton con el tema vigente y el evento `TemaCambiado` |
| `AplicadorDeTema` | `AplicadorDeTema.cs` | Pinta un árbol de controles |
| `ColoresDeEstado` | `ColoresDeEstado.cs` | Los colores de los mensajes, leídos del tema vigente |
| `ConfiguracionDevTools` | `../Infrastructure/ConfiguracionDevTools.cs` | Persistencia de la preferencia |

### Los cuatro temas

| Tema | Id | Acento | Hover | Pressed |
|---|---|---|---|---|
| Verde Esperanza | `verde-esperanza` | `0, 160, 94` | `15, 180, 109` | `0, 128, 73` |
| Azul Ilusión | `azul-ilusion` | `14, 142, 233` | `42, 157, 244` | `11, 114, 186` |
| Verde Esperanza Oscuro | `verde-esperanza-oscuro` | `14, 156, 143` | `25, 178, 163` | `9, 122, 112` |
| Azul Ilusión Oscuro | `azul-ilusion-oscuro` | `74, 147, 190` | `92, 163, 204` | `60, 124, 162` |

**Los `Id` son claves de persistencia.** Cambiar uno deja a los usuarios que lo tenían elegido con el tema por defecto, porque `Temas.Resolver` cae en `PorDefecto` cuando el id no existe. Es la degradación buscada, pero no es gratis.

## 4. Reglas en vigor

1. **Ninguna vista fija colores.** El único lugar donde se decide qué color le toca a cada tipo de control es `AplicadorDeTema.AplicarSegunTipo`. Una vista que asigne `BackColor` o `ForeColor` por su cuenta queda igual en los cuatro temas.
2. **Los roles se declaran con `Tag`.** Un control que se sale de lo común lleva su rol en la propiedad `Tag`, que se carga desde el diseñador. Son cuatro: `primario` (`AplicadorDeTema.cs:21`), `lateral` (`:24`), `sutil` (`:27`) y `separador` (`:30`).
3. **El botón primario va en negrita.** No es decoración: los cuatro acentos rondan 3,4:1 de contraste con el blanco y en regular la etiqueta se lee floja. Lo fuerza `PintarBoton` (`AplicadorDeTema.cs:169`).
4. **Los colores de estado son significado, no identidad.** El verde de "salió bien" es verde también en los temas azules. Salen de `ColoresDeEstado`, nunca de `Color.Firebrick` y compañía.
5. **Una sola acción primaria por pantalla.** Si todo es primario, nada lo es.

## 5. Flujo

### Al abrir la aplicación

1. `ServicioDeTemas.Instancia` lee `configuracion.json` y resuelve el tema (`ServicioDeTemas.cs:16-20`).
2. `MainForm` se suscribe a `TemaCambiado` y se pinta entero (`MainForm.cs:49-50`).
3. `SeleccionarHerramientaInicial` (`MainForm.cs:72`) abre la última herramienta usada, si el usuario lo pidió.
4. Cada vista de herramienta se pinta **al crearse**, antes de entrar al panel (`MainForm.cs`, en `toolsList_SelectedIndexChanged`).

### Al cambiar de tema

1. La tarjeta elegida llama a `ServicioDeTemas.Cambiar` (`ServicioDeTemas.cs:40`).
2. El servicio guarda la preferencia y dispara `TemaCambiado`.
3. `MainForm.OnTemaCambiado` (`MainForm.cs:216`) repinta la ventana, **las vistas en caché** y la pantalla de Configuraciones.

   **El repintado de las vistas en caché es necesario y no es obvio.** Las herramientas ya abiertas quedan en `_viewCache` pero fuera del árbol de controles del formulario, así que el repintado del `MainForm` no las alcanza: sin ese recorrido explícito, volver a una herramienta ya visitada la muestra con el tema anterior.

### Dónde se engancha cada superficie

| Superficie | Quién aplica el tema |
|---|---|
| Ventana principal | `MainForm` en el constructor y en `OnTemaCambiado` |
| Vista de una herramienta | `MainForm`, al crearla y en cada cambio de tema |
| Configuraciones | `MainForm.MostrarConfiguraciones` (`:182`) |
| Diálogos | Cada uno en su constructor, con `ServicioDeTemas.Instancia.AplicarA(this)` |
| `InputDialog` | Antes del `ShowDialog` |

## 6. Decisiones técnicas que conviene no revisitar sin leer esto

### El resaltado del lateral se dibuja a mano

El `ListView` de herramientas usa `OwnerDraw` (`MainForm.cs:59-67`). Sin eso, Windows pinta la fila seleccionada de **su** azul de sistema, que no tiene nada que ver con el tema: en Verde Esperanza se ve un azul suelto, y en los temas oscuros el texto queda negro sobre azul.

Con `View.Details` hay que manejar los tres eventos de dibujo: `DrawColumnHeader` y `DrawSubItem` se anulan con `e.DrawDefault = false` y todo el dibujo de la fila ocurre en `DrawItem` (`MainForm.cs:231`). **Manejar sólo `DrawItem` deja la lista en blanco.**

El resaltado bajo el mouse se lleva en `_indiceBajoElMouse` y se refresca en `MouseMove`/`MouseLeave`. El `ListView` no tiene estado de hover propio.

### Los campos de texto llevan `FixedSingle`, no `None`

`BorderStyle = None` da el borde más limpio, pero **también saca el relleno interno** y colapsa el alto del campo al de la fuente. Conseguir el campo del mockup —borde de 1px del color del tema y 8px de aire— exige envolver cada `TextBox` en dos paneles anidados, o sea rediseñar las cinco vistas.

Se eligió `FixedSingle`, que es plano y de 1px, aunque el color lo decide Windows y no el tema.

**Eso trajo un problema que no se ve hasta probar el tema oscuro**: ese borde se dibuja negro, y sobre un fondo oscuro desaparece, dejando al usuario sin saber dónde escribir. Por eso existe `Tema.FondoCampo`, que en los temas claros es el mismo blanco del panel y en los oscuros es **un tono más claro que el panel**: ahí el campo se distingue por su superficie y no por su borde.

### `UseVisualStyleBackColor = False` es obligatorio en cada botón

Si queda en `True`, Windows ignora el `BackColor` y pinta el botón con el tema del sistema. Lo fuerza `PintarBoton`, pero si algún día se pinta un botón fuera del aplicador, este es el detalle que lo arruina.

### `LinkLabel` va antes que `Label` en el `switch`

Hereda de `Label`. Al revés, el compilador rechaza el `case` por inalcanzable (`CS8120`). Lo mismo vale para cualquier control nuevo: **el tipo derivado va primero**.

### El proyecto que hospeda el BlazorWebView va con `Microsoft.NET.Sdk.Razor`

`AlgoritmoDevTools.Shell` usa el SDK de Razor aunque sea una aplicación de Windows Forms. No es un detalle de estilo: es el SDK que hace que el proyecto genere su propio manifiesto de recursos estáticos (`AlgoritmoDevTools.staticwebassets.runtime.json` en la salida), que es de donde el `BlazorWebView` saca `_framework/blazor.webview.js` y los archivos del proyecto de componentes.

**Con `Microsoft.NET.Sdk` a secas la ventana abre y se queda mostrando el texto inicial del `index.html`, sin ningún error.** No hay excepción, el proceso vive, el motor de Edge arranca y el `.html` se copia a la salida: lo único que falla es que el pedido del script devuelve 404 adentro del WebView. El síntoma —"dice Cargando..."— no se parece en nada a la causa.

Para verificarlo sin abrir la ventana: en la carpeta de salida tiene que existir el manifiesto con el **nombre del ensamblado del host**, no sólo el del proyecto de componentes.

Un efecto secundario del cambio: el SDK de Razor incluye `wwwroot/**` por su cuenta, así que un `<Content Include="wwwroot\**">` escrito a mano hace fallar la compilación con `NETSDK1022` por elementos duplicados.

### La preferencia no vive en `ToolStorage`

`ToolStorage` aísla el almacenamiento **por herramienta**, justamente para que una no vea lo de la otra. El tema tiene que ser visible desde todas, así que va en un JSON suelto en `%LOCALAPPDATA%/AlgoritmoDevTools/configuracion.json`.

`Cargar` y `Guardar` se tragan los errores a propósito: que el archivo esté corrupto o no se pueda escribir no puede impedir que la herramienta abra ni interrumpir lo que el usuario estaba haciendo.

## 7. Cómo extender

### Agregar un tema

1. Sumar la paleta a `Temas.cs` siguiendo el formato de los existentes.
2. Agregarlo a `Temas.Todos`, que define el orden en que se ven.

Nada más: la pantalla de Configuraciones arma las tarjetas recorriendo el catálogo. Por eso `ConfiguracionesView` se construye por código y no por diseñador.

**Al elegir el acento**, mirar el contraste con el blanco: los cuatro actuales rondan 3,4:1 y por eso pesan igual en pantalla. Un acento más claro (un verde lima, un celeste) deja la etiqueta del botón primario ilegible aunque esté en negrita.

### Temar un menú de Windows Forms

`MenuDelTema.Aplicar(menu)`, después de cargarle los ítems. Vale para cualquier `ToolStripDropDown`; hoy lo usa el menú del widget de bandeja.

Hay que llamarlo **cada vez que se arma el menú**, no una sola vez al crearlo: mide los ítems ya cargados, y el tema puede haber cambiado desde la ventana mientras tanto.

Tres cosas de los menús no se pueden pedir de otra forma, y por eso hay una clase entera en vez de un par de propiedades:

| Qué | Por qué |
|---|---|
| El pintado | `ContextMenuStrip` no tiene colores propios más allá del fondo: el resaltado, los separadores, el tilde y las flechas los dibuja el renderizador, y hay que reemplazarlo entero |
| Las esquinas redondeadas | Un menú no tiene borde configurable. Se recorta la ventana con una región, y el borde se redibuja con suavizado porque el recorte sale dentado |
| El alto de cada ítem | De fábrica sale apretado contra el texto; se fija ítem por ítem con `AutoSize = false` |

El renderizador se registra en `ToolStripManager`, que es de toda la aplicación: cada submenú es una ventana aparte y pedírselo uno por uno obligaría a acordarse de los que todavía no existen. Los submenús sí necesitan su propia preparación —región y altos—, y eso se engancha a `DropDownOpening`, porque un submenú recién existe cuando se lo abre.

### Dar de alta un control que el aplicador no conoce

Agregar su `case` en `AplicadorDeTema.AplicarSegunTipo`, **antes** que el de su clase base. Hoy el aplicador cubre lo que usan las vistas: `Button`, `CheckBox`, `RadioButton`, `LinkLabel`, `Label`, `RichTextBox`, `TextBox`, `ComboBox`, `ListBox`, `ListView`, `TreeView`, `Splitter`, `StatusStrip`, `GroupBox` y los contenedores. Un control sin `case` cae en el `default`, que sólo le ajusta el `ForeColor`.

### Sumar una opción a Configuraciones

1. Propiedad nueva en `ConfiguracionDevTools`.
2. Control nuevo en `ConfiguracionesView`, guardando en el handler del cambio.

Las opciones se guardan al tocarlas, no hay botón de Guardar. Es deliberado: con dos opciones, un botón de confirmar es más fricción que ayuda.

## 8. Deuda conocida

| Qué | Por qué está así | Qué costaría arreglarlo |
|---|---|---|
| Barra de título clara en temas oscuros | La pinta Windows | `DwmSetWindowAttribute` por ventana, incluidos los diálogos |
| Íconos de herramientas sin recolorear | Vienen de `.ico` a color | Un juego de íconos por variante, o teñirlos en runtime |
| Cursor de texto negro en campos oscuros | Lo dibuja Windows | No tiene arreglo razonable |
| Un mensaje de estado ya mostrado pierde su color al cambiar de tema | El repintado deja la etiqueta en su color base | Recordar la intención del último mensaje por vista |
| El borde del `TextBox` no sale del tema | Se usa `FixedSingle` | Envolver cada campo en dos paneles: rediseñar las cinco vistas |
| La fuente quedó en la de por defecto | Cambiarla con `AutoScaleMode = Font` re-escala el layout de todas las vistas | Pasar el formulario a `AutoScaleMode = Dpi` y revisar las cinco vistas |

## 9. Qué tendrían que cubrir las pruebas

No hay proyecto de tests en la solución. Si se agrega, lo que vale la pena cubrir:

- `Temas.Resolver` con un id inexistente, vacío y nulo devuelve `PorDefecto`.
- `Temas.Resolver` ignora mayúsculas y minúsculas.
- `ServicioDeTemas.Cambiar` con el tema que ya estaba **no** dispara `TemaCambiado`.
- `ConfiguracionDevTools.Cargar` con un JSON corrupto devuelve la configuración por defecto en lugar de tirar.
- `AplicadorDeTema.Aplicar` dos veces seguidas deja el mismo resultado que una (es idempotente).
- Que los cuatro temas tengan los 14 colores distintos de `Color.Empty`.

Lo que **no** se puede verificar sin mirar: que los colores se vean bien. Eso se prueba abriendo las cuatro combinaciones.

## 10. Piezas relacionadas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.Core/Theming/*` | El sistema completo |
| `AlgoritmoDevTools.Core/Infrastructure/ConfiguracionDevTools.cs` | Persistencia |
| `AlgoritmoDevTools.Core/UI/InputDialog.cs` | Diálogo genérico, se pinta antes de mostrarse |
| `AlgoritmoDevTools.Shell/MainForm.cs` | Aplica el tema y dibuja el lateral |
| `AlgoritmoDevTools.Shell/MainForm.Designer.cs` | Panel lateral, botón de Configuraciones, separador de 1px |
| `AlgoritmoDevTools.Shell/Views/ConfiguracionesView.cs` | Pantalla de Configuraciones |
| `AlgoritmoDevTools.Tools.*/Views/*.Designer.cs` | Declaran roles con `Tag` |
| `AlgoritmoDevTools.Tools.*/Dialogs/*.cs` | Se pintan en su constructor |

---

**Este documento debe actualizarse** cuando se agregue o saque un tema, cuando cambie el contrato de roles de `Tag`, cuando el aplicador empiece a cubrir un tipo de control nuevo, o cuando se salde alguna de las deudas de la sección 8.
