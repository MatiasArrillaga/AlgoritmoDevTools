# Referencia técnica y funcional — Selector de Servicios y control del entorno local (Tye)

Elige con casillas qué microservicios levanta el entorno local, lo levanta, y después lo gobierna desde una barra flotante: reiniciar, bajar y volver a subir un servicio, sumar uno que faltaba, cambiar la base y frenar todo.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md) · Temas: [temas](../AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md)

---

## 1. Objetivo funcional

Dos problemas, el mismo origen: el entorno se levanta a mano desde una consola de PowerShell suelta, y una vez arriba no se puede tocar.

1. **Levantar sólo lo que hace falta**, en vez de los diecisiete, y volver a una combinación usada antes sin rearmarla.
2. **Corregir sin bajar todo.** Hasta ahora, olvidarse un microservicio o darse cuenta de que se levantó contra la base equivocada costaba bajar el entorno entero y esperar los minutos del arranque. Las dos cosas pasan seguido y las dos tienen arreglo en caliente.

## 2. Qué hace

**Armar el entorno** (pantalla del Selector)

- Lee la lista `services:` del `tye.yaml` maestro y la muestra en casillas.
- Genera `tye.devtools.yaml` en la raíz de AlgoritmoCore con la selección.
- Guarda y aplica **perfiles** con nombre.
- **Levanta el entorno** y abre la barra. El botón de **Generar y guardar** sigue estando para quien quiera el archivo y arrancar por su cuenta.
- Muestra, a la vista y sin cartel, si falta algo para que el entorno levante bien (§5).

**Gobernarlo** (barra del entorno, §6)

- Reiniciar todos los servicios, o uno solo.
- Bajar un servicio y volver a subirlo.
- Sumar al entorno vivo un servicio que no se había tildado.
- Cambiar la base de datos sin frenar nada.
- Frenar el entorno, con limpieza de lo que quede colgado.

**El `tye.yaml` maestro no se modifica nunca**: queda limpio en git. El archivo generado tiene que estar en la raíz del repositorio para que las rutas relativas `project:` resuelvan.

## 3. Reglas en vigor

### Sólo se listan microservicios

La lista sale de los ítems en la **columna 0** de `services:`. Las entradas de `env:` tienen la misma forma (`- name: X`) pero indentadas, y no son servicios.

Hasta que se acotó el parseo a la columna 0, `SOFTCEREALCORE_PROCESS_CULTURE`, `ASPNETCORE_URLS` y demás variables aparecían como opciones tildables, y al destildar un servicio sus líneas hijas quedaban a medio comentar.

### Cada servicio arrastra su sidecar de Dapr

Los sidecars (`cereales-dapr`, `produccion-dapr`, …) **no se ofrecen en la lista**: al tildar `produccion` quedan activos `produccion` y `produccion-dapr`, y al destildarlo se comentan los dos.

Hasta el commit `0dd570fa5` de AlgoritmoCore (02/10/2026) los sidecars los derivaba Tye solo desde la extensión `dapr`, así que comentar el servicio alcanzaba. Esa extensión se eliminó y ahora son servicios de primer nivel que ejecutan `Utils/Tye/Start-DevelopmentDapr.ps1`.

**Las dos mitades sueltas fallan**: un servicio sin sidecar no puede invocar a nadie, y un sidecar sin servicio queda esperando para siempre una aplicación que no va a arrancar.

### Hay servicios que no se pueden destildar

`seguridad` y `ui`. La casilla está deshabilitada a propósito y cada una explica su motivo al pasar el mouse: sin `seguridad` no levanta ninguno de los demás, y sin `ui` no hay pantalla desde donde usar lo que se levante.

La lista es `ServiciosObligatorios`, **duplicada en las dos pantallas** (`TyeServiceSelectorPagina.razor.cs` y `Views/TyeServiceSelectorView.cs`): sumar un servicio es agregarlo con su motivo en los dos lados.

### `orquestador` viene tildado, pero se puede destildar

Casi todo circuito termina pasando por él, así que la pantalla lo vuelve a tildar cada vez que arma la selección entera —"Desmarcar todos" y aplicar un perfil—, aunque el perfil no lo traiga. Cuando lo agrega por sugerencia lo dice en la barra de estado, para que la diferencia con lo guardado no se lea como un perfil mal guardado.

Es una sugerencia, no una regla: destildarlo a mano se respeta, y **al refrescar manda el archivo generado**. Si quedó comentado en el `tye.devtools.yaml`, la casilla aparece destildada; re-tildarla ahí pisaría una decisión ya tomada, por el mismo motivo por el que el perfil vigente no se reaplica al abrir (§4).

`orquestadorscheduler` es otro servicio y queda afuera de la sugerencia.

### El toggle es por comentarios de línea

Los servicios destildados se **comentan** (`# `) en vez de borrarse, así re-tildarlos los descomenta sin perder su definición. Es reversible y preserva el formato, cosa que un parser de YAML no haría.

Sólo se toca lo que cambia de estado: un bloque que el maestro ya trae como corresponde se copia tal cual, así sobreviven los comentarios escritos a mano que caen dentro de él —descomentarlos los volvería YAML inválido—.

## 4. El perfil vigente

Se guarda **al generar**, no al elegirlo del combo: lo que cuenta es con qué perfil quedó escrito el archivo, porque el archivo es lo que de verdad va a levantar. Elegir un perfil sin generar no deja registro.

Queda en `ConfiguracionDevTools.UltimoPerfilDeTye` y lo usan dos pantallas: el inicio lo muestra, y esta vuelve a él al abrirse.

**Al abrir no se reaplica**: los servicios tildados salen del archivo generado, y pisarlos borraría cualquier ajuste hecho a mano después. El combo dice de dónde se viene; las casillas dicen cómo está ahora.

> Como es un valor guardado, si se cambian servicios a mano después de aplicar un perfil, el nombre sigue diciendo ese perfil aunque la selección ya no coincida. Hubo una versión que lo deducía comparando los servicios activos contra cada perfil, justamente para evitar esto, pero no acertaba en la práctica y se reemplazó por el valor guardado.

## 5. Levantar el entorno

