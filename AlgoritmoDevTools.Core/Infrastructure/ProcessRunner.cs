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
            catch { /* proceso ya terminÃ³ o no se puede matar */ }
        });

        if (!string.IsNullOrEmpty(stdIn))
        {
            try
            {
                process.StandardInput.Write(stdIn);
                process.StandardInput.Close();
            }
            catch { /* puede haber sido matado mientras escribÃ­amos */ }
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
