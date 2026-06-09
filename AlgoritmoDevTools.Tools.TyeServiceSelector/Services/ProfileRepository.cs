using AlgoritmoDevTools.Core.Abstractions;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services;

/// <summary>
/// Persiste perfiles de selección: un nombre + la lista de servicios que quedan activos.
/// Los servicios se guardan como CSV (los nombres de servicio son identificadores simples sin comas).
/// </summary>
public sealed class ProfileRepository
{
    private readonly IToolStorage _storage;

    public ProfileRepository(IToolStorage storage)
    {
        _storage = storage;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Profiles (
                Name      TEXT PRIMARY KEY,
                Services  TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<string> GetProfileNames()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Name FROM Profiles ORDER BY Name COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));
        return names;
    }

    /// <summary>Servicios activos del perfil, o null si el perfil no existe.</summary>
    public IReadOnlyCollection<string>? GetProfileServices(string name)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Services FROM Profiles WHERE Name = @n COLLATE NOCASE";
        cmd.Parameters.AddWithValue("@n", name);
        if (cmd.ExecuteScalar() is not string csv)
            return null;

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    public void SaveProfile(string name, IEnumerable<string> enabledServices)
    {
        var csv = string.Join(',', enabledServices);
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Profiles (Name, Services, UpdatedAt)
            VALUES (@n, @s, @u)
            ON CONFLICT(Name) DO UPDATE SET Services = @s, UpdatedAt = @u;";
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@s", csv);
        cmd.Parameters.AddWithValue("@u", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteProfile(string name)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM Profiles WHERE Name = @n COLLATE NOCASE";
        cmd.Parameters.AddWithValue("@n", name);
        cmd.ExecuteNonQuery();
    }
}
