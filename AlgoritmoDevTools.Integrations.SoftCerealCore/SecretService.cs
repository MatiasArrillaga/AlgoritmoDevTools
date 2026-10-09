using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public class SecretService
{
    private static readonly Lazy<SecretService> _shared = new(() => new SecretService());
    public static SecretService Shared => _shared.Value;

    public event EventHandler? SecretsChanged;

    /// <summary>
    /// Raíz de AlgoritmoCore, según la carpeta elegida en Configuraciones. Se resuelve en cada
    /// consulta y no en el constructor: este servicio es único para todo el proceso, así que
    /// calcularlo una vez dejaría la ruta vieja hasta reiniciar.
    /// </summary>
    private static string ProjectPath => RutasDeTrabajo.AlgoritmoCore;
    public Dictionary<string, string> Secrets { get; protected set; } = new();

    /// <summary>
    /// Las claves de user-secrets que esta herramienta no administra — hoy las diecisiete de
    /// <c>SoftCerealCore.Seguridad.&lt;Dominio&gt;</c> que trae el archivo de restauración.
    ///
    /// Se guardan porque <see cref="ApplySecrets"/> hace <c>user-secrets clear</c> antes de
    /// reescribir: sin esta copia, aplicar una conexión borraba todo lo que no fuera una de las
    /// cinco cadenas y dejaba los microservicios sin sus secretos de seguridad.
    /// </summary>
    public Dictionary<string, string> SecretosNoAdministrados { get; } = new();
    public string LastRawOutput { get; private set; } = string.Empty;

    /// <summary>
    /// Ejecuta 'dotnet user-secrets list' y actualiza Secrets + LastRawOutput en una sola invocación.
    ///
    /// Llama a dotnet directo y no a través de PowerShell: envuelto, el mismo comando levantaba dos
    /// procesos en vez de uno y tardaba unos 700 ms más. Es la lectura que corre al abrir la
    /// aplicación, así que ese tiempo se siente.
    /// </summary>
    public string RefreshSecrets()
    {
        LastRawOutput = ProcessRunner.RunDotnet(
            $"user-secrets list --project {Constantes.ServiceProyectName}",
            workingDirectory: GetSolutionRoot());
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
        SecretosNoAdministrados.Clear();
        if (string.IsNullOrEmpty(result) || result.Contains(Constantes.NoHaySecretosMessage)) return;

        var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var linea in lines)
        {
            // El primer '=' separa clave de valor; los que siguen son los de la propia cadena de
            // conexion (Server=, Database=, ...).
            var corte = linea.IndexOf('=');
            if (corte < 0) continue;

            var clave = linea.Substring(0, corte).Trim();
            var valor = linea.Substring(corte + 1).Trim();

            // Se compara contra la clave COMPLETA, no con Contains: desde que el archivo de
            // secretos usa SoftCerealCore.Seguridad.<Dominio>, un Contains("Seguridad") matchea
            // esas diecisiete claves y la ultima terminaba pisando la cadena de Seguridad.
            var secret = Constantes.SecretTypes.FirstOrDefault(s => Constantes.GetSecretKey(s) == clave);

            if (secret is null)
                SecretosNoAdministrados[clave] = valor;
            else
                Secrets[secret] = valor;
        }
    }

    /// <summary>
    /// La salida de <c>dotnet user-secrets list</c> reordenada para mostrar: primero las cadenas
    /// que se modifican al aplicar una conexion, despues las que quedan fijas, y al final todo lo
    /// que la herramienta no administra (las claves de seguridad por dominio).
    ///
    /// Se reordena el texto crudo en lugar de rearmarlo desde los diccionarios para no perder las
    /// lineas que no son "clave = valor", como el aviso de que no hay ningun secreto cargado.
    /// </summary>
    public string SalidaParaMostrar => OrdenarParaMostrar(LastRawOutput);

    internal static string OrdenarParaMostrar(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        var lineas = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Rango por linea: 0 modificables, 1 fijas, 2 el resto. Las lineas que no son clave = valor
        // van con las del grupo 2, pero antes: son avisos y arriba se leen.
        int Rango(string linea)
        {
            var corte = linea.IndexOf('=');
            if (corte < 0) return 2;

            var clave = linea.Substring(0, corte).Trim();

            if (Constantes.SecretTypesModificables.Any(t => Constantes.GetSecretKey(t) == clave)) return 0;
            if (Constantes.SecretTypesFijos.Any(t => Constantes.GetSecretKey(t) == clave)) return 1;
            return 2;
        }

        // Dentro de los grupos 0 y 1 manda el orden declarado en Constantes, no el alfabetico: es
        // el mismo que usa SetSecrets, asi que lo que se ve arriba es lo que se reescribe.
        int Posicion(string linea)
        {
            var corte = linea.IndexOf('=');
            if (corte < 0) return int.MaxValue;

            var clave = linea.Substring(0, corte).Trim();
            for (var i = 0; i < Constantes.SecretTypes.Count; i++)
                if (Constantes.GetSecretKey(Constantes.SecretTypes[i]) == clave) return i;

            return int.MaxValue;
        }

        // OrderBy es estable, asi que dentro del grupo 2 se respeta el orden en que vino.
        var ordenadas = lineas
            .OrderBy(Rango)
            .ThenBy(Posicion)
            .ToArray();

        return string.Join(Environment.NewLine, ordenadas);
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
            if (Constantes.SecretTypesModificables.Contains(secretType))
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
                "(modificar uno reescribe las cinco cadenas). Salida de 'dotnet user-secrets list':" +
                Environment.NewLine + Environment.NewLine +
                LastRawOutput);
        }
    }

    private void ApplySecrets(Dictionary<string, string> newSecrets)
    {
        // Las claves ajenas van primero y las administradas después, para que una colisión la gane
        // siempre el valor recién calculado.
        var aEscribir = new Dictionary<string, string>(SecretosNoAdministrados);
        foreach (var par in newSecrets) aEscribir[par.Key] = par.Value;

        var json = System.Text.Json.JsonSerializer.Serialize(aEscribir);
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

    private static string GetSolutionRoot() => ProjectPath;
}
