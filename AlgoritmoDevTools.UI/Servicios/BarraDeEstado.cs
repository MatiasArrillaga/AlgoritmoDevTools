namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Canal por el que una pantalla escribe en la barra de estado de la ventana.
///
/// Los avisos de "listando...", "aplicado", "no se pudo" van ahí y no dentro de la pantalla: son
/// estado de la aplicación, no contenido, y repetirlos en cada herramienta llenaba las vistas de
/// renglones que decían lo mismo en distinto lugar.
///
/// Mientras una pantalla no diga nada, la barra muestra contra qué servidor y base está apuntando
/// el entorno, que es su estado de reposo.
/// </summary>
public sealed class BarraDeEstado
{
    /// <summary>Último mensaje puesto por una pantalla, o null si ninguna dijo nada.</summary>
    public string? Mensaje { get; private set; }

    /// <summary>Clase de estilo del mensaje: exito, error, advertencia o neutro.</summary>
    public string Clase { get; private set; } = "estado-neutro";

    public event EventHandler? Cambio;

    /// <summary>Escribe un aviso en la barra.</summary>
    /// <param name="mensaje">Texto a mostrar.</param>
    /// <param name="clase">Clase de estilo; por defecto, neutro.</param>
    public void Informar(string mensaje, string clase = "estado-neutro")
    {
        Mensaje = mensaje;
        Clase = clase;
        Cambio?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Devuelve la barra a su estado de reposo. La llama la pantalla al dejar de estar visible: su
    /// último aviso no tiene por qué sobrevivirla.
    /// </summary>
    public void Limpiar()
    {
        if (Mensaje is null) return;

        Mensaje = null;
        Clase = "estado-neutro";
        Cambio?.Invoke(this, EventArgs.Empty);
    }
}
