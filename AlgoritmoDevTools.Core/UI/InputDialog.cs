using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Core.UI;

/// <summary>
/// Diálogo simple de entrada de texto. Reemplaza Microsoft.VisualBasic.Interaction.InputBox
/// para evitar la dependencia con el runtime de VB.
/// </summary>
public static class InputDialog
{
    public static string? Show(string prompt, string title, string defaultValue = "", IWin32Window? owner = null)
    {
        using var form = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(420, 140),
            ShowInTaskbar = false
        };

        var label = new Label { Left = 12, Top = 14, AutoSize = true, Text = prompt, MaximumSize = new Size(396, 0) };
        var textBox = new TextBox { Left = 12, Top = 50, Width = 396, Text = defaultValue };
        var okButton = new Button { Text = "OK", Left = 236, Width = 80, Top = 90, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancelar", Left = 322, Width = 86, Top = 90, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        return form.ShowDialog(owner) == DialogResult.OK ? textBox.Text : null;
    }
}
