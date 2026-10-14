# Referencia técnica y funcional — Limpieza de disco (DiskCleaner)

Escanea el disco, explica qué es cada cosa que ocupa espacio y qué se pierde al borrarla, y limpia sólo lo que el usuario tilde.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

> **Estado al 14/10/2026: implementada y probada, sin commitear.** Rama `agregar-limpieza-de-disco`. Escanea, muestra, deja elegir, previsualiza, borra y eleva; 50 pruebas en verde. Lo que falta está en la sección 13.
>
> **La sección 11 es el activo principal**: trece trampas verificadas, cada una con su causa y su solución, y cada una costó un intento fallido o una tarde.
>
> El documento original, del 08/10/2026, **se perdió** —la carpeta quedó vacía y nunca llegó a commitearse—. Éste se reconstruyó desde el plan de implementación, que vive en `.planes\DevTools\DiskCleaner\` y conserva el detalle de cada decisión con su fecha.

---

## 1. Objetivo funcional

Darle al desarrollador un panorama de **qué está ocupando su disco**, con una explicación de qué es cada cosa y qué riesgo tiene borrarla, y permitirle limpiar **lo que él elija**, desde la misma ventana.

No es un `du` con interfaz. El valor está en dos cosas que un medidor genérico no da:

1. **Conoce este stack.** Sabe qué es `bin`/`obj` en una solución de 160 proyectos, qué es un `.ldf` inflado de SQL Express, qué es la caché del VS Installer y qué es un `docker_data.vhdx`.
2. **Conoce las trampas.** Las de la sección 11 son todas reales, verificadas, y ninguna se parece a su causa.

**El origen.** El 08/10/2026 una máquina del equipo quedó con **6,9 GB libres de 476,3 GB**. El relevamiento y la limpieza manual la dejaron en **139,77 GB**, y costaron un día entero, nueve intentos fallidos y la destrucción irrecuperable del inventario de productos de Visual Studio (trampa 11.11). Esta herramienta automatiza ese relevamiento **sin repetir ninguno de esos errores**.

## 2. Qué hace

- **Escanea** el disco y presenta los hallazgos agrupados, con tamaño real medido.
- **Clasifica** cada ítem por riesgo y explica en lenguaje llano qué se pierde al borrarlo.
- **Deja elegir**: nada se borra sin que el usuario lo tilde explícitamente.
- **Recuerda las elecciones** por máquina, para que cada uno mantenga su criterio entre corridas.
- **Previsualiza** (dry-run) antes de cualquier borrado.
- **Eleva sólo cuando hace falta**, y sólo para la acción que lo necesita.

### Fuera de alcance

- No se muestran `C:\Windows\Installer` ni `pagefile.sys`. **Decisión explícita, no un olvido** — ver sección 4.
- **No se toca la configuración de usuario de ninguna herramienta** — ver sección 5. Son carpetas chicas, no mueven la aguja del espacio, y perderlas cuesta horas de reconfiguración.
- No se administra el espacio de otros equipos ni de unidades de red.
- No reemplaza a `cleanmgr` ni a DISM para el mantenimiento del propio Windows.
- No se compacta el `.vhdx` de Docker: se avisa que hay que hacerlo desde Docker Desktop (trampa 11.9), no se ejecuta.
- No se ofrece `DevTools.exe.anterior` como espacio recuperable: lo borra la propia aplicación en el siguiente arranque.

## 3. El catálogo

Tamaños reales medidos el 08/10/2026 en la máquina del incidente. Son **referencia de magnitud, no valores esperables**: ninguna prueba los usa como valor fijo.

### Grupo Seguro — tildados por defecto

| Ítem | Medido | Si lo borrás | Riesgo |
|---|---|---|---|
| `bin`/`obj` bajo la carpeta de repositorios | 19,7 GB (446 carpetas) | Se regenera en el próximo build | **Ninguno** — primer build más lento |
| Cachés NuGet (`.nuget\packages`, `NuGet\v3-cache`) | 16,19 GB | Se vuelven a descargar | **Ninguno** — necesita internet |
| `%LOCALAPPDATA%\Temp` | 5,79 GB | Archivos temporales | **Bajo** — saltear lo que esté en uso |
| Logs `.ldf` de SQL (`SHRINKFILE`) | 34,24 GB | Libera espacio asignado sin usar | **Bajo** en `SIMPLE` — ver 11.8 |

### Grupo Con criterio — destildados, con explicación

| Ítem | Medido | Si lo borrás | Riesgo |
|---|---|---|---|
| Papelera de reciclaje | 20,89 GB | Se pierden los archivos definitivamente | **Irreversible** |
| Hibernación (`hiberfil.sys`) | 15,89 GB | Se pierde hibernar y el inicio rápido | **Medio** — reversible con `powercfg /h on` |
| `Agent\Media` (grabaciones) | 12,89 GB (5.637 `.mkv`) | Se pierden grabaciones corporativas | **Alto** — consultar con IT |
| Ubuntu WSL (`ext4.vhdx`) | 6,18 GB | Se borra la distro completa | **Irreversible** |
| DevExpress duplicado | 6,27 GB | Reinstalar si algún proyecto la usa | **Medio** — verificar contra los `.csproj` |

### Los dos informativos: se muestran, se explican, no se borran

Ocupan 31,58 GB entre los dos y **la herramienta no toca ni un archivo de ellos**. Mide, explica y ofrece el botón que lleva a la vía soportada. Es la misma decisión en los dos casos: lo que se gana borrándolos a mano no compensa lo que se arriesga.

| Ítem | Medido | Qué ofrece en lugar de borrar | Por qué |
|---|---|---|---|
| Caché del VS Installer | 14,45 GB | Abre el instalador de Visual Studio | Borrarla se llevó el inventario de productos instalados y **no se recupera** — ver 10.2 y la trampa 11.11 |
| Disco virtual de Docker | 17,13 GB | Abre Docker Desktop | Borrar el `.vhdx` a mano se lleva imágenes, contenedores y volúmenes de una vez; y un `prune` no achica el archivo — ver 10.6 y la trampa 11.9 |

### Los logs de SQL: un ítem por archivo, y sólo de instancias locales

El catálogo original los sumaba en una línea de 34,24 GB. En la implementación son **un ítem por archivo de log**, por dos razones: el estado es por archivo —uno puede estar trabado por una transacción abierta y los demás no— y el usuario puede elegir cuál achicar.

Dos reglas que no son evidentes:

- **Lo que se ofrece es lo recuperable, no lo asignado.** Un log de 16,71 GB con 0,15 % en uso devuelve casi todo; uno al 66 %, un tercio. Mostrar el tamaño total prometería espacio que el `SHRINKFILE` no va a devolver.
- **Sólo se miran instancias locales.** Un `.ldf` en otro servidor ocupa el disco de ese servidor. Se compara el host contra `.`, `(local)`, `localhost`, `127.0.0.1`, `::1` y el nombre del equipo, separando antes el `,puerto` y la `\INSTANCIA`.

### Detectar, no asumir

Un ítem **no aparece** si no aplica a la máquina:

- **Hibernación**: leer `powercfg /a`. Si ya está deshabilitada, el ítem no se muestra. Si está habilitada, se muestra **destildado** — en el equipo hay gente que hiberna y deshabilitarla le cambia el flujo de trabajo.
- **Docker, WSL, DevExpress, `Agent\Media`**: sólo si la ruta existe.
- **Logs SQL**: sólo las bases que la instancia reporta, consultadas en vivo.

### La grilla

| Ubicación | Tamaño | ¿Borrable? | Descripción y riesgo |
|---|---|---|---|

`¿Borrable?` es un semáforo: ✅ seguro · ⚠️ con criterio. **No hay ❌**, porque lo que no se debe borrar no se muestra.

## 4. Reglas en vigor (prescripciones negativas)

Cada una tiene detrás un error concreto.

- **No mostrar `C:\Windows\Installer` ni `pagefile.sys`, ni siquiera como informativos.** Un grupo "nunca borrar" es una tentación: alguien lee la ruta y la borra por fuera de la herramienta. `Windows\Installer` además no se limpia borrando archivos sino con DISM. El total puede ser honesto sin nombrarlas: *"476 GB ocupados, 90 GB gestionables"*.
- **No borrar la caché del VS Installer.** Ver 10.2 y la trampa 11.11.
- **No usar `Clear-RecycleBin`.** Ver trampa 11.1.
- **No pasar rutas con `$` por `ProcessRunner.RunPowerShell`.** Ver trampa 11.2.
- **No poner `requireAdministrator` en el manifest del Shell.** Ver sección 6.
- **No ofrecer `docker system prune` con `--volumes` sin confirmación escrita y separada.** Se entregó una vez ese comando con el flag puesto y una advertencia que hablaba sólo de imágenes; se corrió tal cual. `--volumes` borra los volúmenes con nombre, o sea **los datos**.
- **No medir el resultado por el delta de espacio libre del disco.** Ver trampa 11.7.
- **No matar procesos para destrabar un archivo.** Reportar qué proceso lo tiene y que decida el usuario.
- **No borrar el historial de conexiones de SSMS, ni ninguna configuración de usuario.** Ver sección 5 y trampa 11.10.

## 5. Rutas protegidas — la lista negra del escáner

Estas rutas **no se enumeran, no se muestran y no se borran**, bajo ninguna opción ni grupo. No es que estén destildadas por defecto: **el escáner ni las mira**.

| Ruta | Qué guarda | Por qué nunca |
|---|---|---|
| `%APPDATA%\Microsoft\SQL Server Management Studio\<ver>\UserSettings.xml` | **Historial de conexiones de SSMS** (nodo `ServerTypes`), más fuentes, layout y opciones del editor | Es la lista de servidores a los que te conectás todos los días. Recuperarla es a mano, servidor por servidor |
| `%APPDATA%\Microsoft\SQL Server Management Studio\RegSrvr*.xml` | Servidores Registrados | Igual: se reconstruye sólo a mano |
| `%APPDATA%\Microsoft\SQL Server Management Studio\` (toda la carpeta) | Plantillas y conexiones guardadas | Son kilobytes: no aportan al espacio y cuestan horas |
| `%APPDATA%\Microsoft\UserSecrets\` | User-secrets de los microservicios | Los administra el Secrets Manager de esta misma suite |
| `%LOCALAPPDATA%\AlgoritmoDevTools\` | Perfiles de las herramientas, incluido el de esta | Borrarlo haría que la herramienta se olvide de lo que el usuario eligió ignorar |
| `%APPDATA%\Microsoft\VisualStudio\` y `%LOCALAPPDATA%\Microsoft\VisualStudio\<instancia>\Settings\` | Configuración de VS | Mismo criterio |

**El criterio general**, que es el que decide los casos nuevos: todo lo que sea *configuración de usuario* queda afuera, aunque ocupe. Lo que se limpia es **lo regenerable por una máquina** —compilados, cachés de descarga, logs, temporales—, nunca lo que un humano configuró a mano.

**La trampa es el solapamiento.** `%LOCALAPPDATA%\Temp` es legítimo y `%LOCALAPPDATA%\AlgoritmoDevTools\` está protegido, y son hermanos. Cualquier regla que barra `%APPDATA%` o `%LOCALAPPDATA%` por patrón tiene que excluir esta lista explícitamente.

**Dónde se consulta.** El predicado `RutasProtegidas.EstaProtegida` se llama **dentro de `EnumeracionSegura`**, no en cada detector. Si cada detector tuviera que acordarse, el día que alguien agregue el detector número doce se olvida y la protección no existió nunca. `EnumeracionSegura` es el único camino por el que el escáner toca el disco.

## 6. Elevación — decisión de diseño

La solución **no tiene ningún `.manifest`**, así que DevTools corre sin elevar. Y `ProcessRunner` fija `UseShellExecute = false` (`ProcessRunner.cs:22` y `:41`), que es **incompatible con elevar**: pedir UAC requiere `UseShellExecute = true` + `Verb = "runas"`.

**Decisión: elevación bajo demanda.** No se pone `requireAdministrator` en el manifest del Shell.

Fundamento medido: de todo lo que se hizo el 08/10, **sólo cuatro operaciones** necesitaron admin —papelera, caché del VS Installer, `powercfg /h off` y DISM—. El escaneo completo y el borrado de `bin`/`obj`, cachés NuGet, `Temp` y el `SHRINKFILE` de SQL funcionaron **sin elevar**.

Elevar todo el Shell costaría un UAC en cada arranque para las seis herramientas, y rompe el arrastrar y soltar que usa el Convertidor a Markdown.

`ElevacionService` relanza el propio ejecutable con un verbo nuevo y `Verb = "runas"`, pasando el plan de limpieza **por archivo JSON temporal** —la línea de comandos tiene un límite de ~32 KB y además quedaría visible en el árbol de procesos—. Si el usuario cancela el UAC, `Process.Start` tira `Win32Exception` con `NativeErrorCode == 1223`: **hay que distinguirlo de un error real** y tratarlo como "el usuario dijo que no".

**Por qué no un `RunPowerShellElevated` en `ProcessRunner`:** elevar exige `UseShellExecute = true`, y eso hace imposible redirigir stdout y stderr, que es todo lo que `ProcessRunner` hace. Devolvería una cadena vacía siempre, y el reporte por ítem de la sección 7 es justamente lo que no se puede perder.

## 7. Flujo

1. **Escaneo** (sin elevar, fuera del hilo de interfaz, cancelable). Cada detector mide lo suyo, con paralelismo acotado.
2. **Grilla**: ítems detectados, con el perfil del usuario ya aplicado (tildes e ignorados).
3. **El usuario ajusta** los tildes. El panel muestra descripción, consecuencia y cómo se revierte del ítem seleccionado.
4. **Dry-run**: rutas exactas, tamaño por ítem, y **qué procesos tienen tomado qué**.
5. **Confirmación**, como bloque dentro de la página. Para `Alto` e `Irreversible`, confirmación escrita aparte.
6. **Ejecución**: primero todo lo que no necesita admin; después, si quedó algo, **un solo** UAC para el resto.
7. **Reporte**: por ítem, cuántos elementos se borraron y cuántos fallaron, **con el motivo**.

## 8. Almacenamiento

`%LOCALAPPDATA%/AlgoritmoDevTools/DiskCleaner/data.db`, por `ToolStorage("DiskCleaner")`. **Esa clave no se renombra nunca**: es lo que aísla los datos, y cambiarla deja a la herramienta sin su historial.

```sql
CREATE TABLE PREFERENCIAS (
    ITEM_ID      TEXT PRIMARY KEY,
    TILDADO      INTEGER NOT NULL,
    IGNORAR      INTEGER NOT NULL DEFAULT 0,  -- no volver a ofrecerlo nunca
    ACTUALIZADO  TEXT NOT NULL
);
```

`IGNORAR` cubre el caso "no me toques Docker, nunca": el ítem **deja de aparecer**, no queda destildado.

`ItemLimpieza.Id` es **estable y no se renombra**: es la clave con la que el perfil recuerda qué ignorar. Cambiarlo hace que el ítem reaparezca tildado en todas las máquinas del equipo.

**Estas preferencias no viajan en el archivo de transferencia de configuración.** Lo que guardan es qué ignorar *en esta máquina* —donde Docker puede existir o no y la papelera pesa lo que pesa acá—, y el README raíz deja afuera a propósito todo lo que es de la máquina o del avance personal de cada uno.

## 9. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/DiskCleanerPagina.razor(.cs)` | Pantalla |
| `AlgoritmoDevTools.UI/Servicios/SesionDeLimpieza.cs` | Lo medido y el análisis en curso, **fuera de la pantalla** para que sobrevivan a cambiar de herramienta — ver 10.9 |
| `Model/ItemLimpieza.cs` | El modelo entero: qué es un ítem, qué se puede elegir y qué no |
| `Services/EscaneoService.cs` | Orquesta los detectores, con paralelismo acotado |
| `Services/Detectores/CatalogoDeDetectores.cs` | Qué se busca y en qué orden |
| `Services/Detectores/*.cs` | Uno por ítem, salvo los cuatro que son "si esta ruta existe, medila" |
| `Services/EnumeracionSegura.cs` | **El único camino al disco**: poda la lista negra, tolera accesos denegados, soporta rutas largas y descarta reparse points |
| `Services/RutasProtegidas.cs` | La lista negra de la sección 5, en las dos direcciones |
| `Services/DetectorProcesos.cs` | Qué hay corriendo que convenga esperar |
| `Services/LimpiezaService.cs` | **Sólo el dry-run.** No tiene ningún método que escriba en disco |
| `Services/EjecutorDeLimpieza.cs` | **La única clase que borra** |
| `Services/QuienLoTiene.cs` | Restart Manager: quién tiene tomado un archivo que no se pudo borrar |
| `Services/PerfilRepository.cs` | SQLite, elecciones del usuario |
| `Services/ElevacionService.cs` | Relanza el helper con `Verb="runas"` y corre del otro lado |
| `AlgoritmoDevTools.Tools.DiskCleaner.Test/` | 50 pruebas. El primer proyecto de tests del repositorio |

