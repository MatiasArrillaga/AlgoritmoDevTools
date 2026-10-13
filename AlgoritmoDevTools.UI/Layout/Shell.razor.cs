using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Layout;

public partial class Shell : IDisposable
{
    [Inject]
    private BarraDeEstado Estado { get; set; } = default!;

    [Inject]
    private ServicioDeActualizaciones Actualizaciones { get; set; } = default!;

    /// <summary>Qué está mostrando el panel central.</summary>
    private enum Vista { Home, Herramienta, Configuraciones }

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private readonly SecretService _secretos = SecretService.Shared;

    private HerramientaUI? _seleccionada;
    private Vista _vista = Vista.Home;
    private string _descripcion = string.Empty;
    private string _servidor = "Servidor: -";
    private string _base = "Base: -";
    private string _mensaje = string.Empty;

    /// <summary>
    /// El paso a la sección de actualizaciones, para el aviso del inicio. Lo baja el armazón porque
    /// es el único que sabe qué se está mostrando; dejar al usuario en Configuraciones a secas lo
    /// obligaría a buscar dónde estaba eso que le acababan de ofrecer.
    /// </summary>
    private EventCallback _irAActualizaciones;

    /// <summary>Con qué sección abre Configuraciones. Null es la primera, que es lo habitual.</summary>
    private Paginas.Seccion? _seccionDeConfiguraciones;

    protected override void OnInitialized()
    {
        _temas.TemaCambiado += OnTemaCambiado;
        _secretos.SecretsChanged += OnSecretosCambiaron;
        Estado.Cambio += OnEstadoCambio;
        Actualizaciones.Cambio += OnEstadoCambio;

        _irAActualizaciones = EventCallback.Factory.Create(
            this, () => AbrirConfiguraciones(Paginas.Seccion.Actualizaciones));

        // Siempre arranca en el inicio: es el resumen de cómo quedó el entorno, que es justo lo
        // que se quiere ver al abrir.
        AbrirHome();
    }

    /// <summary>
    /// Lee los secretos para mostrar contra qué servidor y base está apuntando el entorno. Va fuera
    /// del hilo de la interfaz porque levanta un proceso de dotnet, que tarda lo suyo.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        _mensaje = "Leyendo secretos...";

        try
        {
            await Task.Run(() => _secretos.RefreshSecrets());
            ActualizarBarraDeEstado();
        }
        catch (Exception ex)
        {
            _servidor = "Servidor: -";
            _base = "Base: -";
            _mensaje = $"No se pudieron leer los secretos: {ex.Message}";
        }

        // Después de los secretos y sin esperarla: si GitHub no contesta, la aplicación ya está
        // andando y nadie se entera. El resultado lo muestra el inicio cuando llega.
        await Actualizaciones.BuscarAsync();
    }

    private void ActualizarBarraDeEstado()
    {
        var cadena = _secretos.GetConnectionString(Constantes.SecretKeys.Development);

        if (cadena is null)
        {
            _servidor = "Servidor: -";
            _base = "Base: -";
            _mensaje = "Secreto Development no configurado.";
            return;
        }

        var partes = ConnectionStringParser.Parse(cadena);
        _servidor = $"Servidor: {partes.GetValueOrDefault("Server", "-")}";
        _base = $"Base: {partes.GetValueOrDefault("Database", "-")}";
        _mensaje = string.Empty;
    }

    /// <summary>
    /// El Secrets Manager puede cambiar a qué base apunta el entorno: cuando eso pasa, la barra
    /// tiene que decir la nueva sin que el usuario cambie de pantalla.
    /// </summary>
    private void OnSecretosCambiaron(object? sender, EventArgs e) => InvokeAsync(() =>
    {
        ActualizarBarraDeEstado();
        StateHasChanged();
    });

    private bool EsSeleccionada(string id) => _vista == Vista.Herramienta && _seleccionada?.Id == id;

    private void Abrir(HerramientaUI herramienta)
    {
        _seleccionada = herramienta;
        _vista = Vista.Herramienta;
        _descripcion = herramienta.Descripcion;

    }

    private void AbrirHome()
    {
        _vista = Vista.Home;
        _descripcion = "Resumen de lo que hay configurado en este equipo.";
    }

    private void AbrirConfiguraciones(Paginas.Seccion? seccion = null)
    {
        _seccionDeConfiguraciones = seccion;
        _vista = Vista.Configuraciones;
        _descripcion = "Ajusta la apariencia y el comportamiento de Algoritmo DevTools.";
    }

    private void OnTemaCambiado(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    private void OnEstadoCambio(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        _temas.TemaCambiado -= OnTemaCambiado;
        _secretos.SecretsChanged -= OnSecretosCambiaron;
        Estado.Cambio -= OnEstadoCambio;
        Actualizaciones.Cambio -= OnEstadoCambio;
    }
}
