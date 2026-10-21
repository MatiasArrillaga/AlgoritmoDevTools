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
/// Existe para que el nombre de la entrada, el verbo y el del mutex vivan en un solo lugar: los
/// usan los dos extremos, esta pantalla para saber si hay un widget y para levantarlo, y el widget
/// al nacer para asegurarse de ser uno solo.
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
    /// Qué dice la clave Run: si el widget arranca con la sesión y, en ese caso, si el que arranca
    /// es este ejecutable o es otro.
    ///
    /// Son tres estados y no dos porque la diferencia importa justo cuando se está desarrollando:
    /// con el Shell abierto desde Visual Studio, una entrada que apunta al publicado no es lo mismo
    /// que no tener ninguna, y tratarlas igual hace que prender el tilde pise la buena.
    /// </summary>
    public ArranqueConWindows.ArranqueRegistrado Leer() => ArranqueConWindows.Leer(NombreEnElArranque);

    /// <summary>
    /// Si el ejecutable que está corriendo ahora sale de una carpeta de compilación. Se pregunta
    /// antes de ofrecer el arranque automático: un widget residente levantado desde <c>bin\Debug</c>
    /// mantiene tomados los DLL del proyecto y la siguiente compilación de la solución falla.
    /// </summary>
    public static bool CorreDesdeCompilacion() =>
        ArranqueConWindows.EsRutaDeCompilacion(ShellIntegration.RutaDelEjecutable() ?? string.Empty);

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    /// <param name="aunqueCorraDesdeCompilacion">True cuando ya se avisó que el ejecutable sale de la carpeta de compilación y se eligió seguir igual.</param>
    public string? Activar(bool aunqueCorraDesdeCompilacion = false) =>
        ArranqueConWindows.TryActivar(NombreEnElArranque, _verbo, aunqueCorraDesdeCompilacion);

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
