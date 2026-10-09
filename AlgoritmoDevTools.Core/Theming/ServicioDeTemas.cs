using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Guarda cuál es el tema vigente y avisa cuando cambia. Es único para todo el proceso porque el
/// tema es una sola cosa: lo elige el usuario en Configuraciones y lo tienen que ver al mismo tiempo
/// el Shell, la herramienta abierta, las que quedaron en caché y cualquier diálogo que se abra después.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public sealed class ServicioDeTemas
{
    private readonly ConfiguracionDevTools _configuracion;

    private ServicioDeTemas()
    {
        _configuracion = ConfiguracionDevTools.Compartida;
        Actual = Temas.Resolver(_configuracion.TemaId);
    }

    public static ServicioDeTemas Instancia { get; } = new();

    /// <summary>Tema vigente. Nunca es nulo: si la preferencia no resuelve, queda el tema por defecto.</summary>
    public Tema Actual { get; private set; }

    /// <summary>Preferencias generales, para que Configuraciones lea y escriba las demás opciones.</summary>
    public ConfiguracionDevTools Configuracion => _configuracion;

    /// <summary>
    /// Se dispara después de cambiar el tema. Cada ventana abierta se vuelve a pintar escuchando acá.
    /// </summary>
    public event EventHandler? TemaCambiado;

    /// <summary>
    /// Cambia el tema vigente, lo guarda y avisa. Si es el mismo que ya estaba no hace nada, para no
    /// repintar toda la aplicación al volver a tocar la tarjeta que ya estaba elegida.
    /// </summary>
    public void Cambiar(Tema tema)
    {
        ArgumentNullException.ThrowIfNull(tema);
        if (string.Equals(tema.Id, Actual.Id, StringComparison.OrdinalIgnoreCase)) return;

        Actual = tema;
        _configuracion.TemaId = tema.Id;
        _configuracion.Guardar();
        TemaCambiado?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Aplica el tema vigente a un control y a todo lo que cuelga de él.</summary>
    public void AplicarA(System.Windows.Forms.Control raiz) => AplicadorDeTema.Aplicar(raiz, Actual);
}
