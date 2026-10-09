using System.Drawing;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Colores de los mensajes de estado, leídos del tema vigente. Existe para que una vista pueda pedir
/// "el rojo de error" sin arrastrar el tema hasta el método que arma el mensaje.
///
/// Son significado, no identidad: el verde de "salió bien" es verde también en los temas azules.
/// Lo que cambia entre claro y oscuro es el tono, porque el Firebrick de toda la vida sobre un fondo
/// oscuro no se lee.
///
/// El color de un mensaje ya mostrado no se actualiza al cambiar de tema: el repintado deja la
/// etiqueta en su color base y el estado se vuelve a pintar con la próxima acción. Es a propósito,
/// porque un mensaje viejo no merece que la vista recuerde con qué intención se escribió.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public static class ColoresDeEstado
{
    public static Color Exito => ServicioDeTemas.Instancia.Actual.Exito;

    public static Color Error => ServicioDeTemas.Instancia.Actual.Error;

    public static Color Advertencia => ServicioDeTemas.Instancia.Actual.Advertencia;

    public static Color Informacion => ServicioDeTemas.Instancia.Actual.Informacion;

    /// <summary>Mensajes sin carga: "listo", "analizando...", conteos.</summary>
    public static Color Neutro => ServicioDeTemas.Instancia.Actual.TextoSecundario;
}
