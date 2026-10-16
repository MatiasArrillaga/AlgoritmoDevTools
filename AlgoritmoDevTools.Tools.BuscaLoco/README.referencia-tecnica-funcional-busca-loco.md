# Referencia técnica y funcional — Busca Loco

Encuentra una palabra adentro de los archivos de una carpeta y sus subcarpetas, o encuentra archivos
por su nombre.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

> **Estado al 16/10/2026: implementada y commiteada, sin abrir la aplicación.** Rama
> `agregar-buscador-de-archivos`, siete commits desde `37a9b1f`. **131 pruebas en verde** —75 propias
> más las 56 del Disk Cleaner, que siguen pasando sin tocarlas—. Lo que falta verificar está en la
> sección 10.
>
> **La sección 7 es el activo principal**: las trampas verificadas, cada una con su causa y su
> solución. Dos de ellas se encontraron escribiendo esta herramienta y las dos cerraban la aplicación
> o rompían una sentencia.

---

## 1. Objetivo funcional

Que la búsqueda que uno hace veinte veces por día sobre **este** stack esté donde ya están las otras
herramientas, apuntada a donde hay que apuntar y sin tener que esquivar `bin` y `obj` a mano.

**El origen.** El equipo usaba un `buscador.exe` suelto en el escritorio que resultó ser **Agent
Ransack** de Mythicsoft, renombrado: binario x86 de 2004, verificado por su cabecera PE y sus
recursos. Agent Ransack sigue existiendo y sigue siendo gratuito, así que reemplazarlo nunca fue el
objetivo.

**Por qué entonces existe esta herramienta.** Vale la misma frase con la que se justifica el Disk
Cleaner —*"no es un `du` con interfaz"*—: vale porque **conoce este stack**.

- Arranca apuntada a `RutasDeTrabajo`, donde está casi todo lo que uno busca.
- Saltea `bin`, `obj`, `.git` y `node_modules` sin configurar nada. En AlgoritmoCore son **446
  carpetas `bin`/`obj`** y 19,7 GB de ruido, medidos por el Disk Cleaner el 08/10/2026.
- Trae presets del dominio: los `README.referencia-*`, los `*DbContext*`, las Unidades Funcionales.
- **Lee los archivos con el encoding que de verdad tienen** (trampa 7.1), que es lo único que separa
  a esta herramienta de una que devuelve cero y parece tener razón.

## 2. Qué hace

- Busca **por nombre**, **por contenido**, o por los dos a la vez.
- Nombre en glob —varios separados por coma— o en expresión regular.
- Contenido literal o en expresión regular, con *distinguir mayúsculas* y *palabra entera*.
- Resultados **en vivo**, agrupados por archivo, con número de línea y la coincidencia resaltada.
- Un clic abre el archivo **en esa línea**; el clic derecho ofrece *Abrir con…* de Windows.
- Exporta a CSV y a texto plano.
- Recuerda las últimas veinte búsquedas y carpetas.
- *Buscar en esta carpeta* en el clic derecho del explorador.

### Fuera de alcance

Esto es el grueso de lo que hace **FileLocator Pro**, la versión paga de Agent Ransack, y replicarlo
es desproporcionado para una herramienta interna:

| Qué | Por qué no |
|---|---|
| Índices | Es un producto en sí mismo: hay que mantenerlos, invalidarlos y compartirlos |
| OCR | Depende de un motor externo pesado |
| Outlook PST/MSG, IFilter | Nadie en el equipo busca ahí desde acá |
| Contenido de ZIP, RAR, 7Z | Se puede sumar después; no es lo que se usa |
| Office y PDF | Hay una vía barata para más adelante —el Markdown Converter ya invoca pandoc—, pero pandoc es **opcional** y un buscador que a veces no busca no sirve |
| Expresiones multilínea | Se lee línea por línea; cruzar saltos implica leer por bloques con solapamiento y recalcular los números |
| Buscar y **reemplazar** | Escribir en archivos es otra herramienta y otro nivel de riesgo |
| Plugins COM, exportación XSL, búsqueda por hash | Sin consumidor acá |

