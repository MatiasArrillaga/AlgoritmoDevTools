using System.Text.Json.Serialization;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Lo que alguien cargó a mano y le sirve igual a cualquier otro: las conexiones guardadas, los
/// dominios y los perfiles del selector de servicios. Es lo que viaja en el archivo que se exporta.
///
/// Queda afuera, a propósito, todo lo que es de la máquina o del avance personal de cada uno: el
/// tema, la carpeta de repositorios, los dominios usados recientemente y el baseline de migraciones.
/// Importar cualquiera de esos le cambiaría al que recibe algo que no es suyo —el baseline dice
/// hasta qué commit migró uno, y la carpeta de repositorios casi nunca es la misma en dos máquinas—.
/// </summary>
public sealed class PaqueteDeConfiguracion
{
    /// <summary>
    /// Versión del formato. Va desde el primer archivo para poder leer los viejos el día que esto
    /// cambie de forma: sin el número, el único camino sería adivinar por las propiedades que haya.
    /// </summary>
    public const int FormatoActual = 1;

    public int Formato { get; set; } = FormatoActual;

    public DateTimeOffset Exportado { get; set; } = DateTimeOffset.Now;

    /// <summary>Quién lo exportó. Es informativo: sirve para saber a quién preguntarle.</summary>
    public string? Origen { get; set; }

    /// <summary>
    /// Si las conexiones traen su contraseña. Se guarda explícito para poder avisarle al que importa
    /// que va a tener que completarlas, en lugar de que se entere cuando le falle la conexión.
    /// </summary>
    public bool IncluyeContrasenas { get; set; }

    public List<ConexionExportada> Conexiones { get; set; } = [];

    public List<string> Dominios { get; set; } = [];

    public List<PerfilExportado> PerfilesDeTye { get; set; } = [];

    /// <summary>Si no trae nada, no tiene sentido importarlo. Se calcula, no se guarda.</summary>
    [JsonIgnore]
    public bool EstaVacio => Conexiones.Count == 0 && Dominios.Count == 0 && PerfilesDeTye.Count == 0;
}

/// <summary>
/// Una conexión guardada, sin el Id ni la última base usada: el Id es de la base local de cada uno y
/// la última base es un dato de uso, no parte de la conexión.
/// </summary>
public sealed class ConexionExportada
{
    public string Servidor { get; set; } = string.Empty;

    public string Base { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    /// <summary>Null cuando se exportó sin contraseñas, o cuando la conexión usa autenticación de Windows.</summary>
    public string? Contrasena { get; set; }

    public bool SeguridadIntegrada { get; set; }
}

/// <summary>Un perfil del selector de servicios: el nombre y qué servicios deja activos.</summary>
public sealed class PerfilExportado
{
    public string Nombre { get; set; } = string.Empty;

    public List<string> Servicios { get; set; } = [];
}

/// <summary>
/// Qué entró y qué ya estaba. La importación no pisa nada, así que lo repetido no es un error: es lo
/// que hay que poder contarle al usuario para que entienda por qué el número no da con el archivo.
/// </summary>
public sealed record ResultadoDeImportacion(
    int ConexionesNuevas,
    int ConexionesRepetidas,
    int DominiosNuevos,
    int DominiosRepetidos,
    int PerfilesNuevos,
    int PerfilesRepetidos)
{
    public int TotalNuevo => ConexionesNuevas + DominiosNuevos + PerfilesNuevos;

    public int TotalRepetido => ConexionesRepetidas + DominiosRepetidos + PerfilesRepetidos;
}
