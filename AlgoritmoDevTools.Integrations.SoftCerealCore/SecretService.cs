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
        AsegurarSecretosCargados();
        var newSecrets = BuildSecretsFor(secret, connectionData);
        ApplySecrets(newSecrets);
    }

    public void SetSecrets(SQLService.ConnectionData connectionData)
    {
        AsegurarSecretosCargados();

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
                PreservarSiExiste(newSecrets, secretType);
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
                PreservarSiExiste(newSecrets, secretType);
        }
        return newSecrets;
    }

    /// <summary>
    /// Arrastra el secreto que no se está modificando, sólo si hoy existe. Con el indexer directo
    /// (<c>Secrets[secretType]</c>) esto tiraba KeyNotFoundException cuando el proyecto no tenía
    /// cargados Staging o Production, que es lo normal en una máquina de desarrollo. El problema
    /// es que para entonces <see cref="ApplySecrets"/> ya corrió <c>user-secrets clear</c>, así que
    /// la excepción dejaba al usuario sin ningún secreto.
    /// </summary>
    private void PreservarSiExiste(Dictionary<string, string> destino, string secretType)
    {
        if (Secrets.TryGetValue(secretType, out var actual))
            destino.Add(Constantes.GetSecretKey(secretType), actual);
    }

    /// <summary>
    /// Relee los secretos actuales antes de reescribirlos. Modificar un secreto conserva los otros
    /// tres copiándolos de <see cref="Secrets"/>, así que si ese diccionario está vacío no hay nada
    /// que conservar y Staging y Production se pierden.
    ///
    /// Pasa cuando el proceso no pasó por la pantalla del Secrets Manager: el menú contextual del
    /// explorador levanta el exe de cero y <see cref="Shared"/> nace vacío. Dentro de la pantalla no
    /// se notaba porque el Load ya había llamado a <see cref="RefreshSecrets"/>.
    ///
    /// Si la lectura falla (dotnet que no responde, proyecto que no resuelve) se corta acá: el
    /// único momento seguro para abortar es ANTES de que <see cref="ApplySecrets"/> corra
    /// <c>user-secrets clear</c>. El caso legítimo de "no hay ningún secreto todavía" se distingue
    /// por el mensaje que imprime la propia herramienta.
    /// </summary>
    private void AsegurarSecretosCargados()
    {
        RefreshSecrets();

        if (Secrets.Count == 0 && !LastRawOutput.Contains(Constantes.NoHaySecretosMessage))
        {
            throw new InvalidOperationException(
                "No se pudieron leer los secretos actuales, así que no se modificó ninguno " +
                "(modificar uno reescribe los cuatro). Salida de 'dotnet user-secrets list':" +
                Environment.NewLine + Environment.NewLine +
                LastRawOutput);
        }
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
