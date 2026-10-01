using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;

namespace AlgoritmoDevTools.Tools.CommandsMaker.Services;

/// <summary>
/// Grupo del Commands Maker dentro del menú <see cref="MenuContextualDevTools"/>:
///
/// <code>
/// DevTools ▸
///     Commands Maker ▸
///         Elegir dominio...                  ← cambia el dominio activo
///         ────────────────
///         Add-Migration (Cereales)...        ← los tres, sobre el dominio activo
///         Remove-Migration (Cereales)
///         Update-Database (Cereales)
/// </code>
///
/// <para>
/// El flujo es de dos pasos y con estado: se elige el dominio una vez, y a partir de ahí los tres
/// comandos están a un clic. Elegir no ejecuta nada: sólo cambia cuál es el dominio activo y
/// reescribe el menú.
/// </para>
///
/// <para>
/// <b>El menú tiene 16 ítems de presupuesto para todo el árbol</b> (ver
/// <see cref="MenuContextualDevTools.LimiteDeItems"/>), compartido con las demás tools. Los 13
/// dominios por 3 comandos son 39: no entran ni cerca, y pasarse no da error — el explorador
/// dibuja los primeros 16 y descarta el resto en silencio. Tener un dominio activo en vez de
/// listarlos a todos deja el grupo en 4 ítems fijos, sin importar cuántos dominios haya.
/// </para>
///
/// El menú es ESTÁTICO: el explorador no ejecuta código nuestro para dibujarlo, así que el dominio
/// activo queda clavado al momento de escribirlo. Por eso <see cref="Sincronizar"/> se llama cada
/// vez que cambia la lista de dominios o cuál está activo.
/// </summary>
public static class MenuContextualComandos
{
    public const string VerboAdd = "--comando-add";
    public const string VerboRemove = "--comando-remove";
    public const string VerboUpdate = "--comando-update";
    public const string VerboElegir = "--comando-elegir";

    private const string TEXTO_DEL_GRUPO = "Commands Maker";

    /// <summary>Clave del grupo dentro de la raíz. El prefijo numérico define el orden en el menú.</summary>
    private const string CLAVE_DEL_GRUPO = "20Comandos";

    private const string NOMBRE_DEL_ICONO = "CommandsMaker.ico";

    private static string RutaDelGrupo => MenuContextualDevTools.RutaDelGrupo(CLAVE_DEL_GRUPO);

    public static bool EstaInstalado() => MenuContextualDevTools.GrupoInstalado(CLAVE_DEL_GRUPO);

    /// <summary>
    /// Reescribe el grupo desde cero. Devuelve null si salió bien, o el mensaje de error.
    /// </summary>
    public static string? TryInstalar(IReadOnlyList<string> dominios, IReadOnlyList<string> recientes)
    {
        if (dominios.Count == 0)
            return "no hay dominios cargados: agregá al menos uno en la pantalla del Commands Maker.";

        try
        {
            MenuContextualDevTools.BorrarGrupo(CLAVE_DEL_GRUPO);

            var errorRaiz = MenuContextualDevTools.AsegurarRaiz();
            if (errorRaiz is not null) return errorRaiz;

            var errorGrupo = MenuContextualDevTools.CrearSubmenu(RutaDelGrupo, TEXTO_DEL_GRUPO, IconoDeLaTool());
            if (errorGrupo is not null) return errorGrupo;

            // Si todavía no se usó ninguno, el primero alfabético sirve igual: lo que importa es
            // que el acceso directo exista desde el arranque y no después del primer uso.
            var activo = recientes.FirstOrDefault(d => dominios.Contains(d)) ?? dominios[0];

            // Elegir va primero porque es el paso que habilita a los otros tres.
            MenuContextualDevTools.EscribirItem(RutaDelGrupo, "01Elegir", "Elegir dominio...", VerboElegir);
            EscribirAcciones(activo);

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
    /// Vuelve a escribir el grupo sólo si ya estaba instalado. Hace falta cada vez que cambia la
    /// lista de dominios o cuál se usó último: el menú es estático, así que un dominio borrado
    /// seguiría apareciendo y el acceso directo de arriba apuntaría al anterior.
    /// </summary>
    public static void Sincronizar(IReadOnlyList<string> dominios, IReadOnlyList<string> recientes)
    {
        if (EstaInstalado()) TryInstalar(dominios, recientes);
    }

    /// <summary>Los tres comandos del dominio activo, debajo del separador.</summary>
    private static void EscribirAcciones(string dominio)
    {
        // Los puntos suspensivos son la convención de Windows para "esto abre una ventana":
        // Add-Migration pide el nombre de la migración.
        MenuContextualDevTools.EscribirItem(RutaDelGrupo, "10Add", $"Add-Migration ({dominio})...", $"{VerboAdd} {dominio}", separadorAntes: true);
        MenuContextualDevTools.EscribirItem(RutaDelGrupo, "11Remove", $"Remove-Migration ({dominio})", $"{VerboRemove} {dominio}");
        MenuContextualDevTools.EscribirItem(RutaDelGrupo, "12Update", $"Update-Database ({dominio})", $"{VerboUpdate} {dominio}");
    }

    private static string? IconoDeLaTool()
        => IconLoader.ExtraerIcono(typeof(MenuContextualComandos).Assembly, "icon.ico", NOMBRE_DEL_ICONO);

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
