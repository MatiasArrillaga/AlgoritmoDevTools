namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>Un aviso a la vista, con el tiempo que le queda manejado por <see cref="Avisos"/>.</summary>
/// <param name="Id">Identidad propia: es lo que deja cerrar uno sin tocar los otros.</param>
/// <param name="Texto">Lo que se lee.</param>
/// <param name="Clase">Clase de estilo, la misma familia que usa la barra de estado.</param>
/// <param name="Accion">Etiqueta del botón, cuando el aviso ofrece hacer algo. Null si sólo informa.</param>
/// <param name="AlActuar">Qué hace ese botón.</param>
public sealed record Aviso(Guid Id, string Texto, string Clase, string? Accion = null, Action? AlActuar = null);

/// <summary>
/// Avisos que aparecen un rato en una esquina y se van solos.
///
/// Es el canal para lo que termina <b>cuando el usuario ya no está mirando esa pantalla</b>: el
/// análisis del disco tarda cincuenta segundos y se puede seguir trabajando en otra herramienta
/// mientras corre. La barra de estado no alcanzaba para eso —veintiocho píxeles de alto, texto gris
/// al pie— y quien se había ido no se enteraba de que había terminado.
///
/// No reemplaza a <see cref="BarraDeEstado"/>, que es otra cosa: ahí vive el estado <b>actual</b> de
/// la pantalla y se queda mientras valga. Una pastilla es un hecho puntual y se va sola.
///
/// Se puede llamar desde cualquier hilo: lo que corre en segundo plano avisa desde el suyo. Quien
/// escuche <see cref="Cambio"/> es el que tiene que volver al hilo de la interfaz.
///
/// Cuándo usar uno y cuándo el otro, en la sección 5.2 de
/// AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md.
/// </summary>
public sealed class Avisos
{
    /// <summary>Lo que dura uno normal. Alcanza para leer un renglón sin quedar estorbando.</summary>
    private static readonly TimeSpan Normal = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Los errores duran más: el que está mirando otra cosa necesita más tiempo para darse cuenta
    /// de que algo pasó, y lo que falló suele decir más que "listo".
    /// </summary>
    private static readonly TimeSpan Error = TimeSpan.FromSeconds(12);

    private readonly List<Aviso> _activos = [];
    private readonly Lock _candado = new();

    /// <summary>Cambió la lista de avisos a la vista.</summary>
    public event Action? Cambio;

    /// <summary>Los que están a la vista, del más viejo al más nuevo.</summary>
    public IReadOnlyList<Aviso> Activos
    {
        get
        {
            lock (_candado) return [.. _activos];
        }
    }

    /// <summary>
    /// Muestra un aviso y programa que se vaya solo.
    /// </summary>
    /// <param name="texto">Lo que se lee. Conviene que diga de qué herramienta habla: puede llegar
    /// estando en otra pantalla.</param>
    /// <param name="clase">estado-exito, estado-error, estado-advertencia o estado-neutro.</param>
    public void Mostrar(string texto, string clase = "estado-neutro")
    {
        var aviso = new Aviso(Guid.NewGuid(), texto, clase);

        lock (_candado) _activos.Add(aviso);

        Cambio?.Invoke();

        var cuanto = clase == "estado-error" ? Error : Normal;

        // Sin await: el que avisa no tiene por qué esperar a que el aviso se vaya.
        _ = Expirar(aviso.Id, cuanto);
    }

    /// <summary>
    /// Deja un aviso que <b>no se va solo</b>, con un botón para hacer algo al respecto.
    ///
    /// Para lo que sigue siendo cierto hasta que el usuario haga algo —hay una versión nueva, quedó
    /// una instalada esperando el reinicio—. Uno de estos no puede expirar: si se fuera a los seis
    /// segundos, el botón se iría con él.
    ///
    /// Devuelve el identificador para poder reemplazarlo cuando la situación cambia, que es lo que
    /// evita terminar con tres pastillas diciendo cosas distintas de lo mismo.
    /// </summary>
    public Guid Fijar(string texto, string clase, string accion, Action alActuar)
    {
        var aviso = new Aviso(Guid.NewGuid(), texto, clase, accion, alActuar);

        lock (_candado) _activos.Add(aviso);

        Cambio?.Invoke();
        return aviso.Id;
    }

    private async Task Expirar(Guid id, TimeSpan cuanto)
    {
        await Task.Delay(cuanto).ConfigureAwait(false);
        Cerrar(id);
    }

    /// <summary>Saca un aviso. La usa el botón de cerrar y el vencimiento.</summary>
    public void Cerrar(Guid id)
    {
        lock (_candado)
        {
            if (_activos.RemoveAll(a => a.Id == id) == 0) return;
        }

        Cambio?.Invoke();
    }
}
