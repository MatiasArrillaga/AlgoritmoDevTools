using Microsoft.Win32;

namespace AlgoritmoDevTools.Integrations.VisualStudio;

/// <summary>Lo que hace falta para armar el menú: dónde está DevTools y sobre qué dominio trabaja.</summary>
/// <param name="Ejecutable">Ruta completa a DevTools.exe.</param>
/// <param name="Dominio">Dominio activo del Commands Maker, por ejemplo <c>Cereales</c>.</param>
internal sealed record EntradaDelMenu(string Ejecutable, string Dominio);

/// <summary>
/// Lee del registro el menú contextual que el Commands Maker ya escribió para el explorador, y de
/// ahí saca las dos cosas que esta extensión necesita: la ruta del ejecutable y el dominio activo.
///
/// <para>
/// <b>Por qué el registro y no la base.</b> El dominio activo vive en el SQLite del Commands Maker
/// (<c>%LOCALAPPDATA%\AlgoritmoDevTools\CommandsMaker\data.db</c>), pero elegir cuál está activo no
/// es leer un campo: es «el primer reciente que siga existiendo, o el primero alfabético»
/// (<c>MenuContextualComandos.TryInstalar</c>). Replicar esa regla acá sería una segunda
/// implementación que se desincroniza sin avisar. El menú del explorador ya es el resultado de
/// haberla aplicado, así que leerlo da la misma respuesta sin repetir la regla, y de paso evita
/// depender de Microsoft.Data.Sqlite y su DLL nativa dentro del .vsix.
/// </para>
///
/// <para>
/// <b>Consecuencia:</b> el menú de Visual Studio es un espejo del menú del explorador y no funciona
/// sin él. Si el grupo no está instalado, <see cref="Leer"/> devuelve null y los comandos avisan que
/// hay que instalarlo desde la pantalla del Commands Maker. Es intencional: los dos menús muestran
/// siempre el mismo dominio porque hay una sola fuente, no dos que hay que mantener de acuerdo.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Integrations.VisualStudio/README.referencia-tecnica-funcional-menu-visual-studio.md
/// </summary>
internal static class MenuDelExplorador
{
    /// <summary>
    /// Espejo de <c>MenuContextualDevTools.RutaRaiz</c> + <c>CLAVE_DEL_GRUPO</c> del Commands Maker.
    /// Si allá se renombra el grupo, acá deja de encontrarse y los comandos se apagan solos.
    /// </summary>
    private const string RUTA_DEL_GRUPO =
        @"Software\Classes\Directory\Background\shell\AlgoritmoDevTools\shell\20Comandos\shell";

    /// <summary>
    /// De qué ítem se lee. Cualquiera de los tres sirve —los tres llevan el mismo dominio y el mismo
    /// exe—; se toma Add-Migration porque es el que siempre está cuando el grupo existe.
    /// </summary>
    private const string ITEM_DE_REFERENCIA = "10Add";

    /// <summary>
    /// Se dispara cuando el dominio activo pudo haber cambiado, para que los ítems rehagan su texto.
    /// El menú de Visual Studio se arma una vez al cargar la extensión, así que sin esto seguiría
    /// mostrando el dominio viejo hasta reiniciar el IDE.
    /// </summary>
    public static event Action? Cambio;

    public static void NotificarCambio() => Cambio?.Invoke();

    /// <summary>Devuelve null si el grupo del Commands Maker no está instalado en el explorador.</summary>
    public static EntradaDelMenu? Leer()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey($@"{RUTA_DEL_GRUPO}\{ITEM_DE_REFERENCIA}\command");
            return clave?.GetValue(null) is string linea ? Interpretar(linea) : null;
        }
        catch (Exception)
        {
            // Un registro ilegible no es motivo para tirar abajo la carga de la extensión: los
            // comandos quedan apagados y avisan, que es el mismo camino que "no está instalado".
            return null;
        }
    }

    /// <summary>
    /// Parte la línea que escribió <c>MenuContextualDevTools.EscribirItem</c>, con la forma
    /// <c>"C:\ruta\DevTools.exe" --comando-add Cereales</c>.
    ///
    /// El dominio se toma como TODO lo que sigue al verbo y no como el último token: hoy no hay
    /// dominios con espacios, pero partir por espacios haría que uno nuevo con espacio fallara acá
    /// en silencio en vez de donde corresponde.
    /// </summary>
    internal static EntradaDelMenu? Interpretar(string linea)
    {
        if (linea.Length < 2 || linea[0] != '"') return null;

        var cierre = linea.IndexOf('"', 1);
        if (cierre < 0) return null;

        var ejecutable = linea[1..cierre];
        var argumentos = linea[(cierre + 1)..].Trim();

        var finDelVerbo = argumentos.IndexOf(' ');
        if (finDelVerbo < 0) return null;

        var dominio = argumentos[(finDelVerbo + 1)..].Trim();
        return dominio.Length == 0 || ejecutable.Length == 0 ? null : new EntradaDelMenu(ejecutable, dominio);
    }
}
