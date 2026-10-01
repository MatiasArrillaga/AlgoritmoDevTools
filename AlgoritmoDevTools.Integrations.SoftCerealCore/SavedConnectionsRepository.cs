using AlgoritmoDevTools.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public sealed class SavedConnectionsRepository
{
    private readonly IToolStorage _storage;

    public SavedConnectionsRepository(IToolStorage storage)
    {
        _storage = storage;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var db = _storage.OpenConnection();

        using (var create = db.CreateCommand())
        {
            create.CommandText = @"
                CREATE TABLE IF NOT EXISTS SavedConnections (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Server   TEXT NOT NULL,
                    DataBase TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    Password TEXT NOT NULL,
                    UseIntegratedSecurity INTEGER NOT NULL DEFAULT 0,
                    UNIQUE(Server, DataBase, UserName)
                );";
            create.ExecuteNonQuery();
        }

        // Migración: agregar la columna en BDs viejas que la crearon sin ella.
        if (!ColumnExists(db, "SavedConnections", "UseIntegratedSecurity"))
        {
            using var alter = db.CreateCommand();
            alter.CommandText = "ALTER TABLE SavedConnections ADD COLUMN UseIntegratedSecurity INTEGER NOT NULL DEFAULT 0";
            alter.ExecuteNonQuery();
        }

        if (!ColumnExists(db, "SavedConnections", "LastDataBase"))
        {
            using var alter = db.CreateCommand();
            alter.CommandText = "ALTER TABLE SavedConnections ADD COLUMN LastDataBase TEXT NOT NULL DEFAULT ''";
            alter.ExecuteNonQuery();
        }
    }

    private static bool ColumnExists(SqliteConnection db, string table, string column)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public List<SavedConnection> GetAll()
    {
        var items = new List<SavedConnection>();
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Id, Server, DataBase, UserName, Password, UseIntegratedSecurity, LastDataBase FROM SavedConnections ORDER BY Server, DataBase, UserName";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new SavedConnection
            {
                Id = reader.GetInt32(0),
                Server = reader.GetString(1),
                DataBase = reader.GetString(2),
                User = reader.GetString(3),
                Password = reader.GetString(4),
                UseIntegratedSecurity = reader.GetInt64(5) != 0,
                LastDataBase = reader.GetString(6)
            });
        }
        return items;
    }

    public bool Exists(string server, string dataBase, string user, int? excludeId = null)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = excludeId is null
            ? "SELECT COUNT(1) FROM SavedConnections WHERE Server = @s AND DataBase = @d AND UserName = @u"
            : "SELECT COUNT(1) FROM SavedConnections WHERE Server = @s AND DataBase = @d AND UserName = @u AND Id <> @id";
        cmd.Parameters.AddWithValue("@s", server);
        cmd.Parameters.AddWithValue("@d", dataBase);
        cmd.Parameters.AddWithValue("@u", user);
        if (excludeId is not null) cmd.Parameters.AddWithValue("@id", excludeId.Value);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Inserta una conexión nueva. Tira si ya existe la combinación (Server, DataBase, User).
    /// Usar <see cref="Exists"/> antes si querés evitar la excepción.
    /// </summary>
    public void Add(SavedConnection connection)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO SavedConnections (Server, DataBase, UserName, Password, UseIntegratedSecurity, LastDataBase)
                            VALUES (@server, @database, @user, @password, @integrated, @lastdb)";
        cmd.Parameters.AddWithValue("@server", connection.Server);
        cmd.Parameters.AddWithValue("@database", connection.DataBase);
        cmd.Parameters.AddWithValue("@user", connection.User ?? string.Empty);
        cmd.Parameters.AddWithValue("@password", connection.Password ?? string.Empty);
        cmd.Parameters.AddWithValue("@integrated", connection.UseIntegratedSecurity ? 1 : 0);
        cmd.Parameters.AddWithValue("@lastdb", connection.LastDataBase ?? string.Empty);
        cmd.ExecuteNonQuery();
    }

    public void Update(int id, SavedConnection connection)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            UPDATE SavedConnections
            SET Server = @server,
                DataBase = @database,
                UserName = @user,
                Password = @password,
                UseIntegratedSecurity = @integrated,
                LastDataBase = @lastdb
            WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@server", connection.Server);
        cmd.Parameters.AddWithValue("@database", connection.DataBase ?? string.Empty);
        cmd.Parameters.AddWithValue("@user", connection.User ?? string.Empty);
        cmd.Parameters.AddWithValue("@password", connection.Password ?? string.Empty);
        cmd.Parameters.AddWithValue("@integrated", connection.UseIntegratedSecurity ? 1 : 0);
        cmd.Parameters.AddWithValue("@lastdb", connection.LastDataBase ?? string.Empty);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deja registrada la base contra la que se acaban de aplicar los secretos. Se toca sólo esa
    /// columna a propósito: no forma parte de la clave única, así que no puede chocar con otra fila
    /// ni cambiar la identidad de la conexión.
    /// </summary>
    public void UpdateLastDataBase(int id, string dataBase)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "UPDATE SavedConnections SET LastDataBase = @lastdb WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@lastdb", dataBase ?? string.Empty);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Devuelve una conexión por Id, o null si ya no existe.</summary>
    public SavedConnection? GetById(int id) => GetAll().FirstOrDefault(c => c.Id == id);

    public void Delete(int id)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM SavedConnections WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
