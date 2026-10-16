using AlgoritmoDevTools.Core.Infrastructure;
using Microsoft.Win32;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>
/// Deja «Buscar en esta carpeta» en el clic derecho sobre el fondo de una carpeta.
///
/// Es un <b>ítem suelto</b> y no un submenú con un solo hijo adentro: el presupuesto del explorador
/// es de dieciséis entradas para todo el árbol y está compartido con las demás herramientas, así que
/// un grupo con un único ítem costaría el doble por la misma función. Pasarse no da ningún error
/// —el explorador dibuja las primeras y descarta el resto en silencio—, que es justamente por qué
/// conviene gastar poco.
///
/// Va en <c>HKEY_CURRENT_USER</c>, así que no pide permisos de administrador.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public static class MenuContextualBusqueda
{
    /// <summary>
    /// Con qué se relanza el ejecutable. Le sigue la carpeta sobre la que se hizo clic derecho.
    /// </summary>
    public const string VerboBuscarAca = "--buscar-aca";

    /// <summary>
    /// La clave define el orden, que es alfabético. El número adelante es lo que deja decidirlo sin
    /// depender de cómo se llame la herramienta.
    /// </summary>
    private const string ClaveDelItem = "40BuscaLoco";

    private const string TextoDelItem = "Buscar en esta carpeta";

    /// <summary>Cuántas entradas del presupuesto del explorador gasta esto.</summary>
    public const int ItemsQueOcupa = 1;

    public static bool EstaInstalado()
    {
        using var clave = Registry.CurrentUser.OpenSubKey(MenuContextualDevTools.RutaDelGrupo(ClaveDelItem));
        return clave is not null;
    }

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public static string? TryInstalar()
    {
        try
        {
            var error = MenuContextualDevTools.AsegurarRaiz();
            if (error is not null) return error;

            // %V es la carpeta sobre la que se hizo clic derecho. Va entre comillas porque las rutas
            // del equipo tienen espacios casi siempre.
            MenuContextualDevTools.EscribirItem(
                MenuContextualDevTools.RutaRaiz,
                ClaveDelItem,
                TextoDelItem,
                $"{VerboBuscarAca} \"%V\"");

            ShellIntegration.AvisarAlExplorador();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public static string? TryDesinstalar()
    {
        try
        {
            MenuContextualDevTools.BorrarGrupo(ClaveDelItem);

            // La raíz se borra sólo si no quedó nadie más colgando: es de todas las herramientas.
            MenuContextualDevTools.BorrarRaizSiQuedoVacia();

            ShellIntegration.AvisarAlExplorador();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