El botón **Levantar** genera el archivo, arranca `tye run <yaml> --watch --port 8000` y abre la barra. Tye va **en su propia ventana de consola** (`LanzadorDeTye`, `UseShellExecute = true`): la aplicación no tiene forma de mostrar la salida de un proceso en vivo —no hay un solo `BeginOutputReadLine` en todo el repositorio, todo es `ReadToEnd` bloqueante— y tye ya escribe en consola y en Seq, que es donde uno los busca. Como además queda suelto, cerrar DevTools no se lleva puesto el entorno.

Al frenar desde la barra esa consola **se cierra sola** (`LanzadorDeTye.CerrarLasConsolas`): el `DELETE /api/v1/control` baja los servicios pero no siempre termina el proceso de tye, y una consola viva después de bajar hace creer que el entorno sigue arriba — y deja a `HayTyeCorriendo()` diciendo que sí, que es lo que decide si el próximo arranque va con `--no-build`.

**A quién se cierra sale del PID anotado al levantar** (`ConfiguracionDevTools.PidDeTye`), no sólo del nombre del proceso: `tye` se instala como herramienta global de dotnet y basta con que esa resolución cambie —un shim, un `dotnet tye`— para que `GetProcessesByName("tye")` no encuentre nada y la consola quede abierta sin que nadie se entere. El barrido por nombre queda igual, como red.

**Si el entorno lo levantó el usuario desde su propia consola, esa ventana no es nuestra**: no hay PID anotado, se frena el entorno y la consola vuelve al prompt. Cerrarla sería llevarse puesta una sesión ajena.

Cerrar el proceso cierra su consola, y con ella los procesos que heredaron esa consola — verificado: un hijo lanzado con `-NoNewWindow` muere junto con la ventana.

### El botón principal cambia con el momento

Es uno solo y ofrece cuatro cosas distintas según dónde esté parado el usuario (`QueOfrece`):

| Momento | Dice | Qué hace |
|---|---|---|
| No hay entorno | **Levantar** | Genera el archivo, arranca tye y abre la barra |
| Ya hay un tye corriendo | **Controlar** | Engancha la barra a esa instancia, sin levantar otra |
| Ya hay un tye corriendo **y la barra abierta** | **Dejar de controlar** | Cierra la barra. **El entorno sigue corriendo**: no se frena ningún servicio |
| Se tocó Levantar y todavía no están todos | **Cancelar** | Le pide a la barra que frene |
| El panel contesta con todos arriba | **Cerrar** | Cierra esta ventana; lo que sigue pasa en la barra |

**La ventana ya no se minimiza sola al levantar.** Lo hacía porque «ya cumplió», pero con eso el botón de cancelar no se ve: habría que restaurarla desde la barra de tareas para usarlo, que es más trabajo que frenar desde la barra flotante. Ahora se queda, y se va cuando el usuario toca Cerrar. `IVentanaPrincipal.Minimizar()` sigue existiendo aunque esta pantalla no lo use.

**«Dejar de controlar» cierra la barra y nada más.** Hace falta porque con el entorno arriba y la barra ya abierta, el botón ofrecía «Controlar», que es lo que ya estaba hecho. Son dos cosas distintas y conviene que los nombres no se confundan: `ArranqueDeLaBarra.Cerrar()` manda `WM_CLOSE` a la barra y deja los servicios corriendo, mientras que `PedirQueFrene()` baja el entorno. El tooltip lo dice explícito, porque un botón que diga «dejar de controlar» se puede leer como «frenar».

Si la barra está abierta sale del mismo mutex de instancia única que impide abrir una segunda (`EstaCorriendo`), y se mira en cada vuelta de la revisión: abrir y cerrar un mutex es barato.

**Cancelar no reimplementa el frenado**: manda `AlgoritmoDevTools.Barra.Frenate` —un mensaje registrado, igual que `Mostrate`— y la barra corre su propio `Frenar()`. Ella ya espera a que bajen los servicios, cierra la consola de tye, barre los huérfanos y se cierra. Dos frenados distintos se separan con el tiempo, y el que quedaría peor atendido es éste, que se usa mucho menos. El puente de vuelta —ventana → página— es el evento `FrenadoPedido` de `IVentanaDeLaBarra`, porque el único que había iba en el sentido contrario.

**El paso de Cancelar a Cerrar sale de un sondeo propio** (`MirarElArranque`, cada 3 s, sólo mientras se está levantando). No va colgado de `Revisar()` porque son de costo muy distinto: esto es un GET a localhost y aquélla levanta un `docker ps`, un `netsh` y los user-secrets.

### Con un tye ya corriendo, el botón dice «Controlar»

Un segundo tye con los mismos servicios se pelea por los puertos y se pisa los contenedores. Antes eso figuraba como faltante —«Ya hay una instancia de tye corriendo», con el remedio de frenarla—, pero el faltante **no deshabilitaba el botón**: se podía tocar Levantar igual y arrancar el segundo. Y además el remedio estaba mal planteado: lo que uno quiere no es frenar el entorno que está, sino manejarlo.

Ahora `ResultadoDePrerrequisitos.HayEntornoCorriendo` lo informa como un modo, no como un error, y la pantalla cambia el rótulo del botón a **Controlar**. Ese camino (`Controlar()`):

- **No genera el yaml, no registra el perfil y no escribe el sello de conexión.** Ese entorno lo levantó otro —otra sesión de DevTools, o el usuario desde su consola— y con qué base arrancó no nos consta. Escribir el sello de la conexión configurada ahora sería afirmar algo que no sabemos, y la barra quedaría comparando contra un dato inventado: diría que la base está al día, o que cambió, sin fundamento.
- **Sólo abre la barra.** No hace falta nada más: la barra lee el estado de la API del panel y no de quien la lanzó, así que controla cualquier tye que conteste —reiniciar todo, reiniciar uno, bajar y subir servicios, sumar uno que falta, frenar—. Engancharse es, literalmente, no levantar otro y abrirla.
- **La consola de tye no queda registrada como nuestra**, así que `PidDeTye` no se toca. Al frenar la cierra igual el barrido por nombre, que es el caso que ese código ya contempla.

**La decisión se vuelve a tomar en el clic**, no se confía en el rótulo: entre el render y el clic el entorno pudo haberse frenado, y al revés. `Levantar()` arranca preguntando por `LanzadorDeTye.HayTyeCorriendo()` y deriva a `Controlar()` si hace falta.

Y el chequeo de tye corriendo pasó **antes** del corte por Docker: un entorno ya levantado se controla igual aunque Docker haya dejado de contestar.

