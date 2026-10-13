using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.FileProviders;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// El visor de siempre, leyendo el contenido de adentro del ejecutable en vez de una carpeta.
/// </summary>
internal sealed class VisorEmbebido : BlazorWebView
{
    public override IFileProvider CreateFileProvider(string contentRootDir) => new ContenidoWebEmbebido();
}
