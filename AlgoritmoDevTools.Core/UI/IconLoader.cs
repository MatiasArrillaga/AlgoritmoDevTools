using System.Drawing;
using System.Reflection;

namespace AlgoritmoDevTools.Core.UI;

public static class IconLoader
{
    /// <summary>
    /// Carga un .ico embebido como <see cref="Image"/>. Devuelve null si el recurso no existe.
    /// </summary>
    public static Image? LoadEmbedded(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var icon = new Icon(stream);
        return icon.ToBitmap();
    }

    /// <summary>
    /// Copia un .ico embebido a <c>%LOCALAPPDATA%\AlgoritmoDevTools\icons</c> y devuelve la ruta
    /// del archivo, o null si no se pudo.
    ///
    /// Hace falta porque el registro de Windows sólo sabe apuntar a un archivo (<c>ruta,índice</c>)
    /// y no a un recurso embebido; menos todavía en un publish de archivo único, donde los DLL de
    /// las tools viven adentro del exe. Quien lo use tiene que tolerar el null y quedarse sin
    /// icono: un menú sin icono sirve igual.
    /// </summary>
    public static string? ExtraerIcono(Assembly assembly, string resourceName, string nombreDestino)
    {
        var carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlgoritmoDevTools", "icons");
        var destino = Path.Combine(carpeta, nombreDestino);

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) return null;

            Directory.CreateDirectory(carpeta);
            using (var archivo = File.Create(destino))
                stream.CopyTo(archivo);

            return destino;
        }
        catch
        {
            // Si el archivo ya estaba de una corrida anterior sirve igual, aunque esta vez no se
            // haya podido reescribir (el explorador lo puede tener abierto para dibujar el menú).
            return File.Exists(destino) ? destino : null;
        }
    }
}
