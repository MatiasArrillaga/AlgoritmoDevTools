namespace AlgoritmoDevTools.Tools.ModelDriftChecker.Services;

public enum ChangeSeverity
{
    None = 0,
    Possible = 1,   // cambio en Domain: puede ser propiedad, lógica o comentario
    Likely = 2,     // configuración o convención probable
    Definite = 3    // DbContext, IEntityTypeConfiguration
}

public sealed record ClassifiedFile(string DisplayName, ChangeSeverity Severity, string Reason);

public sealed record ChangeCheckResult
{
    public string BaselineSha { get; init; } = string.Empty;
    public string HeadSha { get; init; } = string.Empty;
    public int CommitsBetween { get; init; }
    public IReadOnlyList<ClassifiedFile> RelevantFiles { get; init; } = Array.Empty<ClassifiedFile>();
    public string? Error { get; init; }

    public bool Success => Error is null;

    public ChangeSeverity HighestSeverity => RelevantFiles.Count == 0
        ? ChangeSeverity.None
        : RelevantFiles.Max(f => f.Severity);

    public static ChangeCheckResult Fail(string error) => new() { Error = error };
}