**Sin vista de Windows Forms y sin `ITool`**, igual que el Disk Cleaner: para una herramienta nueva
conviene sólo la pantalla HTML, así que no aparece bajo `--clasico`.

## 3. Modelo

```csharp
// Lo que el usuario pidió. Los dos campos de texto CONVIVEN: vacío = "no filtres por eso".
public sealed record CriterioDeBusqueda(
    string Raiz, string Texto, string Nombre,
    bool TextoEsExpresion, bool NombreEsExpresion,
    bool Subcarpetas, bool DistinguirMayusculas, bool PalabraEntera, bool SaltearRuido);

// Una coincidencia. El rango es lo que pinta el resaltado, y se guarda al encontrarla: volver a
// correr la expresión al dibujar sería pagar dos veces lo caro.
public sealed record Hallazgo(string Ruta, int Linea, string Texto, int Desde, int Largo);

// Un archivo con todo lo que se encontró adentro: treinta coincidencias en uno son un resultado.
public sealed record ArchivoConHallazgos(string Ruta, IReadOnlyList<Hallazgo> Hallazgos);
```

**Almacenamiento:** `%LOCALAPPDATA%/AlgoritmoDevTools/BuscaLoco/data.db`, por `ToolStorage("BuscaLoco")`.
**Esa clave no se renombra nunca**: es lo que aísla los datos.

```sql
CREATE TABLE BUSQUEDAS (TEXTO TEXT NOT NULL, NOMBRE TEXT NOT NULL,
                        ES_EXPRESION INTEGER NOT NULL, USADA TEXT NOT NULL,
                        PRIMARY KEY (TEXTO, NOMBRE));

CREATE TABLE RAICES (RUTA TEXT PRIMARY KEY, USADA TEXT NOT NULL);
```

Se guardan las **últimas 20** de cada una. **No viajan en el archivo de transferencia de
configuración**: las carpetas de uno no son las de otro, y qué buscó cada uno es su avance personal.

## 4. Reglas en vigor (prescripciones negativas)

Cada una tiene detrás un error concreto o una decisión que costó discutir.

- **No heredar la lista negra del Disk Cleaner.** Ver 5.1. Es la regla que más fácil se rompe
  "reutilizando".
- **No armar el `Regex` sin `NonBacktracking`.** Ver trampa 7.2.
- **No atrapar sólo `ArgumentException` al construirlo.** Ver trampa 7.3. Cuesta la ventana entera.
- **No separar el patrón de nombre por coma cuando está en modo expresión.** Ahí la coma es sintaxis
  —`\w{2,3}`— y partir por ella rompe el patrón en silencio.
- **No envolver el patrón en `\b…\b` cuando es una expresión.** Le cambia el significado sin avisar:
  `\b(foo|bar)\b` está bien, `\bfoo|bar\b` ancla sólo las puntas.
- **No armar la clave compuesta de una tabla pegando textos adentro del SQL.** Ver trampa 7.4.
- **No abrir un diálogo de Windows en el acto desde un `@onclick`.** Cierra la aplicación. Vale para
  elegir carpeta y para exportar; *Abrir con…* es la excepción y está explicada en 5.6.
- **No poner la validación de "se puede buscar" en el manejador del clic.** Ahí el botón se ve
  habilitado y no hace nada: el peor de los dos mundos. Es una propiedad calculada del servicio.

## 5. Decisiones que conviene no revisitar

### 5.1 · La lista negra es del borrado, no del recorrido

El recorrido vive en `RecorridoSeguro`, en el Core, y **la poda es un parámetro**. No es un detalle
de estilo: son dos preguntas distintas que se parecen.

| Herramienta | Qué poda | Por qué |
|---|---|---|
| Disk Cleaner | `RutasProtegidas` | Va a **borrar**: la configuración de usuario no se ofrece, y la garantía es que el escáner «ni las mira» |
| Busca Loco | `bin`, `obj`, `.git`, `node_modules` | Sólo **lee**. Es por velocidad, y por eso el usuario la puede apagar |

