using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// Qué se busca y en qué orden.
///
/// El catálogo es una lista fija en código y no un archivo de configuración: cada detector tiene
/// lógica propia, y un archivo que alguien pueda vaciar convertiría la lista en algo que puede
/// quedar en nada sin que se note.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public static class CatalogoDeDetectores
{
    public static IReadOnlyList<IDetector> Todos(SavedConnectionsRepository conexiones) =>
    [
        new DetectorBinObj(),
        new DetectorCacheNuGet(),
        new DetectorTemporales(),
        new DetectorLogsSql(conexiones),
        new DetectorCacheVsInstaller(),
        new DetectorPapelera(),
        new DetectorHibernacion(),
        Docker(),
        Wsl(),
        Grabaciones(),
        DevExpress()
    ];

    private static DetectorDeRutaConocida Docker() => new(
        "Docker",
        () =>
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return
            [
                Path.Combine(local, "Docker", "wsl", "disk", "docker_data.vhdx"),
                Path.Combine(local, "Docker", "wsl", "data", "ext4.vhdx")
            ];
        },
        new ItemLimpieza
        {
            Id = "docker-vhdx",
            Ubicacion = "Disco virtual de Docker",
            Grupo = GrupoLimpieza.ConCriterio,
            Riesgo = NivelRiesgo.Ninguno,
            Descripcion = "Donde Docker guarda sus imágenes, contenedores y volúmenes, todo en un solo archivo.",
            Consecuencia =
                "Esta herramienta no lo toca. Borrar el archivo a mano se lleva imágenes, contenedores y volúmenes de una vez y puede dejar Docker Desktop sin saber en qué estado quedó. " +
                "Y un prune tampoco alcanza: libera espacio adentro de la máquina virtual, pero el archivo sigue ocupando lo mismo en el disco. La única forma de recuperar los bytes es compactarlo desde Docker Desktop, en Settings, Resources, Clean / Purge data.",
            ComoSeRevierte = null,
            Derivacion = new AccionDerivada("Abrir Docker Desktop", UbicarDockerDesktop() ?? string.Empty)
        });

    /// <summary>
    /// Devuelve <c>null</c> si no se lo encuentra. El ítem se muestra igual: lo que se pierde es el
    /// botón, no la explicación, que es lo que evita que alguien borre el archivo por su cuenta.
    /// </summary>
    private static string? UbicarDockerDesktop()
    {
        string[] candidatos =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Docker", "Docker", "Docker Desktop.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Docker", "Docker", "Docker Desktop.exe")
        ];

        return candidatos.FirstOrDefault(File.Exists);
    }

    private static DetectorDeRutaConocida Wsl() => new(
        "WSL",
        () =>
        {
            var paquetes = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");

            if (!Directory.Exists(paquetes)) return [];

            // El nombre de la carpeta lleva la distro y un sufijo que cambia por máquina, así que
            // se busca el archivo en lugar de escribir la ruta.
            return Directory
                .EnumerateDirectories(paquetes)
                .Select(d => Path.Combine(d, "LocalState", "ext4.vhdx"))
                .Where(File.Exists);
        },
        new ItemLimpieza
        {
            Id = "wsl-ext4",
            Ubicacion = "Disco de las distribuciones de WSL",
            Grupo = GrupoLimpieza.ConCriterio,
            Riesgo = NivelRiesgo.Irreversible,
            Descripcion = "El sistema de archivos completo de cada Linux instalado en WSL: lo que hay instalado adentro y todo lo que guardaste ahí.",
            Consecuencia = "Se pierde la distribución entera con lo que tenga adentro. No va a la papelera.",
            ComoSeRevierte = null
        });

    private static DetectorDeRutaConocida Grabaciones() => new(
        "Grabaciones",
        () =>
        {
            var sistema = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? @"C:\";
            return
            [
                Path.Combine(sistema, "Agent", "Media"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Agent", "Media")
            ];
        },
        new ItemLimpieza
        {
            Id = "grabaciones-agente",
            Ubicacion = "Grabaciones del agente corporativo",
            Grupo = GrupoLimpieza.ConCriterio,
            Riesgo = NivelRiesgo.Alto,
            Descripcion = "Grabaciones que deja una herramienta de la empresa, no una tuya.",
            Consecuencia = "Se pierden las grabaciones. Al no ser software propio, puede haber una política que las necesite: conviene consultarlo con sistemas antes.",
            ComoSeRevierte = null
        });

    private static DetectorDeRutaConocida DevExpress() => new(
        "DevExpress",
        () =>
        {
            var carpetas = new List<string>();
            foreach (var programas in new[]
                     {
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                     })
            {
                if (!Directory.Exists(programas)) continue;

                try
                {
                    carpetas.AddRange(Directory.EnumerateDirectories(programas, "DevExpress*"));
                }
                catch
                {
                    // Una carpeta de programas sin permiso no puede dejar sin respuesta a la otra.
                }
            }

            return carpetas;
        },
        new ItemLimpieza
        {
            Id = "devexpress",
            Ubicacion = "Componentes de DevExpress",
            Grupo = GrupoLimpieza.ConCriterio,
            Riesgo = NivelRiesgo.Medio,
            Descripcion = "Los componentes instalados de DevExpress. Puede haber más de una versión y que sólo una esté en uso.",
            Consecuencia = "Si algún proyecto usa la versión que borrás, deja de compilar hasta reinstalarla. Antes de tocar esto conviene confirmar qué versión declaran los proyectos.",
            ComoSeRevierte = "Reinstalando desde el instalador de DevExpress."
        });
}
