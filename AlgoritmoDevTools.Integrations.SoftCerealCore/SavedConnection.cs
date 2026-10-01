namespace AlgoritmoDevTools.Integrations.SoftCerealCore;

public sealed class SavedConnection
{
    public int Id { get; set; }
    public string Server { get; set; } = string.Empty;
    public string DataBase { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseIntegratedSecurity { get; set; }

    /// <summary>
    /// Última base contra la que se aplicaron los secretos con esta conexión. Es deliberadamente
    /// distinta de <see cref="DataBase"/>: esa forma parte de la clave única (Server, DataBase,
    /// UserName), así que pisarla cambiaría la identidad de la conexión y podría chocar con otra
    /// fila. La conexión es el servidor más las credenciales; la base es un dato de uso.
    /// Es lo que usa el menú contextual del explorador, que no puede consultar el servidor.
    /// </summary>
    public string LastDataBase { get; set; } = string.Empty;

    /// <summary>Base a la que apuntaría un "aplicar" sin elegir nada: la última usada, o la de la conexión.</summary>
    public string BaseEfectiva => !string.IsNullOrWhiteSpace(LastDataBase) ? LastDataBase : DataBase;

    public override string ToString()
    {
        var dbSuffix = string.IsNullOrEmpty(DataBase) ? string.Empty : $", {DataBase}";
        var auth = UseIntegratedSecurity ? "Windows Auth" : User;
        return $"{Server}{dbSuffix} ({auth})";
    }
}
