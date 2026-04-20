namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public static class ConnectionStringParser
{
    public static Dictionary<string, string> Parse(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString)) return result;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            result[kv[0].Trim()] = kv[1].Trim();
        }
        return result;
    }
}
