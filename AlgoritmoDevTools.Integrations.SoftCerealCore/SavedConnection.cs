namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public sealed class SavedConnection
{
    public int Id { get; set; }
    public string Server { get; set; } = string.Empty;
    public string DataBase { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseIntegratedSecurity { get; set; }

    public override string ToString()
    {
        var dbSuffix = string.IsNullOrEmpty(DataBase) ? string.Empty : $", {DataBase}";
        var auth = UseIntegratedSecurity ? "Windows Auth" : User;
        return $"{Server}{dbSuffix} ({auth})";
    }
}