Heredar la del Disk Cleaner haría que el buscador **devuelva cero resultados en carpetas donde el
texto está, sin decir por qué** — la peor falla posible en un buscador, porque el silencio se lee
como "no está". Y buscar texto en el `UserSettings.xml` de SSMS es inofensivo: es justo lo que uno
quiere cuando algo anda mal.

**`EnumeracionSegura` sigue existiendo en el Disk Cleaner como envoltorio** (`EnumeracionSegura.cs`),
delegando con su lista negra siempre puesta. No es sólo para no romper sus pruebas: así **no existe
la variante sin podar** del lado de la limpieza, y ningún detector futuro puede olvidarse de ella.

### 5.2 · Motor propio en C#, no envolver `ripgrep`

DevTools se reparte como **un solo `.exe` autocontenido, sin nada al lado**, y el README raíz trata
eso como propiedad a conservar. Un binario externo la rompe. Pandoc es la excepción y está tratada
como tal: es opcional y la pantalla muestra el comando de instalación en vez de fallar.

### 5.3 · Lo literal no pasa por el motor de expresiones

Va con `IndexOf` ordinal. Sobre miles de archivos se nota, y `Regex.Escape` + `Regex` es la salida
fácil que se paga en cada uno. *Palabra entera* se resuelve mirando el carácter de antes y el de
después (`CoincidenciaEnTexto.EsPalabraEntera`), no armando un `\b…\b`, que devolvería la búsqueda
literal al motor del que se la quiso sacar.

### 5.4 · Los dos campos conviven: no hay switch

Se evaluó un selector *"por nombre / por texto"* y **se descartó**. La búsqueda más útil usa los dos
a la vez —*"`TipoCotizacion`, pero sólo en los `.razor`"*— y un switch la pierde.

| Nombre | Contenido | Qué hace |
|---|---|---|
| `*.cs` | vacío | Lista los archivos `.cs` |
| vacío | `TipoCotizacion` | Busca ese texto en todo |
| `*.razor` | `TipoCotizacion` | Ese texto, **sólo** en los `.razor` |

Lo que hace entendible la regla son los **placeholders**, que dicen qué pasa si el campo queda vacío,
no un control más.

### 5.5 · El modo expresión vive adentro de cada campo

Un botón `.*` dentro del propio campo, como en VS Code. Era una casilla suelta por campo en la fila
de opciones. Se gana que el control queda **pegado al campo al que aplica** —imposible confundirse— y
que la fila de opciones baja de seis casillas a cuatro, que es lugar que el listado necesita.

Son **dos** modos y no uno: el patrón del nombre se escribe casi siempre como glob (`*.cs`) y el del
contenido casi nunca. Con un solo interruptor hay que elegir entre que `*.cs` deje de funcionar o que
el texto no pueda ser una expresión. Es también lo que hace Agent Ransack, cuya ayuda dice *"Make
sure the file name is set to Regular expression in the options tab"*.

### 5.6 · Abrir en la línea, y *Abrir con…* como excepción

Un clic abre con **Visual Studio Code** (`code -g archivo:linea`) y cae al programa asociado si no
está, que abre el archivo pero al principio: es una degradación, no una falla.

El clic derecho muestra el diálogo *Abrir con* de Windows (`rundll32 shell32.dll,OpenAs_RunDLL`), que
**pierde la línea**. Por eso es el camino de excepción: preguntar en cada clic es fricción cuando uno
abre diez resultados seguidos.

**Ninguno de los dos necesita diferirse.** La trampa de los diálogos modales que cierran la
aplicación es de las ventanas que corren en *nuestro* bucle de mensajes; acá los dos son un proceso
aparte, así que el visor nunca entra en reentrada. No hace falta pasarlos por `IDialogosDelSistema`.

### 5.7 · El estado vive fuera de la pantalla

`SesionDeBusqueda` (`AlgoritmoDevTools.UI/Servicios/`), registrada **singleton**, con el molde de
`SesionDeLimpieza` y por los dos motivos que ese archivo ya pagó: irse a otra herramienta durante la
búsqueda **cerraba la aplicación** —el `Dispose` del componente liberaba el `CancellationTokenSource`
con el recorrido en vuelo—, e irse después perdía lo encontrado.

