using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Uno por ítem del catálogo. Devuelve vacío cuando lo suyo no aplica a esta máquina: el catálogo
/// detecta, no asume, y un ítem que no existe acá no se muestra apagado sino que no se muestra.
/// </summary>
public interface IDetector
{
    /// <summary>Para el reporte: si falla, hay que poder decir cuál fue.</summary>
    string Nombre { get; }

    Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion);
}

/// <summary>
/// El disco tal como estaba <b>en el momento del escaneo</b>.
///
/// El libre se lee una sola vez y queda acá congelado. No sirve para medir lo que se liberó: dos
/// mediciones separadas por minutos dieron 6,9 y 40 GB sin que nadie tocara nada, y un borrado de
/// 19,7 GB marcó un delta de 41,07. Lo que se liberó se cuenta sumando lo borrado, no restando esto.
/// </summary>
public sealed record EspacioDelDisco(string Unidad, long Total, long Libre)
{
    public static readonly EspacioDelDisco Desconocido = new("?", 0, 0);
}

/// <summary>
/// El resultado de una corrida.
/// </summary>
/// <param name="Items">Lo detectado, antes de aplicarle el perfil del usuario.</param>
/// <param name="Fallaron">Los detectores que no pudieron terminar. El resto del escaneo igual sirve.</param>
/// <param name="Disco">Cómo estaba el disco al empezar.</param>
public sealed record Escaneo(
    IReadOnlyList<ItemLimpieza> Items,
    IReadOnlyList<string> Fallaron,
    EspacioDelDisco Disco);

/// <summary>
/// Corre los detectores y junta lo que encuentran.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class EscaneoService
{
    /// <summary>
    /// Cuántos detectores corren a la vez.
    ///
    /// Acotado a propósito: el detector de <c>bin</c>/<c>obj</c> recorre cientos de carpetas sobre
    /// una solución de 160 proyectos, y soltar todos los detectores juntos contra el mismo disco
    /// los hace competir entre sí en lugar de terminar antes.
    /// </summary>
    private const int EN_PARALELO = 4;

    private readonly IReadOnlyList<IDetector> _detectores;

    public EscaneoService(IReadOnlyList<IDetector> detectores) => _detectores = detectores;

    public async Task<Escaneo> EscanearAsync(CancellationToken cancelacion = default)
    {
        var disco = LeerDisco();

        var items = new List<ItemLimpieza>();
        var fallaron = new List<string>();
        var candado = new object();

        await Parallel.ForEachAsync(
            _detectores,
            new ParallelOptions { MaxDegreeOfParallelism = EN_PARALELO, CancellationToken = cancelacion },
            async (detector, ct) =>
            {
                try
                {
                    var encontrados = await detector.DetectarAsync(ct).ConfigureAwait(false);
                    lock (candado) items.AddRange(encontrados);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Un detector que falla no puede llevarse puesto el escaneo entero: el resto de
                    // los ítems sigue siendo útil, y de este se informa que no pudo terminar.
                    lock (candado) fallaron.Add(detector.Nombre);
                }
            }).ConfigureAwait(false);

        // El orden de llegada depende del paralelismo, y una grilla que cambia de orden entre
        // corridas es desconcertante. Primero lo seguro, y dentro de cada grupo lo más grande.
        var ordenados = items
            .OrderBy(i => i.Grupo)
            .ThenByDescending(i => i.Bytes)
            .ToArray();

        return new Escaneo(ordenados, fallaron, disco);
    }

    private static EspacioDelDisco LeerDisco()
    {
        try
        {
            var raiz = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (string.IsNullOrWhiteSpace(raiz)) return EspacioDelDisco.Desconocido;

            var unidad = new DriveInfo(raiz);
            return new EspacioDelDisco(unidad.Name, unidad.TotalSize, unidad.AvailableFreeSpace);
        }
        catch
        {
            return EspacioDelDisco.Desconocido;
        }
    }
}
