using System.Diagnostics;
using System.Globalization;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

/// <summary>
/// La pantalla de la limpieza de disco.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public partial class DiskCleanerPagina : IDisposable
{
    /// <summary>
    /// Los dos íconos que encabezan cada grupo, del mismo juego que los del panel lateral: 16x16,
    /// trazo, sin relleno. Encabezan el grupo en lugar de repetirse en cada fila, que era una
    /// columna entera diciendo siempre lo mismo. El color lo pone el CSS, con los colores de
    /// estado: verde y ámbar significan lo mismo en las cuatro paletas.
    /// </summary>
    private const string IconoSeguro =
        """<svg viewBox="0 0 16 16"><circle cx="8" cy="8" r="6"/><path d="m5.4 8.1 1.9 1.9 3.3-3.9"/></svg>""";

    private const string IconoConCriterio =
        """<svg viewBox="0 0 16 16"><path d="M8 2.4 14.4 13H1.6L8 2.4z"/><path d="M8 6.6v2.8M8 11.2v.4"/></svg>""";

    /// <summary>
    /// Lo que hay que escribir para confirmar algo que no se puede deshacer.
    ///
    /// Un botón más no alcanza: el que está por apretar "sí" otra vez ya decidió. Escribir obliga
    /// a detenerse, que es lo único que separa una limpieza de rutina de perder la papelera entera.
    /// </summary>
    private const string PalabraDeConfirmacion = "BORRAR";

    [Inject] private EscaneoService Escaneo { get; set; } = default!;
    [Inject] private LimpiezaService Limpieza { get; set; } = default!;
    [Inject] private EjecutorDeLimpieza Ejecutor { get; set; } = default!;
    [Inject] private ElevacionService Elevacion { get; set; } = default!;
    [Inject] private PerfilRepository Perfil { get; set; } = default!;
    [Inject] private BarraDeEstado Barra { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private Escaneo? _escaneo;
    private List<ItemOfrecido> _items = [];
    private ItemOfrecido? _elegido;
    private bool _escaneando;
    private int _ignorados;
    private PlanDeLimpieza? _plan;
    private ResultadoDeLimpieza? _resultado;
    private bool _borrando;
    private string? _borrandoAhora;
    private DateTime _ultimoAviso;
    private CancellationTokenSource? _cancelandoBorrado;
    private bool _elevando;

    /// <summary>Los grupos plegados. Vale para esta vista: no se recuerda entre corridas.</summary>
    private readonly HashSet<GrupoLimpieza> _contraidos = [];
    private string _confirmacionEscrita = string.Empty;
    private CancellationTokenSource? _cancelacion;

    /// <summary>
    /// El estado intermedio de una casilla no existe en el marcado: es una propiedad del elemento
    /// ya creado, así que no se puede pedir desde Blazor. El componente deja escrito
    /// <c>data-parcial</c> y esto lo traduce después de cada dibujado, igual que el árbol de
    /// exportación.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool primeraVez)
    {
        if (_items.Count > 0) await JS.InvokeVoidAsync("devtools.sincronizarParciales");
    }

    private async Task Escanear()
    {
        _escaneando = true;
        _cancelacion = new CancellationTokenSource();
        _resultado = null;
        Barra.Limpiar();

        try
        {
            var resultado = await Escaneo.EscanearAsync(_cancelacion.Token);
            Mostrar(resultado);

            // Es el unico momento en que este numero es cierto: el inicio lo muestra tal cual, con
            // su fecha, en lugar de volver a medir medio disco para abrir una pantalla.
            Perfil.GuardarEscaneo(Gestionable());

            Barra.Informar(
                $"{_items.Count} cosas encontradas, {Tamanos.Legible(Gestionable())} que se pueden limpiar.",
                "estado-exito");
        }
        catch (OperationCanceledException)
        {
            Barra.Informar("Análisis cancelado.", "estado-neutro");
        }
        catch (Exception ex)
        {
            Barra.Informar($"No se pudo analizar el disco: {ex.Message}", "estado-error");
        }
        finally
        {
            _escaneando = false;
            _cancelacion?.Dispose();
            _cancelacion = null;
        }
    }

    private void Mostrar(Escaneo resultado)
    {
        _escaneo = resultado;
        _items = [.. Perfil.Aplicar(resultado.Items)];
        _ignorados = resultado.Items.Count - _items.Count;
        _elegido = null;
    }

    private void Cancelar() => _cancelacion?.Cancel();

    /// <summary>
    /// El mismo botón abre y cierra la confirmación. Al abrirla se arma el plan en el momento: lo
    /// que se muestra tiene que ser lo que hay ahora, no lo que había cuando se analizó.
    /// </summary>
    private async Task AlternarConfirmacion()
    {
        if (_plan is not null)
        {
            CerrarConfirmacion();
            return;
        }

        // Abrir la confirmación cierra el detalle: dos bloques desplegados a la vez dejan el botón
        // de borrar lejos de lo que lo explica.
        _elegido = null;
        _confirmacionEscrita = string.Empty;

        try
        {
            _plan = await Limpieza.PrevisualizarAsync(_items);
        }
        catch (Exception ex)
        {
            Barra.Informar($"No se pudo preparar la limpieza: {ex.Message}", "estado-error");
        }
    }

    private void CerrarConfirmacion()
    {
        _plan = null;
        _confirmacionEscrita = string.Empty;
    }

    /// <summary>
    /// Ejecuta el plan y se queda con el reporte.
    ///
    /// Al terminar <b>se descarta lo medido</b>: los tamaños que estaban en pantalla dejaron de ser
    /// ciertos en el momento en que se borró. Volver a analizar es una decisión del usuario y no
    /// algo que pase solo, porque tarda y porque el reporte ya dice qué se liberó.
    /// </summary>
    private async Task Borrar()
    {
        if (_plan is null || !SePuedeConfirmar()) return;

        _borrando = true;
        _cancelandoBorrado = new CancellationTokenSource();
        Barra.Informar("Borrando…", "estado-neutro");

        try
        {
            var resultado = await Ejecutor.EjecutarSinElevarAsync(
                _plan, QueSeEstaBorrando(), _cancelandoBorrado.Token);

            _resultado = resultado;
            Perfil.GuardarLimpieza(resultado.BytesLiberados);
            _escaneo = null;
            _items = [];
            _elegido = null;
            _ignorados = 0;
            CerrarConfirmacion();

            Barra.Informar(Cierre(resultado), resultado.Fallidas > 0 ? "estado-advertencia" : "estado-exito");
        }
        catch (Exception ex)
        {
            Barra.Informar($"La limpieza se interrumpió: {ex.Message}", "estado-error");
        }
        finally
        {
            _borrando = false;
            _borrandoAhora = null;
            _cancelandoBorrado?.Dispose();
            _cancelandoBorrado = null;
        }
    }

    /// <summary>
    /// Corta lo que falta. Lo que ya se borró no vuelve: el reporte lo muestra igual, porque es
    /// peor dejar al usuario sin saber qué alcanzó a pasar.
    /// </summary>
    private void CancelarBorrado()
    {
        _cancelandoBorrado?.Cancel();
        _borrandoAhora = "Cortando…";
    }

    /// <summary>
    /// Lo que se está borrando en este momento, para que la espera no parezca un cuelgue.
    ///
    /// Deja pasar a lo sumo un aviso cada 80 ms. Borrar 428 carpetas llega en ráfaga, y redibujar
    /// la pantalla una vez por carpeta cuesta más que el borrado y encima deja un texto que no se
    /// alcanza a leer.
    /// </summary>
    private IProgress<string> QueSeEstaBorrando() => new Progress<string>(que =>
    {
        var ahora = DateTime.UtcNow;
        if (ahora - _ultimoAviso < TimeSpan.FromMilliseconds(80)) return;

        _ultimoAviso = ahora;
        _borrandoAhora = que;
        StateHasChanged();
    });

    /// <summary>
    /// Pide el permiso de administrador <b>una sola vez</b> y borra lo que había quedado,
    /// fusionando lo que salga con el reporte que ya estaba.
    /// </summary>
    private async Task ElevarYBorrar()
    {
        if (_resultado is null || _resultado.ParaElevar.Count == 0) return;

        _elevando = true;
        Barra.Informar("Esperando el permiso de administrador…", "estado-neutro");

        try
        {
            var elevados = await Elevacion.EjecutarAsync(_resultado.ParaElevar);

            if (elevados is null)
            {
                // Cerrar el cartel de permisos es una respuesta, no una falla: lo que quedó sin
                // borrar sigue ofrecido y nadie tiene que arreglar nada.
                Barra.Informar("No se dio el permiso, así que eso quedó sin borrar.", "estado-neutro");
                return;
            }

            _resultado = Fusionar(_resultado, elevados);
            Perfil.GuardarLimpieza(_resultado.BytesLiberados);
            Barra.Informar(Cierre(_resultado), _resultado.Fallidas > 0 ? "estado-advertencia" : "estado-exito");
        }
        catch (Exception ex)
        {
            Barra.Informar($"No se pudo completar con permisos: {ex.Message}", "estado-error");
        }
        finally
        {
            _elevando = false;
        }
    }

    /// <summary>
    /// Suma lo que hizo la parte elevada al reporte, y vacía lo pendiente: ya no queda nada
    /// esperando permiso.
    /// </summary>
    private static ResultadoDeLimpieza Fusionar(ResultadoDeLimpieza previo, IReadOnlyList<ResultadoElevado> elevados)
    {
        var porId = previo.ParaElevar.ToDictionary(i => i.Item.Id, i => i.Item, StringComparer.Ordinal);
        var sumados = new List<ResultadoPorItem>(previo.Items);

        foreach (var elevado in elevados)
        {
            if (!porId.TryGetValue(elevado.ItemId, out var item)) continue;

            // Del otro lado del permiso, quién tiene tomado un archivo vuelve ya armado como texto:
            // el proceso que lo averiguó era otro y su número no sirve de este lado.
            var fallos = elevado.Fallos
                .Select(f => new FalloDeBorrado(f.Ruta, f.Motivo, []))
                .ToArray();

            sumados.Add(new ResultadoPorItem(item, elevado.BytesLiberados, elevado.Borradas, fallos));
        }

        return new ResultadoDeLimpieza(sumados, []);
    }

    /// <summary>
    /// El mensaje de la barra, que es el que queda. Los dos finales van ahí: el que salió bien y el
    /// que no.
    /// </summary>
    private static string Cierre(ResultadoDeLimpieza resultado)
    {
        var liberado = resultado.Cancelado
            ? $"Cortado: alcanzó a liberar {Tamanos.Legible(resultado.BytesLiberados)}"
            : $"Se liberaron {Tamanos.Legible(resultado.BytesLiberados)}";

        if (resultado.Fallidas == 0) return $"{liberado}.";

        return $"{liberado}, y {resultado.Fallidas} " +
               $"{(resultado.Fallidas == 1 ? "cosa quedó" : "cosas quedaron")} sin borrar.";
    }

    /// <summary>Lo que entra al plan y no tiene vuelta atrás.</summary>
    private IReadOnlyList<ItemDelPlan> SinVuelta() =>
        _plan is null ? [] : [.. _plan.Items.Where(i => i.Item.ComoSeRevierte is null)];

    /// <summary>
    /// Sin nada irreversible alcanza con el botón. Con algo irreversible hace falta además la
    /// palabra, escrita tal cual.
    /// </summary>
    private bool SePuedeConfirmar()
    {
        if (_plan is null || _plan.EstaVacio) return false;
        if (SinVuelta().Count == 0) return true;

        return string.Equals(_confirmacionEscrita.Trim(), PalabraDeConfirmacion, StringComparison.Ordinal);
    }

    private void Seleccionar(ItemOfrecido ofrecido)
    {
        // Abrir un detalle cierra la confirmación: si no, el plan que se ve quedaría describiendo
        // una selección que el usuario está cambiando atrás.
        CerrarConfirmacion();
        _elegido = Seleccionado(ofrecido) ? null : ofrecido;
    }

    private bool Seleccionado(ItemOfrecido ofrecido) =>
        _elegido is not null && _elegido.Item.Id == ofrecido.Item.Id;

    private void Tildar(ItemOfrecido ofrecido, bool tildado)
    {
        if (!ofrecido.Item.SePuedeElegir) return;

        var posicion = _items.FindIndex(i => i.Item.Id == ofrecido.Item.Id);
        if (posicion < 0) return;

        _items[posicion] = _items[posicion] with { Tildado = tildado };
        if (Seleccionado(ofrecido)) _elegido = _items[posicion];

        // Lo elegido cambió, así que el plan que estuviera a la vista ya no lo describe.
        CerrarConfirmacion();
        Perfil.Guardar([_items[posicion]]);
    }

    /// <summary>
    /// Tilda o destilda todo lo elegible del grupo. Lo que está trabado o no se borra queda como
    /// está: la casilla de arriba no puede elegir lo que las de abajo no dejan elegir.
    /// </summary>
    private void TildarGrupo(GrupoLimpieza grupo, bool tildado)
    {
        var cambiados = new List<ItemOfrecido>();

        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Item.Grupo != grupo || !_items[i].Item.SePuedeElegir) continue;

            _items[i] = _items[i] with { Tildado = tildado };
            cambiados.Add(_items[i]);
        }

        if (cambiados.Count == 0) return;

        if (_elegido is not null)
            _elegido = _items.FirstOrDefault(i => i.Item.Id == _elegido.Item.Id) ?? _elegido;

        CerrarConfirmacion();
        Perfil.Guardar(cambiados);
    }

    private bool EstaContraido(GrupoLimpieza grupo) => _contraidos.Contains(grupo);

    /// <summary>
    /// Uno por grupo, con nombre, en lugar de una lambda armada en el marcado.
    ///
    /// Una lambda inline se reconstruye en cada dibujado, así que el <c>EventCallback</c> que
    /// recibe el encabezado cambia de identidad todas las veces y lo obliga a redibujarse sin que
    /// nada haya cambiado. Con un método nombrado el delegate es el mismo —mismo objeto, mismo
    /// método— y la comparación da igual.
    /// </summary>
    private void AlternarSeguro(bool _) => AlternarGrupo(GrupoLimpieza.Seguro);

    private void AlternarConCriterio(bool _) => AlternarGrupo(GrupoLimpieza.ConCriterio);

    private void AlternarGrupo(GrupoLimpieza grupo)
    {
        if (!_contraidos.Add(grupo)) _contraidos.Remove(grupo);

        // Plegar no cambia lo elegido, pero sí deja fuera de la vista la fila que explicaba el
        // detalle abierto.
        if (_elegido is not null && _elegido.Item.Grupo == grupo) _elegido = null;
    }

    /// <summary>
    /// Lo elegido del grupo sobre lo que se podría elegir.
    ///
    /// Siempre con las dos cifras, incluso con cero elegido: así el renglón no cambia de forma al
    /// tildar y el número de la izquierda se lee siempre en el mismo lugar.
    ///
    /// El total cuenta sólo lo que se borra: sumar los informativos prometería espacio que la
    /// herramienta no va a liberar.
    /// </summary>
    private string TotalDelGrupo(GrupoLimpieza grupo)
    {
        var delGrupo = _items.Where(i => i.Item.Grupo == grupo).ToArray();
        var elegido = delGrupo.Where(i => i.Tildado).Sum(i => i.Item.Bytes);
        var total = delGrupo.Where(i => i.Item.SeBorra).Sum(i => i.Item.Bytes);

        return $"{Tamanos.Legible(elegido)} de {Tamanos.Legible(total)}";
    }

    private bool HayElegibles(GrupoLimpieza grupo) =>
        _items.Any(i => i.Item.Grupo == grupo && i.Item.SePuedeElegir);

    private EstadoDelTilde EstadoDelGrupo(GrupoLimpieza grupo)
    {
        var elegibles = _items.Where(i => i.Item.Grupo == grupo && i.Item.SePuedeElegir).ToArray();
        if (elegibles.Length == 0) return EstadoDelTilde.Ninguno;

        var tildados = elegibles.Count(i => i.Tildado);

        return tildados == 0 ? EstadoDelTilde.Ninguno
            : tildados == elegibles.Length ? EstadoDelTilde.Todos
            : EstadoDelTilde.Algunos;
    }

    /// <summary>
    /// "No me lo ofrezcas más": el ítem desaparece del resultado en lugar de quedar destildado, que
    /// es lo que pidió quien lo marcó.
    /// </summary>
    private void AlternarIgnorar(ItemOfrecido ofrecido)
    {
        Perfil.Ignorar(ofrecido.Item.Id);
        _items.RemoveAll(i => i.Item.Id == ofrecido.Item.Id);
        _ignorados++;
        _elegido = null;

        Barra.Informar($"{ofrecido.Item.Ubicacion} no se va a volver a ofrecer.", "estado-neutro");
    }

    private void MostrarTodoDeNuevo()
    {
        if (_escaneo is null) return;

        var visibles = _items.Select(i => i.Item.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var item in _escaneo.Items.Where(i => !visibles.Contains(i.Id)))
            Perfil.DejarDeIgnorar(item.Id);

        Mostrar(_escaneo);
        Barra.Informar("Vuelven a ofrecerse todas.", "estado-neutro");
    }

    /// <summary>
    /// Abre lo que el ítem ofrece en lugar de borrar: hoy, el instalador de Visual Studio.
    ///
    /// Es otro ejecutable y no una ventana de esta aplicación, así que no corre el riesgo de los
    /// diálogos modales —que abiertos desde un evento de la página matan el proceso—: lo que se
    /// lanza tiene su propio bucle de mensajes, aparte.
    /// </summary>
    private void Derivar(AccionDerivada derivacion)
    {
        try
        {
            Process.Start(new ProcessStartInfo(derivacion.Destino) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Barra.Informar($"No se pudo abrir: {ex.Message}", "estado-error");
        }
    }

    private long Elegido() => _items.Where(i => i.Tildado).Sum(i => i.Item.Bytes);

    private long Gestionable() => _items.Where(i => i.Item.SeBorra).Sum(i => i.Item.Bytes);

    private long SePuede() => Gestionable() - Elegido();

    private string Resumen() =>
        $"{Tamanos.Legible(_escaneo!.Disco.Libre)} libres de {Tamanos.Legible(_escaneo.Disco.Total)} · {Tamanos.Legible(Gestionable())} se pueden limpiar";

    /// <summary>
    /// Los cuatro tramos de la barra, en orden. El ancho y el mínimo visible los resuelve el
    /// componente: acá sólo se dice qué es cada tramo y cuánto pesa.
    /// </summary>
    private IReadOnlyList<TramoDelDisco> Tramos()
    {
        var total = _escaneo!.Disco.Total;
        var elegido = Elegido();
        var sePuede = SePuede();
        var libre = _escaneo.Disco.Libre;

        // Lo que queda es todo el resto del disco, sin desglosar: ahí adentro está lo que no se
        // debe borrar, y nombrarlo invita a borrarlo por fuera de la herramienta.
        var ocupado = Math.Max(0, total - elegido - sePuede - libre);

        return
        [
            new TramoDelDisco("Elegido para limpiar", "tramo-elegido", elegido),
            new TramoDelDisco("Se puede limpiar", "tramo-posible", sePuede),
            new TramoDelDisco("Ocupado", "tramo-ocupado", ocupado),
            new TramoDelDisco("Libre", "tramo-libre", libre)
        ];
    }

    private string DescripcionDeLaBarra() =>
        $"Disco de {Tamanos.Legible(_escaneo!.Disco.Total)}: {Tamanos.Legible(Elegido())} elegidos para limpiar, " +
        $"{Tamanos.Legible(SePuede())} que se pueden limpiar y {Tamanos.Legible(_escaneo.Disco.Libre)} libres.";


    public void Dispose()
    {
        _cancelacion?.Cancel();
        _cancelacion?.Dispose();
        Barra.Limpiar();
    }

    private enum EstadoDelTilde { Ninguno, Algunos, Todos }

}