El `CancellationTokenSource` se libera en el **`finally` de quien lanzó la operación**, nunca en el
`Dispose` del componente.

### 5.8 · Los resultados se vuelcan por lote

Cada ~150 ms. Repintar Blazor en cada coincidencia es peor que no mostrar nada; esperar al final
hace sentir la pantalla colgada. Se avisa **por tiempo y no por cantidad**: con pocos resultados muy
separados, juntar N dejaría la pantalla quieta igual.

### 5.9 · El menú contextual es un ítem suelto, no un submenú

El presupuesto del explorador son **16 entradas para todo el árbol**, compartidas entre las
herramientas. Un grupo con un único ítem adentro costaría el doble por la misma función. Hoy van
**10 de 16**; con éste, 11.

Pasarse no da ningún error: el explorador dibuja las primeras y descarta el resto en silencio, así
que el síntoma es "me faltan opciones" sin nada que lo explique.

### 5.10 · El CSV va con punto y coma y con BOM

**Punto y coma** porque Excel usa el separador de lista de la configuración regional, y acá es ése:
con coma, cada renglón entra entero en la primera celda. **Con BOM** porque sin la marca Excel lee el
archivo con la codificación regional y todos los acentos salen rotos — el mismo motivo por el que los
archivos de código del repositorio la llevan.

### 5.11 · La ayuda explica todos los símbolos que usa, y se cierra al tocar un ejemplo

**La leyenda tiene que cubrir cada símbolo que aparece en los ejemplos de abajo.** Arrancó
explicando cinco —`.` `\w` `+` `?` `|`— mientras los ejemplos usaban nueve: `\s`, `*`, `^` y `\b`
quedaban sin explicar. El que no sabe expresiones regulares —que es el público del panel— no podía
leer ni los ejemplos que se le ofrecían.

Van **agrupados por para qué sirven** y no en una lista sola, porque nueve símbolos en fila se leen
como una tabla y una tabla en un panel de ayuda no la lee nadie:

| Grupo | Símbolos |
|---|---|
| Qué coincide | `.` cualquier carácter · `\w` letra o número · `\s` un espacio |
| Cuántas veces | `*` cero o más · `+` una o más · `?` opcional |
| Dónde y qué | `^` principio de línea · `\b` borde de palabra · `\|` o esto o aquello |

Dos frases más que hacen falta y no son obvias: **lo que no es un símbolo se busca tal cual** —así
el `//` del último ejemplo son dos barras y nada más—, y **para buscar una palabra como está hay que
apagar el modo**, que es la respuesta correcta a la mitad de las veces que alguien abre este panel.

**El panel se cierra al tocar un ejemplo.** Con los nueve símbolos y seis ejemplos mide poco más de
200 píxeles y **deja el listado en 45**: abierto tapa justo lo que uno vino a mirar. Tocar un ejemplo
es la señal de que ya encontró lo que buscaba ahí.

Si se agrega un ejemplo que use un símbolo nuevo, **va también a la leyenda**. Es la clase de cosa
que se desincroniza sola.

### 5.12 · `ArranqueDirecto`, para abrir en una herramienta

El armazón vive del otro lado del visor y no sabe nada de la línea de comandos, así que la decisión
viaja por un servicio. Sin herramienta pedida, el arranque sigue siendo el inicio de siempre.