**El dry-run y el borrado están en clases separadas a propósito.** El que mira la previsualización tiene que poder confiar en que eso no toca nada, y la forma de garantizarlo es que `LimpiezaService` no tenga ningún camino hasta `EjecutorDeLimpieza`. No es que no borre: es que no sabe borrar.

**No lleva implementación de `ITool` ni vista de Windows Forms.** El README raíz es explícito: para una herramienta nueva conviene hacer sólo la pantalla HTML, y su lista de alta no menciona `ITool`. `Program.CrearHerramientas()` arma el `ITool[]` sólo para `MainForm`, así que bajo `--clasico` esta herramienta no aparece.

### Qué toca fuera de la herramienta

| Archivo | Cambio |
|---|---|
| `AlgoritmoDevTools.Shell.csproj` y `AlgoritmoDevTools.UI.csproj` | Un `ProjectReference` más **en los dos** |
| `AlgoritmoDevTools.UI/CatalogoDeHerramientas.cs` | La entrada en `Todas` y el ícono en `IconosSvg` |
| `MainFormBlazor.RegistrarServiciosDeHerramientas` | Los servicios, con la clave `"DiskCleaner"` |
| `AlgoritmoDevTools.Core/Infrastructure/ProcessRunner.cs` | `RunProcess`, método **nuevo** — ver 10.3 |

