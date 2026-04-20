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
}
