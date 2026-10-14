using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// Los <c>bin</c> y <c>obj</c> de todos los repositorios. En la máquina del incidente eran 446
/// carpetas y 19,7 GB, el ítem más grande del grupo seguro.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorBinObj : IDetector
{
    private static readonly string[] _nombres = ["bin", "obj"];

    public string Nombre => "Compilados (bin y obj)";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            // La carpeta sale de Configuraciones y se resuelve en cada consulta: escribirla a mano
            // haría que cambiarla ahí no tuviera efecto acá.
            var raiz = RutasDeTrabajo.CarpetaDeRepositorios;
            if (!Directory.Exists(raiz)) return [];

            var carpetas = EnumeracionSegura.BuscarCarpetas(raiz, _nombres, cancelacion);
            if (carpetas.Count == 0) return [];

            long bytes = 0;
            foreach (var carpeta in carpetas)
                bytes += EnumeracionSegura.Medir(carpeta, cancelacion).Bytes;

            if (bytes == 0) return [];

            return
            [
                new ItemLimpieza
                {
                    Id = "compilados-bin-obj",
                    Ubicacion = $"Compilados de {carpetas.Count} proyectos, en {raiz}",
                    Bytes = bytes,
                    Grupo = GrupoLimpieza.Seguro,
                    Riesgo = NivelRiesgo.Ninguno,
                    Descripcion = "Lo que deja cada compilación: los ensamblados, los temporales del compilador y las copias de las dependencias.",
                    Consecuencia = "Nada que no vuelva solo. El primer build de cada proyecto tarda más porque rehace todo.",
                    ComoSeRevierte = "Compilando de nuevo.",
                    Rutas = carpetas,
                    // Visual Studio recrea estas carpetas con sus builds de diseño mientras se las
                    // borra. No bloquea el ítem: de 446 carpetas fallaron 3 y reintentar resolvió 2.
                    Bloqueantes = DetectorProcesos.CompilacionesEnCurso()
                }
            ];
        }, cancelacion);
}