### La revisión de prerrequisitos está a la vista, no en un cartel

`ChequeoDePrerrequisitos` mira tres cosas y las muestra permanentemente en la pantalla del Selector, sin interrumpir:

| Qué | Por qué importa |
|---|---|
| Los contenedores de `dapr init` (`dapr_placement`, `dapr_scheduler`, `dapr_redis`, `dapr_zipkin`) | Sin scheduler los sidecars no completan el arranque y las APIs se quedan esperando a Dapr |
| Los puertos que esos contenedores necesitan contra los **rangos excluidos de Windows** | Es la causa real del caso de la §7.6 |
| Que haya una conexión configurada | Sin ella los servicios arrancan y fallan al primer acceso |

El pre-vuelo modal se descartó por pedido: interrumpe cada arranque para decir casi siempre que está todo bien.

**La revisión se repite sola cada cinco segundos mientras falte algo —o mientras haya un entorno corriendo—**, y se apaga cuando no queda nada que mirar. Lo segundo es por el rótulo del botón: si no, se frena el entorno desde la barra, se vuelve a esta pantalla y el botón sigue diciendo «Controlar» para siempre. El remedio de casi todos los faltantes pasa por afuera de la pantalla —abrir Docker Desktop y esperar a que arranque es el caso de todos los días—, así que una revisión que corre una sola vez al abrir deja el cartel viejo colgado: el usuario arregla lo que había que arreglar y la pantalla le sigue diciendo que está roto. Sondear siempre sería caro, porque cada vuelta levanta un `docker ps`, un `netsh` y los user-secrets; por eso sólo mira mientras hay algo que mirar.

**El botón de destrabar pide elevación.** `net stop winnat; net start winnat` sin administrador devuelve "Error de sistema 5", así que `ArregloElevado` relanza con `Verb = "runas"` y trata el `Win32Exception 1223` —el usuario canceló el UAC— como respuesta válida, no como error.

## 6. La barra del entorno

Una barra flotante sin marco, siempre encima, en **su propio proceso** (`DevTools.exe --barra`). Es un proceso aparte para no depender del ciclo de vida de la ventana principal y para evitar IPC: cada proceso lee el estado de la API de tye por su cuenta.

**Pero ya no le sobrevive**: `MainFormBlazor.OnFormClosed` la cierra. El diseño original la dejaba viva —el entorno sigue arriba mucho después de que DevTools se cierre—, y se cambió por pedido: una barra flotando sin la aplicación detrás sorprende más de lo que ayuda. Para mirar el entorno sin tener la ventana abierta está el widget de bandeja. **Cerrar la barra no frena nada**: los servicios siguen corriendo y se la vuelve a abrir desde el Selector con «Controlar».

### Es HTML, como el resto de la aplicación

La primera versión fue Windows Forms, por arranque. Medido de verdad, un host con su propio componente y nada más tarda **2.880 ms** contra **1.672 ms** de la versión en Windows Forms: **1,2 s de diferencia**. Los 6.147 ms que figuraban como costo del WebView son los de la **ventana principal**, que adentro tiene los ~2 s de leer los user-secrets y la consulta de versión — ninguna de las dos cosa del visor.

A cambio, el aspecto deja de ser algo que haya que imitar a mano: la barra usa el mismo `app.css`, las mismas variables y los mismos tokens que el resto, y el tema se aplica solo con `data-tema`. Los dos paneles que colgaban de ella —servicios y bases— se volvieron parte del mismo componente, así que desaparecieron dos ventanas.

**El tema cuelga de `.shell`, no de `:root`**: el envoltorio del componente lleva `class="shell" data-tema="…"` sí o sí, o se ve con el tema claro siempre.

### La ventana

| | |
|---|---|
| Tamaño | **920 × 66** lógicos; ancho guardado entre 620 y 1400 |
| Arrastre | La página manda `postMessage('barra:mover')` desde `.barra-agarre` y el host responde con `ReleaseCapture()` + `WM_NCLBUTTONDOWN`/`HTCAPTION` |
| Estirado | Franja de **6 px** a la derecha, dejada libre con `Padding`: es lo único que el hit-test del formulario puede ver, porque el visor se queda con el mouse |
| Colapso | Queda una **pestaña de 50 px de ancho, con el mismo alto**, pegada al **borde más cercano** de tres: los dos laterales y el de arriba |
| Al volver | **Desde la pestaña hacia adentro**: apoyada a la derecha crece hacia la izquierda; en los otros dos lados, hacia la derecha |
| Posición | Se guarda, y **se acota a la pantalla al restaurarla**: una barra arrastrada fuera del área visible no se podía recuperar, porque no tiene botón en la barra de tareas |

**La pestaña vive siempre contra un borde** (`PegarLaPestaniaAlLateral`, el más cercano de los tres), conservando el otro eje. Suelta en medio de la pantalla es un recuadro flotando sin relación con nada, y además el despliegue necesita saber hacia qué lado crecer.

**La barra se despliega desde la pestaña hacia adentro**, así que el lateral que toca la pantalla es el mismo en los dos estados: apoyada a la izquierda crece hacia la derecha, y apoyada a la derecha hacia la izquierda (`MostrarLaBarra` resta el ancho). Mover la barra sigue siendo mover la pestaña: al colapsar se pega al lateral que le haya quedado más cerca, y puede cambiar de lado.

**Antes se escondía contra el borde más cercano**, con tres variantes, y volvía al pasarle el mouse por encima. Eso tenía dos problemas encadenados:

- La pestaña quedaba siempre en un borde, y si molestaba justo ahí **no había forma de correrla**: acercar el mouse para agarrarla la desplegaba.
- El retorno necesitaba una posición calculada —pegada al borde, centrada en el otro eje— que las tres entradas (la solapa, el hover, la flecha) tenían que compartir, porque cualquier desacuerdo entre ellas hacía saltar la barra de un lugar a otro.

Con el colapso en el lugar se fueron los tres bordes, el centrado, el `Asomar` y `ReplegarSiQuedoPegada`. **El despliegue es por clic y nunca por hover**: con la pestaña en cualquier parte de la pantalla, el mouse la abriría sola cada vez que el cursor pasara cerca.

### Los tres estados, como los paneles de Windows

