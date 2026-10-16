using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>
/// Decide qué nombres de archivo entran en la búsqueda.
///
/// Por defecto el campo es un <b>glob</b>, que es como escribe todo el mundo: <c>*.cs</c>. Admite
/// varios separados por coma o por punto y coma —<c>*.cs, *.razor</c>—, porque Windows usa el punto
/// y coma en sus diálogos y la gente escribe la coma.
///
/// <b>Con el modo expresión regular encendido, la coma deja de separar.</b> Ahí es sintaxis
/// —<c>\w{2,3}</c>— y partir por ella rompería el patrón en silencio, que es la peor forma de
/// romperlo.
///
/// Un patrón sin comodines busca por <b>parte del nombre</b>: escribir <c>Factory</c> encuentra
/// <c>OperacionFactory.cs</c>. Con comodines, en cambio, tiene que calzar el nombre entero, que es
/// lo que uno espera de <c>*.cs</c>.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public sealed class FiltroDeNombre
{
    private static readonly char[] Separadores = [',', ';'];

    private readonly IReadOnlyList<Regex> _patrones;

    private FiltroDeNombre(IReadOnlyList<Regex> patrones) => _patrones = patrones;

    /// <summary>Si no hay patrón, entran todos: el campo vacío significa «no filtres por nombre».</summary>
    public bool DejaPasarTodo => _patrones.Count == 0;

    /// <summary>
    /// Arma el filtro, o explica por qué el patrón no sirve.
    /// </summary>
    /// <param name="patron">El texto del campo, tal cual lo escribió el usuario.</param>
    /// <param name="esExpresion">Si es una expresión regular. Ahí no se separa por coma.</param>
    /// <param name="distinguirMayusculas">
    /// En Windows los nombres no distinguen, pero la opción es una sola para toda la pantalla y se
    /// respeta donde se la pida.
    /// </param>
    /// <param name="error">El mensaje del motor cuando la expresión es inválida.</param>
    public static FiltroDeNombre? Crear(string patron, bool esExpresion, bool distinguirMayusculas, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(patron)) return new FiltroDeNombre([]);

        var opciones = RegexOptions.CultureInvariant;
        if (!distinguirMayusculas) opciones |= RegexOptions.IgnoreCase;

        if (esExpresion)
        {
            try
            {
                return new FiltroDeNombre([new Regex(patron, opciones)]);
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return null;
            }
        }

        var patrones = patron
            .Split(Separadores, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => new Regex(DeGlobAExpresion(p), opciones))
            .ToList();

        return new FiltroDeNombre(patrones);
    }

    /// <summary>Si el nombre del archivo pasa alguno de los patrones.</summary>
    public bool Coincide(string nombre) =>
        DejaPasarTodo || _patrones.Any(p => p.IsMatch(nombre));

    /// <summary>
    /// Traduce un glob a una expresión regular.
    ///
    /// Todo lo que no sea <c>*</c> o <c>?</c> se escapa, así un punto es un punto y no «cualquier
    /// carácter»: sin eso, <c>*.cs</c> encontraría también <c>archivocs</c>.
    /// </summary>
    private static string DeGlobAExpresion(string glob)
    {
        var tieneComodines = glob.Contains('*') || glob.Contains('?');

        var cuerpo = Regex.Escape(glob)
            .Replace(@"\*", ".*", StringComparison.Ordinal)
            .Replace(@"\?", ".", StringComparison.Ordinal);

        // Sin comodines se busca por parte del nombre; con comodines, el nombre entero.
        return tieneComodines ? $"^{cuerpo}$" : cuerpo;
    }
}