## 10. Decisiones que conviene no revisitar

### 10.1 · `System.IO` antes que PowerShell

Para enumerar y borrar. Es más rápido, da mejores errores y **esquiva de raíz la trampa 11.2**: una ruta con `$` nunca llega a un intérprete que la expanda.

### 10.2 · La caché del VS Installer no se borra: se explica y se abre la pantalla

La herramienta mide los 14,45 GB, explica qué son y lleva al usuario a *Ubicaciones de instalación → «Mantener la caché de descarga después de la instalación»*, que es la vía soportada para que no se acumule.

El camino alternativo —enumerar y borrar sólo las carpetas de payload, excluyendo `_Instances` y toda entrada que empiece con `_`— recupera el espacio pero deja el riesgo vivo: un error en ese filtro destruye el inventario de productos **sin vuelta atrás** (trampa 11.11). Así el riesgo **no existe por construcción**, porque la herramienta no enumera esa carpeta para borrar.

Consecuencia en el modelo: `ItemLimpieza` necesita distinguir "borrar estas rutas" de "abrir esto y explicar". Sin eso, el ítem no se puede representar sin mentir.

### 10.3 · `ProcessRunner` necesita un método nuevo, no un cambio

`ProcessRunner.cs:87` convierte **cualquier** `ExitCode != 0` en excepción, y `powercfg /a`, `docker` y `wsl` devuelven códigos no-cero que **no son errores**. Hace falta un `RunProcess` que devuelva `(ExitCode, StdOut, StdErr)` sin tirar.