| Estado | Pin | Qué pasa |
|---|---|---|
| Colapsada | — (no se ve) | Clic en la pestaña → se despliega **sin clavar** |
| | | El icono **alterna**: desplegada sin clavar, el clic la vuelve a cerrar. Clavada no, que es para lo que se clava |
| Desplegada sin clavar | `pin-angle` | Se contrae sola cuando el mouse sale. Clic en el pin → la clava |
| Desplegada y clavada | `pin` | Se queda. Clic en el pin → se colapsa a la pestaña |

El pin **muestra el estado** y el tooltip dice la acción, igual que en los paneles acoplables de Windows.

**El vigilante del mouse sigue existiendo, pero sólo para contraer** (`VigilarElMouse`, cada 250 ms, y sólo mientras `_temporal`). Se vigila con un reloj y no con `MouseEnter`/`MouseLeave` porque el visor ocupa toda la ventana y se queda con esos eventos: entrar a él contaría como salir de la ventana.

**Quién despliega decide si queda clavada**: el clic en la pestaña la deja sin clavar —es un vistazo—, y `Mostrarse()` o `AjustarAlto` la clavan, porque ahí alguien pidió ver algo y no es cuestión de que se le cierre en la cara.

### La pestaña

**Conserva el alto de la barra**: sólo cambia el ancho, de 920 a 44. Achicarla también de alto hacía un salto raro; así lo único que se percibe es que la barra se encoge hacia la izquierda y el icono cambia de relleno.

**Se puede arrastrar**, y por eso el clic que la despliega se decide en el host: `PrepararElArrastre` guarda la posición antes de pasarle el arrastre a Windows y, al volver, si la ventana no se movió, lo trata como un clic. No se usa un `@onclick` en la página porque el arrastre lo maneja Windows con su propio bucle modal —se queda con el botón del mouse— y no hay garantía de que el clic llegue después al visor.

**Sin redondeo contra la pantalla.** `Redondear()` arma el contorno esquina por esquina en vez de usar `MenuDelTema.Contorno`, que redondea las cuatro: un vértice redondeado contra el borde deja ver el escritorio por el hueco y se nota el recorte.

| Apoyo | Redondeadas | Rectas |
|---|---|---|
| Derecha | las dos izquierdas | las dos derechas |
| Izquierda | las dos derechas | las dos izquierdas |
| Arriba, o sin apoyo | las dos inferiores | las dos superiores |

**El caso «sin apoyo» no es decorativo**: `_lado` es nullable y arranca en null, porque recién abierta la barra está donde la dejaron y no se apoya en nada. Con un enum sin nullable arrancaba en su valor por defecto —izquierda— y se dibujaba con esos dos vértices rectos apoyada contra nada. Vuelve a null al arrastrarla: movida a mano deja de apoyarse, y el apoyo se decide de nuevo al colapsar.

Flotando se dibuja igual que apoyada arriba —abajo redondeada, arriba recta—, que es el aspecto que tiene la barra cuando nace.

**El icono no se mueve ni cambia de tamaño al colapsar**: la idea es que lo único que se perciba sea que pasa de contorno a relleno. Eso tuvo tres trampas, y las tres valen para cualquier otro icono que se sume:

1. **La zona de agarre vive a la izquierda de la barra**, así que con la pestaña apoyada a la derecha el icono saltaba de una punta a la otra. Con la clase `apoyo-derecha`, el CSS corre la zona de agarre al final (`order: 99`) y alinea la pestaña a ese lado.
2. **El icono se encogía aunque el CSS dijera 20 px.** La pestaña es un contenedor flex, y con 44 px de ancho y 14 de padding a cada lado quedaban 16 de espacio: el flex lo achicaba para hacerlo entrar. Se arregla con `flex: none` **y** con una pestaña lo bastante ancha (14 + 22 + 14 = 50).
3. **La pestaña se medía en unidades distintas que su contenido.** `AnchoDeLaPestania` está en las unidades del HTML, donde se definen el margen y el icono, pero `Bounds` se asigna en píxeles de pantalla: en un monitor al 125% la ventana quedaba más angosta que su propio contenido y el icono se comía el margen derecho. Por eso `AnchoDeLaPestaniaEnPantalla` lo convierte con `DeviceDpi / 96`. El alto no lo necesita porque sale de `Size`, que ya viene en píxeles reales.

**El color sale del tema**: `var(--acento)` en los aspectos claros —verde o azul según cuál esté puesto— y blanco en los oscuros, que se reconocen por el sufijo `-oscuro` del `data-tema`. El acento oscuro se apaga demasiado contra ese fondo.

**La animación de colapso se descartó.** El tamaño lo define la ventana nativa, así que animarla es redimensionar el `BlazorWebView` una decena de veces en pocos cuadros, y cada paso es un relayout del documento: se ve con tirones, peor que el salto.

### Mostrarla desde afuera

El Selector al levantar y el menú de la bandeja llaman a `ArranqueDeLaBarra.Lanzar()`. Si ya hay una —el mutex `AlgoritmoDevTools.Barra.InstanciaUnica` lo dice—, no abre otra: `TraerAlFrente()` la muestra. Dos cosas que hay que saber de ese método:

- **Va con `SWP_NOMOVE`.** Sin ese flag, los dos ceros de posición de `SetWindowPos` no son «no la muevas» sino «llevala a 0,0». El síntoma era que la barra se mudaba sola a la esquina superior izquierda de la pantalla principal cada vez que alguien tocaba Levantar, y si estaba replegada lo que llegaba ahí era una solapa de 44 px que el usuario no encontraba nunca.
- **Le pide a la barra que se despliegue**, con `PostMessage` de un mensaje registrado por nombre (`AlgoritmoDevTools.Barra.Mostrate`, vía `RegisterWindowMessage`, así los dos procesos llegan al mismo número sin compartir nada más). La barra lo atiende en su `WndProc` y llama a `Mostrarse()`. Hace falta porque desplegarse es algo que sólo ella puede hacer, y ponerla encima de todo sin desplegarla no muestra nada útil.

Y **la barra sale con el clic**, antes de generar el yaml y de arrancar tye: es lo único que el usuario tiene para mirar mientras compila. Si algo falla después, la barra dice «Sin entorno», que es la verdad.

### Los iconos

Casi todos son caracteres de **`Segoe MDL2 Assets`**, la fuente de iconos que trae Windows: la barra los escribe como entidades (`&#xE72C;` es el reinicio). No hace falta instalar nada y pesan cero.

