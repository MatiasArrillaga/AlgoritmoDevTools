using System.Diagnostics;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Pone y saca la extensión de Visual Studio que deja los comandos del Commands Maker en el clic
/// derecho de la Consola del Administrador de paquetes.
///
/// <para>
/// <b>No se parece al resto de las integraciones.</b> El arranque con Windows y los menús del
/// explorador son una clave del registro: se escriben al instante, sin permisos y sin cerrar nada.
/// Esta no. Visual Studio 2026 instala las extensiones a través de su propio instalador, que
/// <b>pide elevación</b> y <b>no puede tocar una instancia abierta</b>. Así que acá no hay switch
/// que se aplique solo: hay que cerrar Visual Studio, lanzar el instalador y que el usuario acepte
/// el UAC. Está probado que <c>/quiet</c> no sirve — ver la sección 11 del documento de la
/// extensión.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Integrations.VisualStudio/README.referencia-tecnica-funcional-menu-visual-studio.md
/// </summary>
public static class ExtensionDeVisualStudio
{
    /// <summary>
    /// Identificador que declara la extensión en su manifiesto. Con él la busca el instalador para
    /// desinstalarla, y con él se la reconoce entre los paquetes registrados.
    ///
    /// Es un espejo del <c>Metadata.id</c> de <c>ExtensionEntrypoint</c>: si allá cambia, acá la
    /// extensión deja de aparecer como instalada y el desinstalar no encuentra nada que sacar.
    /// </summary>
    public const string Identificador = "AlgoritmoDevTools.VisualStudio.5c9e1b7a-3d42-4f18-9a6c-0b2e8d71f403";

    /// <summary>
    /// Con qué empieza el nombre del paquete en el release. Lo que va entre esto y <c>.vsix</c> es
    /// la versión: <c>AlgoritmoDevTools.Integrations.VisualStudio.1.6.0.vsix</c>.
    ///
    /// Que la versión viaje en el nombre es lo que permite saber qué hay publicado <b>sin bajar el
    /// paquete</b>: la consulta del release ya devuelve los nombres de los adjuntos.
    /// </summary>
    private const string PrefijoDelAdjunto = "AlgoritmoDevTools.Integrations.VisualStudio.";

    private const string ExtensionDelPaquete = ".vsix";

    /// <summary>
    /// Cómo se llama el paquete cuando está al lado del ejecutable, para probar sin publicar un
    /// release. Ahí va sin versión: lo que se está probando es ese archivo y no otro.
    /// </summary>
    private const string NombreLocalDelPaquete = PrefijoDelAdjunto + "vsix";

    private const string ProcesoDeVisualStudio = "devenv";

    /// <summary>
    /// Dónde Visual Studio registra lo que tiene instalado. Cada extensión deja una carpeta
    /// <c>&lt;identificador&gt;,version=1.6.0.0</c>, y es de ahí que sale la versión que se muestra.
    /// </summary>
    private static string CarpetaDePaquetes => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Microsoft", "VisualStudio", "Packages");

