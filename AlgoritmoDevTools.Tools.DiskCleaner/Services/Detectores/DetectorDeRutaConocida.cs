using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// Un ítem que es, simplemente, "si esta ruta existe en esta máquina, mostrá cuánto ocupa".
///
/// Son cuatro del catálogo —Docker, WSL, las grabaciones y la copia vieja de DevExpress— y el
/// código de los cuatro sería el mismo salvo los textos y dónde mirar, así que es uno
/// parametrizado. Los que tienen lógica propia —la papelera, la hibernación, los logs— tienen su
/// clase.
///
/// Las rutas se resuelven con una función y no con una lista fija porque varias llevan comodín: la
/// distro de WSL y la versión de DevExpress cambian de nombre en cada máquina.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorDeRutaConocida : IDetector
{
    private readonly Func<IEnumerable<string>> _dondeMirar;
    private readonly ItemLimpieza _molde;

    public DetectorDeRutaConocida(string nombre, Func<IEnumerable<string>> dondeMirar, ItemLimpieza molde)
    {
        Nombre = nombre;
        _dondeMirar = dondeMirar;
        _molde = molde;
    }

    public string Nombre { get; }

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            string[] existentes;
            try
            {
                existentes = _dondeMirar().Where(r => File.Exists(r) || Directory.Exists(r)).ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return [];
            }

            if (existentes.Length == 0) return [];

            long bytes = 0;
            foreach (var ruta in existentes)
            {
                cancelacion.ThrowIfCancellationRequested();
                bytes += Directory.Exists(ruta)
                    ? EnumeracionSegura.Medir(ruta, cancelacion).Bytes
                    : TamanoDeArchivo(ruta);
            }

            if (bytes == 0) return [];

            // Un ítem que deriva no borra nada, así que no lleva rutas: dejarlas cargadas sería
            // dejar a mano una lista de cosas para borrar que justamente se decidió no borrar.
            return
            [
                _molde.Derivacion is null
                    ? _molde with { Bytes = bytes, Rutas = existentes }
                    : _molde with { Bytes = bytes }
            ];
        }, cancelacion);

    private static long TamanoDeArchivo(string ruta)
    {
        try
        {
            return new FileInfo(ruta).Length;
        }
        catch
        {
            return 0;
        }
    }
}