**Los de Bootstrap Icons** (MIT) son por ahora cuatro, y marcan la dirección: la idea es ir reemplazando el resto.

| Dónde | Icono |
|---|---|
| Botón del pin, según el estado | `pin` / `pin-angle` |
| La zona de arrastre de la barra | `hdd-rack` |
| La pestaña, con la barra colapsada | `hdd-rack-fill` |
| El Selector en el menú lateral (`IconosSvg.Servicios`) | `hdd-rack` |

**La pestaña mostraba antes una flecha por borde** apuntando hacia donde iba a salir la barra. El rack dice mejor qué hay detrás, y es el mismo icono del entorno que usa el Selector en el menú: colapsada o no, la barra se reconoce por él.

**La zona de arrastre volvió a los puntos y después al rack.** Se probó con `grip-vertical`, el icono que Bootstrap tiene para eso, y se veía chico: sus puntos ocupan 7 de los 16 de ancho del dibujo y 14 de alto, así que a tamaño nominal quedan mucho más chicos que la grilla que hacía el `radial-gradient`, y agrandarlo los deja apretados en vez de espaciados. **No todo icono de Bootstrap es reemplazo directo de lo que hay.**

**El par de chinchetas vale por los tres estados de arriba**: el icono dice si está clavada y el tooltip qué pasa al tocarlo.

**Van como SVG embebido, no como fuente de iconos.** Unos pocos iconos no justifican sumar un `woff2` y su hoja de estilos, y así la aplicación sigue sin pedirle nada a la red —cosa que importa, porque corre en máquinas de desarrollo con el entorno a medio levantar—. Es además lo que ya hace el resto de la aplicación (`.icono-grupo svg`, `.extension-icono svg`). Los paths están como constantes en `BarraPagina.razor.cs` y en `CatalogoDeHerramientas.cs`, copiados de `twbs/icons`.

**La trampa al sumar los que faltan: los de Bootstrap son siluetas rellenas y los de acá son trazos.** El CSS de la aplicación pinta los SVG con `stroke: currentColor; fill: none` (`.nav-item svg`, `.icono-grupo svg`, `.extension-icono svg`), y con esas reglas un icono de Bootstrap se ve **vacío**. Por eso todos llevan la clase `ico-bi`, que da vuelta las dos propiedades sin tocar a los demás: `.nav-item svg.ico-bi` lo hace para el menú —incluidos el hover y el ítem activo, que cambian de color— y `.barra-accion .ico-bi` y `.barra-solapa .ico-bi` sólo les dan el tamaño, porque ahí el contenedor ya los centra y el color se hereda. **Si algún día se reemplazan todos, conviene revisar esta decisión**: a partir de cierta cantidad, la fuente o un componente que centralice los paths sale más barato que repetir SVG.

### Qué dice

**Ver la lista se habilita apenas el panel contesta**, sin esperar a que terminen de arrancar todos: mientras levanta es justo cuando uno quiere saber cuántos faltan y cuál se quedó. Los servicios que se están levantando solos —el arranque, el reinicio de todos, y el que ya tiene proceso pero todavía no contesta— muestran una rueda en lugar del botón. Ofrecerlo invitaría a tocarlo al pedo, y encima el `touch` de un proyecto que recién arrancó lo haría reiniciar de nuevo. Un servicio bajado a mano sí mantiene su botón, y se distinguen por el PID: al bajarlo se lo matamos, así que no tiene.

**El que se mandó a levantar de a uno queda en una lista de espera hasta que está verde** (`_esperando`, con tope de 3 minutos). Hace falta porque el `touch` no deja rastro: la llamada vuelve en seguida, pero el servicio tarda hasta tres cuartos de minuto en contestar y en todo ese rato tye no informa nada distinto de un servicio caído. Sin la lista, la rueda desaparecía apenas terminaba la llamada y el botón volvía a ofrecerse con el servicio todavía abajo — que fue exactamente el síntoma reportado.

La luz y el título son el estado del entorno, no de la última acción: `levantando`, `listo`, `abajo`, y el ámbar de **la base cambió y todavía no se reinició**. Cuando una acción está en curso el título la nombra —"Reiniciando…", "Frenando…"— y la luz pasa a un arco girando. El reinicio además muestra "N de M arriba", porque sin eso el usuario aprieta y no pasa nada visible durante medio minuto.

El sondeo es cada **3 s**. Nada cambia más rápido que eso.

### Las acciones

| Acción | Cómo está hecha |
|---|---|
| Reiniciar todo | `touch` a `Algoritmo.Microservices.Shared.API\BaseClasses\BaseProgram.cs` |
| Reiniciar uno | `touch` a su `.csproj`, que el panel de tye informa por servicio |
| Bajar uno | Matar su PID **dejando vivo el sidecar** |
| Volver a subirlo | `touch`, que es lo que `dotnet watch` está esperando |
| Sumar uno que falta | Segundo `tye run` con su propio yaml (§7.4) |
| Cambiar la base | Reescribir el user-secret y reiniciar todo (§8) |
| Frenar | `DELETE /api/v1/control` en cada panel, esperar a que no quede ninguno, cerrar la consola y limpiar (§9) |

**Frenar espera.** Bajar los diecisiete tarda, y una barra que desaparece enseguida deja al usuario sin saber si terminó. La barra se cierra recién cuando no queda nada arriba, con un tope de 3 minutos.

## 7. Qué se puede y qué no con tye corriendo

Todo lo de esta sección se probó en vivo contra el entorno el **2026-10-29**, no se infirió del código. Es el cimiento del diseño de la §6.

### 7.1 · El panel expone una API, pero no para servicios sueltos

El fork `Algoritmo.Tye 0.16.0-dev` tiene en `localhost:8000`:

| Endpoint | Qué da |
|---|---|
| `GET /api/v1/services` | Los 17 servicios con `state` por réplica, **pid**, puertos, `restarts`, el `.csproj`, el `.exe` y las 99 variables de entorno |
| `DELETE /api/v1/control` | Baja la aplicación entera. Devolvió 200 |
| `GET /api/v1/logs/{service}` | Los últimos logs en caché |
| `GET /api/v1/application`, `/metrics`, `/metrics/{service}` | Identidad y métricas |

