namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Lo que la interfaz HTML no puede hacer por su cuenta y resuelve la ventana que la hospeda.
///
/// El motor de Edge no da la ruta en disco de un archivo elegido ni arrastrado: por seguridad sólo
/// entrega su contenido. Como las herramientas trabajan con rutas —pandoc convierte un archivo del
/// repositorio, no una copia subida—, elegir archivos tiene que pasar por el diálogo de Windows.
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
    IReadOnlyList<string> ElegirArchivos(string titulo, string filtro, string? carpetaInicial = null);

    /// <summary>
    /// Abre el diálogo de Windows para elegir una carpeta.
    /// </summary>
    /// <param name="titulo">Texto que explica qué se está eligiendo.</param>
    /// <param name="carpetaInicial">Dónde se abre, o null para el escritorio.</param>
    /// <returns>La carpeta elegida, o null si se canceló.</returns>
    string? ElegirCarpeta(string titulo, string? carpetaInicial = null);

    /// <summary>Abre una carpeta en el explorador de Windows.</summary>
    void AbrirCarpeta(string ruta);
}
