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

    public Task<IReadOnlyList<string>> ElegirArchivos(string titulo, string filtro, string? carpetaInicial = null) =>
        Diferir<IReadOnlyList<string>>(() =>
        {
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
        });

    public Task<string?> ElegirCarpeta(string titulo, string? carpetaInicial = null) =>
        Diferir<string?>(() =>
        {
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
        });

    public Task<string?> ElegirDondeGuardar(string titulo, string filtro, string nombreSugerido) =>
        Diferir<string?>(() =>
        {
            using var dialogo = new SaveFileDialog
            {
                Title = titulo,
                Filter = filtro,
                FileName = nombreSugerido,
                OverwritePrompt = true,
                AddExtension = true
            };

            return dialogo.ShowDialog(_ventana) == DialogResult.OK ? dialogo.FileName : null;
        });

    /// <summary>
    /// Abre el diálogo recién después de que termine el aviso del navegador que lo pidió.
    ///
    /// Un diálogo modal corre su propio bucle de mensajes. Abrirlo mientras el visor está entregando
    /// el clic es volver a entrar en el navegador mientras trabaja, y eso no lo tolera: **aborta el
    /// proceso entero**, sin excepción, sin mensaje y sin nada que se pueda atrapar. La ventana
    /// desaparece y listo.
    ///
    /// En el registro de eventos de Windows queda como una falla de EmbeddedBrowserWebView.dll con
    /// el código 0x80000003 —un breakpoint, no una violación de acceso—, y entre los módulos
    /// cargados aparece comdlg32.dll: el diálogo alcanzó a empezar a abrirse.
    ///
    /// <c>BeginInvoke</c> deja el diálogo para el siguiente ciclo del bucle de mensajes, ya fuera
    /// del aviso, y la tarea le devuelve el resultado a la página cuando el usuario lo cierra. Por
    /// eso los tres diálogos son asincrónicos: **no hay forma de ofrecerlos sincrónicos sin cerrar
    /// la aplicación**.
    /// </summary>
    private Task<T> Diferir<T>(Func<T> abrirElDialogo)
    {
        // Asincrónica a propósito: si la continuación corriera acá mismo, volvería a ejecutar
        // la página adentro del bucle de mensajes del diálogo que se acaba de cerrar.
        var resultado = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        _ventana.BeginInvoke(() =>
        {
            try
            {
                resultado.SetResult(abrirElDialogo());
            }
            catch (Exception ex)
            {
                resultado.SetException(ex);
            }
        });

        return resultado.Task;
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
