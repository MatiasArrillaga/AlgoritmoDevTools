using System.Security.Principal;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// La papelera de reciclaje. En la máquina del incidente, 20,89 GB que el Explorador mostraba como
/// vacía.
///
/// <b>Se enumera la carpeta a mano, con System.IO.</b> Dos razones, las dos verificadas:
///
/// <c>Clear-RecycleBin -Force</c> devolvió éxito y liberó cero. De 608 entradas había 572 archivos
/// de datos y sólo 35 índices; sin su índice el shell no puede listar la entrada, así que la API de
/// papelera la ignora —pero los bytes están—.
///
/// Y la ruta <b>no puede pasar por PowerShell</b>: el comando viaja entre comillas dobles, así que
/// <c>$Recycle.Bin</c> se expande a <c>.Bin</c> y falla con un "no se encuentra el archivo" que
/// parece decir que el destino no existe.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorPapelera : IDetector
{
    public string Nombre => "Papelera de reciclaje";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            var sid = WindowsIdentity.GetCurrent().User?.Value;
            if (string.IsNullOrWhiteSpace(sid)) return [];

            var carpetas = new List<string>();
            long bytes = 0;

            foreach (var unidad in DriveInfo.GetDrives())
            {
                cancelacion.ThrowIfCancellationRequested();
                if (!unidad.IsReady || unidad.DriveType != DriveType.Fixed) continue;

                // Concatenando y nunca interpolando en un comando: el $ de la carpeta es parte del
                // nombre.
                var mia = Path.Combine(unidad.RootDirectory.FullName, "$Recycle.Bin", sid);
                if (!Directory.Exists(mia)) continue;

                var medicion = EnumeracionSegura.Medir(mia, cancelacion);
                if (medicion.Bytes == 0) continue;

                bytes += medicion.Bytes;
                carpetas.Add(mia);
            }

            if (carpetas.Count == 0) return [];

            return
            [
                new ItemLimpieza
                {
                    Id = "papelera",
                    Ubicacion = "Papelera de reciclaje",
                    Bytes = bytes,
                    Grupo = GrupoLimpieza.ConCriterio,
                    Riesgo = NivelRiesgo.Irreversible,
                    Descripcion = "Lo que borraste y todavía se puede recuperar. Puede incluir entradas que el Explorador no llega a mostrar aunque ocupen lugar.",
                    Consecuencia = "Se pierden definitivamente, incluso los que la papelera no te está mostrando.",
                    ComoSeRevierte = null,
                    RequiereAdmin = true,
                    Rutas = carpetas
                }
            ];
        }, cancelacion);
}