## 6. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.Core/Infrastructure/RecorridoSeguro.cs` | **El único camino al disco.** Permisos, rutas largas, junctions, poda parametrizada |
| `Model/CriterioDeBusqueda.cs` | Qué se busca. `TieneQueBuscar` es la guarda del botón |
| `Model/Hallazgo.cs` | Una coincidencia y su rango, más `ArchivoConHallazgos` |
| `Services/LectorDeArchivos.cs` | **Encoding híbrido** y descarte de binarios |
| `Services/CoincidenciaEnTexto.cs` | Literal por `IndexOf`; expresión con `NonBacktracking` |
| `Services/FiltroDeNombre.cs` | Glob con varios patrones, y modo expresión |
| `Services/BuscadorService.cs` | Orquesta, con paralelismo acotado y lotes. `SePuedeBuscar` vive acá |
| `Services/PresetsDelStack.cs` | Los cinco atajos que conocen el repositorio |
| `Services/HistorialRepository.cs` | SQLite, últimas búsquedas y carpetas |
| `Services/ExportadorDeResultados.cs` | CSV y texto plano |
| `Services/AperturaDeArchivos.cs` | Abrir en la línea, *Abrir con…*, mostrar en la carpeta |
| `Services/MenuContextualBusqueda.cs` | Alta y baja del ítem del explorador |
| `AlgoritmoDevTools.UI/Servicios/SesionDeBusqueda.cs` | El estado, **fuera de la pantalla** |
| `AlgoritmoDevTools.UI/Servicios/ArranqueDirecto.cs` | Con qué pantalla abrir desde el menú contextual |
| `AlgoritmoDevTools.UI/Paginas/BuscaLocoPagina.razor(.cs)` | La pantalla |
| `AlgoritmoDevTools.Tools.BuscaLoco.Test/` | 75 pruebas |

### Qué toca fuera de la herramienta

| Archivo | Cambio |
|---|---|
| `AlgoritmoDevTools.Core/Infrastructure/RecorridoSeguro.cs` | **Nuevo**: subido del Disk Cleaner, con la poda parametrizada |
| `AlgoritmoDevTools.Tools.DiskCleaner/Services/EnumeracionSegura.cs` | Pasó a ser un envoltorio de 18 líneas |
| `AlgoritmoDevTools.Shell.csproj` y `AlgoritmoDevTools.UI.csproj` | Un `ProjectReference` más **en los dos** |
| `AlgoritmoDevTools.UI/CatalogoDeHerramientas.cs` | La entrada y el ícono |
| `AlgoritmoDevTools.UI/wwwroot/app.css` | El bloque `/* Busca Loco */` y `.listado-hallazgos` en la regla de los bloques que crecen |
| `AlgoritmoDevTools.UI/Componentes/TituloDeGrupo.razor(.cs)` | Un `Acciones` opcional |
| `AlgoritmoDevTools.UI/Layout/Shell.razor.cs` | Consulta `ArranqueDirecto` al arrancar |
| `AlgoritmoDevTools.UI/Servicios/MenusContextuales.cs` | Una entrada más |
| `AlgoritmoDevTools.Shell/MainFormBlazor.cs` | Servicios, y la carpeta inicial |
| `AlgoritmoDevTools.Shell/Program.cs` | El verbo `--buscar-aca` |

## 7. Trampas verificadas

### 7.1 Los archivos sin BOM — la que decide si el buscador sirve

**39 de los 118 archivos `.cs`** del repositorio no tienen BOM y varios guardan sus acentos en
**Windows-1252** (trampa 11.12 del Disk Cleaner). Un `ó` es el byte `F3`, que en UTF-8 no significa
nada: un lector que lo asuma lo reemplaza por el carácter de reemplazo.

Para un buscador eso significa que **`Días`, `Cálculo` o `Código` no se encuentran** en esos
archivos. No falla, no avisa: devuelve cero y parece que la palabra no está.

Se lee en dos intentos: primero UTF-8 **estricto** —el que tira cuando los bytes no son válidos, que
es justamente lo que se quiere saber— y recién ahí Windows-1252.

### 7.2 `Regex` no consulta el `CancellationToken`

Un patrón como `(a+)+$` sobre una línea larga tarda tiempo exponencial. Y acá está lo que no se ve
venir: **el hilo se queda adentro del motor y el botón de cancelar no hace nada**. El síntoma es «se
colgó» y la causa está a tres capas de donde uno mira.

Por eso se arma con `RegexOptions.NonBacktracking`, que garantiza tiempo lineal y hace que el
problema **no exista por construcción**. Verificado: `(a+)+$` contra 5.000 caracteres termina en
0,0 s.

