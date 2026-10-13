using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.SecretsManager.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class SecretsManagerPagina : IDisposable
{
    [Inject]
    private SavedConnectionsRepository Conexiones { get; set; } = default!;

    [Inject]
    private BarraDeEstado Estado { get; set; } = default!;

    private readonly SecretService _secretos = SecretService.Shared;

    private List<SavedConnection> _conexiones = new();
    private List<string> _bases = new();
    private SavedConnection? _editando;
    private int _editandoId;
    private int _conexionElegidaId;
    private string _baseElegida = string.Empty;
    private string _visor = string.Empty;
    private string _estadoPrueba = string.Empty;
    private string _claseEstadoPrueba = "estado-neutro";
    private bool _basesHabilitadas;
    private bool _ocupado;
    private bool _probando;
    private bool _eliminandoConexion;

    protected override async Task OnInitializedAsync()
    {
        RecargarConexiones();

        // No se releen los secretos al entrar: el armazón ya los leyó al arrancar para la barra de
        // estado, y volver a pedirlos lanzaba un segundo proceso de dotnet contra el mismo archivo,
        // que quedaba esperando al primero. El botón "Listar secretos" sigue estando para refrescar.
        _visor = _secretos.SalidaParaMostrar;

        await PreseleccionarLaDelSecreto();
    }

    private void RecargarConexiones() => _conexiones = Conexiones.GetAll();

    private SavedConnection? ConexionActual() =>
        _conexionElegidaId == 0 ? null : _conexiones.FirstOrDefault(c => c.Id == _conexionElegidaId);

    /// <summary>
    /// Deja elegida la conexión a la que apuntan los secretos hoy, con su base.
    ///
    /// Sale del propio secreto Development y no de una preferencia guardada: el secreto es lo que
    /// está realmente aplicado, así que acierta también cuando la última vez se aplicó desde el
    /// menú contextual del explorador, sin pasar por esta pantalla.
    /// </summary>
    private async Task PreseleccionarLaDelSecreto()
    {
        var cadena = _secretos.GetConnectionString(Constantes.SecretKeys.Development);
        if (cadena is null) return;

        var partes = ConnectionStringParser.Parse(cadena);
        var servidor = partes.GetValueOrDefault("Server", string.Empty);
        var baseDeDatos = partes.GetValueOrDefault("Database", string.Empty);

        if (string.IsNullOrWhiteSpace(servidor)) return;

        // Puede haber varias conexiones al mismo servidor: gana la que además recuerde esa base.
        var candidatas = _conexiones
            .Where(c => string.Equals(c.Server, servidor, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidatas.Count == 0) return;

        var conexion = candidatas.FirstOrDefault(c =>
            string.Equals(c.BaseEfectiva, baseDeDatos, StringComparison.OrdinalIgnoreCase))
            ?? candidatas[0];

        _conexionElegidaId = conexion.Id;
        await CargarBases(conexion, baseDeDatos);
    }

    private async Task CambiarConexion(ChangeEventArgs e)
    {
        _conexionElegidaId = int.TryParse(e.Value?.ToString(), out var id) ? id : 0;
        _eliminandoConexion = false;

        var conexion = ConexionActual();
        if (conexion is null)
        {
            _bases = new List<string>();
            _baseElegida = string.Empty;
            _basesHabilitadas = false;
            return;
        }

        await CargarBases(conexion);
    }

    /// <summary>
    /// Trae las bases del servidor y deja elegida la que corresponda.
    /// </summary>
    /// <param name="conexion">Conexión sobre la que se consulta.</param>
    /// <param name="basePreferida">
    /// Base que se quiere dejar elegida si existe en el servidor. Si es nula se usa la última que
    /// se le aplicó a esa conexión.
    /// </param>
    private async Task CargarBases(SavedConnection conexion, string? basePreferida = null)
    {
        var deseada = string.IsNullOrWhiteSpace(basePreferida) ? conexion.BaseEfectiva : basePreferida;

        _bases = new List<string> { "Cargando..." };
        _baseElegida = "Cargando...";
        _basesHabilitadas = false;
        StateHasChanged();

        var resultado = await Task.Run(() => SQLService.TryGetDatabases(
            new SQLService.ConnectionData(conexion.Server, conexion.User, conexion.Password, conexion.UseIntegratedSecurity)));

        if (resultado.IsSuccess && resultado.Databases.Count > 0)
        {
            _bases = resultado.Databases.ToList();
            _baseElegida = _bases.Contains(deseada) ? deseada : _bases[0];
            _basesHabilitadas = true;
            return;
        }

        // Sin conexión al servidor queda la última base conocida, para no perder el dato.
        _bases = new List<string> { deseada };
        _baseElegida = deseada;
        _basesHabilitadas = false;

        if (!resultado.IsSuccess)
        {
            Informar($"No se pudo conectar para listar las bases: {resultado.Error}", "estado-advertencia");
        }
    }

    // --- Alta, edición y baja de conexiones ---------------------------------

    /// <summary>
    /// Los bloques se abren y se cierran con el mismo botón, y abrir uno cierra el otro.
    /// </summary>
    private void NuevaConexion()
    {
        _eliminandoConexion = false;

        // Si ya estaba abierto en alta, el mismo botón lo cierra.
        if (_editando is not null && _editandoId == 0)
        {
            _editando = null;
            return;
        }

        _editandoId = 0;
        _editando = new SavedConnection();
        _estadoPrueba = string.Empty;
    }

    private void AlternarEliminarConexion()
    {
        _eliminandoConexion = !_eliminandoConexion;
        _editando = null;
    }

    private void ModificarConexion()
    {
        if (ConexionActual() is not { } conexion) return;

        _eliminandoConexion = false;

        // Si ya estaba abierto editando esta misma conexión, el mismo botón lo cierra.
        if (_editando is not null && _editandoId == conexion.Id)
        {
            _editando = null;
            return;
        }

        _editandoId = conexion.Id;
        _editando = new SavedConnection
        {
            Server = conexion.Server,
            DataBase = conexion.DataBase,
            User = conexion.User,
            Password = conexion.Password,
            UseIntegratedSecurity = conexion.UseIntegratedSecurity
        };
        _estadoPrueba = string.Empty;
    }

    private async Task ProbarConexion()
    {
        if (_editando is null) return;

        _probando = true;
        _estadoPrueba = "Probando conexión...";
        _claseEstadoPrueba = "estado-neutro";
        StateHasChanged();

        var resultado = await Task.Run(() => SQLService.TryGetDatabases(
            new SQLService.ConnectionData(_editando.Server, _editando.User, _editando.Password, _editando.UseIntegratedSecurity)));

        _probando = false;
        _estadoPrueba = resultado.IsSuccess
            ? "Conexión establecida correctamente."
            : resultado.Error ?? "No se pudo conectar.";
        _claseEstadoPrueba = resultado.IsSuccess ? "estado-exito" : "estado-error";
    }

    private void GuardarConexion()
    {
        if (_editando is null) return;

        if (string.IsNullOrWhiteSpace(_editando.Server))
        {
            _estadoPrueba = "Falta el servidor.";
            _claseEstadoPrueba = "estado-advertencia";
            return;
        }

        if (_editandoId == 0) Conexiones.Add(_editando);
        else Conexiones.Update(_editandoId, _editando);

        _editando = null;
        RecargarConexiones();
        SincronizarMenu();
        Informar("Conexión guardada.", "estado-exito");
    }

    private void EliminarConexion()
    {
        _eliminandoConexion = false;
        if (ConexionActual() is not { } conexion) return;

        Conexiones.Delete(conexion.Id);
        _conexionElegidaId = 0;
        _bases = new List<string>();
        _basesHabilitadas = false;
        RecargarConexiones();
        SincronizarMenu();
        Informar("Conexión eliminada.", "estado-neutro");
    }

    // --- Secretos -----------------------------------------------------------

    private async Task ListarSecretos()
    {
        _ocupado = true;
        Informar("Leyendo secretos...", "estado-neutro");
        StateHasChanged();

        try
        {
            await Task.Run(() => _secretos.RefreshSecrets());
            _visor = _secretos.SalidaParaMostrar;
            Informar("Secretos actualizados.", "estado-exito");
        }
        catch (Exception ex)
        {
            _visor = ex.Message;
            Informar("No se pudieron leer los secretos.", "estado-error");
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task RestaurarSecretos()
    {
        _ocupado = true;
        Informar("Restaurando...", "estado-neutro");
        StateHasChanged();

        try
        {
            await Task.Run(() => _secretos.RestoreSecretsFromFile());
            _visor = _secretos.SalidaParaMostrar;
            Informar("Secretos restaurados.", "estado-exito");
        }
        catch (Exception ex)
        {
            Informar($"No se pudieron restaurar: {ex.Message}", "estado-error");
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task AplicarSecreto()
    {
        if (ConexionActual() is not { } conexion) return;

        var baseDatos = string.IsNullOrWhiteSpace(_baseElegida) || _baseElegida == "Cargando..."
            ? conexion.BaseEfectiva
            : _baseElegida;

        if (string.IsNullOrWhiteSpace(baseDatos))
        {
            Informar("Elegí una base de datos antes de aplicar el secreto.", "estado-advertencia");
            return;
        }

        _ocupado = true;
        Informar("Aplicando...", "estado-neutro");
        StateHasChanged();

        try
        {
            await Task.Run(() => AplicadorDeSecretos.Aplicar(conexion, baseDatos, Conexiones));
            _visor = _secretos.SalidaParaMostrar;
            RecargarConexiones();
            SincronizarMenu();
            Informar($"Secretos apuntados a {baseDatos}.", "estado-exito");
        }
        catch (Exception ex)
        {
            Informar($"No se pudo aplicar: {ex.Message}", "estado-error");
        }
        finally
        {
            _ocupado = false;
        }
    }

    /// <summary>
    /// Reescribe el menú contextual, si está puesto, con las conexiones que hay ahora. El menú es
    /// estático y cada ítem lleva clavada su base: una conexión borrada seguiría apareciendo y otra
    /// reapuntada mostraría la base vieja, que es peor que no tener menú.
    ///
    /// Prenderlo y apagarlo se hace desde Configuraciones; acá sólo se lo mantiene al día.
    /// </summary>
    private void SincronizarMenu() => MenuContextualSecretos.Sincronizar(Conexiones.GetAll());

    private void Informar(string texto, string clase) => Estado.Informar(texto, clase);

    public void Dispose() => Estado.Limpiar();
}
