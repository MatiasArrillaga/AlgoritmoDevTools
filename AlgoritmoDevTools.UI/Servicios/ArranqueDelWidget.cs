using System.Diagnostics;
using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Si el widget de bandeja arranca con la sesión de Windows.
///
/// Lo que queda residente es el widget y no la ventana: al iniciar sesión deja el ícono en la
/// bandeja, con los secretos y los comandos a mano, sin abrir nada. Abrir la ventana entera en cada
/// arranque sería imponerse sobre lo que el usuario vino a hacer.
///
/// Existe para que el nombre de la entrada y el verbo vivan en un solo lugar. Se prende desde dos
/// lados —el menú del widget y Configuraciones—, y con cada uno escribiendo su propia entrada el
/// usuario terminaría con dos programas arrancando, y con un tilde que no coincide con el otro.
///
/// El verbo lo pasa el Shell, que es el que conoce su propia línea de comandos.
/// </summary>
public sealed class ArranqueDelWidget
{
    /// <summary>Nombre de la entrada en la clave Run. No cambiarlo: es lo que la identifica.</summary>
    private const string NombreEnElArranque = "AlgoritmoDevTools.Widget";

    /// <summary>
    /// El mutex con el que el widget se asegura de ser uno solo. Es global al usuario y no a la
    /// sesión: dos widgets serían dos íconos haciendo lo mismo.
    ///
    /// Vive acá, al lado del nombre del arranque, porque lo usan los dos extremos: el widget al
    /// nacer y esta pantalla para saber si ya hay uno.
    /// </summary>
    public const string MutexDeInstanciaUnica = "AlgoritmoDevTools.Widget.InstanciaUnica";

    private readonly string _verbo;

    public ArranqueDelWidget(string verbo) => _verbo = verbo;

    /// <summary>
    /// Mira también que apunte al ejecutable que está corriendo: publicado en otra carpeta, la
    /// entrada vieja sigue levantando el binario anterior, y conviene mostrarlo apagado para que
    /// al prenderlo se regenere.
    /// </summary>
    public bool EstaActivado() => ArranqueConWindows.EstaActivado(NombreEnElArranque);

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public string? Activar() => ArranqueConWindows.TryActivar(NombreEnElArranque, _verbo);

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public string? Desactivar() => ArranqueConWindows.TryDesactivar(NombreEnElArranque);

    /// <summary>
    /// Si ya hay un widget corriendo. Se pregunta por el mutex y no por los procesos: el mismo
    /// ejecutable corre también esta ventana y los verbos del menú contextual, así que encontrar un
    /// proceso con ese nombre no dice nada.
    /// </summary>
    public static bool EstaCorriendo()
    {
        if (!Mutex.TryOpenExisting(MutexDeInstanciaUnica, out var mutex)) return false;

        mutex.Dispose();
        return true;
    }

    /// <summary>
    /// Levanta el widget. Devuelve null si salió bien, o el mensaje de error.
    ///
    /// Arranca el mismo ejecutable con su verbo: es un proceso aparte a propósito, para que cerrar
    /// esta ventana no se lleve puesto el ícono de la bandeja.
    /// </summary>
    public string? Lanzar()
    {
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
}
