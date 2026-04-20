using AlgoritmoDevTools.Core.Abstractions;

namespace AlgoritmoDevTools.Tools.ModelDriftChecker.Services;

/// <summary>
/// Persiste, por repo, el SHA que el usuario confirmó como "ya migré hasta acá".
/// </summary>
public sealed class BaselineRepository
{
    private readonly IToolStorage _storage;

    public BaselineRepository(IToolStorage storage)
    {
        _storage = storage;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS SchemaBaseline (
                RepoPath TEXT PRIMARY KEY,
                Sha      TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    public string? GetBaseline(string repoPath)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Sha FROM SchemaBaseline WHERE RepoPath = @p COLLATE NOCASE";
        cmd.Parameters.AddWithValue("@p", repoPath);
        return cmd.ExecuteScalar() as string;
    }

    public void SetBaseline(string repoPath, string sha)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO SchemaBaseline (RepoPath, Sha, UpdatedAt)
            VALUES (@p, @s, @u)
            ON CONFLICT(RepoPath) DO UPDATE SET Sha = @s, UpdatedAt = @u;";
        cmd.Parameters.AddWithValue("@p", repoPath);
        cmd.Parameters.AddWithValue("@s", sha);
        cmd.Parameters.AddWithValue("@u", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }
}
