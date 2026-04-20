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

    public static string GetConnectionString(SQLService.ConnectionData connectionData, string? secretType = "")
        => DefaultConectionString
            .Replace(ConstKeyWords.ServerKey, connectionData.Server)
            .Replace(ConstKeyWords.DataBaseKey, connectionData.DataBase)
            .Replace(ConstKeyWords.UserKey, !string.Equals(secretType, SecretKeys.DAPR)
                ? connectionData.User
                : "dapr")
            .Replace(ConstKeyWords.PasswordKey, connectionData.Password)
            + (!string.Equals(secretType, SecretKeys.DAPR) ? ";TrustServerCertificate=Yes" : string.Empty);
}
