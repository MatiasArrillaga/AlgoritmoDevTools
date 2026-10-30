using AlgoritmoDevTools.UI.Servicios;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Lo que la interfaz puede pedirle a la ventana que la hospeda.
/// </summary>
public sealed class VentanaPrincipal : IVentanaPrincipal
{
    private readonly Form _ventana;

    public VentanaPrincipal(Form ventana) => _ventana = ventana;

    /// <summary>
    /// Se difiere con <c>BeginInvoke</c> por lo mismo que el reinicio y los diálogos: esto llega
    /// desde un clic que el visor todavía está entregando, y tocar la ventana en el medio de eso
    /// termina mal.
    /// </summary>
    public void Minimizar() => _ventana.BeginInvoke(() => _ventana.WindowState = FormWindowState.Minimized);
}
