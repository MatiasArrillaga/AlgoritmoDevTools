using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using Microsoft.Win32;

namespace AlgoritmoDevTools.Tools.SecretsManager.Services;

/// <summary>
/// Grupo del Secrets Manager dentro del menú <see cref="MenuContextualDevTools"/>:
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
/// Estas acciones no operan sobre el archivo en el que hacés clic: el proyecto destino siempre es
/// el mismo (<c>~\source\repos\AlgoritmoCore</c>, ver <see cref="SecretService"/>), así que el
/// lugar del clic es irrelevante y lo único que se busca es tener la acción a mano.
///
/// El menú es ESTÁTICO, así que las bases de datos no se pueden consultar en vivo y cada ítem de
/// conexión lleva clavada su <see cref="SavedConnection.BaseEfectiva"/>. Cuando se agrega, borra o
/// reapunta una conexión hay que reescribirlo: de eso se encarga <see cref="Sincronizar"/>.
/// </summary>
public static class MenuContextualSecretos
{
    public const string VerboRestaurar = "--secretos-restaurar";
    public const string VerboAplicar = "--secretos-aplicar";
    public const string VerboElegir = "--secretos-elegir";

    private const string TEXTO_DEL_GRUPO = "Secrets Manager";

    /// <summary>Clave del grupo dentro de la raíz. El prefijo numérico define el orden en el menú.</summary>
    private const string CLAVE_DEL_GRUPO = "10Secrets";

    private const string NOMBRE_DEL_ICONO = "SecretsManager.ico";

    /// <summary>
    /// Primera versión del menú, que colgaba los verbos de una raíz propia en vez de DevTools.
    /// Se borra al instalar y al desinstalar para que no queden dos menús en el explorador.
    /// </summary>
    private const string RUTA_LEGACY = @"Software\Classes\Directory\Background\shell\AlgoritmoSoftCerealCore";

    private static string RutaDelGrupo => MenuContextualDevTools.RutaDelGrupo(CLAVE_DEL_GRUPO);

    public static bool EstaInstalado() => MenuContextualDevTools.GrupoInstalado(CLAVE_DEL_GRUPO);

    /// <summary>
    /// Reescribe el grupo desde cero con las conexiones que haya ahora. Devuelve null si salió
    /// bien, o el mensaje de error.
    /// </summary>
    public static string? TryInstalar(IReadOnlyList<SavedConnection> conexiones)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(RUTA_LEGACY, throwOnMissingSubKey: false);

            // El grupo se borra entero para que las conexiones que ya no existen no queden
            // colgadas. Se borra el grupo y no la raíz: ahí viven las demás tools.
            MenuContextualDevTools.BorrarGrupo(CLAVE_DEL_GRUPO);

            var errorRaiz = MenuContextualDevTools.AsegurarRaiz();
            if (errorRaiz is not null) return errorRaiz;

            // El grupo lleva el icono de su propia tool: con el mismo icono que la raíz no se
            // distinguiría un nivel del otro.
            var errorGrupo = MenuContextualDevTools.CrearSubmenu(RutaDelGrupo, TEXTO_DEL_GRUPO, IconoDeLaTool());
            if (errorGrupo is not null) return errorGrupo;

            EscribirItem("01Restaurar", "Restaurar secretos", VerboRestaurar);

            // Las conexiones sin base conocida no se listan: no hay nada contra lo que aplicar.
            // Se llega a ellas por "Elegir base...", que sí consulta el servidor.
            var listables = conexiones.Where(c => !string.IsNullOrWhiteSpace(c.BaseEfectiva)).ToList();
            for (int i = 0; i < listables.Count; i++)
            {
                var conexion = listables[i];
                EscribirItem(
                    $"10Conexion{i:D2}",
                    TextoDeConexion(conexion),
                    $"{VerboAplicar} {conexion.Id}",
                    separadorAntes: i == 0);
            }

            EscribirItem("90Elegir", "Elegir base...", VerboElegir, separadorAntes: true);

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
            MenuContextualDevTools.BorrarGrupo(CLAVE_DEL_GRUPO);
            MenuContextualDevTools.BorrarRaizSiQuedoVacia();

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
    /// Vuelve a escribir el grupo sólo si ya estaba instalado. Se llama cada vez que cambia la
    /// lista de conexiones o la base de alguna: si no, el menú queda mostrando una base vieja o una
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

    private static void EscribirItem(string clave, string texto, string argumentos, bool separadorAntes = false)
        => MenuContextualDevTools.EscribirItem(RutaDelGrupo, clave, texto, argumentos, separadorAntes);

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
            // No es motivo para fallar el desinstalado: el menú ya se fue, que es lo que importa.
        }
    }
}
