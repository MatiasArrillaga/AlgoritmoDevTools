using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class HerramientaPendiente
{
    [Parameter, EditorRequired]
    public required HerramientaUI Herramienta { get; set; }
}
