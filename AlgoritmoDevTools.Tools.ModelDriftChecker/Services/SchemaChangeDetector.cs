using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.ModelDriftChecker.Services;

/// <summary>
/// Analiza el diff de archivos entre dos SHAs de git y clasifica qué archivos
/// podrían requerir una migración EF Core. Filtra eventos, rules, repos, enums y [NotMapped].
/// </summary>
public static class SchemaChangeDetector
{
    public static string AlgoritmoCoreRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "source", "repos", "AlgoritmoCore");

    private static readonly Regex DbContextRegex =
        new(@"Microservicios[\\/][^\\/]+[\\/]Algoritmo\.[^\\/]+\.Infrastructure[\\/].*DbContext[^\\/]*\.cs$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConfigurationRegex =
        new(@"Microservicios[\\/][^\\/]+[\\/]Algoritmo\.[^\\/]+\.Infrastructure[\\/].*Configuration[^\\/]*\.cs$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DomainFileRegex =
        new(@"Microservicios[\\/][^\\/]+[\\/]Algoritmo\.[^\\/]+\.Domain[\\/].*\.cs$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DomainFromPathRegex =
        new(@"Microservicios[\\/]([^\\/]+)[\\/]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EnumRegex =
        new(@"\bpublic\s+enum\s+\w+", RegexOptions.Compiled);

    private static readonly string[] IgnoredDomainSuffixes =
    {
        "Event",
        "Rule",
        "Repository",
        "RepositoryAsync",
        "Error",
        "Errors",
        "Request",
        "Requests",
        "Command",
        "Commands",
        "Handler",
        "Handlers",
        "Response",
        "Responses",
        "Query",
        "Queries"
    };

    public static string? GetCurrentHead(string repoPath)
    {
        try { return RunGit("rev-parse HEAD", repoPath).Trim(); }
        catch { return null; }
    }

    public static bool ShaExists(string repoPath, string sha)
    {
        try
        {
            RunGit($"rev-parse --verify {sha}^{{commit}}", repoPath);
            return true;
        }
        catch { return false; }
    }

    public static string DescribeCommit(string repoPath, string sha)
    {
        try
        {
            return RunGit($"log -1 --format=\"%h | %s | %cr\" {sha}", repoPath).Trim();
        }
        catch (Exception ex)
        {
            return $"{sha} (no se pudo leer: {ex.Message})";
        }
    }

    public static ChangeCheckResult Check(string repoPath, string baselineSha, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(Path.Combine(repoPath, ".git")))
            return ChangeCheckResult.Fail($"'{repoPath}' no es un repo git (no se encontró .git).");
        if (!ShaExists(repoPath, baselineSha))
            return ChangeCheckResult.Fail($"El baseline '{baselineSha}' no existe en el repo.");

        var head = GetCurrentHead(repoPath);
        if (head is null) return ChangeCheckResult.Fail("No se pudo obtener HEAD.");

        cancellationToken.ThrowIfCancellationRequested();

        string diffOutput;
        try
        {
            diffOutput = RunGit($"diff --name-only {baselineSha} {head}", repoPath, cancellationToken);
        }
        catch (Exception ex)
        {
            return ChangeCheckResult.Fail($"git diff falló: {ex.Message}");
        }

        var changedFiles = diffOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        // Clasificar + filtrar + dedup por (severidad, displayName).
        var classified = changedFiles
            .Select(f => ClassifyAndFilter(repoPath, f))
            .Where(c => c is not null)
            .Select(c => c!.Value)
            .GroupBy(x => (x.Severity, x.DisplayName), tuple => tuple)
            .Select(g => new ClassifiedFile(g.Key.DisplayName, g.Key.Severity, g.First().Reason))
            .OrderByDescending(c => c.Severity)
            .ThenBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int commitCount = CountCommitsBetween(repoPath, baselineSha, head);

        return new ChangeCheckResult
        {
            BaselineSha = baselineSha,
            HeadSha = head,
            CommitsBetween = commitCount,
            RelevantFiles = classified
        };
    }

    private static (ChangeSeverity Severity, string DisplayName, string Reason)? ClassifyAndFilter(string repoPath, string relPath)
    {
        if (DbContextRegex.IsMatch(relPath))
        {
            var domain = ExtractDomain(relPath);
            return (ChangeSeverity.Definite, domain, "DbContext");
        }

        if (ConfigurationRegex.IsMatch(relPath))
        {
            var filename = Path.GetFileNameWithoutExtension(relPath);
            const string suffix = "EntityConfiguration";
            var entity = filename.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? filename[..^suffix.Length]
                : filename;
            if (entity.EndsWith("Configuration", StringComparison.OrdinalIgnoreCase))
                entity = entity[..^"Configuration".Length];
            return (ChangeSeverity.Likely, entity, "EntityConfiguration");
        }

        if (DomainFileRegex.IsMatch(relPath))
        {
            var filename = Path.GetFileNameWithoutExtension(relPath);

            // Exclusiones por nombre: eventos, rules, repos, errors, CQRS (request/command/handler/response/query), etc.
            foreach (var suffix in IgnoredDomainSuffixes)
            {
                if (filename.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            // Interfaces de repositorio (I*Repository*)
            if (filename.StartsWith("I", StringComparison.Ordinal) && filename.Contains("Repository", StringComparison.OrdinalIgnoreCase))
                return null;

            // Exclusiones por contenido (enum, [NotMapped])
            var absPath = Path.Combine(repoPath, relPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absPath))
            {
                try
                {
                    var content = File.ReadAllText(absPath);
                    if (EnumRegex.IsMatch(content)) return null;
                    if (content.Contains("[NotMapped]", StringComparison.Ordinal)) return null;
                }
                catch { /* si no se puede leer, lo dejamos entrar */ }
            }

            return (ChangeSeverity.Possible, filename, "Entidad/dominio");
        }

        return null;
    }

    private static string ExtractDomain(string relPath)
    {
        var m = DomainFromPathRegex.Match(relPath);
        return m.Success ? m.Groups[1].Value : "?";
    }

    private static int CountCommitsBetween(string repoPath, string from, string to)
    {
        try
        {
            var output = RunGit($"rev-list --count {from}..{to}", repoPath).Trim();
            return int.TryParse(output, out var n) ? n : 0;
        }
        catch { return 0; }
    }

    private static string RunGit(string arguments, string workingDirectory, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("No se pudo iniciar git.");
        using var _ = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        cancellationToken.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);

        return stdout;
    }
}
