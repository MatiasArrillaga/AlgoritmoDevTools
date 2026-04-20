using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public class SecretService
{
    private static readonly Lazy<SecretService> _shared = new(() => new SecretService());
    public static SecretService Shared => _shared.Value;

    public event EventHandler? SecretsChanged;

    private readonly string _projectPath;
    public Dictionary<string, string> Secrets { get; protected set; } = new();
    public string LastRawOutput { get; private set; } = string.Empty;

    public SecretService()
    {
        _projectPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "source",
            "repos",
            "AlgoritmoCore"
        );
    }

    /// <summary>
    /// Ejecuta 'dotnet user-secrets list' y actualiza Secrets + LastRawOutput en una sola invocación.
    /// </summary>
    public string RefreshSecrets()
    {
        LastRawOutput = ProcessRunner.RunPowerShell(
            $"dotnet user-secrets list --project {Constantes.ServiceProyectName}",
            GetSolutionRoot());
        ParseRawOutput(LastRawOutput);
        SecretsChanged?.Invoke(this, EventArgs.Empty);
        return LastRawOutput;
    }

    /// <summary>
    /// Devuelve la connection string del ambiente solicitado, o null si no hay secretos cargados.
    /// </summary>
    public string? GetConnectionString(string secretType)
        => Secrets.TryGetValue(secretType, out var cs) ? cs : null;

    private void ParseRawOutput(string result)
    {
        Secrets.Clear();
        if (string.IsNullOrEmpty(result) || result.Contains(Constantes.NoHaySecretosMessage)) return;

        var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var secretCS in lines)
        {
            var secret = Constantes.SecretTypes.SingleOrDefault(s => secretCS.Contains(s));
            if (secret is null) continue;
            Secrets[secret.Trim()] = secretCS.Substring(secretCS.IndexOf('=') + 1).Trim();
        }
    }

    public void RemoveSecret(string secrets)
        => ProcessRunner.RunPowerShell(
            $"dotnet user-secrets remove {Constantes.ServiceProyectName.Replace(Constantes.ConstKeyWords.SecretName, secrets)}" +
            $" --project {Constantes.ServiceProyectName}",
            GetSolutionRoot());

    public void RemoveAllSecrets()
        => ProcessRunner.RunPowerShell(
            $"dotnet user-secrets clear --project {Constantes.ServiceProyectName}",
            GetSolutionRoot());

    public void SetSecrets(string secret, SQLService.ConnectionData connectionData)
    {
        var newSecrets = BuildSecretsFor(secret, connectionData);
        ApplySecrets(newSecrets);
    }

    public void SetSecrets(SQLService.ConnectionData connectionData)
    {
        var newSecrets = new Dictionary<string, string>();
        foreach (var secretType in Constantes.SecretTypes)
        {
            if (secretType.Equals(Constantes.SecretKeys.Development) ||
                secretType.Equals(Constantes.SecretKeys.DAPR))
            {
                newSecrets.Add(Constantes.GetSecretKey(secretType), Constantes.GetConnectionString(connectionData, secretType));
            }
            else
            {
                newSecrets.Add(Constantes.GetSecretKey(secretType), Secrets[secretType]);
            }
        }
        ApplySecrets(newSecrets);
    }

    private Dictionary<string, string> BuildSecretsFor(string secret, SQLService.ConnectionData connectionData)
    {
        var newSecrets = new Dictionary<string, string>();
        foreach (var secretType in Constantes.SecretTypes)
        {
            if (secretType.Equals(secret))
                newSecrets.Add(Constantes.GetSecretKey(secret), Constantes.GetConnectionString(connectionData, secretType));
            else
                newSecrets.Add(Constantes.GetSecretKey(secretType), Secrets[secretType]);
        }
        return newSecrets;
    }

    private void ApplySecrets(Dictionary<string, string> newSecrets)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(newSecrets);
        RemoveAllSecrets();
        ProcessRunner.RunDotnet($"user-secrets set --project {Constantes.ServiceProyectName}", json, GetSolutionRoot());
        RefreshSecrets();
    }

    public void RestoreSecretsFromFile()
    {
        ProcessRunner.RunPowerShell(
            $"Get-Content secrets\\{Constantes.DefaultConectionDataFile} | " +
            $"dotnet user-secrets set --project {Constantes.ServiceProyectName}",
            GetSolutionRoot());
        RefreshSecrets();
    }

    private string GetSolutionRoot()
    {
        if (_projectPath is null)
            throw new InvalidOperationException("No se encontró la raíz del proyecto AlgoritmoCore.");
        return _projectPath;
    }
}
