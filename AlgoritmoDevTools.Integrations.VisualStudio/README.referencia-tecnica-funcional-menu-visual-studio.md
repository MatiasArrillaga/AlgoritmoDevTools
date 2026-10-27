# Referencia técnica y funcional — El menú de DevTools dentro de Visual Studio

Extensión que pone los comandos de migración del Commands Maker en el clic derecho de la
**Consola del Administrador de paquetes**, que es exactamente donde se pegan.

Documento hermano: [Commands Maker](../AlgoritmoDevTools.Tools.CommandsMaker/README.referencia-tecnica-funcional-commands-maker.md),
que es el dueño del modelo (dominios, dominio activo, cómo se arma cada comando). Acá sólo está lo
propio de Visual Studio.

## 1. Objetivo funcional

Las migraciones se corren pegando un comando en la Package Manager Console. Que el único acceso al
generador fuera el menú del explorador de Windows obligaba a cambiar de ventana para algo cuyo
destino estaba en la misma pantalla, a un panel de distancia.

## 2. Qué hace

Clic derecho **dentro de la Consola del Administrador de paquetes**:

```
Commands Maker ▸
    Elegir dominio...
    ────────────────
    Add-Migration (Cereales)
    Remove-Migration (Cereales)
    Update-Database (Cereales)
```

Un solo nivel: el menú arranca en *Commands Maker*, sin un *DevTools* arriba, y en su propia sección
del menú. Los tres comandos dejan el comando en el portapapeles y
hay que pegarlo con Ctrl+V — que se escriba solo se probó y no se puede (sección 8); ninguno abre
ventana. "Elegir dominio" abre el diálogo
de DevTools y al cerrarse los tres ítems de arriba cambian de texto solos.

**También aparece al hacer clic derecho sobre un archivo de código**, y eso no se puede evitar: la
consola muestra el mismo menú contextual que el editor. La sección 5 explica por qué y por qué el
intento de separarlos fracasó.

## 3. La decisión central: es un espejo, no una segunda implementación

La extensión **no sabe** armar un comando de migración ni **decide** cuál es el dominio activo. Lee
el menú que el Commands Maker ya escribió en el registro para el explorador, y lanza el mismo
ejecutable con el mismo verbo.

De la clave `10Add` salen las dos únicas cosas que necesita:

```
HKCU\Software\Classes\Directory\Background\shell\AlgoritmoDevTools\shell\20Comandos\shell\10Add\command
    = "C:\...\DevTools.exe" --comando-add Cereales
         |__ ejecutable __|               |dominio|
```

**Por qué no leer el SQLite del Commands Maker.** El dominio activo no es un campo: es *el primer
reciente que siga existiendo, o el primero alfabético* (`MenuContextualComandos.TryInstalar`).
Replicar esa regla acá sería una segunda implementación que se desincroniza sin que nada avise. El
menú del explorador ya es el resultado de haberla aplicado. De paso, evita meter
`Microsoft.Data.Sqlite` y su DLL nativa adentro del `.vsix`.

**Consecuencia aceptada:** el menú de Visual Studio **no funciona sin el del explorador**. Si el
grupo no está instalado, los ítems se muestran con el nombre pelado y al usarlos explican que hay
que instalarlo desde la pantalla del Commands Maker.

## 4. Por qué el proyecto es net8.0 y no referencia a nadie

Lo fija el SDK de extensibilidad, que hoy publica `17.14` y no tiene build para net10. Por eso este
proyecto **no puede** referenciar al resto de la solución (net10.0-windows) y se comunica con
DevTools igual que el explorador: lanzando el `.exe` con un verbo y esperando a que termine.

El Shell ya resuelve esos verbos sin abrir ventana, así que acá no se duplica lógica de negocio. Lo
que **sí** quedó duplicado son los cuatro verbos (`VerbosDelShell`) — ver sección 12.

## 5. Dónde se ancla el menú, y por qué ahí

**La Consola del Administrador de paquetes no tiene menú contextual propio.** Está hosteada sobre un
`IVsTextView` (`PowerConsoleToolWindow.VsTextView`, en NuGet.Client), así que lo que muestra al clic
derecho es el menú del **editor**. En el `.vsct` de NuGet no hay ningún menú contextual al que
colgarse: sólo declaran una toolbar de ventana (`guidPowerConsoleCmdSet`, `idToolbar`).

