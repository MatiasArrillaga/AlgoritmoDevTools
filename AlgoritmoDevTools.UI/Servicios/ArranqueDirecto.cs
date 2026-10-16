namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Con qué pantalla abre la aplicación cuando no la abrió una persona desde el ícono.
///
/// Normalmente arranca en el inicio, que es el resumen de cómo quedó el entorno y es justo lo que
/// uno quiere ver al abrir. Pero cuando la abre el menú contextual del explorador —«Buscar en esta
/// carpeta»— el usuario ya dijo a qué venía, y dejarlo en el inicio le hace dar un rodeo para
/// llegar a lo que pidió.
///
/// Es un servicio y no un parámetro de la ventana porque quien decide la vista es el armazón, que
/// vive del otro lado del visor y no sabe nada de la línea de comandos.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md
/// </summary>
public sealed class ArranqueDirecto
{
    /// <summary>
    /// El <c>Id</c> de la herramienta con la que abrir, o null para el inicio de siempre. Es el
    /// mismo Id del catálogo y de <c>ToolStorage</c>.
    /// </summary>
    public string? HerramientaInicial { get; init; }
}
