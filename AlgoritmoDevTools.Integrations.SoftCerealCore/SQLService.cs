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
    /// Un archivo de log, con lo que hace falta para decidir si conviene achicarlo.
    /// </summary>
    /// <param name="Database">La base a la que pertenece.</param>
    /// <param name="LogicalName">
    /// El nombre con el que lo conoce SQL Server, que es el que pide SHRINKFILE. <b>No tiene por
    /// qué parecerse al del archivo</b>: varias bases restauradas del mismo backup comparten el
    /// nombre lógico aunque sus archivos se llamen distinto.
    /// </param>
    /// <param name="PhysicalPath">El archivo en disco.</param>
    /// <param name="AllocatedBytes">Lo que ocupa.</param>
    /// <param name="UsedBytes">Lo que de eso está realmente en uso.</param>
    /// <param name="ReuseWait">
    /// Qué impide reusar el espacio. Con ACTIVE_TRANSACTION achicar no recupera nada y puede
    /// trabarse.
    /// </param>
    /// <param name="RecoveryModel">SIMPLE, FULL o BULK_LOGGED.</param>
    public sealed record LogFile(
        string Database,
        string LogicalName,
        string PhysicalPath,
        long AllocatedBytes,
        long UsedBytes,
        string ReuseWait,
        string RecoveryModel);

    public sealed record GetLogFilesResult(IReadOnlyList<LogFile> Logs, string? Error)
    {
        public bool IsSuccess => Error is null;
        public static GetLogFilesResult Ok(IReadOnlyList<LogFile> logs) => new(logs, null);
        public static GetLogFilesResult Fail(string error) => new(Array.Empty<LogFile>(), error);
    }

    /// <summary>
    /// Los archivos de log de las bases indicadas. Nunca tira.
    ///
    /// <b>Se conecta a cada base y pregunta ahí adentro.</b> No es un rodeo: <c>sys.database_files</c>
    /// es por base, y preguntarle a master da los nombres lógicos de master. Varias bases salidas
    /// del mismo backup tienen todas el mismo nombre lógico de log, así que resolverlo desde afuera
    /// termina apuntando al archivo equivocado.
    ///
    /// Una base puede tener más de un log y por eso se devuelven todas las filas: un archivo suelto
    /// que no figure acá no es necesariamente una base huérfana.
    ///
    /// Una base que no se pueda abrir se saltea; las demás se devuelven igual.
    /// </summary>
    public static GetLogFilesResult TryGetLogFiles(ConnectionData args, IEnumerable<string> databases)
    {
        if (string.IsNullOrWhiteSpace(args.Server)) return GetLogFilesResult.Fail("El servidor es obligatorio.");

        const string consulta = @"
            SELECT  f.name,
                    f.physical_name,
                    CAST(f.size AS bigint) * 8192,
                    CAST(ISNULL(FILEPROPERTY(f.name, 'SpaceUsed'), 0) AS bigint) * 8192,
                    d.log_reuse_wait_desc,
                    d.recovery_model_desc
            FROM    sys.database_files f
            JOIN    sys.databases d ON d.name = DB_NAME()
            WHERE   f.type = 1;";

        var logs = new List<LogFile>();
        string? ultimoError = null;

        foreach (var baseDeDatos in databases)
        {
            if (string.IsNullOrWhiteSpace(baseDeDatos)) continue;

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = args.Server,
                InitialCatalog = baseDeDatos,
                TrustServerCertificate = true,
                ConnectTimeout = 10
            };

            if (args.UseIntegratedSecurity)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = args.User;
                builder.Password = args.Password;
            }

            try
            {
                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();

                using var command = new SqlCommand(consulta, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(new LogFile(
                        baseDeDatos,
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetInt64(2),
                        reader.GetInt64(3),
                        reader.IsDBNull(4) ? "NOTHING" : reader.GetString(4),
                        reader.IsDBNull(5) ? string.Empty : reader.GetString(5)));
                }
            }
            catch (Exception ex)
            {
                ultimoError = ex.Message;
            }
        }

        return logs.Count == 0 && ultimoError is not null
            ? GetLogFilesResult.Fail(ultimoError)
            : GetLogFilesResult.Ok(logs);
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
            if (string.IsNullOrWhiteSpace(args.Password)) return GetDatabasesResult.Fail("La contraseña es obligatoria.");
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
