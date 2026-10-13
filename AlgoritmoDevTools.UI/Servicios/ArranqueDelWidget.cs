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
}
