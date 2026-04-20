using AlgoritmoDevTools.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

/// <summary>
/// Lista de dominios de SoftCerealCore editable por el usuario (usado por CommandsMaker para generar
/// comandos, y por ModelDriftChecker para elegir qué dominios chequear).
/// </summary>
public sealed class DomainRepository
{
    private readonly IToolStorage _storage;

    public DomainRepository(IToolStorage storage)
    {
        _storage = storage;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS DOMINIOS (Primary_Key INTEGER PRIMARY KEY, Nombre NVARCHAR(2048) NULL)";
        cmd.ExecuteNonQuery();
    }

    public List<string> GetAll()
    {
        var entries = new List<string>();
        using var db = _storage.OpenConnection();
        using var cmd = new SqliteCommand("SELECT Nombre FROM DOMINIOS ORDER BY Nombre", db);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(reader.GetString(0));
        }
        return entries;
    }

    public void Add(string nombre)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO DOMINIOS VALUES (NULL, @nombre)";
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.ExecuteNonQuery();
    }

    public void Remove(string nombre)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM DOMINIOS WHERE Nombre = @nombre";
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.ExecuteNonQuery();
    }
}
