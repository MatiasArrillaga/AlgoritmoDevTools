using System.Diagnostics;
using System.Runtime.InteropServices;

using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Levanta la barra del entorno, que es un proceso aparte.
///
/// <para>
/// <b>Aparte a propósito</b>, por lo mismo que el widget de bandeja: la barra tiene que seguir
/// mientras siga el entorno, y la ventana principal se minimiza o se cierra bastante antes que
/// eso. Si fuera una ventana de la aplicación, cerrarla dejaría el entorno corriendo sin nadie
/// mirándolo.
/// </para>
///
/// <para>
/// La lanzan dos lugares —la pantalla del Selector al levantar y el menú de la bandeja—, y ninguno
/// necesita hablar con ella después: la barra lee el estado del entorno por su cuenta.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public sealed class ArranqueDeLaBarra
{
    /// <summary>
    /// Con esto la barra se asegura de ser una sola. Dos barras serían dos ventanas encima de todo
    /// mostrando lo mismo.
    /// </summary>
    public const string MutexDeInstanciaUnica = "AlgoritmoDevTools.Barra.InstanciaUnica";

    private readonly string _verbo;

    public ArranqueDeLaBarra(string verbo) => _verbo = verbo;

    /// <summary>
    /// Si ya hay una barra. Se pregunta por el mutex y no por el nombre del proceso: el mismo
    /// ejecutable corre la ventana, el widget y los verbos del menú contextual.
    /// </summary>
    public static bool EstaCorriendo()
    {
        if (!Mutex.TryOpenExisting(MutexDeInstanciaUnica, out var mutex)) return false;

        mutex.Dispose();
        return true;
    }

    /// <summary>El título de la barra, que es por donde se la encuentra si ya está abierta.</summary>
    public const string TituloDeLaVentana = "Entorno local";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? claseDeVentana, string tituloDeVentana);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr ventana, IntPtr insertarDespuesDe, int x, int y, int ancho, int alto, uint opciones);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string nombre);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr ventana, uint mensaje, IntPtr parametro, IntPtr otro);

    private static readonly IntPtr SiempreEncima = new(-1);
    private const uint SinMover = 0x0002;
    private const uint SinRedimensionar = 0x0001;
    private const uint SinActivar = 0x0010;

    /// <summary>
    /// Con esto se le pide a la barra que se muestre. Los dos procesos llegan al mismo número
    /// registrando esta misma cadena, que es todo lo que comparten.
    /// </summary>
    private const string NombreDelMensajeMostrate = "AlgoritmoDevTools.Barra.Mostrate";

    /// <summary>
    /// El número del mensaje para pedirle a la barra que se muestre, que ella reconoce en su
    /// <c>WndProc</c>. Devuelve 0 si Windows no lo pudo registrar, y entonces no hay que comparar
    /// contra él: 0 es <c>WM_NULL</c>.
    /// </summary>
    public static uint MensajeParaMostrarse() => RegisterWindowMessage(NombreDelMensajeMostrate);

    /// <summary>
    /// Devuelve null si salió bien, o el mensaje de error.
    ///
    /// <para>
    /// Si ya hay una barra, no abre otra: la muestra, desplegándola si hacía falta. Antes devolvía
    /// «todo bien» sin hacer nada, y como la barra puede estar replegada o detrás de otra ventana,
    /// el usuario tocaba Levantar y no veía aparecer nada.
    /// </para>
    /// </summary>
    public string? Lanzar()
    {
        if (EstaCorriendo())
        {
            return TraerAlFrente()
                ? null
                : "ya hay una barra abierta, pero no se la encontró para traerla al frente.";
        }

        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return "no se pudo determinar la ruta del ejecutable.";

        try
        {
            Process.Start(new ProcessStartInfo(exe, _verbo) { UseShellExecute = true });
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Muestra la barra que ya existe: la despliega si estaba escondida y la pone encima de todo.
    /// Es de otro proceso, pero eso se puede; lo que no se hace es activarla, para no robarle el
    /// foco a lo que el usuario esté haciendo.
    ///
    /// <para>
    /// <b>Va con <c>SWP_NOMOVE</c>.</b> Sin ese flag los dos ceros de posición no son «no la
    /// muevas» sino «llevala a 0,0»: la barra se iba sola a la esquina superior izquierda de la
    /// pantalla principal cada vez que alguien tocaba Levantar.
    /// </para>
    ///
    /// <para>
    /// <b>Y además se le pide que se despliegue.</b> Ponerla encima no alcanza cuando está
    /// replegada: lo que queda a la vista es una solapa de 44 px, el usuario no la encuentra y
    /// vuelve a tocar Levantar. Desplegarse es algo que sólo ella puede hacer, así que se le manda
    /// el mensaje y lo resuelve en su <c>WndProc</c>.
    /// </para>
    /// </summary>
    private static bool TraerAlFrente()
    {
        var ventana = FindWindow(null, TituloDeLaVentana);
        if (ventana == IntPtr.Zero) return false;

        var mostrate = MensajeParaMostrarse();
        if (mostrate != 0) PostMessage(ventana, mostrate, IntPtr.Zero, IntPtr.Zero);

        SetWindowPos(ventana, SiempreEncima, 0, 0, 0, 0, SinMover | SinRedimensionar | SinActivar);
        return true;
    }
}
