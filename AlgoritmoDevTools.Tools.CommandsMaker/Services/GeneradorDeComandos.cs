using AlgoritmoDevTools.Integrations.SoftCerealCore;

namespace AlgoritmoDevTools.Tools.CommandsMaker.Services;

/// <summary>
/// Arma los comandos de migración que se pegan en la Package Manager Console.
///
/// Estaba adentro de <c>CommandsMakerView</c> como constantes privadas. Se sacó acá porque ahora
/// hay dos consumidores —la pantalla y el menú contextual del explorador— y porque así se puede
/// verificar lo que genera sin instanciar un UserControl.
/// </summary>
public static class GeneradorDeComandos
{
    private const string DOMINIO_TOKEN = "*DOMINIO*";
    private const string MIGRATION_NAME = "*MIGRATION_NAME*";
    private const string CONNECTION_STRING = "*CONNECTION_STRING*";

    private const string PROJECT = "Algoritmo." + DOMINIO_TOKEN + ".Infrastructure";

    /// <summary>
    /// Cola fija de la connection string, compartida por los tres comandos. Encrypt y
    /// TrustServerCertificate son obligatorios: Microsoft.Data.SqlClient cifra la conexión por
    /// defecto y el SQL de desarrollo usa un certificado autofirmado, así que sin ellos el
    /// comando falla con "The certificate chain was issued by an authority that is not trusted".
    /// </summary>
    private const string CONNECTION_SUFFIX =
        "Integrated Security = true; MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;";

    // CS usada por PM Console: Server y Database del secreto Development + Integrated Security (Windows user con permisos SA).
    private const string FALLBACK_CONNECTION_STRING =
        "Server=localhost,1433;Database=Algoritmo;" + CONNECTION_SUFFIX;

    private const string COMMON_COMMAND = "-Context " + DOMINIO_TOKEN + "DbContext -Project " + PROJECT + " -StartupProject " + PROJECT;
    private const string ARGS = " -Args '--Connection \"" + CONNECTION_STRING + "\"'";

    private const string AddMigrationTemplate = "add-migration " + MIGRATION_NAME + " " + COMMON_COMMAND + ARGS;
    private const string RmvMigrationTemplate = "remove-migration -force " + COMMON_COMMAND + ARGS;

    // update-database manda la cadena DOS veces a propósito y no es redundante: la de -Args la lee
    // AlgoritmoMetadataExplorer para armar el inventario en tiempo de diseño, y la de -Connection la
    // necesita EF para abrir la conexión real, porque el factory arma el contexto con UseSqlServer()
    // sin cadena. Sacar cualquiera de las dos rompe el comando.
    private const string UpdateDbTemplate = "update-database " + COMMON_COMMAND + " -Connection \"" + CONNECTION_STRING + "\"" + ARGS;

    public static string AddMigration(string dominio, string nombreDeMigracion)
        => Armar(AddMigrationTemplate.Replace(MIGRATION_NAME, nombreDeMigracion), dominio);

    public static string RemoveMigration(string dominio) => Armar(RmvMigrationTemplate, dominio);

    public static string UpdateDatabase(string dominio) => Armar(UpdateDbTemplate, dominio);

    /// <summary>
    /// Nombre sugerido para una migración nueva: <c>[Dominio].MIG20261001-143052</c>.
    ///
    /// Lleva la marca temporal y no un texto fijo para que dos migraciones seguidas no choquen y
    /// para que la carpeta quede ordenada por nombre en el mismo orden en que se crearon.
    /// </summary>
    public static string NombreDeMigracionSugerido(string dominio, bool conPrefijoDeDominio = true)
    {
        var prefijo = conPrefijoDeDominio && !string.IsNullOrWhiteSpace(dominio) ? $"[{dominio}]." : string.Empty;
        return prefijo + "MIG" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }

    /// <summary>
    /// Toma Server y Database del secreto Development. La autenticación se fuerza a Integrated
    /// Security porque PM Console corre con el user de Windows (que tiene SA en dev).
    /// </summary>
    public static string ResolveConnectionString()
    {
        var dev = SecretService.Shared.GetConnectionString(Constantes.SecretKeys.Development);
        if (string.IsNullOrEmpty(dev)) return FALLBACK_CONNECTION_STRING;

        var parts = ConnectionStringParser.Parse(dev);
        var server = parts.GetValueOrDefault("Server");
        var database = parts.GetValueOrDefault("Database");
        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database)) return FALLBACK_CONNECTION_STRING;

        return $"Server={server};Database={database};" + CONNECTION_SUFFIX;
    }

    private static string Armar(string plantilla, string dominio)
        => plantilla
            .Replace(CONNECTION_STRING, ResolveConnectionString())
            .Replace(DOMINIO_TOKEN, dominio);
}
