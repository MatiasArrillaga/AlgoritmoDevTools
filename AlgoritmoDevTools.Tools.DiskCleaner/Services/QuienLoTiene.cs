using System.Diagnostics;
using System.Runtime.InteropServices;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Quién tiene tomado un archivo.
///
/// Es la diferencia entre "acceso denegado" y "lo tiene Visual Studio": lo primero no le dice a
/// nadie qué hacer, lo segundo se resuelve cerrando una ventana. Se pregunta <b>sólo cuando un
/// borrado ya falló</b>, nunca antes: averiguarlo cuesta abrir una sesión del sistema por consulta,
/// y sobre cientos de carpetas haría inusable la previsualización.
///
/// Usa el Restart Manager de Windows, que es la misma pieza que usan los instaladores para decir
/// "cerrá estos programas antes de continuar".
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public static class QuienLoTiene
{
    private const int ERROR_MORE_DATA = 234;
    private const int RM_SESSION_KEY_LEN = 32;

    /// <summary>
    /// Cuántos archivos se miran cuando lo que falló es una carpeta. El Restart Manager pregunta
    /// por archivos, no por carpetas, y con el primero que esté tomado ya alcanza para nombrar al
    /// culpable: recorrer la carpeta entera no agregaría información.
    /// </summary>
    private const int ARCHIVOS_A_MIRAR = 8;

    /// <summary>
    /// Los procesos que tienen tomada la ruta. Vacío si no se puede averiguar: no saber quién es
    /// no puede convertirse en un error propio encima del que ya falló.
    /// </summary>
    public static IReadOnlyList<ProcesoBloqueante> De(string ruta)
    {
        try
        {
            var archivos = ArchivosAConsultar(ruta);
            return archivos.Length == 0 ? [] : Consultar(archivos, ruta);
        }
        catch
        {
            return [];
        }
    }

    private static string[] ArchivosAConsultar(string ruta)
    {
        if (File.Exists(ruta)) return [ruta];
        if (!Directory.Exists(ruta)) return [];

        try
        {
            return Directory
                .EnumerateFiles(ruta, "*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true
                })
                .Take(ARCHIVOS_A_MIRAR)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<ProcesoBloqueante> Consultar(string[] archivos, string ruta)
    {
        var clave = Guid.NewGuid().ToString("N")[..RM_SESSION_KEY_LEN];
        if (RmStartSession(out var sesion, 0, clave) != 0) return [];

        try
        {
            if (RmRegisterResources(sesion, (uint)archivos.Length, archivos, 0, null, 0, null) != 0) return [];

            uint cantidad = 0;
            var resultado = RmGetList(sesion, out var necesarios, ref cantidad, null, out _);

            // La primera llamada se hace con el arreglo vacío justamente para que conteste cuántos
            // son; recién la segunda trae los datos.
            if (resultado != ERROR_MORE_DATA || necesarios == 0) return [];

            var procesos = new RM_PROCESS_INFO[necesarios];
            cantidad = necesarios;
            if (RmGetList(sesion, out _, ref cantidad, procesos, out _) != 0) return [];

            var propio = Environment.ProcessId;
            var encontrados = new List<ProcesoBloqueante>();

            for (var i = 0; i < cantidad; i++)
            {
                var pid = procesos[i].Process.dwProcessId;
                if (pid == propio) continue;

                // El Restart Manager devuelve el nombre amigable, que a veces viene vacío. El del
                // proceso es menos lindo pero siempre está, y es el que se busca en el Administrador
                // de tareas.
                var nombre = string.IsNullOrWhiteSpace(procesos[i].strAppName)
                    ? NombreDelProceso(pid)
                    : procesos[i].strAppName;

                encontrados.Add(new ProcesoBloqueante(pid, nombre, ruta));
            }

            return encontrados;
        }
        finally
        {
            RmEndSession(sesion);
        }
    }

    private static string NombreDelProceso(int pid)
    {
        try
        {
            using var proceso = Process.GetProcessById(pid);
            return proceso.ProcessName;
        }
        catch
        {
            return $"proceso {pid}";
        }
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint pSessionHandle,
        uint nFiles,
        string[]? rgsFilenames,
        uint nApplications,
        RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        uint dwSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
        out uint lpdwRebootReasons);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strAppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string strServiceShortName;

        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }
}
