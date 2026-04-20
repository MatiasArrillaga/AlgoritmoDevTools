using Microsoft.Data.SqlClient;

namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public static class SQLService
{
    public class ConnectionData
    {
        public ConnectionData(string server, string user, string password, bool useIntegratedSecurity = false)
        {
            Server = server;
            User = user;
            Password = password;
            UseIntegratedSecurity = useIntegratedSecurity;
        }

        public ConnectionData(string server, string user, string password, string dataBase, bool useIntegratedSecurity = false)
            : this(server, user, password, useIntegratedSecurity)
        {
            DataBase = dataBase;
        }

        public string Server { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DataBase { get; set; } = string.Empty;
        public bool UseIntegratedSecurity { get; set; }
    }

    public sealed record GetDatabasesResult(IReadOnlyList<string> Databases, string? Error)
    {
        public bool IsSuccess => Error is null;
        public static GetDatabasesResult Ok(IReadOnlyList<string> databases) => new(databases, null);
        public static GetDatabasesResult Fail(string error) => new(Array.Empty<string>(), error);
    }

    /// <summary>
    /// Verifica que se pueda abrir una conexión con las credenciales dadas (sin requerir una BD específica).
    /// Devuelve null si OK, o un mensaje de error.
    /// </summary>
    public static string? TryTestConnection(ConnectionData args)
    {
        if (string.IsNullOrWhiteSpace(args.Server)) return "El servidor es obligatorio.";
        if (!args.UseIntegratedSecurity)
        {
            if (string.IsNullOrWhiteSpace(args.User)) return "El usuario es obligatorio.";
            if (string.IsNullOrWhiteSpace(args.Password)) return "La contraseña es obligatoria.";
        }

        var cs = args.UseIntegratedSecurity
            ? $"Server={args.Server};Integrated Security=true;TrustServerCertificate=Yes;Connect Timeout=8"
            : $"Server={args.Server};User Id={args.User};Password={args.Password};TrustServerCertificate=Yes;Connect Timeout=8";

        try
        {
            using var conn = new SqlConnection(cs);
            conn.Open();
            return null;
        }
        catch (SqlException ex) { return ex.Message; }
        catch (InvalidOperationException ex) { return ex.Message; }
        catch (Exception ex) { return $"Error inesperado: {ex.Message}"; }
    }

    /// <summary>
    /// Intenta listar las bases de datos del servidor. Nunca tira: devuelve un resultado con Error poblado si falla.
    /// </summary>
    public static GetDatabasesResult TryGetDatabases(ConnectionData args)
    {
        if (string.IsNullOrWhiteSpace(args.Server)) return GetDatabasesResult.Fail("El servidor es obligatorio.");
        if (!args.UseIntegratedSecurity)
        {
            if (string.IsNullOrWhiteSpace(args.User)) return GetDatabasesResult.Fail("El usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(args.Password)) return GetDatabasesResult.Fail("La contraseÃ±a es obligatoria.");
        }

        var connectionString = args.UseIntegratedSecurity
            ? $"Server={args.Server};Integrated Security=true;TrustServerCertificate=Yes;Connect Timeout=10"
            : $"Server={args.Server};User Id={args.User};Password={args.Password};TrustServerCertificate=Yes;Connect Timeout=10";

        try
        {
            var databases = new List<string>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE state_desc = 'ONLINE' AND name NOT IN ('master','model','msdb','tempdb') ORDER BY name",
                connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                databases.Add(reader.GetString(0));
            }
            return GetDatabasesResult.Ok(databases);
        }
        catch (SqlException ex)
        {
            return GetDatabasesResult.Fail(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return GetDatabasesResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return GetDatabasesResult.Fail($"Error inesperado: {ex.Message}");
        }
    }
}