**No hay endpoint para arrancar o parar un servicio suelto.** Es la limitación que manda sobre todo el diseño: todo lo demás son rodeos alrededor de esto.

### 7.2 · Reiniciar es `touch`, no matar el proceso

- **Matar el PID no reinicia, pero sí baja**: tye cuenta el `restarts` y no lo relanza, así que el servicio queda en `replicas: 0` con el resto intacto. El log lo dice: `watch: … process exited with exit code -1` seguido de `Waiting for a file to change before restarting dotnet...`.
- **`touch` a un archivo del proyecto sí reinicia**, porque tye corre con `--watch`. Medido: **~25 s** con el proceso vivo (PID 11504 → 6820: reinicio real, **no** hot reload) y **~40 s** si ya estaba muerto.
- **`touch` a `BaseProgram.cs` reinicia todos**: los 7 servicios cayeron y volvieron escalonados en **menos de un minuto**.
- Al bajar hay que matar **sólo la API y no su sidecar**: sin sidecar, la API que vuelve se cuelga esperando los secretos de Dapr con espera indefinida (`Seguridad:EsperaDaprSegundos` es 0), y el sidecar tampoco lo relanza tye.
- El `touch` **no ensucia el repositorio**: git compara contenido, no fecha. `git status` quedó limpio.

### 7.3 · La conexión no está en ningún lado que tye o Docker vean

De las 99 variables de entorno de un servicio, **ninguna es la cadena de conexión**. Sale de user-secrets (`SoftCerealCore.Development.ConnectionString`, store `softcerealcore` compartido por los 16) y cada API la lee **al arrancar**, a través de la indirección `UserSecrets:` que resuelve `UnitOfWork.cs:151-153`.

De ahí salen las dos consecuencias que gobiernan la §8: **DevTools es el único que puede decir contra qué base corre el entorno**, porque ya sabe leer esos secretos; y **cambiarla exige reiniciar**, porque nadie la vuelve a leer.

### 7.4 · Sumar un servicio al entorno vivo funciona

Contra lo que dice `docs/autenticacion-dapr-despliegue.md:136`, un segundo `tye run` convive con el principal. Verificado con invocación cruzada:

```
contabilidad (entorno principal) → stock (segundo tye) : HTTP 200 "Healthy" en 30 ms
stock (segundo tye) → directorio (principal)           : HTTP 200 en 32 ms
sidecar de ui (3500) → stock                           : HTTP 200 "Healthy" en 57 ms
```

Se ven porque los sidecars de las dos instancias comparten `.tye/dapr-name-resolution.db`, el resolvedor sqlite de `components/softcerealcore.local.yaml`.

**Las tres condiciones, todas necesarias** — se descubrieron fallando:

1. **El yaml lleva sólo el servicio y su sidecar.** Con `ui` external adentro, el segundo tye se llevó puestos los contenedores del primero y tiró 7 servicios: `ui` y la extensión `seq` declaran puertos que el principal ya tiene (5190, 5341), y tye levanta un contenedor proxy por cada servicio con binding.
2. **`--no-build`.** Sin eso recompila las bibliotecas compartidas y mata los procesos del principal, que quedan esperando un cambio de archivo.
3. **`--port 8001`**, porque el panel del principal ocupa el 8000. El segundo tye queda con su propia API, y por eso el proveedor **barre diez puertos** desde el 8000 en vez de mirar uno.

El front lo ve sin diferencia: en `appsettings.Development.json` cada servicio tiene su puerto fijo (`{ "Name": "Stock", "Connector": { "Port": "2743" } }`).

El yaml suelto lo escribe `GeneradorDeYamlSuelto` en `<CarpetaDeRepositorios>\.devYamels\`, **fuera del repositorio**, reescribiendo `project:` y `workingDirectory:` con prefijo `../AlgoritmoCore`.

### 7.5 · Los contenedores con nombre de servicio no son los servicios

`contabilidad-proxy_<hash>` y los otros ocho corren `dotnet Microsoft.Tye.Proxy.dll` con `PROXY_PORT=80:2736` y `CONTAINER_HOST=host.docker.internal`: reenvían desde la red de Docker al host, donde corre el proceso .NET. **Reiniciarlos no reinicia nada.** Existen sólo para que algo *dentro* de Docker alcance un servicio del host.

### 7.6 · El caso que costó un día: puertos reservados por Windows

`dapr_placement` y `dapr_scheduler` quedaron en `Exited (0)` y no arrancaban:

```
bind: Intento de acceso a un socket no permitido por sus permisos de acceso
```

Piden los puertos **58080** y **58081**, que cayeron dentro del rango **58021-58120**, reservado por Windows para puertos efímeros (`netsh interface ipv4 show excludedportrange protocol=tcp`). Se arregla con `net stop winnat; net start winnat` **como administrador**.

**`dapr uninstall && dapr init` no sirve**: esos puertos son la convención de `dapr init` (58080 = 5 + healthz 8080), así que recrea el mismo conflicto.

Mientras tanto el entorno levantaba **a medias, 8 de 16, sin un cartel que dijera por qué**: sin scheduler los sidecars no completan el arranque y las APIs se quedan esperando a Dapr, que es el "fallar cerrado" de `ConfiguracionSecretosSeguridad`. Ese síntoma —"levantó la mitad"— es la razón de que la revisión de prerrequisitos esté siempre a la vista.

### 7.7 · El 10013 del 30/10: era IIS, no Hyper-V

Al agregar `stock` en caliente, el derivado se generó bien, el segundo tye levantó en el 8001, el sidecar quedó listo y la API arrancó del todo —leyó la configuración, armó el contenedor de dependencias, preparó los metadatos de STK— y recién ahí murió:

```
[FTL] Programa terminado inesperadamente (stock)!
System.Net.Sockets.SocketException (10013): Intento de acceso a un socket no permitido por sus permisos de acceso.
   at ...SocketTransportOptions.CreateDefaultBoundListenSocket
