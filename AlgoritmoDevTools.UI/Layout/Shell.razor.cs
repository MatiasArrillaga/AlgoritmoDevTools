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

    [Inject]
    private Avisos Avisos { get; set; } = default!;

    [Inject]
    private IReinicioDeLaAplicacion _reinicio { get; set; } = default!;

    /// <summary>Con qué pantalla abrir, cuando la abrió el menú del explorador y no una persona.</summary>
    [Inject]
    private ArranqueDirecto Arranque { get; set; } = default!;

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

    /// <summary>Con qué sección abre Configuraciones. Null es la primera, que es lo habitual.</summary>
    private Paginas.Seccion? _seccionDeConfiguraciones;

    /// <summary>
    /// Cuántas veces se pidió una sección puntual. Es el <c>@key</c> de Configuraciones: cambiarlo
    /// la rearma para que tome la sección pedida. Ver <see cref="AbrirConfiguraciones"/>.
    /// </summary>
    private int _pedidoDeSeccion;

    /// <summary>La pastilla de la versión que está a la vista, para poder reemplazarla.</summary>
    private Guid? _avisoDeVersion;

    /// <summary>
    /// Qué situación de versión ya se avisó. Es lo que hace que cerrar la pastilla la cierre de
    /// verdad: sin esto, el siguiente chequeo la volvería a poner.
    /// </summary>
    private string? _versionAvisada;

    protected override void OnInitialized()
    {
        _temas.TemaCambiado += OnTemaCambiado;
        _secretos.SecretsChanged += OnSecretosCambiaron;
        Estado.Cambio += OnEstadoCambio;
        Actualizaciones.Cambio += OnActualizacionesCambio;

        // Arranca en el inicio, que es el resumen de cómo quedó el entorno y es justo lo que se
        // quiere ver al abrir. La excepción es cuando la abrió el menú contextual del explorador:
        // ahí el usuario ya dijo a qué venía, y mandarlo al inicio sería hacerle dar un rodeo.
        var pedida = Arranque.HerramientaInicial is null
            ? null
            : CatalogoDeHerramientas.Todas.FirstOrDefault(h => h.Id == Arranque.HerramientaInicial);

        if (pedida is not null)
        {
            Abrir(pedida);
            return;
        }

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

        // Sólo cuando se pide una sección puntual: es lo que hace que el componente se arme de
        // nuevo y la tome aunque ya se esté en Configuraciones. Entrar por el ícono no lleva
        // sección, y ahí no hay por qué rearmar nada.
        if (seccion is not null) _pedidoDeSeccion++;

        _vista = Vista.Configuraciones;
        _descripcion = "Ajusta la apariencia y el comportamiento de Algoritmo DevTools.";
    }

    private void OnTemaCambiado(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    private void OnEstadoCambio(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    private void OnActualizacionesCambio(object? sender, EventArgs e) => InvokeAsync(() =>
    {
        RefrescarAvisoDeVersion();
        StateHasChanged();
    });

    /// <summary>
    /// Deja a la vista una pastilla con lo que hay para hacer con la versión, y nada si no hay nada.
    ///
    /// Antes esto era un bloque en el inicio, y ahí sólo se veía estando en el inicio: quien abría
    /// la aplicación y se iba derecho a una herramienta no se enteraba de que había una versión
    /// nueva. La pastilla vive en el armazón, así que aparece donde sea que esté el usuario.
    ///
    /// Esta <b>no expira</b>: lleva un botón, y un ofrecimiento que se va solo a los seis segundos
    /// no es un ofrecimiento.
    ///
    /// Se avisa una vez por situación —lo recuerda <c>_versionAvisada</c>—, así que cerrarla la
    /// cierra de verdad y no vuelve con el siguiente chequeo. Para volver a ella está
    /// Configuraciones, en Actualizaciones.
    /// </summary>
    private void RefrescarAvisoDeVersion()
    {
        var instalada = Actualizaciones.EsperandoReinicio;
        var nueva = Actualizaciones.Disponible;

        var situacion = instalada is not null ? $"reinicio:{instalada.Etiqueta}"
            : nueva is not null ? $"nueva:{nueva.Etiqueta}"
            : null;

        if (situacion == _versionAvisada) return;

        if (_avisoDeVersion is { } anterior) Avisos.Cerrar(anterior);

        _versionAvisada = situacion;
        _avisoDeVersion = null;

        if (instalada is not null)
        {
            _avisoDeVersion = Avisos.Fijar(
                $"Quedó instalada la {instalada.Etiqueta}. Empieza a correr cuando reinicies.",
                "estado-exito",
                "Reiniciar ahora",
                ReiniciarAhora);
        }
        else if (nueva is not null)
        {
            _avisoDeVersion = Avisos.Fijar(
                $"Hay una versión nueva: {nueva.Etiqueta}. Tenés la {ServicioDeActualizaciones.VersionActual.ToString(3)}.",
                "estado-neutro",
                "Actualizar",
                IrAActualizaciones);
        }
    }

    /// <summary>
    /// Lleva a Configuraciones, en Actualizaciones, desde la pastilla.
    ///
    /// El repintado va explícito y no se puede sacar: cambiar de vista desde acá no es un clic del
    /// armazón sino de <c>Pastillas</c>, y Blazor sólo redibuja solo al componente cuyo controlador
    /// atendió el clic. Sin esto, el botón cambia la vista y la pantalla se queda como estaba,
    /// exactamente como si no hiciera nada.
    /// </summary>
    private void IrAActualizaciones()
    {
        AbrirConfiguraciones(Paginas.Seccion.Actualizaciones);
        InvokeAsync(StateHasChanged);
    }

    /// <summary>Cierra y vuelve a abrir, para que empiece a correr la versión ya instalada.</summary>
    private void ReiniciarAhora() => _reinicio.Reiniciar();

    public void Dispose()
    {
        _temas.TemaCambiado -= OnTemaCambiado;
        _secretos.SecretsChanged -= OnSecretosCambiaron;
        Estado.Cambio -= OnEstadoCambio;
        Actualizaciones.Cambio -= OnActualizacionesCambio;
    }
}
