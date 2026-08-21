using System.Diagnostics;
using System.Text;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Services;

/// <summary>
/// Ejecuta pandoc capturando stderr en UTF-8. No se usa <c>ProcessRunner</c> del Core porque ese
/// wrapper expone dotnet y powershell, y llamar a pandoc a traves de powershell obligaria a escapar
/// rutas con espacios y comillas. Se replica el mismo criterio: sin ventana, UTF-8 y muerte del
/// arbol de procesos al cancelar.
/// </summary>
internal static class PandocRunner
{
    /// <summary>
    /// Busca el ejecutable de pandoc. Primero al lado del Shell (para poder repartir el .exe con
    /// pandoc en la misma carpeta), despues la instalacion de winget, y por ultimo el PATH.
    /// Devuelve null si no esta en ninguna parte.
    /// </summary>
    public static string? Locate()
    {
        var alLado = Path.Combine(AppContext.BaseDirectory, "pandoc.exe");
        if (File.Exists(alLado)) return alLado;

        var winget = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pandoc", "pandoc.exe");
        if (File.Exists(winget)) return winget;

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;

            try
            {
                var candidato = Path.Combine(dir.Trim(), "pandoc.exe");
                if (File.Exists(candidato)) return candidato;
            }
            catch (ArgumentException)
            {
                // Una entrada invalida del PATH no tiene que cortar la busqueda.
            }
        }

        return null;
    }

    /// <summary>
    /// Corre pandoc. Devuelve null si salio bien, o el mensaje de error si fallo.
    /// </summary>
    public static string? TryRun(
        string pandocPath,
        IEnumerable<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = pandocPath,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return "No se pudo iniciar pandoc.";

            using var _ = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch { /* ya termino o no se puede matar */ }
            });

            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            cancellationToken.ThrowIfCancellationRequested();

            if (process.ExitCode != 0)
            {
                return PrimeraLinea(error);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string PrimeraLinea(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "pandoc devolvio un error sin detalle.";

        foreach (var linea in texto.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(linea)) return linea.Trim();
        }

        return texto.Trim();
    }
}