```

Ocho segundos después del arranque, `exit code 1`, y tye en `Waiting for a file to change before restarting dotnet...` con la consola huérfana.

**La causa era un IIS local con sitios en esos puertos.** IIS los registra en `http.sys`, que los reserva **a nivel kernel**: aparecen en `netsh interface ipv4 show excludedportrange` igual que las reservas de Hyper-V, y cualquier otro proceso que intente bindearlos recibe 10013. Sacando IIS del medio, los cinco puertos afectados —cereales 2735, cuentacorriente 2737, finanzas 2739, stock 2743, logistica 2745— desaparecieron de la lista y quedaron libres, verificado con un `TcpListener`.

**Las dos trampas de diagnóstico, que acá costaron medio día:**

1. **10013 no es 10048.** `AccessDenied` es «Windows no te lo presta»; `AddressAlreadyInUse` es «otro proceso lo tiene». Con un 10013, buscar qué proceso ocupa el puerto no encuentra nada, porque la reserva es del kernel. Un `TcpListener` de prueba distingue los dos casos en un segundo, y conviene incluir un puerto de control que sí esté en uso para confirmar que la prueba discrimina.
2. **La tabla de `netsh` no dice de quién es cada tramo.** Una exclusión puede venir de Hyper-V (§7.6) o de http.sys, y se ven iguales. Por eso `net stop winnat; net start winnat` —que arregla el caso de la §7.6— no movió nada acá: no era de WinNAT. El asterisco tampoco ayuda a distinguirlo: marca las exclusiones administradas, no el dueño.

**Dónde mirar según el sospechoso**: `netsh http show servicestate` o `netsh http show urlacl` para lo que reserva http.sys; parar WinNAT y ver si se liberan, para lo de Hyper-V.

**No quedó código chequeando esto.** Se llegó a implementar un cruce de los puertos del `tye.yaml` contra `excludedportrange`, con el botón Destrabar reiniciando WinNAT, y se revirtió al aparecer la causa real: detectaba el síntoma pero ofrecía un remedio que para un puerto tomado por IIS no hace absolutamente nada, y encima con un cartel que señalaba al culpable equivocado.

## 8. El sello de conexión

Como la conexión no se puede leer de ningún lado (§7.3), **DevTools la anota al levantar**: `servidor|base|fechaISO` en `ConfiguracionDevTools`. Es el único registro de contra qué base está corriendo el entorno.

La barra compara el sello contra la conexión configurada ahora:

- **Iguales** → todo normal.
- **Distintas** → ámbar en la luz, en el nombre de la base y en el botón de reiniciar, que es lo que falta hacer para que los servicios tomen la base nueva.

El cambio de base **no reinicia solo**: lo decide el usuario, que puede estar a mitad de algo. La barra lo señala y espera.

**El sello se reescribe cuando ya no queda ningún servicio con el PID que tenía al cambiar la base** (`_pidsConLaBaseAnterior`): si todos volvieron a arrancar, todos leyeron la conexión nueva. Sin eso la barra seguiría en ámbar avisando de una diferencia que ya no existe.

**Mira el resultado y no el camino, y eso importa.** La primera versión lo reescribía en un solo lugar: al terminar un reinicio general, y dentro del tope de tres minutos. Quedaba colgado en ámbar para siempre en dos casos que pasan seguido:

- **Reiniciar los servicios de a uno** —o bajarlos y volver a subirlos—, porque ese camino no setea el cronómetro del reinicio general y nadie tocaba el sello.
- **Un reinicio general que tarde más de tres minutos**, porque al vencer el tope se limpia el cronómetro sin reescribirlo. Con recompilación completa es fácil llegar: entre el primer servicio que vuelve y el último puede haber dos minutos.

El tope de tres minutos sigue existiendo, pero ahora sólo gobierna la rueda de los servicios que están volviendo.

`Comparar()` devuelve la base configurada **aunque no haya sello**: la barra tiene que poder mostrar contra qué base se va a levantar antes de que exista entorno.

Los combos de servidor y base del desplegable vienen preseleccionados con lo que está en uso. No es cosmético: sin eso el combo intentaba conectarse al primer servidor de la lista —remoto— y se quedaba hasta el timeout.

## 9. Frenar y limpiar

Después de `DELETE /api/v1/control` quedan vivos **9 `daprd`**, 9 `dapr.exe` y 8 `python.exe`, ocupando el 3500, el 45736, el 42736 y los demás. Pasa porque tye lanza el sidecar como `powershell → python → dapr → daprd` y al bajar mata al abuelo, no al nieto. También quedan redes `tye_network_*` vacías — había **4 acumuladas** del 19, 20, 23 y 26 de octubre.

No es basura inofensiva: se queda con los puertos que el próximo arranque necesita, así que **el síntoma no es "quedó basura", es que la próxima vez el entorno levanta mal y la causa está en otro lado**.

Un **Ctrl+C limpio sí limpia** — cero contenedores, cero redes. Las redes huérfanas vienen de cierres que no lo fueron.

`LimpiadorDelEntorno` corre **al frenar y no al arrancar**: es el único momento en que se sabe con certeza que lo que quedó colgado sobra, porque el frenado lo pidió el usuario recién.

**El criterio es el app-id, no el nombre del proceso.** Un sidecar está huérfano si su app-id no figura en *ningún* panel vivo. No alcanza con "no corre tye": cuando hay un servicio agregado hay dos instancias, y barrer por nombre de proceso se llevaría puesto el entorno que sigue en pie.

**Qué no se toca nunca**: los contenedores de `dapr init` —placement, scheduler, redis, zipkin— son infraestructura del usuario; y el front levantado a mano, que no es del entorno.

## 10. Almacenamiento

| Dónde | Qué |
|---|---|
| `%LOCALAPPDATA%/AlgoritmoDevTools/TyeServiceSelector/data.db` | Tabla `Profiles` (`Name` como clave, `Services` en CSV) |
| `%LOCALAPPDATA%/AlgoritmoDevTools/configuracion.json` | El sello, la posición y el ancho de la barra, el perfil vigente |
| `<CarpetaDeRepositorios>\.devYamels\` | Los yaml de un solo servicio, fuera del repositorio |

La raíz del repositorio sale de `RutasDeTrabajo.AlgoritmoCore`, o sea de la carpeta elegida en Configuraciones.

## 11. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/TyeServiceSelectorPagina.razor(.cs)` | Pantalla del Selector |
| `Views/TyeServiceSelectorView.cs` | Pantalla de la ventana clásica, bajo `--clasico` |
| `Services/TyeServiceToggler.cs` | Lectura del maestro y generación del archivo |
| `Services/ProfileRepository.cs` | CRUD de perfiles |
| `Services/Entorno/IProveedorDeEntorno.cs` | **La costura.** Todo lo que la barra sabe hacer pasa por acá |
| `Services/Entorno/ProveedorTye.cs` | La implementación contra la API de tye |
| `Services/Entorno/LectorDeEstadoDeTye.cs` | Parser del JSON del panel |
| `Services/Entorno/EstadoDelEntorno.cs` | Los records en memoria |
| `Services/Entorno/LanzadorDeTye.cs` | Arranca tye en su consola, y la cierra al frenar |
| `Services/Entorno/ChequeoDePrerrequisitos.cs` | Contenedores de Dapr, puertos y conexión |
| `Services/Entorno/LectorDeRangosExcluidos.cs` | Parser de `netsh` y el cruce con los puertos pedidos |
| `Services/Entorno/ArregloElevado.cs` | Relanza el arreglo con UAC |
| `Services/Entorno/SelloDeConexion.cs` | Contra qué base se levantó |
| `Services/Entorno/GeneradorDeYamlSuelto.cs` | El yaml de un servicio para el segundo tye |
| `Services/Entorno/LimpiadorDelEntorno.cs` | Sidecars, contenedores y redes que sobraron |
| `AlgoritmoDevTools.UI/Paginas/BarraPagina.razor(.cs)` | La barra: estado, acciones y los tres desplegables |
| `AlgoritmoDevTools.UI/RaizDeLaBarra.razor` | Lo que se monta en la ventana de la barra |
| `AlgoritmoDevTools.UI/Servicios/IVentanaDeLaBarra.cs` | Lo que el componente le pide a su ventana |
| `AlgoritmoDevTools.UI/Servicios/ArranqueDeLaBarra.cs` | La lanza, o la trae al frente si ya está |
| `AlgoritmoDevTools.Shell/Barra/BarraDeEntorno.cs` | El `Form` sin marco que la hospeda |
| `AlgoritmoDevTools.UI/wwwroot/app.css` | Sección "Barra del entorno local" |

