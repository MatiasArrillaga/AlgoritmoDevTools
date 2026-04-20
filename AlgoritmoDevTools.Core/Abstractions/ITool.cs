using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Core.Abstractions;

/// <summary>
/// Contrato que debe implementar cada herramienta que se aloja en el Shell.
/// El Id se usa para aislar su almacenamiento local — cambiarlo rompe la BD existente.
/// </summary>
public interface ITool
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    Image? Icon { get; }
    UserControl CreateView();
}