**No tocar `RunPowerShell` ni `RunDotnet`**: las otras cinco herramientas dependen de que tiren.

### 10.4 · Rutas largas por `\\?\`, no por manifest

Soportar rutas de más de 260 caracteres se puede con `longPathAware` en el manifest o con el prefijo `\\?\`. Se elige el prefijo: la solución no tiene ningún `.manifest`, y agregar uno al Shell para una sola herramienta abre la puerta a que alguien le sume `requireAdministrator` más adelante. El prefijo además no depende de la política del sistema.

### 10.5 · La medición se cuenta, no se resta

Ver trampa 11.7. El total liberado sale de sumar el tamaño conocido de cada ruta **antes** de borrarla y contar éxitos y fallos, nunca del espacio libre antes y después.

Vale también para la barra del disco: el espacio libre se lee **una sola vez, al escanear**, y después de una limpieza la barra se redibuja **re-escaneando**, nunca restando.

### 10.6 · Docker tampoco se borra

Mismo criterio que el VS Installer, por tres razones en orden: borrar el `.vhdx` a mano se lleva imágenes, contenedores y volúmenes de una vez y puede dejar Docker Desktop sin saber en qué estado quedó; un `prune` tampoco alcanza, porque libera adentro de la máquina virtual pero el archivo sigue ocupando lo mismo (trampa 11.9); y compactarlo **sólo se puede desde Docker Desktop**.

Consecuencia en el código: el ítem lleva `Derivacion` y `Rutas` vacío. **Un ítem que deriva nunca carga rutas** — dejarlas sería tener a mano una lista de cosas para borrar que justamente se decidió no borrar.

### 10.7 · Un proceso en ejecución advierte, no impide

En una máquina de desarrollo casi siempre hay un `MSBuild` o un `dotnet` vivo. Tomarlos como impedimento dejaría `bin`/`obj` —el ítem más grande del grupo seguro— apagado prácticamente siempre.

De 446 carpetas fallaron 3, y **reintentar una vez resolvió 2**: el costo de intentarlo es un reintento, no un desastre. Lo que sí impide elegir es `MotivoParaNoTocarlo`, que es donde un detector pone una razón real —hoy, el log con una transacción abierta—.

La asimetría que sí importa: **los instaladores bloquean** (el daño es irreversible), **las compilaciones advierten**.

### 10.8 · La lista negra se consulta también del otro lado del permiso

El helper elevado borra con permisos de administrador y su entrada es un archivo. Una entrada manipulada sería un borrado arbitrario con privilegios, y lo que lo impide es que **`EjecutorDeLimpieza.BorrarRutas` revalida cada ruta ahí adentro**, no antes de elevar.

Lo mismo vale para el borrado recursivo: `Directory.Delete(recursive: true)` **no consulta nada**, le pide al sistema el árbol entero. Por eso existe `RutasProtegidas.ContieneAlgunaProtegida` —la pregunta inversa— y se hace justo antes de tocar el disco.

### 10.9 · Lo medido y el análisis en curso viven fuera de la pantalla

El estado del análisis —lo medido, lo encontrado con su tildado, y si hay un recorrido corriendo— está en `SesionDeLimpieza` (`AlgoritmoDevTools.UI/Servicios/`), registrada **singleton**, y no en campos de `DiskCleanerPagina`. La pantalla sólo lee de ahí y se suscribe a `Cambio`.

Dos motivos, y los dos se vieron en uso:

1. **Cambiar de herramienta durante el análisis cerraba la aplicación.** `Escanear()` era un controlador asincrónico de cincuenta segundos; al irse, Blazor desarma el componente y su `Dispose` cancelaba **y liberaba** el `CancellationTokenSource` en el acto, con el `Parallel.ForEachAsync` todavía en vuelo. Al volver del `await`, el controlador intentaba repintar un componente que ya no existía. En el visor, una excepción que nadie atrapa **cierra la ventana entera**, así que el síntoma es "se cerró el programa" y no un error.
2. **Cambiar de herramienta después del análisis lo perdía**, y había que esperar los cincuenta segundos otra vez para ver lo mismo.

Reglas que se siguen de esto:

- **El `CancellationTokenSource` se libera en el `finally` de quien lanzó la operación**, no en el `Dispose` del componente: ese es el único momento en que nadie más lo usa. Cancelar desde afuera sí es válido; liberar, no.
- **Lo que avisa desde segundo plano vuelve al hilo de la interfaz antes de tocarla** (`InvokeAsync`), y mira si la pantalla sigue montada. Por eso `AlCambiarLaSesion` traga `ObjectDisposedException` —la ventana entre el aviso y la desuscripción— y el progreso del borrado chequea `_enPantalla`.
- **`Dispose` de la pantalla no limpia la barra de estado si hay un análisis corriendo**: ese aviso es justamente lo que tiene que quedar a la vista desde otra herramienta.
- **El aviso de fin lleva "Disk Cleaner:" adelante**, porque puede llegar estando en otra pantalla y ahí "23 cosas encontradas" no dice de qué habla.
- **El fin del análisis avisa por los dos canales**: la barra de estado y una pastilla. Sólo la barra no alcanzaba —es lo que se probó primero—: cualquier pantalla que escriba la pisa y el `Dispose` de la que lo puso la limpia, así que el que se había ido a trabajar en otra herramienta no se enteraba nunca. Cancelar, en cambio, no lleva pastilla: eso lo acaba de hacer el usuario mirando. La diferencia entre los dos canales está en la sección 5.2 de `AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md`.

**Lo que todavía no está resuelto:** el **borrado** sigue siendo un controlador del componente. Irse de la pantalla mientras borra no lo corta —y no debería cortarlo— pero su final tampoco está puesto en la sesión. Hoy sólo está protegido el repintado del progreso. Si se toca esa parte, conviene mudarlo a `SesionDeLimpieza` con el mismo patrón.

## 11. Trampas verificadas

### 11.1 La papelera miente

`Clear-RecycleBin -Force` **devolvió éxito y liberó 0 GB**. El Explorador mostraba la papelera vacía mientras ocupaba 20,89 GB.

Causa: de 608 entradas, había **572 archivos de datos `$R` y sólo 35 índices `$I`**. Sin su `$I`, el shell no puede listar la entrada, así que la API de papelera la ignora — pero los bytes están.

Solución: enumerar `C:\$Recycle.Bin\<SID>` directo con `System.IO` y borrar cada entrada. **Requiere admin**: sin elevar, las 572 fallan con *acceso denegado*.

### 11.2 Las rutas con `$`

`RunPowerShell` arma `-NoProfile -Command "{command}"` (`ProcessRunner.cs:17`): el comando viaja **entre comillas dobles**, así que PowerShell expande todo `$loquesea` antes de ejecutarlo. `C:\$Recycle.Bin` se convierte en `C:\.Bin` y falla con "no encuentra el archivo", que despista porque suena a que el destino no existe.

Para estas rutas: trabajar con `System.IO` desde C#, o construir la ruta concatenando, nunca interpolando.

### 11.3 Acceso denegado a mitad de enumeración

Un `Get-ChildItem -Recurse` sobre `C:\` muere en la primera carpeta sin permiso y se pierde el recorrido entero. En C#: `EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true }`.

### 11.4 Rutas largas

El `CLAUDE.md` de SoftCerealCore fija el umbral seguro en **245 caracteres** y documenta dos carpetas de feature que ya rozan el límite. El escáner tiene que soportar rutas de más de 260 — `\\?\` o long paths habilitados.

### 11.5 Archivos bloqueados

De 446 carpetas `bin`/`obj`, **3 fallaron**: dos `obj` por *"el directorio no está vacío"* —Visual Studio las estaba recreando en paralelo con sus builds de diseño— y una por *"acceso denegado"* al propio ejecutable de DevTools corriendo.

Dos conclusiones: **reintentar una vez** resuelve el caso de la recreación concurrente —las dos `obj` se borraron en el segundo intento—, y el reporte tiene que decir **qué proceso** tiene tomado el archivo, no un error genérico.

> El ejecutable hoy se llama `DevTools.exe`, y la aplicación se actualiza sola renombrándose a `DevTools.exe.anterior`. Ese `.anterior` **no es basura a ofrecer**: lo borra la propia aplicación en el siguiente arranque.

### 11.6 Operaciones en vuelo

Mientras se analizaba el disco había un **VS Installer corriendo**:

```
setup.exe modify --installPath "C:\Program Files\Microsoft Visual Studio\18\Community"
          --add Microsoft.NetCore.Component.Runtime.6.0
