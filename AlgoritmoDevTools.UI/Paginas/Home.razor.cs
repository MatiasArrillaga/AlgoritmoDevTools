using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
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

    private const int ServiciosQueSeListan = 6;

    private readonly SecretService _secretos = SecretService.Shared;

    /// <summary>
    /// Nombre de pila. Las cuentas del dominio vienen como "Nombre.Apellido" y saludar con el
    /// apellido pegado suena a sistema, no a persona.
    /// </summary>
    private string _usuario = Environment.UserName.Split('.')[0];
    private string _servidor = string.Empty;
    private string _baseDeDatos = string.Empty;
    private string _nombresActivos = string.Empty;
    private string? _baseline;
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

        LeerSecretos();
        _leyendoSecretos = !_haySecreto;

        LeerServicios();
        LeerBaseline();

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

    public void Dispose() => _secretos.SecretsChanged -= AlLlegarLosSecretos;

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

        var activos = servicios.Where(s => s.Enabled).Select(s => s.Name).ToList();
        _serviciosActivos = _hayYamlGenerado ? activos.Count : servicios.Count;

        if (!_hayYamlGenerado) return;

        // Con muchos servicios la lista no entra ni se lee: se nombran los primeros y se cuenta el resto.
        _nombresActivos = activos.Count <= ServiciosQueSeListan
            ? string.Join(", ", activos)
            : string.Join(", ", activos.Take(ServiciosQueSeListan)) + $" y {activos.Count - ServiciosQueSeListan} más";
    }

    private void LeerBaseline()
    {
        var sha = Baselines.GetBaseline(RutasDeTrabajo.AlgoritmoCore);
        _baseline = sha is null ? null : (sha.Length >= 7 ? sha[..7] : sha);
    }
}