Entonces el anclaje es al menú del editor, aportando un **grupo propio**:

| Constante | Valor | Qué es |
|---|---|---|
| `guidSHLMainMenu` | `d309f791-903f-11d0-9efc-00a0c911004f` | Menú principal del shell |
| `IDM_VS_CTXT_CODEWIN` | `0x040D` (1037) | El **menú** contextual del editor de código |

### `IDM_` es un menú; `IDG_` es una sección adentro de ese menú

Esta distinción es la que decide dónde termina apareciendo el ítem, y cuesta un ciclo entero
descubrirla por ensayo:

- Colgarse de un **grupo ajeno** (`IDG_…`) mete el ítem *dentro* de esa sección. Anclado a
  `IDG_VS_CODEWIN_TEXTEDIT` (`0x01C0`) aparecía **entre Cortar y Copiar**; anclado al 518 del
  ejemplo oficial —que es `IDG_VS_CTXT_PROJECT_BUILD`— aparecía **entre Compilar y Recompilar**.
- Para tener **sección propia**, con línea separadora arriba y abajo (como *Acciones rápidas y
  refactorizaciones*), hay que aportar un `CommandGroupConfiguration` propio y colgarlo del
  **menú**:

```csharp
[VisualStudioContribution]
public static CommandGroupConfiguration GrupoDeDevTools =>
    new(GroupPlacement.VsctParent(MenuPrincipalDelShell, id: MENU_DEL_EDITOR, priority: 0x0600))
    {
        Children = [GroupChild.Menu(MenuCommandsMaker)],
    };
```

El menú entonces **no declara `Placements`**: lo ubica el grupo.

Los valores de `vsshlids.h` que da el ejemplo oficial de Microsoft, comentados como "context menu",
son en realidad grupos:

| Valor | Constante real |
|---|---|
| 518 | `IDG_VS_CTXT_PROJECT_BUILD` |
| 521 | `IDG_VS_CTXT_ITEM_OPEN` |
| 537 | `IDG_VS_CTXT_SOLUTION_BUILD` |

### El menú sale también en los archivos de código, y no se puede evitar

La consola no muestra un menú *parecido* al del editor: muestra **el mismo**. Al clic derecho dentro
de la consola aparecen *Run Test(s)*, *Acciones rápidas y refactorizaciones*, *Punto de
interrupción*, *Run To Cursor* y *Cortar / Copiar / Pegar* — exactamente lo mismo que sobre un
archivo `.cs`. Por eso cualquier cosa anclada ahí sale en los dos lados: es un solo menú.

### NO reintentar separarlos con `VisibleWhen` — ya se probó y no funciona

El intento obvio es restringir los comandos al content type de la consola:

```csharp
// NO FUNCIONA
VisibleWhen = ActivationConstraint.ClientContext(
    ClientContextKey.Shell.ActiveEditorContentType,
    "^PackageConsole$"),
```

`PackageConsole` es efectivamente lo que declara `PowerConsoleWindow.ContentType` en NuGet.Client,
pero **`ActiveEditorContentType` mira el documento activo del editor, no la ventana que tiene el
foco**. Con el cursor dentro de la consola, el documento activo sigue siendo el archivo abierto, así
que el valor es el de ese archivo (`CSharp`) y la condición nunca da verdadera.

El resultado es el peor posible para diagnosticar: los comandos **no aparecen en ningún lado** —ni
en la consola ni en el editor—, el manifiesto se ve correcto, y no hay error ni advertencia en
ninguna etapa. Se gasta un ciclo entero de compilar, cerrar Visual Studio, instalar y abrir para
descubrir que no se ve nada.

Nota aparte: aunque funcionara, la restricción tendría que ir en los comandos igual, porque
**`MenuConfiguration` no expone `VisibleWhen`** (ni `Icon`).

## 6. El texto de los ítems se arma dos veces

Visual Studio lee `CommandConfiguration` **del manifiesto, antes de cargar la extensión**: ahí el
dominio todavía no se puede consultar. Entonces lo que queda compilado es el nombre pelado
(`Add-Migration`) y el dominio se agrega en `InitializeAsync`, ya en ejecución.

Como el menú se arma una sola vez al cargar, hace falta rehacerlo cuando el dominio cambia: para eso
está el evento `MenuDelExplorador.Cambio`, que `ComandoDelCommandsMaker` dispara después de que el
proceso de DevTools termina. **Por eso se espera al proceso** (`WaitForExitAsync`) en vez de
lanzarlo y olvidarse.

