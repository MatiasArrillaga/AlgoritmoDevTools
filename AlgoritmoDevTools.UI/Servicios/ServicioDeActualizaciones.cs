using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Si hay una versión más nueva publicada en GitHub.
///
/// La búsqueda la dispara el armazón al arrancar, una sola vez, y el resultado queda acá para que lo
/// usen las dos pantallas que lo necesitan: el inicio, que lo avisa, y Configuraciones, que lo
/// instala. Preguntándolo cada una por su cuenta serían dos consultas a GitHub por sesión para
/// enterarse de lo mismo.
///
/// Mientras no haya nada nuevo, <see cref="Disponible"/> queda en null y las pantallas no muestran
/// nada: no hay por qué anunciar que uno está al día.
/// </summary>
public sealed class ServicioDeActualizaciones
{
    /// <summary>La versión publicada, si es más nueva que ésta. Null mientras no la haya.</summary>
    public VersionPublicada? Disponible { get; private set; }

    /// <summary>
    /// La versión que ya quedó instalada en disco y empieza a correr en el próximo arranque. Es lo
    /// que separa "hay una para bajar" de "ya está bajada": mientras no sea null, lo único que falta
    /// es reiniciar, y las dos pantallas lo ofrecen en lugar de volver a ofrecer la descarga.
    /// </summary>
    public VersionPublicada? EsperandoReinicio { get; private set; }

    /// <summary>Si hay una búsqueda en curso, para no largar dos y para poder decirlo en pantalla.</summary>
    public bool Buscando { get; private set; }

    /// <summary>Qué pasó en el último intento, cuando alguien lo pidió a mano.</summary>
    public string? UltimoResultado { get; private set; }

    public event EventHandler? Cambio;

    /// <summary>La versión que está corriendo.</summary>
    public static Version VersionActual => Actualizador.VersionActual;

    public async Task BuscarAsync()
    {
        if (Buscando) return;

        Buscando = true;
        Avisar();

        try
        {
            Disponible = await Actualizador.BuscarAsync();
            UltimoResultado = Disponible is null ? "Estás en la última versión." : null;
        }
        finally
        {
            Buscando = false;
            Avisar();
        }
    }

    /// <summary>
    /// Baja la versión nueva y la deja puesta. No reinicia: eso lo decide la pantalla, que es la que
    /// sabe si hay algo a medio hacer.
    /// </summary>
    /// <returns>Null si salió bien, o el mensaje de error.</returns>
    public async Task<string?> InstalarAsync()
    {
        if (Disponible is not { } version) return "no hay ninguna versión para instalar.";

        var error = await Actualizador.ActualizarAsync(version);

        if (error is null)
        {
            // Deja de estar disponible y pasa a estar esperando el reinicio: ya no hay nada que
            // bajar, y ofrecerlo de nuevo sería bajar 55 MB para dejar el mismo archivo.
            Disponible = null;
            EsperandoReinicio = version;
        }

        Avisar();
        return error;
    }

    private void Avisar() => Cambio?.Invoke(this, EventArgs.Empty);
}
