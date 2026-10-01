using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using Microsoft.Win32;

namespace AlgoritmoDevTools.Tools.SecretsManager.Services;

/// <summary>
/// Agrega al clic derecho sobre el fondo de una carpeta (y el escritorio) un menú anidado:
///
/// <code>
/// DevTools ▸
///     Secrets Manager ▸
///         Restaurar secretos
///         ────────────────
///         localhost,1433 - Algoritmo93 (softcereal)
///         srv-dev - AlgoritmoQA (sa)
///         ────────────────
///         Elegir base...
/// </code>
///
/// La raíz es DevTools y no el Secrets Manager para que las demás tools puedan colgar su propio
/// grupo del mismo menú en vez de agregar cada una un verbo suelto al nivel de arriba.
///
/// Va en <c>Directory\Background\shell</c> y no sobre una extensión como el Convertidor a Markdown:
/// estas acciones no operan sobre el archivo en el que hacés clic. El proyecto destino siempre es
/// el mismo (<c>~\source\repos\AlgoritmoCore</c>, ver <see cref="SecretService"/>), así que el
/// lugar del clic es irrelevante y lo único que se busca es tener la acción a mano.
///
/// Cada nivel de submenú se arma con <c>subcommands</c> (valor REG_SZ vacío) más una subclave
/// <c>shell</c> con un ítem por entrada; los ítems se ordenan alfabéticamente por nombre de clave,
/// de ahí los prefijos numéricos.
///
/// Es un menú ESTÁTICO: el explorador no ejecuta código nuestro para dibujarlo. Por eso las bases
/// de datos no se pueden consultar en vivo y cada ítem de conexión lleva clavada su
/// <see cref="SavedConnection.BaseEfectiva"/>. Cuando se agrega, borra o reapunta una conexión hay
/// que volver a escribir el menú: de eso se encarga <see cref="Sincronizar"/>.
/// </summary>
public static class MenuContextualSecretos
{
    public const string VerboRestaurar = "--secretos-restaurar";
    public const string VerboAplicar = "--secretos-aplicar";
    public const string VerboElegir = "--secretos-elegir";

    private const string TEXTO_RAIZ = "DevTools";
    private const string TEXTO_GRUPO_SECRETOS = "Secrets Manager";

    private const string BASE_FONDO_DE_CARPETA = @"Software\Classes\Directory\Background\shell";
    private const string RUTA_RAIZ = BASE_FONDO_DE_CARPETA + @"\AlgoritmoDevTools";

    /// <summary>Grupo del Secrets Manager dentro de la raíz. Otras tools colgarían de acá al lado.</summary>
    private const string RUTA_GRUPO_SECRETOS = RUTA_RAIZ + @"\shell\10Secrets";

    /// <summary>
    /// Primera versión del menú, que colgaba los verbos de una raíz propia en vez de DevTools.
    /// Se borra al instalar y al desinstalar para que no queden dos menús en el explorador.
    /// </summary>
    private const string RUTA_LEGACY = BASE_FONDO_DE_CARPETA + @"\AlgoritmoSoftCerealCore";

    // Dibuja una línea separadora arriba del ítem (ECF_SEPARATORBEFORE).
    private const int SEPARADOR_ANTES = 0x20;

    public static bool EstaInstalado()
    {
        using var clave = Registry.CurrentUser.OpenSubKey(RUTA_GRUPO_SECRETOS);
        return clave is not null;
    }