### 7.3 `NonBacktracking` rechaza los lookaround con `NotSupportedException`

**Encontrada escribiendo las pruebas de esta herramienta, y cerraba la aplicación.**

`NonBacktracking` no soporta lookarounds ni referencias hacia atrás, y cuando el patrón las usa falla
**al construirse**. El detalle que mata: lanza **`NotSupportedException`**, no `ArgumentException`.

```
System.NotSupportedException : RegexOptions.NonBacktracking is not supported in conjunction with
expressions containing: 'positive lookahead (?= pattern) or positive lookbehind (?<= pattern)'.
```

Atrapar sólo `ArgumentException` —que es lo que uno escribe de memoria para un patrón mal formado—
deja escapar la otra. Y en el visor, **una excepción que nadie atrapa cierra la ventana entera**:
escribir `Monto(?=\s*=)` en el campo no daba un error en pantalla, cerraba el DevTools.

Se atrapan las dos, cada una por su motivo: la primera es "el patrón está bien pero este motor no
sabe", y se sigue al motor clásico con tope de tiempo; la segunda es "está mal escrito", y se deja
pasar al segundo intento para reportarla con el mensaje del motor.

### 7.4 Una clave compuesta pegada adentro del SQL rompe la sentencia

**Encontrada por la prueba de humo, antes de que llegara a la pantalla.**

La poda del historial borraba lo que sobraba identificando la fila por su clave. `BUSQUEDAS` la tiene
compuesta (texto + nombre), y armarla pegando los dos textos con un separador **adentro del SQL**
reventaba la sentencia entera:

```
SQLite Error 1: 'unrecognized token: "'"'
```

Se identifica la fila por **`rowid`**, que SQLite le da a toda tabla y que además sirve igual para las
dos. La salida "obvia" —usar un separador común en vez del nulo— es peor: cualquier texto que lo
contenga haría colisionar dos entradas distintas.

### 7.5 Mover la clase al Core rompía las pruebas del Disk Cleaner

El plan decía "mover `EnumeracionSegura` y parametrizar la poda, con el default en no podar". Eso
chocaba con dos cosas del test existente, que no se podía tocar:

- Su `using` apunta al namespace del Disk Cleaner, así que mover la clase lo deja sin compilar.
- `Una_ruta_protegida_mide_cero` llama a `Medir` **sin pasar poda** y espera `0`. Con el default en
  "no podar", ese test falla.

La salida fue el **envoltorio** (5.1), que además resultó mejor diseño que el plan original.

### 7.6 Un diálogo de Windows abierto en el acto cierra la aplicación

No deja rastro: ni excepción, ni log, ni pantalla de error. Un diálogo modal corre su propio bucle de
mensajes, y abrirlo desde un `@onclick` es reentrar en el navegador mientras entrega el clic.

Alcanza a esta herramienta dos veces: **elegir carpeta** y **exportar**. Los dos van con `await`.
*Abrir con…* no, porque es otro proceso (5.6).

### 7.7 Un botón adentro de otro botón no es HTML válido

`TituloDeGrupo` acepta ahora `Acciones`, pero **no tiene efecto en un encabezado plegable**: ahí el
título entero es un `<button>`, y el navegador saca el botón de adentro del medio. El clic terminaría
plegando la sección en lugar de ejecutar la acción.

## 8. Qué cubren las pruebas

**75 pruebas, en menos de un segundo.** Contra archivos de verdad y no contra un disco simulado: un
archivo en Windows-1252, uno binario y una ruta de más de 260 caracteres no existen en una
abstracción.

```bash
dotnet test AlgoritmoDevTools.Tools.BuscaLoco.Test -c Release
```

Lo que más importa, en orden:

- **Un archivo en cp1252 sin BOM con `Días` adentro se encuentra buscando `Días`** (7.1).
- **Un patrón con backtracking catastrófico termina** (7.2).
- **Un patrón con lookaround funciona** en vez de tirar (7.3).
- **Un texto con comillas, punto y coma o guiones no rompe la poda del historial** (7.4).
- La poda del recorrido se prueba **en las dos direcciones**: la misma carpeta se recorre o no según
  quién pregunte (5.1). El caso concreto de `RutasProtegidas` lo siguen cubriendo las 56 del Disk
  Cleaner, que **pasan sin haber sido tocadas**.
