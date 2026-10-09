namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public static class Constantes
{
    public const string ServiceProyectName = "Algoritmo.Microservices.Shared.API";
    public const string DefaultConectionDataFile = "SoftCerealCore.Secrets.json";
    public const string DefaultConectionString = $"Server={ConstKeyWords.ServerKey};Database={ConstKeyWords.DataBaseKey};User Id={ConstKeyWords.UserKey};Password={ConstKeyWords.PasswordKey}";
    public const string SecretConectionStringName = $"SoftCerealCore.{ConstKeyWords.SecretName}.ConnectionString";
    public const string NoHaySecretosMessage = "No secrets configured for this application";

    /// <summary>
    /// Las cadenas que se reescriben al aplicar una conexion. Son las unicas que siguen al servidor
    /// y la base elegidos; el resto se arrastra tal como estaba.
    ///
    /// Esta lista manda en dos lugares a la vez: que reescribe <c>SetSecrets</c> y que grupo va
    /// primero en pantalla. Agregar un tipo aca alcanza para las dos cosas.
    /// </summary>
    public static IReadOnlyList<string> SecretTypesModificables { get; } = new List<string>
    {
        SecretKeys.Development,
        SecretKeys.DAPR,
        SecretKeys.Seguridad
    };

    /// <summary>Las que la herramienta conoce pero nunca reescribe: se preservan como esten.</summary>
    public static IReadOnlyList<string> SecretTypesFijos { get; } = new List<string>
    {
        SecretKeys.Staging,
        SecretKeys.Production
    };

    /// <summary>
    /// Todas las cadenas administradas, en el orden en que se muestran: primero las modificables,
    /// despues las fijas.
    /// </summary>
    public static IReadOnlyList<string> SecretTypes { get; } =
        SecretTypesModificables.Concat(SecretTypesFijos).ToList();

    public static class SecretKeys
    {
        public const string Development = "Development";
        public const string Staging = "Staging";
        public const string Production = "Production";
        public const string DAPR = "DAPR";

        /// <summary>
        /// Base del módulo de seguridad. Se reescribe junto con Development y DAPR, pero su cadena
        /// tiene otra forma: va por <see cref="ParametrosDeSeguridad"/>, no por usuario y clave.
        /// </summary>
        public const string Seguridad = "Seguridad";
    }

    public static class ConstKeyWords
    {
        public const string ServerKey = "@SERVER@";
        public const string DataBaseKey = "@DATABASE@";
        public const string UserKey = "@USER@";
        public const string PasswordKey = "@PASS@";
        public const string SecretName = "@SECRET_NAME@";
    }

    public static string GetSecretKey(string secretType)
        => SecretConectionStringName.Replace(ConstKeyWords.SecretName, secretType);

    /// <summary>
    /// Cola de cifrado que llevan los secretos de Development, DAPR, Staging y Production. Tiene que coincidir con la del archivo de
    /// restauración (<see cref="DefaultConectionDataFile"/>), porque "Restaurar Secretos" y
    /// "Modificar Secreto" escriben las mismas claves: si no coinciden, la cadena cambia de forma
    /// según cuál de los dos botones tocaste.
    ///
    /// <c>Encrypt=False</c> no es decorativo: Microsoft.Data.SqlClient cifra por defecto desde la
    /// versión 4, así que sin esto la conexión al SQL de desarrollo falla por el certificado
    /// autofirmado. Antes DAPR se quedaba sin la cola y Development la recibía como
    /// <c>TrustServerCertificate=Yes</c>, que no incluye el <c>Encrypt</c>.
    /// </summary>
    public const string ParametrosDeCifrado = ";TrustServerCertificate=True;Encrypt=False";

    /// <summary>
    /// Cola del secreto de Seguridad, que no lleva usuario ni clave: autentica con la cuenta de
    /// Windows del desarrollador.
    ///
    /// Está escrita exactamente como aparece en el archivo de restauración — minúsculas incluidas,
    /// y sin <c>Encrypt</c>, al revés que <see cref="ParametrosDeCifrado"/>. Vale la misma regla
    /// que allá: si las dos formas no coinciden carácter por carácter, la cadena cambia según
    /// hayas tocado "Restaurar Secretos" o "Modificar Secreto".
    /// </summary>
    public const string ParametrosDeSeguridad = ";Integrated Security=true;TrustServerCertificate=true";

    /// <summary>Cadena del secreto de Seguridad: de la conexión elegida sólo toma servidor y base.</summary>
    private const string SeguridadConnectionString = $"Server={ConstKeyWords.ServerKey};Database={ConstKeyWords.DataBaseKey}";

    public static string GetConnectionString(SQLService.ConnectionData connectionData, string? secretType = "")
    {
        // Seguridad sale por otra rama entera, no por un Replace más: no lleva User Id ni Password,
        // así que la plantilla de los otros cuatro dejaría los placeholders @USER@ y @PASS@ puestos.
        if (string.Equals(secretType, SecretKeys.Seguridad))
            return SeguridadConnectionString
                .Replace(ConstKeyWords.ServerKey, connectionData.Server)
                .Replace(ConstKeyWords.DataBaseKey, connectionData.DataBase)
                + ParametrosDeSeguridad;

        return DefaultConectionString
            .Replace(ConstKeyWords.ServerKey, connectionData.Server)
            .Replace(ConstKeyWords.DataBaseKey, connectionData.DataBase)
            .Replace(ConstKeyWords.UserKey, !string.Equals(secretType, SecretKeys.DAPR)
                ? connectionData.User
                : "dapr")
            .Replace(ConstKeyWords.PasswordKey, connectionData.Password)
            + ParametrosDeCifrado;
    }
}