### Trampa: los textos van literales, no por `string-resources.json`

El SDK avisa con `CEE0027` que los textos deberían declararse en `string-resources.json` y
referenciarse como `%clave%`. **Acá van literales a propósito** (`<NoWarn>CEE0027</NoWarn>`): la
herramienta es interna y está toda en castellano, igual que el resto de la solución.

Hay además una razón de seguridad. Una primera versión usó `%clave%` y el archivo
**nunca quedó empaquetado** en el `.vsix` — las targets del SDK no lo recogen solas. El resultado:
Visual Studio dibujó el menú mostrando las claves crudas,

```
%AlgoritmoDevTools.MenuDevTools.DisplayName%
%AlgoritmoDevTools.ElegirDominio.DisplayName%
```

y **no hubo ningún error ni advertencia** — ni en compilación, ni al empaquetar, ni al instalar. El
manifiesto se ve perfecto, porque guarda la clave sin resolver. Si alguna vez se vuelve a
`%clave%`, verificar que `.vsextension/string-resources.json` exista **dentro del `.vsix`**, no en
el proyecto.

## 7. El menú va sin icono, y no se puede poner uno en *Commands Maker*

**`MenuConfiguration` no expone `Icon`.** Sus únicas propiedades son `Children`, `DisplayName`,
`Id`, `Placements`, `TooltipText` y `Type` — verificado en la referencia oficial de la API, no
deducido. El nivel *Commands Maker* no puede llevar icono y punto.

Lo que sí se puede es ponerlo en **cada ítem** (`CommandConfiguration.Icon`), pero eso repite el
mismo icono cuatro veces en el submenú y queda peor que sin ninguno, así que se sacó.

