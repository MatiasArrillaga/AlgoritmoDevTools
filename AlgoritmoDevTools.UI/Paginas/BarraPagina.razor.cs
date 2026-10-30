using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.SecretsManager.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;
using AlgoritmoDevTools.UI.Servicios;

using Microsoft.AspNetCore.Components;

namespace AlgoritmoDevTools.UI.Paginas;

/// <summary>
/// El estado de la barra del entorno.
///
/// <para>
/// <b>Sondea cada tres segundos.</b> El widget de bandeja tiene escrito que relee el estado al abrir
/// el menú y «nunca por sondeo», y para el widget está bien: su estado cambia sólo cuando el usuario
/// hace algo. Acá cambia solo —un servicio se cae mientras mirás— y una barra que muestra lo que ya
/// no es cierto no sirve.
/// </para>
///
/// <para>
/// <b>La ventana la maneja el host.</b> El alto, el repliegue y el cierre se le piden por
/// <see cref="IVentanaDeLaBarra"/>: el HTML no puede agrandar la ventana que lo contiene.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public partial class BarraPagina : IDisposable
{
    /// <summary>Lo que mide la barra sola, sin nada desplegado. Tiene que coincidir con el CSS.</summary>
    public const int AltoDeLaBarra = 66;

    private const int AltoDeFila = 44;
    private const int MargenDelPanel = 28;
    private const int AltoMaximo = 420;

    private static readonly TimeSpan Sondeo = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Hasta cuándo se espera a un servicio que se mandó a levantar de a uno. Medido, tarda unos
    /// 40 segundos; pasado este tope se asume que no va a volver y se le devuelve el botón.
    /// </summary>
    private static readonly TimeSpan EsperaPorServicio = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan AvisoDeListo = TimeSpan.FromSeconds(8);

    [Inject]
    private IVentanaDeLaBarra Ventana { get; set; } = default!;

    private readonly IProveedorDeEntorno _entorno = new ProveedorTye();
    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private readonly CancellationTokenSource _corte = new();

    private EntornoCompleto _ultimo = EntornoCompleto.Vacio;
    private Desplegable _desplegado = Desplegable.Nada;
    private List<FilaDeServicio> _filas = [];
    private List<string> _servidores = [];
    private List<string> _basesDelServidor = [];
    private string? _servidorElegido;
    private string? _baseElegida;
    private bool _cargandoBases;
    private string? _errorDeBases;
    private DateTime? _reiniciandoDesde;

    private bool _levantando;
    private bool _frenando;

    /// <summary>
    /// Los servicios que se mandaron a levantar de a uno, con el momento en que se pidió.
    ///
    /// <para>
    /// Hace falta porque el <c>touch</c> no deja rastro: la llamada vuelve en seguida, pero el
    /// servicio tarda hasta tres cuartos de minuto en contestar y durante todo ese rato tye no
    /// informa nada distinto de un servicio caído. Sin esta lista la rueda desaparecía apenas
    /// terminaba la llamada y el botón volvía a ofrecerse con el servicio todavía abajo.
    /// </para>
    /// </summary>
    private readonly Dictionary<string, DateTime> _esperando = new(StringComparer.OrdinalIgnoreCase);
    private bool _desfasada;
    private bool _recienListo;
    private DateTime? _listoDesde;
    private string? _baseQueMuestra;
    private string? _enCurso;
    private int _faltanPorBajar;

    private bool HayEntorno => _ultimo.Vivo && !_levantando && !_frenando;

    /// <summary>
    /// Ver la lista se habilita apenas el panel contesta, sin esperar a que terminen de arrancar
    /// todos: mientras levanta es justo cuando uno quiere saber cuántos faltan y cuál se quedó.
    /// </summary>
    private bool PuedeMirar => _ultimo.Vivo && !_frenando;

    private enum Desplegable { Nada, Servicios, Agregar, Bases }

    private sealed record FilaDeServicio(string Nombre, string? Puerto, bool Arriba, string Accion, string Gerundio, bool EsAgregado, bool Arrancando);

    /// <summary>Si lo elegido en los combos es distinto de lo que está configurado ahora.</summary>
    private bool HayQueCambiar =>
        !string.IsNullOrWhiteSpace(_servidorElegido)
        && !string.IsNullOrWhiteSpace(_baseElegida)
        && !(string.Equals(_servidorElegido, _servidorEnUso, StringComparison.OrdinalIgnoreCase)
             && string.Equals(_baseElegida, _baseEnUso, StringComparison.OrdinalIgnoreCase));

    private string _servidorEnUso = string.Empty;
    private string _baseEnUso = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _temas.TemaCambiado += AlCambiarElTema;
        Ventana.AspectoCambiado += AlCambiarElAspecto;
        await Refrescar();
        _ = Sondear();
    }

    public void Dispose()
    {
        _temas.TemaCambiado -= AlCambiarElTema;
        Ventana.AspectoCambiado -= AlCambiarElAspecto;
        _corte.Cancel();
        _corte.Dispose();
    }

    private void AlCambiarElTema(object? sender, EventArgs e) => _ = InvokeAsync(StateHasChanged);

    private void AlCambiarElAspecto(object? sender, EventArgs e) => _ = InvokeAsync(StateHasChanged);

    private async Task Sondear()
    {
        using var reloj = new PeriodicTimer(Sondeo);

        try
        {
            while (await reloj.WaitForNextTickAsync(_corte.Token))
            {
                await Refrescar();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // La barra se está cerrando.
        }
    }

    // --- Estado --------------------------------------------------------------

    private async Task Refrescar()
    {
        if (_frenando) return;

        _ultimo = await _entorno.ObtenerEstadoAsync(_corte.Token);

        foreach (var nombre in _esperando.Keys.ToList())
        {
            var volvio = _ultimo.Buscar(nombre)?.Arriba == true;
            if (volvio || DateTime.Now - _esperando[nombre] > EsperaPorServicio) _esperando.Remove(nombre);
        }

        var listos = _ultimo.Listos;
        var total = _ultimo.Servicios.Count;

        // Tye corriendo pero sin panel que conteste es un entorno arrancando: está compilando o
        // todavía no abrió. Se deduce acá y no se avisa desde afuera, así la barra muestra lo mismo
        // aunque la hayan abierto a mano en el medio.
        var arrancando = !_ultimo.Vivo && LanzadorDeTye.HayTyeCorriendo();

        // El salto de «arrancando» a «todos listos» llega cuando uno ya se fue a hacer otra cosa:
        // se avisa una vez, con sonido, y el cartel queda unos segundos.
        if (_levantando && !arrancando && _ultimo.Vivo && total > 0 && listos == total)
        {
            _listoDesde = DateTime.Now;
            System.Media.SystemSounds.Asterisk.Play();
        }

        _levantando = arrancando;

        // El reinicio termina cuando vuelven a estar todos arriba. Los primeros segundos no cuentan:
        // el toque al archivo tarda en hacer efecto y hasta entonces siguen todos levantados.
        if (_reiniciandoDesde is not null)
        {
            var reciente = DateTime.Now - _reiniciandoDesde < TimeSpan.FromSeconds(6);
            var terminado = !reciente && _ultimo.Vivo && total > 0 && listos == total;

            if (terminado || DateTime.Now - _reiniciandoDesde > TimeSpan.FromMinutes(3))
            {
                _reiniciandoDesde = null;

                if (terminado)
                {
                    // Los servicios volvieron a arrancar, así que leyeron la conexión que está
                    // configurada ahora: el sello pasa a ser ésa. Sin esto la barra seguiría en
                    // ámbar avisando de una diferencia que el reinicio acaba de resolver.
                    var conexion = await Task.Run(ChequeoDePrerrequisitos.ConexionConfigurada, _corte.Token);
                    SelloDeConexion.Escribir(conexion.Servidor, conexion.Base);

                    _listoDesde = DateTime.Now;
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
        }

        _recienListo = _listoDesde is not null && DateTime.Now - _listoDesde < AvisoDeListo;
        if (_listoDesde is not null && !_recienListo) _listoDesde = null;

        // Fuera del hilo de la interfaz: la primera lectura de los secretos levanta un proceso de
        // dotnet y tarda un par de segundos.
        var (sello, levantadoCon, baseConfigurada) = await Task.Run(SelloDeConexion.Comparar, _corte.Token);
        _desfasada = _ultimo.Vivo && sello == EstadoDelSello.Difiere;
        _baseQueMuestra = _desfasada ? baseConfigurada : levantadoCon?.Base ?? baseConfigurada;

        if (_desplegado is Desplegable.Servicios or Desplegable.Agregar) ArmarLasFilas();
    }

    private string ClaseDeLaLuz() =>
        _levantando || _frenando || _reiniciandoDesde is not null ? "levantando"
        : !_ultimo.Vivo ? "sin-entorno"
        : _desfasada ? string.Empty
        : _ultimo.Listos == _ultimo.Servicios.Count ? "listo"
        : string.Empty;

    private string Titulo() =>
        _frenando ? "Frenando..."
        : _reiniciandoDesde is not null ? "Reiniciando..."
        : _recienListo ? "Entorno listo"
        : _ultimo.Vivo ? $"{_ultimo.Listos} de {_ultimo.Servicios.Count} servicios"
        : _levantando ? "Levantando..."
        : "Sin entorno";

    private string Detalle()
    {
        if (_frenando) return $"Esperando que bajen {_faltanPorBajar} servicio(s)";
        if (_reiniciandoDesde is not null) return $"{_ultimo.Listos} de {_ultimo.Servicios.Count} arriba";
        if (_levantando) return "Tye está arrancando en su consola";
        if (!_ultimo.Vivo) return "Levantalo desde DevTools";

        var perfil = ConfiguracionDevTools.Compartida.UltimoPerfilDeTye;
        var contexto = string.IsNullOrWhiteSpace(perfil) ? string.Empty : $"perfil {perfil}";

        if (!_desfasada) return contexto;

        var levantadoCon = SelloDeConexion.Leer()?.Base;
        return string.IsNullOrWhiteSpace(contexto)
            ? $"levantado contra {levantadoCon}"
            : $"{contexto} · levantado contra {levantadoCon}";
    }

    // --- Los desplegables ----------------------------------------------------

    private void VerLosServicios() => Desplegar(Desplegable.Servicios);

    private void VerLosQueFaltan() => Desplegar(Desplegable.Agregar);

    private void VerLasBases()
    {
        if (_desplegado == Desplegable.Bases)
        {
            Desplegar(Desplegable.Nada);
            return;
        }

        ArmarLasBases();
        Desplegar(Desplegable.Bases);
    }

    private void Desplegar(Desplegable cual)
    {
        _desplegado = _desplegado == cual ? Desplegable.Nada : cual;

        if (_desplegado is Desplegable.Servicios or Desplegable.Agregar) ArmarLasFilas();

        AjustarElAlto();
    }

    private void AjustarElAlto()
    {
        var cuantas = _desplegado switch
        {
            // Los dos combos entran en una fila, pero con el aviso de error pueden ser dos.
            Desplegable.Bases => 2,
            Desplegable.Nada => 0,
            _ => _filas.Count
        };

        var alto = _desplegado == Desplegable.Nada
            ? AltoDeLaBarra
            : AltoDeLaBarra + Math.Min(Math.Max(cuantas, 1) * AltoDeFila + MargenDelPanel, AltoMaximo);

        Ventana.AjustarAlto(alto);
    }

    /// <summary>
    /// Cada botón muestra una mitad: la lista, lo que está corriendo; el más, lo que falta. Mostrar
    /// todo en los dos lugares obligaba a buscar en diecisiete lo que se viene a ver de un vistazo.
    /// </summary>
    private void ArmarLasFilas()
    {
        var levantados = _ultimo.Servicios.ToDictionary(s => s.Nombre, StringComparer.OrdinalIgnoreCase);

        var delMaestro = TyeServiceToggler.ReadServices()
            .Select(s => s.Name)
            .Where(nombre => !nombre.Equals("ui", StringComparison.OrdinalIgnoreCase));

        var nombres = (_desplegado == Desplegable.Agregar
                ? delMaestro.Where(nombre => !levantados.ContainsKey(nombre))
                : delMaestro.Where(levantados.ContainsKey))
            .OrderBy(nombre => nombre, StringComparer.OrdinalIgnoreCase);

        _filas = nombres.Select(nombre =>
        {
            var servicio = levantados.GetValueOrDefault(nombre);
            var arriba = servicio?.Arriba == true;
            var esAgregado = servicio is not null && servicio.PuertoDelPanel != EntornoCompleto.PuertoPrincipal;

            var (accion, gerundio) = !arriba
                ? servicio is null ? ("Agregar", "agregando...") : ("Subir", "levantando...")
                : esAgregado ? ("Sacar", "sacando...") : ("Bajar", "apagando...");

            var puerto = servicio is not null && servicio.Puertos.Count > 0
                ? servicio.Puertos[0].ToString()
                : TyeServiceToggler.PuertoDelMaestro(nombre)?.ToString();

            // Tiene proceso pero todavía no contesta: tye lo lanzó y está arrancando. Un servicio
            // que se bajó a mano no tiene PID —se lo matamos—, así que no se confunden.
            var arrancando = servicio is { Arriba: false, Pid: not null };

            return new FilaDeServicio(nombre, puerto, arriba, accion, gerundio, esAgregado, arrancando);
        }).ToList();
    }

    /// <summary>
    /// Arma el cambio de base: los servidores salen de las conexiones guardadas y arranca parado en
    /// el que está en uso, con su base.
    ///
    /// <para>
    /// No se consulta nada al abrir, que es lo que hacía inusable al diálogo del Secrets Manager:
    /// aquél le pide las bases al servidor de la conexión que quede seleccionada, y si ésa es remota
    /// la pantalla se cuelga hasta que corta el tiempo de espera. Acá la consulta va sólo cuando el
    /// usuario elige un servidor, que es cuando dijo a cuál quiere ir.
    /// </para>
    /// </summary>
    private void ArmarLasBases()
    {
        var cadena = SecretService.Shared.GetConnectionString(Constantes.SecretKeys.Development);
        var partes = string.IsNullOrWhiteSpace(cadena)
            ? new Dictionary<string, string>()
            : ConnectionStringParser.Parse(cadena);

        _servidorEnUso = partes.GetValueOrDefault("Server", string.Empty);
        _baseEnUso = partes.GetValueOrDefault("Database", string.Empty);

        _servidores = new SavedConnectionsRepository(new ToolStorage("Shared"))
            .GetAll()
            .Select(c => c.Server)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _servidorElegido = _servidores.FirstOrDefault(s => string.Equals(s, _servidorEnUso, StringComparison.OrdinalIgnoreCase))
            ?? _servidores.FirstOrDefault();

        _baseElegida = _baseEnUso;
        _basesDelServidor = string.IsNullOrWhiteSpace(_baseEnUso) ? [] : [_baseEnUso];
        _errorDeBases = null;

        if (_servidorElegido is not null) _ = CargarLasBases(_servidorElegido);
    }

    private async Task CambiarDeServidor(ChangeEventArgs evento)
    {
        _servidorElegido = evento.Value?.ToString();
        if (_servidorElegido is null) return;

        await CargarLasBases(_servidorElegido);
    }

    private void CambiarDeBase(ChangeEventArgs evento) => _baseElegida = evento.Value?.ToString();

    /// <summary>
    /// Le pide las bases al servidor elegido. Es la única consulta que toca la red, y va con la
    /// credencial de la conexión guardada de ese servidor.
    /// </summary>
    private async Task CargarLasBases(string servidor)
    {
        _cargandoBases = true;
        _errorDeBases = null;
        StateHasChanged();

        try
        {
            var conexion = new SavedConnectionsRepository(new ToolStorage("Shared"))
                .GetAll()
                .FirstOrDefault(c => string.Equals(c.Server, servidor, StringComparison.OrdinalIgnoreCase));

            if (conexion is null)
            {
                _errorDeBases = "No hay una conexión guardada para ese servidor.";
                return;
            }

            var datos = new SQLService.ConnectionData(
                conexion.Server, conexion.User, conexion.Password, conexion.UseIntegratedSecurity);

            var resultado = await Task.Run(() => SQLService.TryGetDatabases(datos), _corte.Token);

            if (!resultado.IsSuccess)
            {
                _errorDeBases = resultado.Error;
                _basesDelServidor = [];
                return;
            }

            _basesDelServidor = resultado.Databases.ToList();
            _baseElegida = _basesDelServidor.FirstOrDefault(b => string.Equals(b, _baseEnUso, StringComparison.OrdinalIgnoreCase))
                ?? _basesDelServidor.FirstOrDefault(b => string.Equals(b, conexion.BaseEfectiva, StringComparison.OrdinalIgnoreCase))
                ?? _basesDelServidor.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _errorDeBases = ex.Message;
        }
        finally
        {
            _cargandoBases = false;
            StateHasChanged();
        }
    }

    private string ClaseDeLaFila(FilaDeServicio fila) =>
        _enCurso == fila.Nombre || EstaVolviendo(fila) ? "enproceso"
        : fila.Arriba ? "listo"
        : "abajo";

    private string AyudaDeLaFila(FilaDeServicio fila) =>
        _enCurso == fila.Nombre ? "Trabajando..."
        : EstaVolviendo(fila) ? $"{char.ToUpper(TextoDeLaEspera(fila)[0])}{TextoDeLaEspera(fila)[1..]}..."
        : fila.Arriba ? "Está corriendo"
        : "No está levantado";

    /// <summary>
    /// Un servicio que se está levantando solo no espera que nadie lo suba: muestra una rueda en vez
    /// del botón. Ofrecerlo invita a tocarlo al pedo, y encima el <c>touch</c> de un proyecto que
    /// recién arrancó lo haría reiniciar de nuevo.
    ///
    /// <para>
    /// Son tres momentos: el arranque del entorno, el reinicio de todos, y el servicio suelto que
    /// se mandó a levantar y todavía no volvió —por la lista de espera mientras no hay proceso, y
    /// por el PID una vez que tye lo lanzó—.
    /// </para>
    /// </summary>
    private bool EstaVolviendo(FilaDeServicio fila) =>
        !fila.Arriba
        && (_levantando || _reiniciandoDesde is not null || fila.Arrancando || _esperando.ContainsKey(fila.Nombre));

    /// <summary>Lo que dice la rueda: reiniciar todos y levantar uno no son lo mismo.</summary>
    private string TextoDeLaEspera(FilaDeServicio fila) =>
        _reiniciandoDesde is not null && !_esperando.ContainsKey(fila.Nombre) ? "reiniciando" : "levantando";

    // --- Acciones ------------------------------------------------------------

    private async Task Accionar(FilaDeServicio fila)
    {
        _enCurso = fila.Nombre;
        StateHasChanged();

        try
        {
            if (fila.Accion == "Agregar") await _entorno.SumarServicioAsync(fila.Nombre, _corte.Token);
            else if (fila.Accion == "Subir") await _entorno.ReiniciarServicioAsync(fila.Nombre, _corte.Token);
            else if (fila.Accion == "Sacar") await _entorno.SacarServicioAsync(fila.Nombre, _corte.Token);
            else await _entorno.BajarServicioAsync(fila.Nombre, _corte.Token);

            // Levantar uno deja la rueda hasta que el servicio está verde; bajarlo la saca, porque
            // lo que se espera es justo lo contrario.
            if (fila.Accion is "Agregar" or "Subir") _esperando[fila.Nombre] = DateTime.Now;
            else _esperando.Remove(fila.Nombre);
        }
        finally
        {
            _enCurso = null;
        }

        await Refrescar();
        AjustarElAlto();
    }

    /// <summary>
    /// Aplica el servidor y la base elegidos. No reinicia nada: los servicios que ya están arriba
    /// siguen con la anterior, y para eso el botón de reiniciar queda resaltado. Reiniciar solo
    /// bajaría los diecisiete sin preguntar, justo cuando quizá estabas probando algo.
    /// </summary>
    private async Task AplicarLaConexion()
    {
        if (_servidorElegido is null || _baseElegida is null) return;

        _enCurso = _baseElegida;
        StateHasChanged();

        try
        {
            var repositorio = new SavedConnectionsRepository(new ToolStorage("Shared"));
            var conexion = repositorio.GetAll()
                .FirstOrDefault(c => string.Equals(c.Server, _servidorElegido, StringComparison.OrdinalIgnoreCase));

            if (conexion is null) return;

            await Task.Run(() => AplicadorDeSecretos.Aplicar(conexion, _baseElegida, repositorio), _corte.Token);
        }
        finally
        {
            _enCurso = null;
        }

        Desplegar(Desplegable.Nada);
        await Refrescar();
    }

    /// <summary>
    /// Pide el reinicio y deja el estado a la vista: el pedido vuelve en milisegundos pero los
    /// servicios tardan cerca de un minuto en caerse y volver, y sin esto la barra se queda igual
    /// y parece que no hizo nada.
    /// </summary>
    private async Task ReiniciarTodo()
    {
        _reiniciandoDesde = DateTime.Now;
        StateHasChanged();

        await _entorno.ReiniciarTodoAsync(_corte.Token);
    }

    /// <summary>
    /// Frena y espera: bajar los diecisiete tarda, y una barra que desaparece enseguida deja al
    /// usuario sin saber si terminó.
    /// </summary>
    private async Task Frenar()
    {
        _frenando = true;
        _desplegado = Desplegable.Nada;
        _faltanPorBajar = _ultimo.Servicios.Count;
        Ventana.AjustarAlto(AltoDeLaBarra);
        StateHasChanged();

        await _entorno.FrenarAsync(_corte.Token);

        var limite = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < limite)
        {
            var estado = await _entorno.ObtenerEstadoAsync(_corte.Token);
            if (!estado.Vivo) break;

            _faltanPorBajar = estado.Servicios.Count;
            StateHasChanged();
            await Task.Delay(TimeSpan.FromSeconds(1), _corte.Token);
        }

        SelloDeConexion.Borrar();

        // Los servicios ya bajaron, así que la consola de tye no está haciendo nada: cerrarla es
        // parte de frenar. Si queda abierta el usuario no sabe si el entorno bajó, y la próxima vez
        // HayTyeCorriendo() dice que sí y no deja levantar.
        await Task.Run(LanzadorDeTye.CerrarLasConsolas, _corte.Token);

        // Barrer acá y no al arrancar: es el único momento en que se sabe con certeza que lo que
        // quedó colgado sobra, porque el frenado lo pidió el usuario recién.
        await new LimpiadorDelEntorno(_entorno).LimpiarAsync(_corte.Token);

        Ventana.Cerrar();
    }

    private void AbrirElPanel()
    {
        var puerto = _ultimo.Paneles.Count > 0 ? _ultimo.Paneles[0].PuertoDelPanel : EntornoCompleto.PuertoPrincipal;

        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo($"http://localhost:{puerto}") { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Sin navegador asociado no hay nada que hacer, y tampoco es algo que deba romper la barra.
        }
    }

    /// <summary>
    /// La flecha dice qué va a pasar, no dónde está: con la barra a la vista apunta arriba, porque
    /// al tocarla se esconde; escondida apunta abajo, porque al tocarla baja a la vista.
    /// </summary>
    private void Replegar()
    {
        Ventana.AlternarElRepliegue();
        StateHasChanged();
    }

    /// <summary>
    /// La flecha de la solapa apunta hacia donde va a salir la barra: abajo si está escondida
    /// arriba, hacia adentro si está en un costado.
    /// </summary>
    private MarkupString FlechaDeLaSolapa() => (MarkupString)(Ventana.SolapaContra switch
    {
        "Izquierda" => "&#xE76C;",
        "Derecha" => "&#xE76B;",
        _ => "&#xE70D;"
    });
}
