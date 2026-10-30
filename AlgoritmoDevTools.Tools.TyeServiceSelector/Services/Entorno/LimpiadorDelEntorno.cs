using System.Text.RegularExpressions;

using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>Un proceso de sidecar que quedó vivo sin la aplicación que acompañaba.</summary>
/// <param name="Pid">Proceso.</param>
/// <param name="AppId">Qué servicio acompañaba, leído de su línea de comandos.</param>
public sealed record SidecarHuerfano(int Pid, string AppId);

/// <summary>Lo que sobró de entornos anteriores.</summary>
public sealed record RestosDelEntorno(
    IReadOnlyList<SidecarHuerfano> Sidecars,
    IReadOnlyList<string> Contenedores,
    IReadOnlyList<string> Redes)
{
    public int Total => Sidecars.Count + Contenedores.Count + Redes.Count;

    public bool HayAlgo => Total > 0;
}

/// <summary>
/// Barre lo que queda colgado cuando el entorno se baja.
///
/// <para>
/// <b>Qué queda y por qué.</b> Tye lanza cada sidecar como <c>powershell → python → dapr → daprd</c>
/// y al bajar mata al abuelo, no al nieto: los <c>daprd</c> siguen vivos, quedándose con los
/// puertos que el próximo arranque necesita. El síntoma no es «quedó basura», es que la próxima vez
/// el entorno levanta mal y la causa está en otro lado. Las redes de Docker quedan cuando tye no
/// cerró bien, que es de donde salieron las cuatro acumuladas entre el 19 y el 26 de octubre.
/// </para>
///
/// <para>
/// <b>Qué NO se toca.</b> Los contenedores de <c>dapr init</c> —placement, scheduler, redis,
/// zipkin— son infraestructura del usuario y tienen que seguir. El front que haya levantado a mano,
/// tampoco: no es del entorno.
/// </para>
///
/// <para>
/// <b>El criterio.</b> Un sidecar está huérfano si su app-id no figura en <b>ningún</b> panel vivo.
/// No alcanza con «no corre tye»: cuando hay un servicio agregado hay dos instancias, y barrer por
/// nombre de proceso se llevaría puesto el entorno que sigue en pie.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public sealed class LimpiadorDelEntorno(IProveedorDeEntorno entorno)
{
    private static readonly Regex AppIdEnLaLinea = new(@"--app-id\s+(\S+)", RegexOptions.Compiled);

    public async Task<RestosDelEntorno> RevisarAsync(CancellationToken cancelacion = default)
    {
        var vivos = (await entorno.ObtenerEstadoAsync(cancelacion))
            .Paneles
            .SelectMany(p => p.Servicios)
            .Select(s => s.Nombre)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sidecars = SidecarsVivos()
            .Where(s => !vivos.Contains(s.AppId))
            .ToList();

        var hayEntorno = vivos.Count > 0;

        return new RestosDelEntorno(
            sidecars,
            // Los contenedores de tye sólo sobran si no quedó ninguna instancia: mientras haya una,
            // los suyos están en uso.
            hayEntorno ? [] : ContenedoresDeTye(),
            RedesSinContenedores());
    }

    /// <summary>Devuelve cuántas cosas se limpiaron.</summary>
    public async Task<int> LimpiarAsync(CancellationToken cancelacion = default)
    {
        var restos = await RevisarAsync(cancelacion);
        var limpiadas = 0;

        foreach (var sidecar in restos.Sidecars)
        {
            if (MatarElArbol(sidecar.Pid)) limpiadas++;
        }

        foreach (var contenedor in restos.Contenedores)
        {
            if (Docker("rm", "-f", contenedor) is not null) limpiadas++;
        }

        foreach (var red in restos.Redes)
        {
            if (Docker("network", "rm", red) is not null) limpiadas++;
        }

        return limpiadas;
    }

    // --- Lo que hay dando vueltas --------------------------------------------

    /// <summary>
    /// Los <c>daprd</c> corriendo, con el servicio que acompañan. El app-id sale de la línea de
    /// comandos porque es el único lugar donde está: el proceso se llama igual para todos.
    /// </summary>
    private static IReadOnlyList<SidecarHuerfano> SidecarsVivos()
    {
        var salida = PowerShell(
            "Get-CimInstance Win32_Process -Filter \\\"Name='daprd.exe'\\\" | " +
            "ForEach-Object { $_.ProcessId.ToString() + ' ' + $_.CommandLine }");

        return salida is null ? [] : LeerSidecars(salida);
    }

    /// <summary>Separado de la consulta para poder probarlo con una salida guardada.</summary>
    public static IReadOnlyList<SidecarHuerfano> LeerSidecars(string salida)
    {
        var sidecars = new List<SidecarHuerfano>();

        foreach (var linea in salida.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var limpia = linea.Trim();
            var corte = limpia.IndexOf(' ');
            if (corte <= 0) continue;

            if (!int.TryParse(limpia[..corte], out var pid)) continue;

            var appId = AppIdEnLaLinea.Match(limpia);
            if (appId.Success) sidecars.Add(new SidecarHuerfano(pid, appId.Groups[1].Value));
        }

        return sidecars;
    }

    private static IReadOnlyList<string> ContenedoresDeTye()
    {
        var salida = Docker("ps", "-a", "--format", "{{.Names}}");
        if (salida is null) return [];

        return salida
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // Los proxies de tye y el seq que levanta la extensión; nunca los de dapr init.
            .Where(nombre => nombre.Contains("-proxy_", StringComparison.OrdinalIgnoreCase)
                || nombre.StartsWith("seq_", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<string> RedesSinContenedores()
    {
        var salida = Docker("network", "ls", "--format", "{{.Name}}");
        if (salida is null) return [];

        var huerfanas = new List<string>();

        foreach (var red in salida.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!red.StartsWith("tye_network", StringComparison.OrdinalIgnoreCase)) continue;

            var cuantos = Docker("network", "inspect", red, "--format", "{{len .Containers}}");
            if (cuantos?.Trim() == "0") huerfanas.Add(red);
        }

        return huerfanas;
    }

    // --- Herramientas --------------------------------------------------------

    private static bool MatarElArbol(int pid)
    {
        // Con /T para llevarse también al python y al powershell que cuelgan del sidecar, que es
        // justamente la cadena que tye deja a medio matar.
        var resultado = ProcessRunner.RunProcess("taskkill", ["/PID", pid.ToString(), "/T", "/F"]);
        return resultado.ExitCode == 0;
    }

    private static string? Docker(params string[] argumentos)
    {
        try
        {
            var resultado = ProcessRunner.RunProcess("docker", argumentos);
            return resultado.ExitCode == 0 ? resultado.StdOut : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? PowerShell(string comando)
    {
        try
        {
            var resultado = ProcessRunner.RunProcess("powershell", ["-NoProfile", "-Command", comando]);
            return resultado.ExitCode == 0 ? resultado.StdOut : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