- `*.cs, *.razor` encuentra los dos tipos; en modo expresión, `\w{2,3}` no se parte por la coma.
- *Palabra entera* no cuenta `Cotizacion` dentro de `TipoCotizacionDTO`.
- Los dos campos combinan con Y, y con los dos vacíos no se puede buscar.
- Ninguna ruta devuelta empieza con `\\?\`.
- El CSV conserva sus cuatro columnas con una coma en la ruta y con un punto y coma en el texto.

### Lo que no se puede cubrir con una prueba

| Qué | Por qué |
|---|---|
| Que la pantalla se vea bien en los cuatro temas | Se prueba abriendo las cuatro combinaciones |
| Que el diálogo de Windows no cierre la aplicación | El cartel no se puede automatizar; lo que se verifica es que los llamados van con `await` |
| Que el ítem del explorador aparezca en el menú | El registro sí se verifica —instalar, contar, desinstalar—; que Windows lo dibuje, no |

## 9. Deuda conocida

| Qué | Estado |
|---|---|
| **Sin abrir la aplicación** | Lo más importante de la sección 10 |
| Rendimiento sobre AlgoritmoCore | **Sin medir.** Buscar contenido **lee** los archivos, no sólo los enumera: es otro orden de trabajo que el escaneo del Disk Cleaner |
| El lote de 150 ms | Sin medir. El número salió de la intuición, no de una prueba |
| `Encoding.Latin1` en vez de Windows-1252 completo | Cubre las vocales acentuadas, la eñe y `« »`. Queda afuera el tramo `80-9F`: las comillas tipográficas `“ ”` y el guión largo. Evita agregar `System.Text.Encoding.CodePages` |
| El historial no se usa todavía en la pantalla | Se guarda y se puede leer, pero no hay desplegable que lo ofrezca |
| Timeout para unidades de red | Soportadas por `RecorridoSeguro`, pero una ruta caída hace esperar mucho por carpeta |
| Office y PDF | Segunda etapa (sección 2) |

## 10. Qué falta verificar

**Nada de esto se vio andando.** La aplicación no se abrió en ningún momento de la implementación.

- La pantalla dibujada, en los cuatro temas.
- Que el listado scrollee adentro y la pantalla no, con resultados de verdad.
- El panel de ayuda de expresiones, y que los ejemplos escriban en el campo.
- Exportar: el diálogo, el archivo, y que Excel lo abra con los acentos bien.
- Abrir en el editor y *Abrir con…*.
- El ítem del explorador: que aparezca, y que abra la ventana apuntada a esa carpeta.
- Que irse a otra herramienta con una búsqueda corriendo **no cierre la aplicación** — el motivo por
  el que el estado vive en `SesionDeBusqueda` (5.7).

```bash
dotnet run --project AlgoritmoDevTools.Shell
```

## 11. Piezas relacionadas

| Pieza | Ruta |
|---|---|
| Plan de implementación | `.planes\DevTools\BuscaLoco\` |
| Preview de la pantalla | `.planes\DevTools\BuscaLoco\preview-busca-loco.html` |
| Cómo se da de alta una herramienta | [README.md](../README.md) — los 8 pasos y los 12 lugares |
| Trampas de la interfaz | [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md) |
| Temas | [temas](../AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md) |
| El molde de operación larga | `AlgoritmoDevTools.UI/Servicios/SesionDeLimpieza.cs` |
| El recorrido del disco, y la trampa 11.12 | [limpieza de disco](../AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md) |

---

**Este documento debe actualizarse** cuando se verifique algo de la sección 10, cuando se mida el
rendimiento y deje de ser deuda, cuando cambie el alcance de la sección 2, o cuando aparezca una
trampa nueva. **La sección 7 es el activo principal**: toda trampa que cueste un intento fallido va
ahí, con su causa y su solución.
