using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// Los paquetes que NuGet guarda para no volver a bajarlos. En la máquina del incidente, 16,19 GB.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorCacheNuGet : IDetector
{
    public string Nombre => "Cachés de NuGet";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] candidatas =
            [
                Path.Combine(perfil, ".nuget", "packages"),
                Path.Combine(local, "NuGet", "v3-cache")
            ];

            var rutas = candidatas.Where(Directory.Exists).ToArray();
            if (rutas.Length == 0) return [];

            long bytes = 0;
            foreach (var ruta in rutas)
                bytes += EnumeracionSegura.Medir(ruta, cancelacion).Bytes;

            if (bytes == 0) return [];

            return
            [
                new ItemLimpieza
                {
                    Id = "cache-nuget",
                    Ubicacion = "Paquetes y descargas de NuGet",
                    Bytes = bytes,
                    Grupo = GrupoLimpieza.Seguro,
                    Riesgo = NivelRiesgo.Ninguno,
                    Descripcion = "Los paquetes que ya se bajaron alguna vez, guardados para no volver a pedirlos.",
                    Consecuencia = "La próxima restauración los vuelve a descargar. Hace falta internet y tarda.",
                    ComoSeRevierte = "Restaurando los paquetes de cualquier solución.",
                    Rutas = rutas
                }
            ];
        }, cancelacion);
}
