using System.Diagnostics;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Qué hay corriendo que convenga esperar antes de borrar.
///
/// Son dos consultas y no una porque los procesos no estorban a lo mismo: una instalación en curso
/// rompe la caché del VS Installer, y una compilación en curso recrea las carpetas que se están
/// borrando. Juntarlas obligaría a cada detector a repetir la lista de nombres.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public static class DetectorProcesos
{
    /// <summary>
    /// El 08/10/2026 había un <c>setup.exe modify</c> corriendo mientras se analizaba el disco.
    /// Borrar la caché del VS Installer en ese momento rompe la instalación en curso.
    /// </summary>
    private static readonly string[] _instaladores = ["setup", "vs_installer", "msiexec"];

    /// <summary>
    /// De 446 carpetas, dos fallaron porque Visual Studio las estaba recreando con sus builds de
    /// diseño mientras se las borraba.
    /// </summary>
    private static readonly string[] _compiladores = ["devenv", "MSBuild", "dotnet"];

    /// <summary>Instalaciones en curso.</summary>
    public static IReadOnlyList<ProcesoBloqueante> InstaladoresEnCurso() => Buscar(_instaladores);

    /// <summary>Compilaciones en curso.</summary>
    public static IReadOnlyList<ProcesoBloqueante> CompilacionesEnCurso() => Buscar(_compiladores);

    private static IReadOnlyList<ProcesoBloqueante> Buscar(string[] nombres)
    {
        var propio = Environment.ProcessId;
        var encontrados = new List<ProcesoBloqueante>();

        foreach (var nombre in nombres)
        {
            Process[] procesos;
            try
            {
                procesos = Process.GetProcessesByName(nombre);
            }
            catch
            {
                // Un nombre que el sistema no quiera resolver no puede dejar sin respuesta a los
                // otros cinco.
                continue;
            }

            foreach (var proceso in procesos)
            {
                try
                {
                    // Corriendo con "dotnet run" durante el desarrollo, la propia herramienta
                    // aparecería en la lista y se reportaría a sí misma como estorbo.
                    if (proceso.Id != propio)
                        encontrados.Add(new ProcesoBloqueante(proceso.Id, proceso.ProcessName, null));
                }
                catch
                {
                    // Un proceso de otro usuario, o uno que terminó entre la consulta y la lectura.
                }
                finally
                {
                    proceso.Dispose();
                }
            }
        }

        return encontrados;
    }
}
