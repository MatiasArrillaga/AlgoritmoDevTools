using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// La carpeta de temporales del usuario. En la máquina del incidente, 5,79 GB.
///
/// Ojo con el vecindario: <c>%LOCALAPPDATA%\Temp</c> se limpia, pero su hermana
/// <c>%LOCALAPPDATA%\AlgoritmoDevTools</c> está protegida. De eso se encarga
/// <see cref="EnumeracionSegura"/>, que poda la rama protegida sola; acá no hay que acordarse.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorTemporales : IDetector
{
    public string Nombre => "Temporales";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            var temp = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");

            if (!Directory.Exists(temp)) return [];

            var medicion = EnumeracionSegura.Medir(temp, cancelacion);
            if (medicion.Bytes == 0) return [];

            return
            [
                new ItemLimpieza
                {
                    Id = "temporales-usuario",
                    Ubicacion = temp,
                    Bytes = medicion.Bytes,
                    Grupo = GrupoLimpieza.Seguro,
                    Riesgo = NivelRiesgo.Bajo,
                    Descripcion = "Lo que los programas dejan mientras trabajan y después no borran.",
                    Consecuencia = "Nada, salvo que algo esté usando un archivo justo en ese momento. Lo que esté en uso se saltea.",
                    ComoSeRevierte = null,
                    Rutas = [temp]
                }
            ];
        }, cancelacion);
}
