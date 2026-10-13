namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Lo que la interfaz HTML no puede hacer por su cuenta y resuelve la ventana que la hospeda.
///
/// El motor de Edge no da la ruta en disco de un archivo elegido ni arrastrado: por seguridad sólo
/// entrega su contenido. Como las herramientas trabajan con rutas —pandoc convierte un archivo del
/// repositorio, no una copia subida—, elegir archivos tiene que pasar por el diálogo de Windows.
///
/// Los tres diálogos devuelven una tarea y hay que esperarlos. No es por rendimiento: abrirlos en el
/// acto cierra la aplicación. Está explicado en <c>DialogosDelSistema</c>, en el Shell.
/// </summary>
public interface IDialogosDelSistema
{
    /// <summary>
    /// Abre el diálogo de Windows para elegir archivos.
    /// </summary>
    /// <param name="titulo">Título de la ventana.</param>
    /// <param name="filtro">Filtro en el formato de Windows, por ejemplo "Documentos|*.docx;*.html".</param>
    /// <param name="carpetaInicial">Carpeta donde se abre, o null para la última que usó Windows.</param>
    /// <returns>Las rutas elegidas, o una lista vacía si se canceló.</returns>
    Task<IReadOnlyList<string>> ElegirArchivos(string titulo, string filtro, string? carpetaInicial = null);

    /// <summary>
    /// Abre el diálogo de Windows para elegir una carpeta.
    /// </summary>
    /// <param name="titulo">Texto que explica qué se está eligiendo.</param>
    /// <param name="carpetaInicial">Dónde se abre, o null para el escritorio.</param>
    /// <returns>La carpeta elegida, o null si se canceló.</returns>
    Task<string?> ElegirCarpeta(string titulo, string? carpetaInicial = null);

    /// <summary>
    /// Abre el diálogo de Windows para elegir dónde guardar un archivo. Avisa si se va a pisar uno
    /// que ya existe, que es la única parte del diálogo que no se puede dar por descontada.
    /// </summary>
    /// <param name="titulo">Título de la ventana.</param>
    /// <param name="filtro">Filtro en el formato de Windows, por ejemplo "Configuración|*.json".</param>
    /// <param name="nombreSugerido">Con qué nombre aparece propuesto.</param>
    /// <returns>La ruta elegida, o null si se canceló.</returns>
    Task<string?> ElegirDondeGuardar(string titulo, string filtro, string nombreSugerido);

    /// <summary>Abre una carpeta en el explorador de Windows.</summary>
    void AbrirCarpeta(string ruta);
}