    /// <summary>
    /// Reescribe el menú desde cero con las conexiones que haya ahora. Devuelve null si salió bien,
    /// o el mensaje de error.
    /// </summary>
    public static string? TryInstalar(IReadOnlyList<SavedConnection> conexiones)
    {
        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return "no se pudo determinar la ruta del ejecutable.";

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(RUTA_LEGACY, throwOnMissingSubKey: false);

            // El grupo se borra entero para que las conexiones que ya no existen no queden
            // colgadas. Se borra el grupo y no la raíz: ahí podrían vivir las demás tools.
            Registry.CurrentUser.DeleteSubKeyTree(RUTA_GRUPO_SECRETOS, throwOnMissingSubKey: false);

            // La raíz lleva el icono del exe; el grupo, el de su propia tool. Si no se pudo
            // extraer el .ico queda sin icono, que es preferible a repetir el de DevTools: con el
            // mismo icono en los dos niveles no se distingue la raíz del grupo.
            var errorRaiz = CrearSubmenu(RUTA_RAIZ, TEXTO_RAIZ, exe + ",0");
            if (errorRaiz is not null) return errorRaiz;

            var errorGrupo = CrearSubmenu(RUTA_GRUPO_SECRETOS, TEXTO_GRUPO_SECRETOS, IconoDeLaTool());
            if (errorGrupo is not null) return errorGrupo;

            EscribirItem("01Restaurar", "Restaurar secretos", exe, VerboRestaurar);

            // Las conexiones sin base conocida no se listan: no hay nada contra lo que aplicar.
            // Se llega a ellas por "Elegir base...", que sí consulta el servidor.
            var listables = conexiones.Where(c => !string.IsNullOrWhiteSpace(c.BaseEfectiva)).ToList();
            for (int i = 0; i < listables.Count; i++)
            {
                var conexion = listables[i];
                EscribirItem(
                    $"10Conexion{i:D2}",
                    TextoDeConexion(conexion),
                    exe,
                    $"{VerboAplicar} {conexion.Id}",
                    separadorAntes: i == 0);
            }

            EscribirItem("90Elegir", "Elegir base...", exe, VerboElegir, separadorAntes: true);

            ShellIntegration.AvisarAlExplorador();
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return "el registro no permitió la escritura (revisar políticas del equipo).";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static string? TryDesinstalar()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(RUTA_LEGACY, throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree(RUTA_GRUPO_SECRETOS, throwOnMissingSubKey: false);

            // La raíz se borra sólo si ninguna otra tool colgó su grupo: un DevTools vacío en el
            // menú no le sirve a nadie.
            if (!TieneGrupos(RUTA_RAIZ))
                Registry.CurrentUser.DeleteSubKeyTree(RUTA_RAIZ, throwOnMissingSubKey: false);

            BorrarIconoDeLaTool();
            ShellIntegration.AvisarAlExplorador();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Vuelve a escribir el menú sólo si ya estaba instalado. Se llama cada vez que cambia la lista
    /// de conexiones o la base de alguna: si no, el menú queda mostrando una base vieja o una
    /// conexión borrada, que es peor que no tener el menú.
    /// </summary>
    public static void Sincronizar(IReadOnlyList<SavedConnection> conexiones)
    {
        if (EstaInstalado()) TryInstalar(conexiones);
    }

    /// <summary>Texto del ítem: servidor, base y autenticación, sin el usuario cuando es Windows Auth.</summary>
    public static string TextoDeConexion(SavedConnection conexion)
    {
        var auth = conexion.UseIntegratedSecurity ? "Windows Auth" : conexion.User;
        return $"{conexion.Server} - {conexion.BaseEfectiva} ({auth})";
    }

    /// <summary>
    /// Crea (o deja listo) un nivel de submenú. El valor por defecto va vacío porque, con MUIVerb
    /// presente, es MUIVerb el que da el texto; <c>subcommands</c> vacío es lo que convierte el
    /// verbo en submenú en vez de en una acción.
    /// </summary>
    private static string? CrearSubmenu(string ruta, string texto, string? icono)
    {
        using var clave = Registry.CurrentUser.CreateSubKey(ruta);
        if (clave is null) return $"no se pudo crear la clave del registro '{ruta}'.";

        clave.SetValue(null, string.Empty);
        clave.SetValue("MUIVerb", texto);
        clave.SetValue("subcommands", string.Empty);

        if (icono is not null) clave.SetValue("Icon", icono);
        return null;
    }

    private const string NOMBRE_DEL_ICONO = "SecretsManager.ico";

    /// <summary>Icono propio del Secrets Manager, volcado a disco para que el registro lo alcance.</summary>
    private static string? IconoDeLaTool()
        => IconLoader.ExtraerIcono(typeof(MenuContextualSecretos).Assembly, "icon.ico", NOMBRE_DEL_ICONO);

    /// <summary>
    /// Saca el .ico que habíamos dejado en disco: sin el menú no apunta nada a él. Si no se puede
    /// borrar (el explorador puede tenerlo abierto) se deja estar — son 8 KB y la próxima
    /// instalación lo pisa.
    /// </summary>
    private static void BorrarIconoDeLaTool()
    {
        try
        {
            var ruta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AlgoritmoDevTools", "icons", NOMBRE_DEL_ICONO);

            if (File.Exists(ruta)) File.Delete(ruta);
        }
        catch
        {
            // No es motivo para fallar el desinstalado: el menu ya se fue, que es lo que importa.
        }
    }

    /// <summary>True si la raíz todavía tiene algún grupo colgando.</summary>
    private static bool TieneGrupos(string rutaRaiz)
    {
        using var shell = Registry.CurrentUser.OpenSubKey(rutaRaiz + @"\shell");
        return shell is not null && shell.GetSubKeyNames().Length > 0;
    }

    private static void EscribirItem(string clave, string texto, string exe, string argumentos, bool separadorAntes = false)
    {
        using var item = Registry.CurrentUser.CreateSubKey($@"{RUTA_GRUPO_SECRETOS}\shell\{clave}");
        if (item is null) return;

        item.SetValue("MUIVerb", texto);
        if (separadorAntes) item.SetValue("CommandFlags", SEPARADOR_ANTES, RegistryValueKind.DWord);

        using var comando = item.CreateSubKey("command");
        comando?.SetValue(null, $"\"{exe}\" {argumentos}");
    }
}
