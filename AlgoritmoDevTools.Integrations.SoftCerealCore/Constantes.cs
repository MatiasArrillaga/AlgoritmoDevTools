namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public static class Constantes
{
    public const string ServiceProyectName = "Algoritmo.Microservices.Shared.API";
    public const string DefaultConectionDataFile = "SoftCerealCore.ConnectionString.json";
    public const string DefaultConectionString = $"Server={ConstKeyWords.ServerKey};Database={ConstKeyWords.DataBaseKey};User Id={ConstKeyWords.UserKey};Password={ConstKeyWords.PasswordKey}";
    public const string SecretConectionStringName = $"SoftCerealCore.{ConstKeyWords.SecretName}.ConnectionString";
    public const string NoHaySecretosMessage = "No secrets configured for this application";

    public static IReadOnlyList<string> SecretTypes { get; } = new List<string>
    {
        SecretKeys.Development,
        SecretKeys.DAPR,
        SecretKeys.Staging,
        SecretKeys.Production
    };

    public static class SecretKeys
    {
        public const string Development = "Development";
        public const string Staging = "Staging";
        public const string Production = "Production";
        public const string DAPR = "DAPR";
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
    /// Cola de cifrado que llevan los cuatro secretos. Tiene que coincidir con la del archivo de
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

    public static string GetConnectionString(SQLService.ConnectionData connectionData, string? secretType = "")
        => DefaultConectionString
            .Replace(ConstKeyWords.ServerKey, connectionData.Server)
            .Replace(ConstKeyWords.DataBaseKey, connectionData.DataBase)
            .Replace(ConstKeyWords.UserKey, !string.Equals(secretType, SecretKeys.DAPR)
                ? connectionData.User
                : "dapr")
            .Replace(ConstKeyWords.PasswordKey, connectionData.Password)
            + ParametrosDeCifrado;
}
