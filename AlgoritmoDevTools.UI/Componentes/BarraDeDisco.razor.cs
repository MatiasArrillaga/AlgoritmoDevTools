using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace AlgoritmoDevTools.UI.Componentes;

/// <summary>Un tramo de la barra. El color lo decide la clase, que le da su significado.</summary>
public sealed record TramoDelDisco(string Nombre, string Clase, long Bytes);

public partial class BarraDeDisco
{
    /// <summary>
    /// Los anchos van en el atributo style, así que el separador decimal tiene que ser el punto:
    /// con la cultura de la máquina, "15,9" no es un porcentaje válido y el tramo se dibuja con
    /// ancho cero sin que nada avise.
    /// </summary>
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    /// <summary>
    /// El ancho mínimo que se le da a un tramo, en porcentaje.
    ///
    /// Con 6,9 GB libres sobre 476 el tramo es el 1,4 % —unos cuatro píxeles— y desaparece justo
    /// cuando más importa mirarlo.
    /// </summary>
    private const double MinimoVisible = 1.2;

    [Parameter, EditorRequired]
    public required long Total { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyList<TramoDelDisco> Tramos { get; set; }

    /// <summary>
    /// Si acompaña con los nombres y los valores. En una tarjeta angosta no entra, y ahí los
    /// números ya están escritos debajo.
    /// </summary>
    [Parameter]
    public bool ConLeyenda { get; set; }

    /// <summary>Lo que lee un lector de pantalla, que no puede ver los anchos.</summary>
    [Parameter]
    public string? Descripcion { get; set; }

    /// <summary>Un tramo en cero no se dibuja: ocuparía su ancho mínimo mintiendo.</summary>
    private IEnumerable<TramoDelDisco> Visibles() => Tramos.Where(t => t.Bytes > 0);

    private double Porcentaje(TramoDelDisco tramo) =>
        Total <= 0 ? 0 : Math.Max(MinimoVisible, tramo.Bytes * 100d / Total);
}
