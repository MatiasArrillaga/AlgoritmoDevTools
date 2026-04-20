using Microsoft.Data.SqlClient;

namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public static class SQLService
{
    public class ConnectionData
    {
        public ConnectionData(string server, string user, string password)
        {
            Server = server;
            User = user;
            Password = password;
        }

        public ConnectionData(string server, string user, string password, string dataBase)
            : this(server, user, password)
        {
            DataBase = dataBase;
        }

        public string Server { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DataBase { get; set; } = string.Empty;
    }

    public static List<string> GetDatabases(ConnectionData args)
    {
        var databases = new List<string>();
        var connectionString =
            $"Server={args.Server};User Id={args.User};Password={args.Password};TrustServerCertificate=Yes";

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(
            "SELECT name FROM sys.databases WHERE state_desc = 'ONLINE' AND name NOT IN ('master','model','msdb','tempdb') ORDER BY name",
            connection
        );

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            databases.Add(reader.GetString(0));
        }

        return databases;
    }
}
