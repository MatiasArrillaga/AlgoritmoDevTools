using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.CommandsMaker.Dialogs;

/// <summary>
/// Elige el dominio sobre el que trabaja el menú contextual.
///
/// Elegir es un paso aparte de ejecutar: una vez elegido, los tres comandos del menú pasan a ser
/// los de ese dominio y quedan a un clic. Por eso acá no hay botones de Add/Remove/Update.
///
/// Existe por el límite de 16 ítems del menú (ver <see cref="MenuContextualComandos"/>): los 13
/// dominios no entran como ítems, así que el menú lleva uno activo y este diálogo lo cambia.
/// </summary>
public partial class ElegirDominioDialog : Form
{
    /// <summary>Dominio elegido. Sólo tiene valor cuando el diálogo devuelve OK.</summary>
    public string Dominio { get; private set; } = string.Empty;

    public ElegirDominioDialog(IReadOnlyList<string> dominios, string? dominioActual)
    {
        InitializeComponent();

        DominiosCmb.DataSource = dominios.ToList();
        if (!string.IsNullOrWhiteSpace(dominioActual) && dominios.Contains(dominioActual))
            DominiosCmb.SelectedItem = dominioActual;
    }

    private void AceptarBtn_Click(object? sender, EventArgs e)
    {
        if (DominiosCmb.SelectedItem is not string dominio || string.IsNullOrWhiteSpace(dominio)) return;

        Dominio = dominio;
        DialogResult = DialogResult.OK;
        Close();
    }
}
