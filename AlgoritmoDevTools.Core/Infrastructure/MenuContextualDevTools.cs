using Microsoft.Win32;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Raíz compartida del menú contextual del explorador. Cada tool cuelga su propio grupo:
///
/// <code>
/// DevTools ▸
///     Secrets Manager ▸ ...
///     Commands Maker ▸ ...
/// </code>
///
/// Está en Core porque ninguna tool puede ser dueña de la raíz de las otras: antes la creaba el
/// Secrets Manager, y eso dejaba al Commands Maker dependiendo de una tool con la que no tiene
/// nada que ver.
///
/// Va en <c>Directory\Background\shell</c>, que es el clic derecho sobre el fondo de una carpeta y
/// del escritorio. Es el lugar para acciones que NO operan sobre el archivo en el que hacés clic
/// (para esas está <c>SystemFileAssociations</c>, que es lo que usa el Convertidor a Markdown).
///
/// Todo se escribe en HKEY_CURRENT_USER: aplica sólo a este usuario y no pide permisos de
/// administrador.
///
/// Cada nivel de submenú se arma con <c>subcommands</c> (valor REG_SZ vacío) más una subclave
/// <c>shell</c> con un ítem por entrada; los ítems se ordenan alfabéticamente por nombre de clave,
/// así que conviene prefijarlos con números.
///
/// Son menús ESTÁTICOS: el explorador no ejecuta código nuestro para dibujarlos, así que todo lo
/// que muestran tiene que estar decidido al momento de escribirlos. Cuando cambia el dato que
/// alimenta un grupo, ese grupo hay que reescribirlo.
/// </summary>
public static class MenuContextualDevTools
{
    private const string TEXTO_RAIZ = "DevTools";
    private const string BASE_FONDO_DE_CARPETA = @"Software\Classes\Directory\Background\shell";

    public const string RutaRaiz = BASE_FONDO_DE_CARPETA + @"\AlgoritmoDevTools";

    /// <summary>Dibuja una línea separadora arriba del ítem (ECF_SEPARATORBEFORE).</summary>
    private const int SEPARADOR_ANTES = 0x20;

    /// <summary>
    /// Tope de Windows para un menú estático: 16 entradas en TODA la entrada de menú contextual,
    /// submenús incluidos y contando lo que hay adentro de ellos. No es por nivel.
    ///
    /// Pasarse no da ningún error: el explorador dibuja las primeras 16 y descarta el resto en
    /// silencio, así que el síntoma es "me faltan opciones" sin nada que lo explique. Por eso las
    /// tools preguntan cuánto queda antes de escribir lo opcional.
    ///
    /// El presupuesto es COMPARTIDO entre todas las tools que cuelgan de la raíz: si el Secrets
    /// Manager suma conexiones, al Commands Maker le quedan menos.
    ///
    /// https://learn.microsoft.com/en-us/windows/win32/shell/how-to--create-cascading-menus-with-the-subcommands-registry-entry
    /// </summary>
    public const int LimiteDeItems = 16;

    public static string RutaDelGrupo(string claveDelGrupo) => $@"{RutaRaiz}\shell\{claveDelGrupo}";

    /// <summary>
    /// Cuenta cada entrada que el explorador va a dibujar: los grupos, sus ítems, los submenús de
    /// adentro y los ítems de esos submenús.
    /// </summary>
    public static int ContarItems() => ContarDesde(RutaRaiz);

    /// <summary>Cuántas entradas más entran antes de que el explorador empiece a descartar.</summary>
    public static int ItemsDisponibles() => Math.Max(0, LimiteDeItems - ContarItems());

    private static int ContarDesde(string ruta)
    {
        using var shell = Registry.CurrentUser.OpenSubKey(ruta + @"\shell");
        if (shell is null) return 0;

        var hijos = shell.GetSubKeyNames();
        return hijos.Length + hijos.Sum(h => ContarDesde(ruta + @"\shell\" + h));
    }

    public static bool GrupoInstalado(string claveDelGrupo)
    {
        using var clave = Registry.CurrentUser.OpenSubKey(RutaDelGrupo(claveDelGrupo));
        return clave is not null;
    }

    /// <summary>
    /// Crea la raíz si no está, con el icono del ejecutable. Devuelve null si salió bien, o el
    /// mensaje de error.
    /// </summary>
    public static string? AsegurarRaiz()
    {
        var exe = RutaDelEjecutable();
        if (exe is null) return "no se pudo determinar la ruta del ejecutable.";

        return CrearSubmenu(RutaRaiz, TEXTO_RAIZ, exe + ",0");
    }

    /// <summary>
    /// Crea (o deja listo) un nivel de submenú. El valor por defecto va vacío porque, con MUIVerb
    /// presente, es MUIVerb el que da el texto; <c>subcommands</c> vacío es lo que convierte el
    /// verbo en submenú en vez de en una acción. Sin <paramref name="icono"/> el nivel queda sin
    /// icono, que es preferible a repetir el del nivel de arriba.
    /// </summary>
    public static string? CrearSubmenu(string ruta, string texto, string? icono = null, bool separadorAntes = false)
    {
        using var clave = Registry.CurrentUser.CreateSubKey(ruta);
        if (clave is null) return $"no se pudo crear la clave del registro '{ruta}'.";

        clave.SetValue(null, string.Empty);
        clave.SetValue("MUIVerb", texto);
        clave.SetValue("subcommands", string.Empty);

        if (icono is not null) clave.SetValue("Icon", icono);
        if (separadorAntes) clave.SetValue("CommandFlags", SEPARADOR_ANTES, RegistryValueKind.DWord);
        return null;
    }

    /// <summary>
    /// Escribe un ítem que ejecuta el propio exe con <paramref name="argumentos"/>.
    /// <paramref name="rutaDelSubmenu"/> es el nivel que lo contiene; la clave es lo que define el
    /// orden, así que conviene numerarla.
    /// </summary>
    public static void EscribirItem(string rutaDelSubmenu, string clave, string texto, string argumentos, bool separadorAntes = false)
    {
        var exe = RutaDelEjecutable();
        if (exe is null) return;

        using var item = Registry.CurrentUser.CreateSubKey($@"{rutaDelSubmenu}\shell\{clave}");
        if (item is null) return;

        item.SetValue("MUIVerb", texto);
        if (separadorAntes) item.SetValue("CommandFlags", SEPARADOR_ANTES, RegistryValueKind.DWord);

        using var comando = item.CreateSubKey("command");
        comando?.SetValue(null, $"\"{exe}\" {argumentos}");
    }

    /// <summary>Borra el grupo entero. No toca la raíz: para eso está <see cref="BorrarRaizSiQuedoVacia"/>.</summary>
    public static void BorrarGrupo(string claveDelGrupo)
        => Registry.CurrentUser.DeleteSubKeyTree(RutaDelGrupo(claveDelGrupo), throwOnMissingSubKey: false);

    /// <summary>
    /// Saca la raíz si ninguna otra tool dejó su grupo colgando: un DevTools vacío en el menú no le
    /// sirve a nadie.
    /// </summary>
    public static void BorrarRaizSiQuedoVacia()
    {
        using (var shell = Registry.CurrentUser.OpenSubKey(RutaRaiz + @"\shell"))
        {
            if (shell is not null && shell.GetSubKeyNames().Length > 0) return;
        }

        Registry.CurrentUser.DeleteSubKeyTree(RutaRaiz, throwOnMissingSubKey: false);
    }

    private static string? RutaDelEjecutable() => ShellIntegration.RutaDelEjecutable();
}
