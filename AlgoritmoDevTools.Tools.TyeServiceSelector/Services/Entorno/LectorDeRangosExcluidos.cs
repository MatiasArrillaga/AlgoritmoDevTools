using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>Un tramo de puertos que Windows se reservó y que nadie más puede usar.</summary>
public sealed record RangoExcluido(int Desde, int Hasta)
{
    public bool Contiene(int puerto) => puerto >= Desde && puerto <= Hasta;
}

/// <summary>
/// Lee los rangos de puertos que Windows tiene reservados, de la salida de
/// <c>netsh interface ipv4 show excludedportrange protocol=tcp</c>.
///
/// <para>
/// <b>Para qué hace falta esto.</b> Windows le reserva tramos de puertos a Hyper-V y a WSL, y los
/// mueve cada tanto. Cuando uno de esos tramos cae encima de un puerto que un contenedor tiene
/// fijado desde que se creó, el contenedor deja de arrancar con un mensaje que no dice nada útil:
/// «Intento de acceso a un socket no permitido por sus permisos de acceso». Pasó con los
/// contenedores de Dapr, que piden el 58080 y el 58081.
/// </para>
///
/// <para>
/// El parseo busca las líneas con dos números y nada más: el encabezado de <c>netsh</c> está
/// traducido y cambia con el idioma de Windows, así que fijarse en los títulos no serviría.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public static class LectorDeRangosExcluidos
{
    private static readonly Regex Fila = new(@"^\s*(\d+)\s+(\d+)\s*\*?\s*$", RegexOptions.Compiled);

    public static IReadOnlyList<RangoExcluido> Parsear(string salidaDeNetsh)
    {
        var rangos = new List<RangoExcluido>();
        if (string.IsNullOrWhiteSpace(salidaDeNetsh)) return rangos;

        foreach (var linea in salidaDeNetsh.Split('\n'))
        {
            var fila = Fila.Match(linea);
            if (!fila.Success) continue;

            if (int.TryParse(fila.Groups[1].Value, out var desde)
                && int.TryParse(fila.Groups[2].Value, out var hasta)
                && desde <= hasta)
            {
                rangos.Add(new RangoExcluido(desde, hasta));
            }
        }

        return rangos;
    }

    /// <summary>
    /// De los puertos pedidos, cuáles caen en un tramo reservado. Devuelve el puerto con el rango
    /// que lo tapa, que es lo que hay que mostrarle al usuario para que el diagnóstico sea verificable.
    /// </summary>
    public static IReadOnlyList<(int Puerto, RangoExcluido Rango)> Cruzar(
        IEnumerable<int> puertosPedidos,
        IReadOnlyList<RangoExcluido> rangos)
    {
        var choques = new List<(int, RangoExcluido)>();

        foreach (var puerto in puertosPedidos)
        {
            var rango = rangos.FirstOrDefault(r => r.Contiene(puerto));
            if (rango is not null) choques.Add((puerto, rango));
        }

        return choques;
    }
}
