using AlgoritmoDevTools.Tools.DiskCleaner.Model;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Lo medido del disco y el análisis en curso, fuera de la pantalla que los muestra.
///
/// Vive mientras vive la aplicación, y no mientras se está mirando el Disk Cleaner, por dos cosas
/// que pasaban cuando el estado era del componente:
///
/// <list type="bullet">
/// <item>
/// <b>Cambiar de herramienta durante el análisis cerraba la aplicación.</b> El análisis es un
/// controlador asincrónico de cincuenta segundos; al irse, Blazor desarma el componente y el
/// <c>Dispose</c> cancelaba <b>y liberaba</b> el <see cref="CancellationTokenSource"/> en el acto,
/// con el recorrido del disco todavía en vuelo. Cuando el controlador volvía del <c>await</c>
/// intentaba repintar un componente que ya no existía, y en el visor una excepción que nadie atrapa
/// cierra la ventana entera.
/// </item>
/// <item>
/// <b>Cambiar de herramienta después del análisis lo perdía.</b> Lo medido se iba con el
/// componente y había que esperar los cincuenta segundos de nuevo para volver a ver lo mismo.
/// </item>
/// </list>
///
/// Acá el <see cref="CancellationTokenSource"/> se libera cuando la operación terminó de verdad, en
/// el <c>finally</c> de quien la lanzó, que es el único momento en que nadie más lo está usando.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class SesionDeLimpieza
{
    private readonly EscaneoService _servicio;
    private readonly PerfilRepository _perfil;
    private readonly BarraDeEstado _barra;
    private readonly Avisos _avisos;

    private CancellationTokenSource? _cancelacion;

    public SesionDeLimpieza(EscaneoService servicio, PerfilRepository perfil, BarraDeEstado barra, Avisos avisos)
    {
        _servicio = servicio;
        _perfil = perfil;
        _barra = barra;
        _avisos = avisos;
    }

    /// <summary>Lo último que se midió, o null si todavía no se analizó o se limpió.</summary>
    public Escaneo? Medido { get; private set; }

    /// <summary>
    /// Lo encontrado, con el tildado de cada cosa. Es <b>la misma lista</b> que modifica la
    /// pantalla al tildar: lo elegido también tiene que sobrevivir a irse a otra herramienta.
    /// </summary>
    public List<ItemOfrecido> Items { get; private set; } = [];

    /// <summary>Cuántas cosas no se muestran porque el usuario pidió no verlas.</summary>
    public int Ignorados { get; private set; }

    /// <summary>Si hay un análisis corriendo, se esté mirando la pantalla o no.</summary>
    public bool Analizando { get; private set; }

    /// <summary>
    /// Cambió algo que la pantalla muestra. Se dispara desde el hilo en el que terminó el análisis,
    /// así que quien escuche tiene que volver al suyo antes de tocar la interfaz.
    /// </summary>
    public event Action? Cambio;

    /// <summary>Lo que se puede limpiar, sin contar lo que sólo se informa.</summary>
    public long Gestionable() => Items.Where(i => i.Item.SeBorra).Sum(i => i.Item.Bytes);

    /// <summary>
    /// Lanza el análisis y vuelve en el acto. Si ya hay uno corriendo no hace nada: dos recorridos
    /// del disco a la vez tardan más que uno y se pisan en el mismo resultado.
    /// </summary>
    public void Analizar()
    {
        if (Analizando) return;

        var cancelacion = new CancellationTokenSource();

        _cancelacion = cancelacion;
        Analizando = true;
        Medido = null;
        Items = [];
        Ignorados = 0;

        _barra.Informar("Analizando el disco…", "estado-neutro");
        Cambio?.Invoke();

        // Sin await y sin guardar la tarea: lo que hay que saber de ella —si sigue, y qué dio— está
        // en las propiedades de acá, y lo que falle lo informa ella misma por la barra.
        _ = Correr(cancelacion);
    }

    private async Task Correr(CancellationTokenSource cancelacion)
    {
        try
        {
            var resultado = await _servicio.EscanearAsync(cancelacion.Token).ConfigureAwait(false);

            Medido = resultado;
            Items = [.. _perfil.Aplicar(resultado.Items)];
            Ignorados = resultado.Items.Count - Items.Count;

            // Es el único momento en que este número es cierto: el inicio lo muestra tal cual, con
            // su fecha, en lugar de volver a medir medio disco para abrir una pantalla.
            _perfil.GuardarEscaneo(Gestionable());

            // Con el nombre de la herramienta adelante porque el aviso puede llegar estando en otra
            // pantalla, y ahí solo "23 cosas encontradas" no dice de qué habla.
            Avisar(
                $"Disk Cleaner: {Items.Count} cosas encontradas, " +
                $"{Tamanos.Legible(Gestionable())} que se pueden limpiar.",
                "estado-exito");
        }
        catch (OperationCanceledException)
        {
            // Cancelar es algo que acaba de hacer el usuario, mirando: no hace falta una pastilla.
            _barra.Informar("Análisis cancelado.", "estado-neutro");
        }
        catch (Exception ex)
        {
            Avisar($"Disk Cleaner: no se pudo analizar el disco. {ex.Message}", "estado-error");
        }
        finally
        {
            Analizando = false;
            _cancelacion = null;

            // Recién acá: liberarlo mientras el recorrido todavía lo usa es lo que rompía al
            // cambiar de herramienta en el medio.
            cancelacion.Dispose();

            Cambio?.Invoke();
        }
    }

    /// <summary>
    /// Deja el aviso en los dos lados, porque sirven para cosas distintas: en la barra queda
    /// mientras la pantalla valga, y la pastilla aparece aunque el usuario esté en otra herramienta.
    ///
    /// Sólo la barra no alcanzaba: lo que escribe una pantalla lo borra la siguiente, así que el
    /// que se iba a hacer otra cosa nunca se enteraba de que el análisis había terminado.
    /// </summary>
    private void Avisar(string texto, string clase)
    {
        _barra.Informar(texto, clase);
        _avisos.Mostrar(texto, clase);
    }

    /// <summary>Corta el análisis. Lo que ya se midió de los detectores que terminaron se descarta.</summary>
    public void Cancelar() => _cancelacion?.Cancel();

    /// <summary>
    /// Vuelve a filtrar lo medido con el perfil, sin volver a medir. La usa la pantalla cuando
    /// cambia qué cosas se ignoran: el disco no cambió, cambió qué se muestra de lo que ya se vio.
    ///
    /// El tildado no se pierde al rearmar la lista porque lo guarda el perfil en cuanto se toca una
    /// casilla, y <c>Aplicar</c> lo devuelve puesto.
    /// </summary>
    public void ReaplicarPerfil()
    {
        if (Medido is null) return;

        Items = [.. _perfil.Aplicar(Medido.Items)];
        Ignorados = Medido.Items.Count - Items.Count;
        Cambio?.Invoke();
    }

    /// <summary>
    /// Descarta lo medido. La usa la pantalla después de limpiar: los tamaños que estaban dejaron
    /// de ser ciertos en el momento en que se borró.
    /// </summary>
    public void Olvidar()
    {
        Medido = null;
        Items = [];
        Ignorados = 0;
        Cambio?.Invoke();
    }
}
