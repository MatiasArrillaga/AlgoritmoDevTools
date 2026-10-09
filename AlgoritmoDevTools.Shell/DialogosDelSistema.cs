using System.Diagnostics;
using AlgoritmoDevTools.UI.Servicios;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Resuelve con Windows Forms lo que la interfaz HTML no puede hacer sola: abrir el diálogo de
/// archivos de Windows y mostrar una carpeta en el explorador.
///
/// Los diálogos se abren sobre la ventana principal para que queden modales a ella y no detrás.
/// </summary>
public sealed class DialogosDelSistema : IDialogosDelSistema
{
    private readonly Form _ventana;

    public DialogosDelSistema(Form ventana) => _ventana = ventana;

    public IReadOnlyList<string> ElegirArchivos(string titulo, string filtro, string? carpetaInicial = null)
    {
        // El diálogo tiene que correr en el hilo de la interfaz: el llamador es un componente Blazor,
        // que no necesariamente está en él.
        if (_ventana.InvokeRequired)
        {
            return (IReadOnlyList<string>)_ventana.Invoke(() => ElegirArchivos(titulo, filtro, carpetaInicial));
        }

        using var dialogo = new OpenFileDialog
        {
            Title = titulo,
            Filter = filtro,
            Multiselect = true,
            CheckFileExists = true
        };

        if (!string.IsNullOrWhiteSpace(carpetaInicial) && Directory.Exists(carpetaInicial))
        {
            dialogo.InitialDirectory = carpetaInicial;
        }

        return dialogo.ShowDialog(_ventana) == DialogResult.OK
            ? dialogo.FileNames
            : Array.Empty<string>();
    }

    public string? ElegirCarpeta(string titulo, string? carpetaInicial = null)
    {
        if (_ventana.InvokeRequired)
        {
            return (string?)_ventana.Invoke(() => ElegirCarpeta(titulo, carpetaInicial));
        }

        using var dialogo = new FolderBrowserDialog
        {
            Description = titulo,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrWhiteSpace(carpetaInicial) && Directory.Exists(carpetaInicial))
        {
            dialogo.SelectedPath = carpetaInicial;
        }

        return dialogo.ShowDialog(_ventana) == DialogResult.OK ? dialogo.SelectedPath : null;
    }

    public void AbrirCarpeta(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !Directory.Exists(ruta)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = ruta,
            UseShellExecute = true
        });
    }
}
