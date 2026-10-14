using System.Diagnostics;
using System.Text;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Wrapper para ejecutar procesos externos (powershell, dotnet, etc.) capturando stdout/stderr en UTF-8.
/// Soporta cancelación vía <see cref="CancellationToken"/>: mata el proceso si se cancela.
/// </summary>
public static class ProcessRunner
{
    public static string RunPowerShell(string command, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };

        return Execute(psi, stdIn: null, cancellationToken);
    }

    public static string RunDotnet(string arguments, string? stdIn = null, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardInput = stdIn is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

        return Execute(psi, stdIn, cancellationToken);
    }

    /// <summary>
    /// Lo que devolvió un proceso, sin interpretarlo.
    /// </summary>
    public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

    /// <summary>
    /// Corre un ejecutable y devuelve su salida <b>sin tirar por el código de salida</b>.
    ///
    /// Existe porque <see cref="RunPowerShell"/> y <see cref="RunDotnet"/> convierten cualquier
    /// ExitCode distinto de cero en excepción, y hay herramientas —powercfg, docker, wsl— que
    /// devuelven códigos no-cero que no son errores sino respuestas. Para esas, la excepción tapa
    /// el dato.
    ///
    /// No reemplaza a los otros dos: ellos siguen tirando, que es de lo que dependen las
    /// herramientas que ya los usan.
    /// </summary>
    public static ProcessResult RunProcess(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };

        // Por lista y no concatenando: así una ruta con espacios no necesita comillas ni escapes, y
        // nada de lo que se pasa se reinterpreta por el camino.
        foreach (var argumento in arguments) psi.ArgumentList.Add(argumento);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar '{fileName}'.");

        using var _ = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch { /* ya termino o no se puede matar */ }
        });

        // Los dos pipes se leen en paralelo y antes de esperar: si uno se llena, el proceso se
        // bloquea escribiendo y la espera no termina nunca.
        var salida = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errores = process.StandardError.ReadToEndAsync(cancellationToken);

        process.WaitForExit();
        cancellationToken.ThrowIfCancellationRequested();

        return new ProcessResult(process.ExitCode, salida.Result, errores.Result);
    }

    private static string Execute(ProcessStartInfo psi, string? stdIn, CancellationToken cancellationToken)
    {
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar '{psi.FileName}'.");

        using var _ = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch { /* proceso ya terminó o no se puede matar */ }
        });

        if (!string.IsNullOrEmpty(stdIn))
        {
            try
            {
                process.StandardInput.Write(stdIn);
                process.StandardInput.Close();
            }
            catch { /* puede haber sido matado mientras escribíamos */ }
        }

        string output = string.Empty;
        string error = string.Empty;
        try
        {
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
        }
        catch when (cancellationToken.IsCancellationRequested) { /* ignorar */ }

        process.WaitForExit();

        cancellationToken.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? output : error);

        return output;
    }
}