    /// <summary>
    /// Ruta de instalación de Visual Studio, o null si no hay ninguno. Se pregunta con
    /// <c>vswhere</c>, que es la única forma soportada: la carpeta cambia con la edición y la
    /// versión, así que armarla a mano se rompe en la máquina del que tenga otra.
    /// </summary>
    public static string? RutaDeVisualStudio()
    {
        try
        {
            var vswhere = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio", "Installer", "vswhere.exe");

            if (!File.Exists(vswhere)) return null;

            var salida = Ejecutar(vswhere, "-latest -prerelease -property installationPath");
            var primera = salida?.Split('\n').FirstOrDefault()?.Trim();

            return string.IsNullOrEmpty(primera) || !Directory.Exists(primera) ? null : primera;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool HayVisualStudio() => RutaDeVisualStudio() is not null;

    /// <summary>Si hay alguna instancia abierta. Con una sola alcanza para que no se pueda instalar.</summary>
    public static bool EstaAbierto() => Process.GetProcessesByName(ProcesoDeVisualStudio).Length > 0;

    /// <summary>
    /// Versión instalada, o null si no está. Sale del nombre de la carpeta que deja el instalador,
    /// no de un archivo adentro: es lo que sobrevive a que la carpeta de la extensión tenga un
    /// nombre aleatorio distinto en cada instalación.
    /// </summary>
    public static string? VersionInstalada()
    {
        try
        {
            if (!Directory.Exists(CarpetaDePaquetes)) return null;

            var carpeta = Directory
                .EnumerateDirectories(CarpetaDePaquetes, Identificador + ",version=*")
                .Select(Path.GetFileName)
                .FirstOrDefault();

            if (carpeta is null) return null;

            var corte = carpeta.IndexOf("version=", StringComparison.OrdinalIgnoreCase);
            return corte < 0 ? null : carpeta[(corte + "version=".Length)..];
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool EstaInstalada() => VersionInstalada() is not null;

    /// <summary>
    /// Baja el paquete del último release y abre el instalador de Visual Studio para ponerlo.
    ///
    /// <para>
    /// <b>El instalador se abre con su ventana y no en silencio.</b> No es una preferencia: con
    /// <c>/quiet</c> el instalador de Visual Studio 2026 no puede mostrar el UAC que necesita y
    /// termina con error, sin decir que lo que faltaba era un permiso.
    /// </para>
    ///
    /// Antes de bajar nada se descartan los dos casos en los que no va a poder: que no haya Visual
    /// Studio y que esté abierto. Avisar eso es más útil que una descarga que termina en un error
    /// del instalador.
    /// </summary>
    /// <returns>Null si el instalador terminó bien, o el mensaje de error.</returns>
    public static async Task<string?> TryInstalarAsync(CancellationToken cancelacion = default)
    {
        var visualStudio = RutaDeVisualStudio();
        if (visualStudio is null) return "no se encontró ninguna instalación de Visual Studio.";
        if (EstaAbierto()) return "hay que cerrar Visual Studio antes de instalar la extensión.";

        var instalador = Path.Combine(visualStudio, "Common7", "IDE", "VSIXInstaller.exe");
        if (!File.Exists(instalador)) return "no se encontró el instalador de extensiones de Visual Studio.";

        var paquete = await ConseguirElPaqueteAsync(cancelacion);
        if (paquete.Error is not null) return paquete.Error;

        return await CorrerElInstaladorAsync(instalador, $"\"{paquete.Ruta}\"", cancelacion);
    }

    /// <summary>
    /// Saca la extensión. Pide los mismos permisos y la misma ventana que instalarla, por lo mismo.
    /// </summary>
    /// <returns>Null si el instalador terminó bien, o el mensaje de error.</returns>
    public static async Task<string?> TryDesinstalarAsync(CancellationToken cancelacion = default)
    {
        var visualStudio = RutaDeVisualStudio();
        if (visualStudio is null) return "no se encontró ninguna instalación de Visual Studio.";
        if (EstaAbierto()) return "hay que cerrar Visual Studio antes de quitar la extensión.";

        var instalador = Path.Combine(visualStudio, "Common7", "IDE", "VSIXInstaller.exe");
        if (!File.Exists(instalador)) return "no se encontró el instalador de extensiones de Visual Studio.";

        return await CorrerElInstaladorAsync(instalador, $"/uninstall:{Identificador}", cancelacion);
    }

    /// <summary>
    /// Qué versión hay publicada en el último release, o null si no se pudo averiguar (sin
    /// internet, o un release que no trae el paquete).
    ///
    /// <b>No baja el paquete</b>: la saca del nombre del adjunto, que es para lo que lleva la
    /// versión adentro.
    /// </summary>
    public static async Task<string?> VersionPublicadaAsync(CancellationToken cancelacion = default)
    {
        var adjunto = await Actualizador.BuscarAdjuntoAsync(PrefijoDelAdjunto, cancelacion);
        return adjunto is null ? null : VersionDelNombre(adjunto.Nombre);
    }

    /// <summary>
    /// Saca la versión de un nombre con la forma <c>&lt;prefijo&gt;1.6.0.vsix</c>. Devuelve null si
    /// el nombre no tiene esa forma — por ejemplo el del paquete local, que va sin versión.
    /// </summary>
    internal static string? VersionDelNombre(string nombre)
    {
        if (!nombre.StartsWith(PrefijoDelAdjunto, StringComparison.OrdinalIgnoreCase)) return null;
        if (!nombre.EndsWith(ExtensionDelPaquete, StringComparison.OrdinalIgnoreCase)) return null;

        var version = nombre[PrefijoDelAdjunto.Length..^ExtensionDelPaquete.Length];
        return Version.TryParse(version, out _) ? version : null;
    }

    /// <summary>
    /// Si lo publicado es más nuevo que lo instalado.
    ///
    /// <b>Se comparan tres números y no cuatro</b>, a propósito: lo instalado lo reporta Visual
    /// Studio con cuatro (<c>1.6.0.0</c>) y lo publicado viene con tres (<c>1.6.0</c>). Comparados
    /// tal cual, <c>Version</c> da que 1.6.0 es MENOR que 1.6.0.0 —porque la revisión ausente vale
    /// -1— y la misma versión aparecería como una actualización disponible, para siempre.
    /// </summary>
    public static bool HayVersionNueva(string? instalada, string? publicada)
    {
        if (!Version.TryParse(instalada, out var puesta) || !Version.TryParse(publicada, out var ofrecida))
            return false;

        return Normalizar(ofrecida) > Normalizar(puesta);
    }

    private static Version Normalizar(Version version)
        => new(version.Major, version.Minor, Math.Max(version.Build, 0));

    /// <summary>
    /// De dónde sale el <c>.vsix</c>: del último release, salvo que haya uno al lado del
    /// ejecutable. Ese atajo es para poder probar un paquete recién compilado sin publicar un
    /// release, que de otro modo sería la única manera.
    /// </summary>
    private static async Task<(string? Ruta, string? Error)> ConseguirElPaqueteAsync(CancellationToken cancelacion)
    {
        var alLado = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? ".", NombreLocalDelPaquete);
        if (File.Exists(alLado)) return (alLado, null);

        var adjunto = await Actualizador.BuscarAdjuntoAsync(PrefijoDelAdjunto, cancelacion);
        if (adjunto is null) return (null, "el último release no trae el paquete de la extensión.");

        var destino = Path.Combine(Path.GetTempPath(), adjunto.Nombre);
        var error = await Actualizador.DescargarAdjuntoAsync(adjunto, destino, cancelacion);

        return error is null ? (destino, null) : (null, $"no se pudo bajar la extensión: {error}");
    }

    /// <summary>
    /// Corre el instalador y espera a que termine. El código 1001 es "ya estaba instalada" y no es
    /// un error: se llega ahí reinstalando la misma versión.
    /// </summary>
    private static async Task<string?> CorrerElInstaladorAsync(string instalador, string argumentos, CancellationToken cancelacion)
    {
        try
        {
            using var proceso = Process.Start(new ProcessStartInfo(instalador, argumentos) { UseShellExecute = true });
            if (proceso is null) return "no se pudo abrir el instalador de extensiones.";

            await proceso.WaitForExitAsync(cancelacion);

            return proceso.ExitCode is 0 or 1001
                ? null
                : $"el instalador terminó con el código {proceso.ExitCode}. El detalle queda en %TEMP%\\dd_VSIXInstaller_*.log.";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>Corre un programa y devuelve lo que imprimió, o null si no se pudo.</summary>
    private static string? Ejecutar(string programa, string argumentos)
    {
        var arranque = new ProcessStartInfo(programa, argumentos)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proceso = Process.Start(arranque);
        if (proceso is null) return null;

        var salida = proceso.StandardOutput.ReadToEnd();
        proceso.WaitForExit();

        return salida;
    }
}
