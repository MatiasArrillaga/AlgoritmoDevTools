using System.Diagnostics;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Wrapper para ejecutar procesos externos (powershell, dotnet, etc.) capturando stdout/stderr.
/// </summary>
public static class ProcessRunner
{
    public static string RunPowerShell(string command, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };

        return Execute(psi, stdIn: null);
    }

    public static string RunDotnet(string arguments, string? stdIn = null, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardInput = stdIn is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };

        return Execute(psi, stdIn);
    }

    private static string Execute(ProcessStartInfo psi, string? stdIn)
    {
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar '{psi.FileName}'.");

        if (!string.IsNullOrEmpty(stdIn))
        {
            process.StandardInput.Write(stdIn);
            process.StandardInput.Close();
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? output : error);

        return output;
    }
}