## 12. Qué tendrían que cubrir las pruebas

**Selector**

- Una variable de `env:` nunca aparece como servicio.
- Tildar un servicio activa también su sidecar; destildarlo comenta los dos.
- Los obligatorios (`seguridad`, `ui`) quedan activos aunque se pida desmarcar todos, y su casilla no responde al clic.
- `orquestador` queda tildado al desmarcar todos y al aplicar un perfil que no lo incluye; destildarlo a mano funciona, y refrescar respeta lo que diga el archivo generado.
- Un bloque que ya estaba como corresponde se copia sin tocar, con sus comentarios a mano.
- Volver a tildar un servicio comentado restituye su definición completa.
- Generar deja registrado el perfil; elegirlo del combo no.

**Entorno**

- El parser sobrevive a un `pid` o un `exitCode` en `null` (§13.1) y a un JSON truncado.
- Los sidecars, los servicios externos y la infraestructura no entran en la lista de servicios de la barra — `seq` no se muestra.
- El cruce de puertos excluidos detecta el caso 58080/58081 dentro de 58021-58120, y no marca un puerto que quedó justo afuera del rango.
- El sello compara bien, y devuelve la base configurada aunque no haya sello.
- El yaml suelto reescribe `project:` y `workingDirectory:` con el prefijo, y no arrastra `ui` ni la extensión `seq`.
- El limpiador respeta un sidecar cuyo app-id figura en un panel vivo, y se lleva el que no figura en ninguno.

## 13. Trampas y deuda conocida

### 13.1 · Las que ya se pisaron

**`JsonElement.TryGetInt32()` lanza si el valor no es un número; no devuelve `false`.** El panel manda `pid` y `exitCode` en `null` de rutina para los servicios que no están corriendo, así que leer un entero sin mirar antes el `ValueKind` tira una excepción que **se lleva puesto el panel completo** — y la barra muestra que no hay entorno con el entorno andando.

**Un `HttpClient` por consulta agota los puertos efímeros.** La barra sondea cada 3 s y cada sondeo mira 10 puertos: con un cliente nuevo por consulta son **200 sockets por minuto**, cada uno un rato en TIME_WAIT. A los pocos minutos fallan todas las conexiones y la barra deja de ver el panel *aunque el panel conteste en 200 ms*. Va uno solo para el proceso, con el tiempo de espera por llamada vía `CancellationTokenSource`.

**Una ventana sin botón en la barra de tareas que se arrastra fuera de la pantalla no se recupera.** Por eso la posición guardada se acota al restaurar.

**Un diálogo modal abierto desde un clic del WebView aborta el proceso entero, sin excepción** (`DialogosDelSistema.cs:72-108`). Todo lo que dispare una ventana desde una pantalla HTML tiene que diferirse con `BeginInvoke`.

**La barra abierta impide compilar en Debug** con `MSB3021`/`MSB3027`, igual que el widget: hay que cerrarla antes.

**39 de 118 archivos `.cs` del repositorio no tienen BOM y guardan acentos en cp1252.** Editarlos con una herramienta que asume UTF-8 destruye acentos de líneas ajenas al cambio.

### 13.2 · Deuda

| Qué | Por qué |
|---|---|
| El perfil guardado puede quedar desfasado de la selección | Se eligió el valor guardado sobre la deducción, que no acertaba |
| El `tye.devtools.yaml` aparece sin trackear en AlgoritmoCore | Conviene agregarlo al `.gitignore` de ese repositorio |
| Las rutas del maestro y del generado ya no se muestran | Se quitaron de la pantalla por pedido; quedan en el documento |
| Los logs por servicio no se muestran en la barra | `GET /api/v1/logs/{service}` existe y ahora sería barato (otro desplegable); embeber la consola real de tye sigue sin ser viable (§5) |
| Bajar un servicio deja su sidecar vivo a propósito | Es lo que permite volver a subirlo con un `touch`; si el servicio no vuelve, ese sidecar queda consumiendo su puerto hasta el frenado |

---

**Este documento debe actualizarse** cuando cambie el formato del `tye.yaml` maestro, la convención de los sidecars, el esquema de perfiles, la lista de servicios obligatorios o la del sugerido, la API del panel de tye, la forma en que se resuelve la cadena de conexión, o cuando se migre el entorno de tye a Docker Compose — en ese caso lo que cambia es **la implementación de `IProveedorDeEntorno`**, y esa es exactamente la razón por la que la interfaz existe.
