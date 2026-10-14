using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// La caché de descarga del instalador de Visual Studio. En la máquina del incidente, 14,45 GB.
///
/// <b>Este ítem no borra nada, y es deliberado.</b> Borrar esa carpeta a mano se llevó una vez
/// <c>_Instances</c>, que no es caché sino el registro de qué productos hay instalados: el
/// instalador dejó de listar productos, <c>vswhere</c> dejó de devolver nada, y no se pudo
/// recuperar —Microsoft responde que la salida es desinstalar y reinstalar—.
///
/// La vía soportada es la opción del propio instalador, así que el ítem mide, explica y lleva hasta
/// ahí. <see cref="ItemLimpieza.Rutas"/> queda vacío a propósito: no hay nada que borrar, y así no
/// hay forma de que un cambio futuro lo mande a la limpieza por descuido.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorCacheVsInstaller : IDetector
{
    public string Nombre => "Caché del instalador de Visual Studio";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            var packages = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "VisualStudio", "Packages");

            if (!Directory.Exists(packages)) return [];

            var medicion = EnumeracionSegura.Medir(packages, cancelacion);
            if (medicion.Bytes == 0) return [];

            return
            [
                new ItemLimpieza
                {
                    Id = "cache-vs-installer",
                    Ubicacion = "Caché de descarga del instalador de Visual Studio",
                    Bytes = medicion.Bytes,
                    Grupo = GrupoLimpieza.ConCriterio,
                    Riesgo = NivelRiesgo.Ninguno,
                    Descripcion = "Los instaladores que Visual Studio ya descargó. Adentro, mezclado, está también el registro de qué productos tenés instalados.",
                    Consecuencia = "Borrarla a mano es lo que rompe ese registro, y no se recupera: el instalador deja de listar los productos y hay que desinstalar y reinstalar. Por eso esta herramienta no la toca.",
                    ComoSeRevierte = null,
                    Rutas = [],
                    Derivacion = new AccionDerivada(
                        "Abrir el instalador de Visual Studio",
                        UbicarElInstalador() ?? string.Empty),
                    // Con una instalación en curso ni siquiera conviene abrir el instalador.
                    Bloqueantes = DetectorProcesos.InstaladoresEnCurso()
                }
            ];
        }, cancelacion);

    /// <summary>
    /// Devuelve <c>null</c> si no se lo encuentra. El ítem se muestra igual: lo que se pierde es el
    /// botón, no la explicación, que es lo que evita que alguien lo borre por fuera.
    /// </summary>
    private static string? UbicarElInstalador()
    {
        string[] candidatos =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio", "Installer", "setup.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Microsoft Visual Studio", "Installer", "setup.exe")
        ];

        return candidatos.FirstOrDefault(File.Exists);
    }
}
