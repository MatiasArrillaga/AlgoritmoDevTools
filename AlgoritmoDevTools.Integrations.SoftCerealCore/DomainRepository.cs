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

        using var prefs = db.CreateCommand();
        prefs.CommandText = "CREATE TABLE IF NOT EXISTS PREFERENCIAS (Clave TEXT PRIMARY KEY, Valor TEXT NOT NULL)";
        prefs.ExecuteNonQuery();
    }

    /// <summary>
    /// Dominios usados recientemente, del más reciente al más viejo, en CSV.
    ///
    /// Lo necesita el menú contextual, que es estático y además tiene un presupuesto de 16 ítems
    /// para TODO el árbol (submenús incluidos): no puede ofrecer los 13 dominios por 3 comandos.
    /// Se lleva solo, por uso, en vez de pedir que alguien marque favoritos a mano.
    /// </summary>
    private const string CLAVE_RECIENTES = "DominiosRecientes";

    /// <summary>Cuántos se recuerdan. De más no sirve: el menú nunca va a poder mostrarlos.</summary>
    private const int MaxRecientes = 6;

    /// <summary>Dominios usados recientemente, del más reciente al más viejo.</summary>
    public List<string> GetRecientes()
    {
        var csv = LeerPreferencia(CLAVE_RECIENTES);
        if (string.IsNullOrWhiteSpace(csv)) return new List<string>();

        var vigentes = GetAll();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            // Un dominio borrado sigue en el CSV: se filtra acá en vez de salir a limpiarlo.
            .Where(d => vigentes.Contains(d))
            .ToList();
    }

    /// <summary>Último dominio usado, o null si todavía no se usó ninguno.</summary>
    public string? GetUltimoDominio() => GetRecientes().FirstOrDefault();

    /// <summary>Lo pone primero en la lista de recientes.</summary>
    public void RegistrarUso(string dominio)
    {
        if (string.IsNullOrWhiteSpace(dominio)) return;

        var recientes = GetRecientes()
            .Where(d => !string.Equals(d, dominio, StringComparison.OrdinalIgnoreCase))
            .ToList();

        recientes.Insert(0, dominio);
        GuardarPreferencia(CLAVE_RECIENTES, string.Join(",", recientes.Take(MaxRecientes)));
    }

    private string? LeerPreferencia(string clave)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Valor FROM PREFERENCIAS WHERE Clave = @clave";
        cmd.Parameters.AddWithValue("@clave", clave);
        return cmd.ExecuteScalar() as string;
    }

    private void GuardarPreferencia(string clave, string valor)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO PREFERENCIAS (Clave, Valor) VALUES (@clave, @valor)
                            ON CONFLICT(Clave) DO UPDATE SET Valor = @valor";
        cmd.Parameters.AddWithValue("@clave", clave);
        cmd.Parameters.AddWithValue("@valor", valor);
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
