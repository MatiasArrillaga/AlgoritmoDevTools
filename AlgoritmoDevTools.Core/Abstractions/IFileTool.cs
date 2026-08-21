namespace AlgoritmoDevTools.Core.Abstractions;

/// <summary>
/// La implementa la tool que sabe trabajar con archivos que le llegan de afuera, por ejemplo desde
/// el menu contextual del explorador. El Shell la usa para decidir a que tool mandarle los archivos
/// que recibio por linea de comandos.
/// </summary>
public interface IFileTool
{
    /// <summary>
    /// True si esta tool puede hacer algo con ese archivo. Se decide por la extension, sin abrirlo.
    /// </summary>
    bool CanOpen(string filePath);
}

/// <summary>
/// La implementa la vista de una <see cref="IFileTool"/> para recibir los archivos una vez creada.
/// Va aparte de <see cref="IFileTool"/> porque quien decide es la tool pero quien trabaja es la
/// vista, y la vista recien existe cuando el Shell la crea.
/// </summary>
public interface IFileReceiver
{
    void ReceiveFiles(IReadOnlyList<string> filePaths);
}
