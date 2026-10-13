using Microsoft.AspNetCore.Components;

namespace AlgoritmoDevTools.UI.Componentes;

public partial class TituloDeGrupo
{
    /// <summary>Nombre de la sección.</summary>
    [Parameter, EditorRequired]
    public required string Texto { get; set; }

    /// <summary>Qué hace la sección. Sin esto, el signo de pregunta no aparece.</summary>
    [Parameter]
    public string? Ayuda { get; set; }
}
