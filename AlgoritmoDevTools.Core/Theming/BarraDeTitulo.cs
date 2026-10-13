using System.Runtime.InteropServices;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Pinta la barra de título de una ventana con el tema elegido.
///
/// La barra no es parte de la ventana: la dibuja Windows, y una aplicación no puede pintarla con el
/// pincel que quiera. Lo único que se puede hacer es pedírselo al administrador de ventanas, y lo
/// que conteste depende de la versión:
///
/// Se pide sólo claro u oscuro, que es lo que entienden todas las versiones desde Windows 10 1809:
/// la barra queda blanca o negra, la del sistema. Windows 11 además deja elegir el tono exacto, pero
/// no se usa a propósito —la barra es del sistema y se la deja parecerse a las demás—, y así se ve
/// igual en todas las máquinas del equipo, que es lo que permite que el panel se funda con ella
/// siempre del mismo modo.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public static class BarraDeTitulo
{
    /// <summary>Barra clara u oscura. El 19 es el mismo atributo en las primeras builds de Windows 10.</summary>
    private const int ModoOscuro = 20;
    private const int ModoOscuroEnBuildsViejas = 19;

    /// <summary>Mensaje con el que Windows avisa que hay que redibujar el marco por un cambio de foco.</summary>
    private const int CambioDeActivacion = 0x0086;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr ventana, int atributo, ref int valor, int tamanio);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr ventana);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    /// <summary>
    /// Se llama al crearse la ventana y cada vez que cambia el tema. Antes de mostrarla, para que no
    /// se vea el cambio de color.
    /// </summary>
    public static void Pintar(IntPtr ventana, Tema tema)
    {
        if (ventana == IntPtr.Zero) return;

        var oscuro = tema.EsOscuro ? 1 : 0;

        // Si el atributo nuevo no existe se prueba el viejo: entre una build y otra de Windows 10 le
        // cambiaron el número, y no hay forma de saber cuál anda más que intentando.
        if (DwmSetWindowAttribute(ventana, ModoOscuro, ref oscuro, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(ventana, ModoOscuroEnBuildsViejas, ref oscuro, sizeof(int));
        }

        RefrescarElMarco(ventana);
    }

    /// <summary>
    /// Obliga a Windows a redibujar el marco.
    ///
    /// El atributo se aplica en el momento, pero el marco sigue dibujado como estaba hasta que pasa
    /// algo que lo invalide, y lo primero que pasa es que la ventana cambie de foco. Hasta entonces
    /// la barra queda del color anterior y la ventana se ve partida al medio: el contenido ya cambió
    /// y el marco no.
    ///
    /// Se simula ese cambio de foco, que es lo que menos se nota: dos avisos seguidos, el primero al
    /// revés de como está y el segundo como corresponde. Redimensionar la ventana un píxel también
    /// funciona, pero se ve saltar.
    /// </summary>
    private static void RefrescarElMarco(IntPtr ventana)
    {
        // Antes de mostrarse no hay nada dibujado todavía: el color va a salir bien de una.
        if (!IsWindowVisible(ventana)) return;

        var activa = GetActiveWindow() == ventana;

        SendMessage(ventana, CambioDeActivacion, activa ? IntPtr.Zero : 1, IntPtr.Zero);
        SendMessage(ventana, CambioDeActivacion, activa ? 1 : IntPtr.Zero, IntPtr.Zero);
    }

}