```

Borrar la caché del VS Installer en ese momento rompe la instalación en curso.

**Antes de borrar, detectar**: `setup.exe`, `vs_installer`, `devenv`, `msiexec`, `MSBuild`, `dotnet` (en particular `dotnet workload restore`). Si alguno está activo, bloquear el ítem afectado y explicar por qué.

### 11.7 El espacio libre no sirve como medida

Dos mediciones separadas por minutos dieron **6,9 GB** y **40 GB** libres sin intervención de por medio; nunca se explicó el salto. Y al borrar `bin`/`obj` el delta de espacio libre marcó **41,07 GB** cuando el contenido real era **19,7 GB**, porque otra operación corría en paralelo.

**Medir contando elementos borrados vs. con error, y sumando el tamaño conocido de cada ruta antes de borrarla.** Nunca por `FreeSpace` antes/después.

### 11.8 Los logs de SQL

Lo que se creyó al principio —*"están en `FULL` sin backups de log"*— era **falso**. Verificado:

| Base | Modelo | Log asignado | % usado | `log_reuse_wait` |
|---|---|---|---|---|
| `AlgoritmoOld` | **SIMPLE** | 16,71 GB | **0,15 %** | `NOTHING` |
| `Algoritmo93` | **SIMPLE** | 13,02 GB | **0,03 %** | `NOTHING` |
| `Algoritmo` | FULL | 4,51 GB | **65,98 %** | `ACTIVE_TRANSACTION` |

El problema no era el *recovery model* sino que **los archivos nunca se achicaron** después de un pico. Crecer es automático, achicar no.

Tres trampas acá:

1. **El nombre lógico no es el nombre del archivo.** Las tres bases vienen del mismo backup y sus logs se llaman **todos `Algoritmo_log`**, en archivos `AlgoritmoOld.ldf`, `Algoritmo93.ldf` y `Algoritmo_log.ldf`. Hay que resolver el nombre lógico **dentro de cada base** (`sys.database_files`), no desde `master`.
2. **Una base puede tener más de un archivo de log.** `AlgoritmoOld` tiene dos: `Algoritmo_log` (13,51 GB) y `Algoritmo1_log` (3,20 GB → `AlgoritmoOld1.ldf`). Un `AlgoritmoOld1.ldf` suelto **no** es una base huérfana; confirmar siempre contra `sys.master_files`.
3. **No achicar con `log_reuse_wait = ACTIVE_TRANSACTION`.** Hay una transacción abierta sosteniendo el log; el shrink no va a recuperar nada y puede trabarse. Mostrar el ítem deshabilitado con el motivo, y ofrecer `DBCC OPENTRAN` como diagnóstico.

Sólo achicar **logs**. Achicar un `.mdf` fragmenta los índices y no se ofrece.

### 11.9 El `.vhdx` no se achica solo

`docker system prune` libera espacio **dentro** de la VM, pero `docker_data.vhdx` mantiene su tamaño en el host. Para recuperar los bytes hay que compactar: Docker Desktop → Settings → Resources → Clean / Purge data. Si la herramienta ofrece el `prune`, tiene que decir esto o el usuario no ve bajar nada.

### 11.10 El historial de conexiones de SSMS se pierde sin aviso

El 09/10/2026 SSMS 22 empezó a tirar esto en cada conexión:

```
La conexión se realizó correctamente, pero se produjo un error al guardar el historial de conexiones.
No se ha encontrado el elemento. (Excepción de HRESULT: 0x80070490)
(Microsoft.VisualStudio.Data.Tools.Design.Core)
```

`0x80070490` es `ERROR_NOT_FOUND`. La causa, verificada en el archivo: el nodo `ServerTypes` de `%APPDATA%\Microsoft\SQL Server Management Studio\22.0\UserSettings.xml` estaba **vacío**. Comprobado contra muestra de control en la misma máquina, porque un nodo vacío podría ser el estado normal y no lo es: SSMS 18.0 y 19.0 tenían 1 nodo con 2 entradas cada uno; **SSMS 22.0, cero**.

**No se pudo establecer que la limpieza lo haya causado.** El XML estaba bien formado y ninguno de los comandos ejecutados toca esa ruta. La explicación más plausible es que venga de la pérdida de `_Instances` (trampa 11.11), porque el error sale de `Microsoft.VisualStudio.Data.Tools.Design.Core` y que la capa de data tools no pueda resolver la metadata de su instalación encaja mejor que las otras hipótesis; también es un bug conocido de SSMS 21/22. **Sin verificar.**

Aun así, el episodio deja la regla: **la configuración de usuario no es espacio recuperable y no entra al catálogo** (sección 5).

**Arreglo**, por si reaparece: cerrar SSMS —si queda abierto, al salir reescribe el archivo y pisa el cambio—, **renombrar** `UserSettings.xml` y volver a abrir, que lo regenera con la estructura completa. Se pierden las preferencias de esa versión; **no** se pierden los Servidores Registrados, que viven aparte en `RegSrvr*.xml`. **Renombrar, nunca borrar.**

### 11.11 `Packages\_Instances` NO es caché: es el inventario de productos instalados

**La peor consecuencia de la limpieza del 08/10/2026, y la causó una recomendación mal acotada.**

Se entregó este comando presentándolo como "sólo instaladores descargados":

```powershell
Remove-Item 'C:\ProgramData\Microsoft\VisualStudio\Packages\*' -Recurse -Force
```

Dentro de `Packages` no sólo está la caché de payload. Está **`_Instances\<InstallationID>\`**, con el `state.json` de cada producto instalado: el registro que el VS Installer y `vswhere` leen para saber qué hay en la máquina. El comando se lo llevó.

**Resultado**: el VS Installer abre y **no lista ningún producto**. `vswhere -all` no devuelve nada. Los productos siguen en disco y **arrancan sin problema** —verificado con VS 18 Community, VS 2022 Community y SSMS 22—, pero no se puede reparar, modificar ni actualizar ninguno.

**No se puede recuperar.** Confirmado en los hilos de Microsoft Q&A y Developer Community: `_Instances` no se descarga ni se repara por separado, y la salida es desinstalar y reinstalar. También se verificó que `Package Cache` **no** guarda el bootstrapper de VS ni de SSMS 22.

**Lo que sí sobrevive y sirve para reconstruir**: cada producto conserva su `*.isolation.ini` en `<installPath>\Common7\IDE\`, con `InstallationID`, `InstallationName` y `ChannelId`. En la máquina del caso:

| Producto | InstallationID | ChannelId |
|---|---|---|
| VS 18 Community | `7947e009` | `VisualStudio.18.Release` |
| VS 2022 Community | `8993d621` | `VisualStudio.17.Release` |
| SSMS 22 | `52bf8656` | `SSMS.22.SSMS.Release` |

Reconstruir `_Instances` a mano no está soportado. La recuperación es volver a correr el *bootstrapper* de cada producto apuntando al mismo `installPath`, que vuelve a registrar la instancia.

**La regla para la herramienta está en 10.2**: la caché del VS Installer no se limpia borrando archivos. La documentación de Microsoft directamente no recomienda borrar a mano la carpeta de caché de descarga — esa frase sola habría evitado todo esto.

### 11.12 Los acentos de los archivos sin BOM se pierden al editarlos

**39 de los 118 archivos `.cs` de este repositorio no tienen BOM**, y varios guardan sus acentos en cp1252. Al editarlos con una herramienta que asume UTF-8, esos bytes no forman secuencia válida: se reemplazan por `U+FFFD` y **el acento se pierde para siempre**, en líneas que uno ni tocó.

Pasó al agregarle un método a `ProcessRunner` y otro a `SQLService`: se perdieron los acentos de tres comentarios ajenos al cambio. El síntoma es un diff que marca como modificadas líneas que uno no escribió, y que a simple vista se ven iguales.

**Cómo detectarlo**, sobre los archivos modificados:

```bash
grep -c $'\xEF\xBF\xBD' <archivo>
```

**Cómo repararlo**: leer el archivo en modo híbrido —UTF-8 donde los bytes forman secuencia válida, cp1252 donde no—, restaurar las líneas rotas contra su versión en `HEAD`, y **guardar en UTF-8 con BOM**, que es lo que evita que vuelva a pasar y lo que pide la convención del repositorio.

### 11.13 El widget residente impide compilar

Con un `DevTools.exe` vivo —típicamente el widget de bandeja—, cualquier compilación en Debug muere con `MSB3027` / `MSB3021`: *«el archivo se ha bloqueado por: DevTools (PID)»*. La ventana nunca abre y el error no menciona al widget.

Es la razón por la que su menú tiene **Salir** a mano. `dotnet build -c Release` no se ve afectado, porque escribe en otra carpeta.

## 12. Qué tendrían que cubrir las pruebas

- El dry-run **no borra nada**. Es la prueba que más importa.
- **`_Instances` —y toda entrada que empiece con `_`— nunca aparece en las rutas a borrar de ningún ítem.** Hoy es trivial porque el ítem no borra; existe para que siga siendo cierto si alguien le agrega borrado después.
- **Ninguna ruta protegida de la sección 5 aparece en el resultado del escaneo**, en particular el `UserSettings.xml` de SSMS. Debe correr sobre la lista negra completa, no sobre un caso suelto.
- Una regla que barra `%APPDATA%` por patrón excluye la lista negra.
- Un detector sobre un árbol de prueba devuelve el tamaño correcto y **no sigue** symlinks/junctions.
- La enumeración sobrevive a una carpeta sin permisos y a una ruta de más de 260 caracteres.
- El perfil persiste e `IGNORAR` saca el ítem del resultado.
- Un ítem con proceso bloqueante **se reporta y se intenta igual**: advierte, no impide.
- Ningún ítem del catálogo expone `Windows\Installer` ni `pagefile.sys`.

**Estado al 14/10/2026: 50 pruebas, todas en verde, en 17 segundos.**

```bash
dotnet test AlgoritmoDevTools.Tools.DiskCleaner.Test -c Release
```

Se prueba contra archivos de verdad y no contra un disco simulado: un junction, una carpeta sin permiso y una ruta de más de 260 caracteres no existen en una abstracción. `ArbolDePrueba` crea una carpeta temporal y la borra sola, y los casos que necesitan `mklink /J` o `icacls /deny` **se saltean solos si el escenario no se puede montar**, en lugar de dar por bueno algo que no se ejercitó.

El escaneo del catálogo se comparte entre sus pruebas con `IClassFixture`: medir el disco tarda cerca de un minuto, y repetirlo por prueba convertía la serie en algo que nadie corre.

### Dos cosas que no se pueden cubrir con una prueba

| Qué | Por qué | Cómo se verificó |
|---|---|---|
| El nombre lógico con dos bases que lo comparten | Necesita un SQL Server con esas bases restauradas del mismo backup | **En vivo**: el escaneo real encontró `Algoritmo` con sus dos archivos de log |
| Cancelar el permiso se distingue de un fallo | El cartel de Windows no se puede automatizar | Todo lo que pasa **después** del cartel está probado llamando al helper directamente |

## 13. Deuda conocida y preguntas abiertas

| Tema | Estado |
|---|---|
| El salto de 6,9 → 40 GB libres sin intervención | **Sin explicación.** Se descartó que fuera obra de la limpieza: ocurrió antes del primer borrado |
| Si el `docker system prune -a --volumes` del 08/10 llegó a ejecutarse | **Sin verificar.** El daemon ya no respondía 5 minutos antes, así que pudo haber fallado |
| Estado de Docker en esa máquina | `com.docker.service` **Stopped**, `ExitCode 1077`; `dockerd.log` sin escrituras desde el 26/03/2026. **Dejó de importar**: el ítem ya no depende del daemon, porque no ejecuta nada |
| Si DevExpress 24.2 es realmente prescindible | **Sin verificar.** El `CLAUDE.md` de la UI declara 24.1; hay que confirmarlo contra los `.csproj` antes de ofrecerlo |
| `Agent\Media` | 5.637 `.mkv`, nada escrito desde el **29/07/2025**. Es software corporativo: la herramienta debe exigir confirmación con IT, no sólo del usuario. Falta definir qué significa eso en la pantalla |
| Conexión SQL ausente | Si no hay ninguna conexión guardada, el ítem de logs no aparece. Es coherente con "detectar, no asumir", pero alguien puede extrañar esos GB sin saber por qué |
| Backups `.bak` viejos | **No implementado.** Falta definir dónde buscarlos —la carpeta depende de cada instancia— y sobre todo **qué cuenta como "viejo"**, que es un criterio funcional. Sin eso, el ítem no encuentra nada o propone borrar el backup de ayer |
| WSL borra su `ext4.vhdx` directamente | La vía soportada para dar de baja una distro es `wsl --unregister`. Es el mismo caso que Docker, con una diferencia: ahí lo borrado se vuelve a bajar, acá **la distro es el dato**. Ya está marcado `Irreversible` y pide confirmación escrita |
| `Agent\Media` sin verificar en una máquina que lo tenga | Las rutas candidatas son razonables pero **no se confirmaron**: en la máquina de desarrollo no existe, así que el ítem no aparece |
| Sin pantalla bajo `--clasico` | Deliberado (sección 9). Si alguien la pide, es una vista de Windows Forms más una implementación de `ITool` |

## 14. Piezas relacionadas

| Pieza | Ruta |
|---|---|
| Plan de implementación | `.planes\DevTools\DiskCleaner\DiskCleaner - Limpieza de disco (plan).md` |
| Cómo se da de alta una herramienta | [README.md](../README.md) — los 8 pasos y la lista completa de 12 lugares |
| Catálogo e íconos | `AlgoritmoDevTools.UI/CatalogoDeHerramientas.cs` |
| Trampas de la interfaz | [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md) |
| Temas | [temas](../AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md) |
| Rutas de repositorio | `AlgoritmoDevTools.Core/Infrastructure/RutasDeTrabajo.cs` |
| Ejecución de procesos | `AlgoritmoDevTools.Core/Infrastructure/ProcessRunner.cs` |
| Almacenamiento por herramienta | `AlgoritmoDevTools.Core/Abstractions/IToolStorage.cs` |
| Servicios SQL reutilizables | `AlgoritmoDevTools.Integrations.SoftCerealCore/SQLService.cs` |

---

**Este documento debe actualizarse** cuando se agregue o quite un ítem del catálogo, cuando cambie el modelo de elevación, cuando se modifique el esquema de `PREFERENCIAS`, o cuando se descubra una trampa nueva. **La sección 11 es el activo principal**: toda trampa que cueste un intento fallido va ahí, con su causa y su solución. Toda ruta que haya que proteger va a la lista de la sección 5.
