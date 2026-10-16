using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>
/// Encuentra el texto buscado adentro de una línea, sea literal o expresión regular.
///
/// <b>Lo literal no pasa por el motor de expresiones.</b> Va con <c>IndexOf</c> ordinal, que sobre
/// miles de archivos es bastante más rápido que compilar y correr una expresión escapada.
///
/// <b>Y una expresión no puede colgar la búsqueda.</b> Un patrón como <c>(a+)+$</c> sobre una línea
/// larga tarda tiempo exponencial, y acá está lo que no se ve venir: <c>Regex.IsMatch</c> no
/// consulta el <c>CancellationToken</c>, así que el hilo se queda adentro del motor y el botón de
/// cancelar no hace nada. El síntoma es «se colgó» y la causa está a tres capas de donde uno mira.
///
/// Por eso se arma con <see cref="RegexOptions.NonBacktracking"/>, que garantiza tiempo lineal y
/// hace que el problema no exista. No acepta lookarounds ni referencias hacia atrás, y cuando el
/// patrón las usa falla al construirse —no al correr—, que es cuando se puede reaccionar: ahí se
/// cae al motor de siempre, pero con tope de tiempo.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public sealed class CoincidenciaEnTexto
{
    /// <summary>
    /// Cuánto se le deja a una línea antes de darla por perdida. Sólo corre en la rama con
    /// backtracking, o sea cuando el patrón trae un lookaround.
    /// </summary>
    private static readonly TimeSpan TopeDeTiempo = TimeSpan.FromSeconds(2);

    private readonly string _texto;
    private readonly Regex? _expresion;
    private readonly StringComparison _comparacion;
    private readonly bool _palabraEntera;

    private CoincidenciaEnTexto(string texto, Regex? expresion, StringComparison comparacion, bool palabraEntera)
    {
        _texto = texto;
        _expresion = expresion;
        _comparacion = comparacion;
        _palabraEntera = palabraEntera;
    }

    /// <summary>
    /// Arma el buscador, o explica por qué la expresión no sirve.
    ///
    /// Devuelve el error en lugar de tirar porque una expresión a medio escribir es el estado normal
    /// mientras alguien tipea: hay que poder decirlo en pantalla, no romper.
    /// </summary>
    /// <param name="texto">Lo que se busca.</param>
    /// <param name="esExpresion">Si <paramref name="texto"/> es una expresión regular.</param>
    /// <param name="distinguirMayusculas">Vale para los dos modos.</param>
    /// <param name="palabraEntera">Se ignora cuando es expresión: ahí el <c>\b</c> lo pone quien escribe.</param>
    /// <param name="error">El mensaje del motor, con la posición, cuando la expresión es inválida.</param>
    public static CoincidenciaEnTexto? Crear(
        string texto,
        bool esExpresion,
        bool distinguirMayusculas,
        bool palabraEntera,
        out string? error)
    {
        error = null;

        var comparacion = distinguirMayusculas ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (!esExpresion)
        {
            return new CoincidenciaEnTexto(texto, null, comparacion, palabraEntera);
        }

        var opciones = RegexOptions.CultureInvariant;
        if (!distinguirMayusculas) opciones |= RegexOptions.IgnoreCase;

        try
        {
            // El motor que no puede explotar.
            return new CoincidenciaEnTexto(texto, new Regex(texto, opciones | RegexOptions.NonBacktracking), comparacion, false);
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
        {
            // Dos motivos distintos, y hay que atrapar los dos:
            //
            // - NotSupportedException: el patrón está bien escrito pero usa algo que este motor no
            //   sabe hacer, casi siempre un lookaround. Es el caso normal y hay que seguir.
            // - ArgumentException: el patrón está mal escrito. Igual se deja pasar al segundo
            //   intento, que vuelve a fallar y ahí sí se reporta con el mensaje del motor.
            //
            // Atrapar sólo ArgumentException —que es lo que uno escribe de memoria— deja escapar la
            // primera, y entonces un lookaround no da un error en pantalla: tira la aplicación.
        }

        try
        {
            return new CoincidenciaEnTexto(texto, new Regex(texto, opciones | RegexOptions.Compiled, TopeDeTiempo), comparacion, false);
        }
        catch (ArgumentException ex)
        {
            // Ahora sí la expresión está mal escrita. El mensaje del motor dice qué y dónde.
            error = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Dónde aparece lo buscado dentro de la línea, una entrada por coincidencia.
    ///
    /// Puede tirar <see cref="RegexMatchTimeoutException"/> en la rama con backtracking. Quien
    /// llama tiene que atraparla y marcar ese archivo como no evaluado: un archivo raro no puede
    /// arruinar un resultado de siete mil.
    /// </summary>
    public IEnumerable<(int Desde, int Largo)> Buscar(string linea)
    {
        if (string.IsNullOrEmpty(linea) || string.IsNullOrEmpty(_texto)) yield break;

        if (_expresion is not null)
        {
            foreach (Match coincidencia in _expresion.Matches(linea))
            {
                // Una expresión puede matchear vacío —`a*` contra una línea sin aes— y eso no es un
                // hallazgo: resaltaría la nada y avanzaría de a cero.
                if (coincidencia.Length == 0) continue;

                yield return (coincidencia.Index, coincidencia.Length);
            }

            yield break;
        }

        var desde = 0;

        while (desde <= linea.Length - _texto.Length)
        {
            var donde = linea.IndexOf(_texto, desde, _comparacion);
            if (donde < 0) yield break;

            if (!_palabraEntera || EsPalabraEntera(linea, donde, _texto.Length))
            {
                yield return (donde, _texto.Length);
            }

            desde = donde + 1;
        }
    }

    /// <summary>
    /// Si lo encontrado está suelto y no es parte de una palabra más larga: <c>Cotizacion</c> no
    /// cuenta adentro de <c>TipoCotizacionDTO</c>.
    ///
    /// Se mira a mano, mirando el carácter de antes y el de después, en vez de armar un
    /// <c>\b…\b</c>: mantiene la búsqueda literal fuera del motor de expresiones, que es de donde
    /// sale su ventaja.
    /// </summary>
    private static bool EsPalabraEntera(string linea, int desde, int largo)
    {
        if (desde > 0 && EsDePalabra(linea[desde - 1])) return false;

        var siguiente = desde + largo;
        return siguiente >= linea.Length || !EsDePalabra(linea[siguiente]);
    }

    private static bool EsDePalabra(char caracter) => char.IsLetterOrDigit(caracter) || caracter == '_';
}