Si algún día el SDK permite icono en menús, los PNG se regeneran del `icon.ico` del Commands Maker
con el patrón que espera el moniker (`CommandsMaker.<w>.<h>.png` en `Images\`):

```powershell
Add-Type -AssemblyName System.Drawing
foreach ($size in 16,20,24,32) {
  $icon = New-Object System.Drawing.Icon("<ruta>\icon.ico", $size, $size)
  $icon.ToBitmap().Save("Images\CommandsMaker.$size.$size.png", [System.Drawing.Imaging.ImageFormat]::Png)
}
```

## 8. El pegado automático en la consola: PROBADO Y DESCARTADO

La idea era que los tres comandos, además de dejar el comando en el portapapeles, lo escribieran
directo en la consola para ahorrar el Ctrl+V. **Se implementó en la 1.4.0 y no funciona: no pega
nada.** El código se sacó; queda acá para no reintentarlo a ciegas.

```csharp
// NO FUNCIONA: la vista que devuelve no es la consola
using var vista = await context.GetActiveTextViewAsync(cancellationToken);
if (vista is null || !EsLaConsola(vista)) return;

await this.Extensibility.Editor().EditAsync(
    batch => vista.Document.AsEditable(batch).Replace(vista.Selection.Extent, comando),
    cancellationToken);
```

**Por qué no anda.** `GetActiveTextViewAsync` devuelve la vista activa del **editor**, y con el foco
puesto en la consola esa vista sigue siendo el archivo abierto detrás. Es exactamente el mismo
muro que el filtro por content type de la sección 5, donde `ActiveEditorContentType` daba `CSharp`
estando el cursor en la consola. **Dos evidencias independientes de lo mismo: el modelo
fuera-de-proceso no ve la Consola del Administrador de paquetes como un editor.**

### La guarda que evitó el desastre, y por qué hay que ponerla si se reintenta

El riesgo de esto no era que no pegara: era **pegar en el lugar equivocado**. Si la vista activa es
el archivo de código, escribir ahí mete el comando de migración adentro del fuente, sin aviso y sin
que nadie lo note hasta que no compile.

Por eso la prueba se hizo con una guarda que exigía las dos condiciones, y ante cualquier duda no
escribía:

1. Que `FilePath` **no** fuera un archivo existente en el disco — la consola no lo es, un `.cs` sí.
2. Que el texto del documento contuviera el prompt `PM>`.

Gracias a eso, el experimento fallido no tocó ningún archivo. **Cualquier reintento por esta vía
tiene que llevar la misma guarda**, o peor: no intentarse.

### Las alternativas, y por qué no se tomaron

- **Simular `Ctrl+V` con `SendInput`.** Pegaría en donde esté el foco, que es justo lo que no se
  puede determinar — el mismo riesgo de escribir en el código, pero ahora *sin* forma de poner una
  guarda. Descartada.
- **Una extensión VSIX clásica (in-proc, .NET Framework).** Es la única vía realista: corriendo
  dentro de `devenv` se llega a los servicios de NuGet y a la consola de verdad. El costo es rehacer
  la extensión en el modelo viejo y perder lo que se ganó acá (compilar sin la carga de trabajo,
  net8.0, `dotnet build`). No se justifica por ahorrar un Ctrl+V.

## 9. Cómo se compila

No hace falta la carga de trabajo *Desarrollo de extensiones de Visual Studio*: **todo el SDK llega
por NuGet**. Verificado en un equipo que sólo tiene Azure, CoreEditor, Data, ManagedDesktop y NetWeb.

```powershell
dotnet build AlgoritmoDevTools.Integrations.VisualStudio -c Release
# -> bin\Release\net8.0-windows\AlgoritmoDevTools.Integrations.VisualStudio.vsix
```

**Si el restore falla con `NU1301`**, es el feed privado `packages.algoritmo.com.ar`, que sólo
resuelve desde la red de la oficina. Este proyecto no usa paquetes de ahí:

```powershell
dotnet restore AlgoritmoDevTools.Integrations.VisualStudio -s https://api.nuget.org/v3/index.json
```

**Trampa: los dos paquetes del SDK van con `PrivateAssets="all"`.** Sin eso, sus targets fluyen al
proyecto de pruebas y lo hacen fallar con `VSEXT0004: los proyectos de VisualStudio.Extensibility
deben contribuir con una clase que extienda Extension`, un error que no tiene nada que ver con lo
que el proyecto de pruebas hace.

## 10. Instalar: hay que cerrar VS y NO puede ser silenciosa

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe" "<ruta>\AlgoritmoDevTools.Integrations.VisualStudio.vsix"
```

**Sin `/quiet`, a propósito.** Visual Studio 2026 instala las extensiones a través del VS Installer
(`setup.exe`), que **necesita elevación**. Con `/quiet` no puede mostrar el UAC y muere con
`ExitCode 1` y este error en `%TEMP%\dd_VSIXInstaller_*.log`:

```
RequiresInstallerException: Requested operation must be completed using Visual Studio Installer.
Multiple targets are currently not supported for this operation; retry with the instance you wish
to target on the command line.
```

Dos trampas en ese mensaje:

1. **«Multiple targets» no son instancias de Visual Studio**, aunque el texto diga «retry with the
   instance». Son las **dos arquitecturas** que el SDK declara en el manifiesto (`amd64` y `arm64`).
   Pasar `/instanceIds:<id>` **no lo arregla** — probado, falla idéntico. Tampoco hay propiedad
   MSBuild para dejar una sola.
2. **El error no nombra ni el UAC ni la elevación.**

**Dónde queda instalada**, que no es la carpeta clásica de extensiones del usuario:

```
C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSExtensions\<carpeta-aleatoria>\
```

y registrada en `%PROGRAMDATA%\Microsoft\VisualStudio\Packages\` y en
`...\IDE\VSExtensions\HotloadRegistration.txt`. Para verificar que entró, buscar el `.dll` **por
nombre**: el de la carpeta es aleatorio y cambia en cada instalación.

## 11. Compatibilidad con versiones de Visual Studio

El manifiesto declara `Version="[17.14,)"`: mínimo 17.14 y **sin tope**. Sale de la versión de los
paquetes del SDK, no de una elección propia — **subir los paquetes sube el mínimo exigido**.

Que no haya tope importa: hay reportes de que un manifiesto con `[17.0, 18.0)` falla con
`NoApplicableSKUsException` en VS 2026, pese a que la documentación dice que el tope se ignora.

## 12. Piezas

| Pieza | Qué resuelve |
|---|---|
| `ExtensionEntrypoint` | Metadatos de la extensión |
| `MenusDeDevTools` | El árbol del menú y contra qué grupo de VS se ancla |
| `MenuDelExplorador` | Lee ejecutable y dominio del registro; avisa cuando el dominio cambió |
| `ComandoDelCommandsMaker` | Lanza DevTools, espera, reporta errores, y define `SoloEnLaConsolaDePaquetes` |
| `ComandoDeMigracion` | Lo común a los tres: el texto con el dominio entre paréntesis |
| `VerbosDelShell` | Los cuatro verbos, duplicados del Commands Maker |

## 13. Qué tendrían que cubrir las pruebas

- `MenuDelExplorador.Interpretar` con la línea real del registro, con rutas con espacios, con un
  dominio con espacios, y con basura — tiene que devolver null y nunca tirar excepción.
- Que los cuatro verbos de `VerbosDelShell` sigan coincidiendo con `MenuContextualComandos`.

## 14. Deuda conocida

- **Los verbos están duplicados** (`VerbosDelShell` vs `MenuContextualComandos`) y nada los ata. Si
  uno cambia y el otro no, el síntoma es que el ítem *no hace nada*: el Shell cae en el `switch` sin
  coincidir y termina abriendo la ventana principal. Lo mismo vale para la ruta del grupo en el
  registro (`20Comandos`), espejo de `CLAVE_DEL_GRUPO`.
- **El anclaje depende de que la consola siga sobre el editor.** Si NuGet le diera menú propio, el
  menú desaparece de la consola sin aviso.
- **El menú también sale en el clic derecho de los archivos de código** (sección 5). Es el precio de
  que la consola no tenga menú propio, no un descuido.
- **El menú no tiene icono** porque el SDK no lo permite en ese nivel (sección 7).
- **El comando hay que pegarlo con Ctrl+V.** Que se escriba solo en la consola se intentó y no es
  posible con este SDK (sección 8). La única vía realista sería una extensión VSIX clásica
  in-proc, que no se justifica por ahorrar un atajo.

## 15. Qué está verificado y qué no

**Verificado en este equipo** (Visual Studio 18.10.3 Community):

- Compila en Release sin advertencias y genera el `.vsix` sin la carga de trabajo de extensiones.
- El manifiesto generado ancla el menú al grupo `448` (`0x01C0`), el menú quedó en un solo nivel y
  **no hay ninguna clave `%...%`** sin resolver.
- **La consola dibuja el menú contextual completo del editor de código**, confirmado en pantalla:
  Run Test(s), refactorizaciones, Punto de interrupción y Cortar / Copiar / Pegar. O sea que el
  grupo `IDG_VS_CODEWIN_TEXTEDIT` está presente ahí.
- **Que el filtro por `ActiveEditorContentType` no sirve** para distinguir la consola del editor
  (sección 5): con él puesto, los comandos no aparecían en ningún lado.
- Las 11 pruebas de `Interpretar` pasan, incluida la línea real leída del registro.
- El registro contiene el grupo del Commands Maker con el formato que `Interpretar` espera.
- **El menú aparece y funciona en la consola**, confirmado en pantalla con la versión 1.2.0: se vio
  *Commands Maker* con los cuatro ítems y el dominio activo (`Cereales`) en los tres comandos.

- **La sección propia se dibuja con sus líneas separadoras**, confirmado en pantalla con la 1.3.0.
  Quedaba al fondo del menú, debajo de *Anotación*, porque la prioridad era `0x0600`.

- **La altura de la sección se controla con la prioridad del grupo**, y como Visual Studio no
  publica las de sus propios grupos, se ajusta por ensayo. Lo medido en pantalla:

  | Prioridad | Dónde cae |
  |---|---|
  | `0x0600` | Último de todo, debajo de *Anotación* |
  | `0x0200` | Cuarto bloque, entre *Ejecutar pruebas* y *Punto de interrupción* |
  | `0x0150` | **El actual**: pegado a *Acciones rápidas*, que es donde se lo quiere |
- **El pegado automático NO funciona** (sección 8), probado con la 1.4.0: `GetActiveTextViewAsync`
  no alcanza la consola. La guarda evitó que escribiera en el archivo de código.

**No verificado todavía:** nada pendiente. Las tres incógnitas que tuvo este desarrollo —dónde se
ancla el menú, si se podía limitar a la consola y si se podía pegar solo— están resueltas y
documentadas, las dos últimas en negativo.

## 16. Cuándo actualizar este documento

Cuando cambien los verbos del Commands Maker, la ruta o la forma del grupo en el registro, el
anclaje del menú, el content type de la consola, o cuando la extensión pase a hacer algo más que
lanzar el ejecutable.
