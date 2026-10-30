using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Preferencias del usuario que valen para toda la aplicación, no para una herramienta en particular.
/// Viven en un JSON suelto bajo %LOCALAPPDATA%/AlgoritmoDevTools/ y no en el SQLite de
/// <see cref="ToolStorage"/>, porque ese almacenamiento está aislado por herramienta justamente para
/// que una no vea lo de la otra, y el tema tiene que ser visible desde todas.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public sealed class ConfiguracionDevTools
{
    private const string RootFolderName = "AlgoritmoDevTools";
    private const string FileName = "configuracion.json";

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Id del tema elegido. Si no resuelve a ninguno conocido se usa el tema por defecto.</summary>
    public string? TemaId { get; set; }

    /// <summary>
    /// Último perfil del selector de servicios que se eligió. Vacío significa que no hay ninguno
    /// elegido, o que se trabajó marcando servicios a mano.
    /// </summary>
    public string? UltimoPerfilDeTye { get; set; }

    /// <summary>
    /// Con qué conexión se levantó el entorno local: servidor, base y cuándo, separados por barra
    /// vertical. Vacío significa que no hay entorno levantado desde acá.
    ///
    /// Se guarda porque la conexión no se puede averiguar mirando el entorno: cada servicio la lee
    /// de los user-secrets al arrancar y se la queda. Si alguien la cambia después, los que ya están
    /// corriendo siguen con la anterior y no hay forma de notarlo salvo recordando con cuál arrancaron.
    /// </summary>
    public string? SelloDeConexion { get; set; }

    /// <summary>
    /// Proceso de tye que lanzó DevTools, para poder cerrar su consola al frenar. Vacío significa
    /// que el entorno no lo levantamos nosotros —lo arrancó el usuario desde su propia consola— y
    /// entonces esa ventana no es nuestra para cerrarla.
    /// </summary>
    public int? PidDeTye { get; set; }

    /// <summary>
    /// Dónde dejó el usuario la barra del entorno, como "x,y". Vacío la pone arriba a la derecha.
    /// </summary>
    public string? PosicionDeLaBarra { get; set; }

    /// <summary>
    /// Ancho que el usuario le dejó a la barra del entorno. Vacío significa el de fábrica.
    /// </summary>
    public int? AnchoDeLaBarra { get; set; }

    /// <summary>
    /// Carpeta que contiene los repositorios. Vacía significa la predeterminada, que es la de
    /// siempre: la carpeta source/repos del perfil del usuario. Ver <see cref="RutasDeTrabajo"/>.
    /// </summary>
    public string? CarpetaDeRepositorios { get; set; }

    private static readonly Lazy<ConfiguracionDevTools> _compartida = new(Cargar);

    /// <summary>
    /// La configuración del proceso. Es una sola para todos: el tema, las rutas y el resto de las
    /// preferencias tienen que verse iguales desde cualquier herramienta, y dos instancias leyendo
    /// el mismo archivo se pisarían al guardar.
    /// </summary>
    public static ConfiguracionDevTools Compartida => _compartida.Value;

    public static string Ruta => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        RootFolderName,
        FileName);

    /// <summary>
    /// Lee las preferencias del disco. Cualquier problema de lectura devuelve la configuración por
    /// defecto en lugar de tirar: que el archivo esté corrupto o a medio escribir no puede impedir
    /// que la aplicación abra.
    /// </summary>
    public static ConfiguracionDevTools Cargar()
    {
        try
        {
            if (!File.Exists(Ruta)) return new ConfiguracionDevTools();
            var json = File.ReadAllText(Ruta);
            return JsonSerializer.Deserialize<ConfiguracionDevTools>(json, Opciones) ?? new ConfiguracionDevTools();
        }
        catch (Exception)
        {
            return new ConfiguracionDevTools();
        }
    }

    /// <summary>
    /// Guarda las preferencias. Se traga los errores por la misma razón que <see cref="Cargar"/>:
    /// no poder escribir la preferencia no justifica interrumpir lo que el usuario estaba haciendo.
    /// </summary>
    public void Guardar()
    {
        try
        {
            var carpeta = Path.GetDirectoryName(Ruta);
            if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(this, Opciones));
        }
        catch (Exception)
        {
            // Sin acción: la preferencia se pierde al cerrar, pero la sesión actual sigue bien.
        }
    }
}
