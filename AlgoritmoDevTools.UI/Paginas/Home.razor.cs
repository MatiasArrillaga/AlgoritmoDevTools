using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;
using AlgoritmoDevTools.Tools.ModelDriftChecker.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class Home : IDisposable
{
    [Inject]
    private BaselineRepository Baselines { get; set; } = default!;

    [Inject]
    private PerfilRepository Limpieza { get; set; } = default!;

    [Inject]
    private ServicioDeActualizaciones Actualizaciones { get; set; } = default!;

    [Inject]
    private IReinicioDeLaAplicacion Reinicio { get; set; } = default!;

    /// <summary>
    /// El paso a Configuraciones, que es donde se instala. Lo baja el armazón: esta pantalla no sabe
    /// navegar, sólo avisa.
    /// </summary>
    [CascadingParameter(Name = "IrAActualizaciones")]
    private EventCallback IrAActualizaciones { get; set; }

    private const int ServiciosQueSeListan = 6;

    private readonly SecretService _secretos = SecretService.Shared;

    /// <summary>
    /// Nombre de pila. Las cuentas del dominio vienen como "Nombre.Apellido" y saludar con el
    /// apellido pegado suena a sistema, no a persona.
    /// </summary>
    private string _usuario = ConMayuscula(Environment.UserName.Split('.')[0]);
    private string _servidor = string.Empty;
    private string _baseDeDatos = string.Empty;
    private string _nombresActivos = string.Empty;
    private string? _baseline;
    private UltimaCorrida _ultimaCorrida = UltimaCorrida.Ninguna;
    private long _discoTotal;
    private long _discoOcupado;
    private string? _rama;
    private string? _perfil;
    private int _serviciosActivos;
    private int _totalDeServicios;
    private bool _haySecreto;

    /// <summary>
    /// La lectura de secretos la dispara el armazón al arrancar y tarda alrededor de un segundo,
    /// así que esta pantalla se dibuja antes de que termine. Hasta que llega el aviso se muestra
    /// que está en curso, en vez de afirmar que no hay nada configurado.
    /// </summary>
    private bool _leyendoSecretos;
    private bool _hayYamlGenerado;
    private bool _hayAlgoritmoCore;

    protected override void OnInitialized()
    {
        // Los secretos los lee el armazón una sola vez al arrancar, y puede no haber terminado
        // cuando esta pantalla se dibuja: por eso además de leerlos ahora se escucha el aviso.
        _secretos.SecretsChanged += AlLlegarLosSecretos;

        // La consulta a GitHub la larga el armazón y termina después de que esta pantalla se dibujó.
        // Sin escucharla, el aviso no aparece hasta que uno entra y sale: Blazor no vuelve a dibujar
        // un componente sin parámetros sólo porque el padre se dibujó de nuevo.
        Actualizaciones.Cambio += AlCambiarLasActualizaciones;

        LeerSecretos();
        _leyendoSecretos = !_haySecreto;

        LeerServicios();
        LeerBaseline();
        LeerDisco();

        _perfil = ConfiguracionDevTools.Compartida.UltimoPerfilDeTye;
        if (string.IsNullOrWhiteSpace(_perfil)) _perfil = null;

        _hayAlgoritmoCore = Directory.Exists(RutasDeTrabajo.AlgoritmoCore);
        if (_hayAlgoritmoCore) _rama = LeerRamaActual(RutasDeTrabajo.AlgoritmoCore);
    }

    /// <summary>
    /// Lee la rama en la que está parado el repositorio.
    ///
    /// Se saca del archivo HEAD y no de <c>git branch</c>: esta pantalla es lo primero que se ve y
    /// levantar un proceso para un dato de una línea la haría arrancar con demora.
    /// </summary>
    /// <param name="repositorio">Raíz del repositorio.</param>
    /// <returns>El nombre de la rama, los primeros caracteres del commit si está en HEAD suelto,
    /// o null si no se pudo leer.</returns>
    private static string? LeerRamaActual(string repositorio)
    {
        try
        {
            var head = Path.Combine(repositorio, ".git", "HEAD");
            if (!File.Exists(head)) return null;

            var contenido = File.ReadAllText(head).Trim();

            const string prefijo = "ref: refs/heads/";
            if (contenido.StartsWith(prefijo, StringComparison.Ordinal))
            {
                return contenido[prefijo.Length..];
            }

            // Sin "ref:" el repositorio está en un commit suelto y HEAD trae el SHA pelado.
            return contenido.Length >= 7 ? contenido[..7] : contenido;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void AlLlegarLosSecretos(object? sender, EventArgs e) => InvokeAsync(() =>
    {
        _leyendoSecretos = false;
        LeerSecretos();
        StateHasChanged();
    });

    private void AlCambiarLasActualizaciones(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    /// <summary>Cierra y vuelve a abrir, para que empiece a correr la versión ya instalada.</summary>
    private void Reiniciar() => Reinicio.Reiniciar();

    public void Dispose()
    {
        _secretos.SecretsChanged -= AlLlegarLosSecretos;
        Actualizaciones.Cambio -= AlCambiarLasActualizaciones;
    }

    private void LeerSecretos()
    {
        var cadena = _secretos.GetConnectionString(Constantes.SecretKeys.Development);
        if (cadena is null) return;

        var partes = ConnectionStringParser.Parse(cadena);
        _servidor = partes.GetValueOrDefault("Server", "-");
        _baseDeDatos = partes.GetValueOrDefault("Database", "-");
        _haySecreto = true;
    }

    private void LeerServicios()
    {
        if (!File.Exists(TyeServiceToggler.MasterYamlPath)) return;

        var servicios = TyeServiceToggler.ReadServices();
        _totalDeServicios = servicios.Count;
        _hayYamlGenerado = File.Exists(TyeServiceToggler.GeneratedYamlPath);

        var activos = servicios.Where(s => s.Enabled).Select(s => ConMayuscula(s.Name)).ToList();
        _serviciosActivos = _hayYamlGenerado ? activos.Count : servicios.Count;

        if (!_hayYamlGenerado) return;

        // Con muchos servicios la lista no entra ni se lee: se nombran los primeros y se cuenta el resto.
        _nombresActivos = activos.Count <= ServiciosQueSeListan
            ? string.Join(", ", activos)
            : string.Join(", ", activos.Take(ServiciosQueSeListan)) + $" y {activos.Count - ServiciosQueSeListan} más";
    }

    /// <summary>
    /// Primera letra en mayúscula.
    ///
    /// Los nombres que se muestran acá los escribió otro —la cuenta de Windows, el yaml de Tye— y
    /// ahí viven en minúscula porque son identificadores. En la pantalla son nombres, y un "hola
    /// matias" o un "van a levantar: ui, cereales" se leen como un error de la aplicación.
    ///
    /// Sólo la primera letra: separar "operacionescomerciales" en palabras sería adivinar, y lo que
    /// el yaml trae pegado no hay forma de desarmarlo sin equivocarse en algún caso.
    /// </summary>
    private static string ConMayuscula(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0]) + texto[1..];

    private void LeerBaseline()
    {
        var sha = Baselines.GetBaseline(RutasDeTrabajo.AlgoritmoCore);
        _baseline = sha is null ? null : (sha.Length >= 7 ? sha[..7] : sha);
    }

    /// <summary>
    /// El estado del disco. <b>No mide nada</b>, y eso no es una omisión: medir lo que se puede
    /// liberar tarda cerca de un minuto, y esta pantalla tiene que aparecer al instante.
    ///
    /// Lo ocupado sale del sistema y es de ahora; lo liberable es lo que quedó anotado la última
    /// vez que alguien analizó el disco. Son dos cosas distintas y por eso la fecha va al lado del
    /// número: sin ella, un dato de hace un mes se lee como si fuera de recién.
    /// </summary>
    private void LeerDisco()
    {
        _ultimaCorrida = Limpieza.LeerUltimaCorrida();

        try
        {
            var raiz = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (string.IsNullOrWhiteSpace(raiz)) return;

            var unidad = new DriveInfo(raiz);
            _discoTotal = unidad.TotalSize;
            _discoOcupado = unidad.TotalSize - unidad.AvailableFreeSpace;
        }
        catch
        {
            // Una unidad que no se pueda consultar deja la tarjeta sin cifras, no a la pantalla sin
            // abrir.
        }
    }

    /// <summary>
    /// Cuándo fue, en palabras. Un "hace 2 días" dice más de un vistazo que una fecha, que obliga a
    /// restar mentalmente para saber si el número de al lado todavía sirve.
    /// </summary>
    private static string HaceCuanto(DateTime cuando)
    {
        var transcurrido = DateTime.Now - cuando;

        if (transcurrido < TimeSpan.FromMinutes(1)) return "recién";
        if (transcurrido < TimeSpan.FromHours(1)) return $"hace {(int)transcurrido.TotalMinutes} min";
        if (transcurrido < TimeSpan.FromDays(1)) return $"hace {(int)transcurrido.TotalHours} h";
        if (transcurrido < TimeSpan.FromDays(2)) return "ayer";
        if (transcurrido < TimeSpan.FromDays(30)) return $"hace {(int)transcurrido.TotalDays} días";

        return cuando.ToString("d/M/yyyy");
    }

    /// <summary>
    /// Pasado un tiempo, lo medido deja de ser representativo y la tarjeta lo dice en gris en lugar
    /// de esconderlo: un número viejo con su fecha es más útil que ningún número.
    /// </summary>
    private static bool EstaViejo(DateTime cuando) => DateTime.Now - cuando > TimeSpan.FromDays(7);

    /// <summary>
    /// Los tramos de la barra del inicio. Son tres y no cuatro: "elegido para limpiar" sale de lo
    /// que el usuario tilda, y acá no hay nada tildado.
    ///
    /// Lo liberable es lo que quedó anotado del último análisis, así que puede no corresponderse
    /// con el ocupado de ahora. Es el precio de no medir medio disco para abrir una pantalla, y por
    /// eso la fecha va escrita debajo.
    /// </summary>
    private IReadOnlyList<TramoDelDisco> TramosDelDisco()
    {
        var libre = _discoTotal - _discoOcupado;
        var liberable = Math.Min(_ultimaCorrida.Liberable, _discoOcupado);

        return
        [
            new TramoDelDisco("Se puede liberar", "tramo-posible", liberable),
            new TramoDelDisco("Ocupado", "tramo-ocupado", _discoOcupado - liberable),
            new TramoDelDisco("Libre", "tramo-libre", libre)
        ];
    }

    private string DescripcionDelDisco() =>
        $"Disco de {Tamanos.Legible(_discoTotal)}: {Tamanos.Legible(_discoOcupado)} ocupados, " +
        $"de los cuales {Tamanos.Legible(_ultimaCorrida.Liberable)} se pueden liberar.";
}
