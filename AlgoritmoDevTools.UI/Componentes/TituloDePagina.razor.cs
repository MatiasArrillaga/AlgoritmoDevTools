using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Componentes;

public partial class TituloDePagina
{
    /// <summary>Nombre de la herramienta.</summary>
    [Parameter, EditorRequired]
    public required string Texto { get; set; }

    /// <summary>Explicación propia de la pantalla. Si no se pasa, se usa la del catálogo.</summary>
    [Parameter]
    public string? Ayuda { get; set; }

    [CascadingParameter(Name = "AyudaDeLaHerramienta")]
    private string? AyudaHeredada { get; set; }

    /// <summary>
    /// Une lo que dice el catálogo sobre la herramienta con lo que agrega la pantalla. Son
    /// complementarias: la primera dice para qué sirve, la segunda qué hace exactamente acá.
    /// </summary>
    private string? TextoDeAyuda
    {
        get
        {
            var partes = new[] { AyudaHeredada, Ayuda }
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!.Trim());

            var texto = string.Join(" ", partes);
            return string.IsNullOrWhiteSpace(texto) ? null : texto;
        }
    }
}
