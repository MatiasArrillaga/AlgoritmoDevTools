using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// El único camino por el que el escáner toca el disco, con la lista negra <b>siempre</b> puesta.
///
/// El recorrido en sí vive en <see cref="RecorridoSeguro"/>, en el Core, porque hay dos herramientas
/// que lo necesitan. Lo que queda acá es la decisión que es sólo de la limpieza: <b>qué no se mira
/// nunca</b>.
///
/// Es un envoltorio y no una llamada directa a propósito. Si cada detector tuviera que acordarse de
/// pasar <see cref="RutasProtegidas.EstaProtegida"/>, el día que alguien agregue el detector número
/// doce se olvida y la protección no existió nunca. Acá no hay forma de olvidarse: no existe la
/// variante sin podar.
///
/// Busca Loco usa el del Core directo, con su propia poda, que es de velocidad y no de seguridad.
/// La diferencia está contada en <see cref="RecorridoSeguro"/>.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public static class EnumeracionSegura
{
    /// <summary>
    /// Suma lo que ocupa todo lo que cuelga de la ruta.
    ///
    /// Una ruta protegida mide cero: para la herramienta esa carpeta no existe, y devolver su
    /// tamaño sería el primer paso para ofrecerla.
    /// </summary>
    public static Medicion Medir(string raiz, CancellationToken cancelacion = default) =>
        RecorridoSeguro.Medir(raiz, RutasProtegidas.EstaProtegida, cancelacion);

    /// <summary>
    /// Las carpetas que se llaman de alguna de esas maneras, en cualquier nivel bajo la raíz.
    ///
    /// No entra en lo que encuentra: una <c>obj</c> adentro de otra <c>obj</c> ya está contada por
    /// la de afuera, y devolver las dos haría que el total se sumara dos veces.
    /// </summary>
    public static IReadOnlyList<string> BuscarCarpetas(
        string raiz,
        IReadOnlyCollection<string> nombres,
        CancellationToken cancelacion = default) =>
        RecorridoSeguro.BuscarCarpetas(raiz, nombres, RutasProtegidas.EstaProtegida, cancelacion);
}
