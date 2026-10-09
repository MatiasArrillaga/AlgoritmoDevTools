using System.Drawing;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Paleta completa de un tema: los once colores con los que se pinta toda la aplicación.
/// Cualquier color que haga falta en una vista tiene que salir de acá y no de un literal suelto,
/// porque si no el control queda igual en los cuatro temas.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public sealed class Tema
{
    /// <summary>Clave con la que se guarda la preferencia. Cambiarla desconfigura a los usuarios.</summary>
    public required string Id { get; init; }

    /// <summary>Nombre que ve el usuario en Configuraciones.</summary>
    public required string Nombre { get; init; }

    /// <summary>
    /// Marca los temas de fondo oscuro. No cambia ningún color por sí misma: sirve para los detalles
    /// que no se deducen de la paleta, como el color de los íconos del panel lateral.
    /// </summary>
    public required bool EsOscuro { get; init; }

    /// <summary>Color de marca: botón primario, ítem activo del lateral, tilde de los checkboxes.</summary>
    public required Color Acento { get; init; }

    /// <summary>Acento al pasar el mouse. El salto es instantáneo — WinForms no interpola colores.</summary>
    public required Color AcentoHover { get; init; }

    /// <summary>Acento con el botón apretado.</summary>
    public required Color AcentoPresionado { get; init; }

    /// <summary>Fondo del panel lateral y de la barra de estado.</summary>
    public required Color FondoLateral { get; init; }

    /// <summary>Fondo del panel central y de las listas.</summary>
    public required Color FondoPrincipal { get; init; }

    /// <summary>
    /// Fondo de los campos de texto y los combos. En los temas claros es el mismo blanco del panel
    /// y el campo se distingue por su borde; en los oscuros es un tono más claro que el panel,
    /// porque ahí el borde de 1px que dibuja Windows es negro y no se llega a ver.
    /// </summary>
    public required Color FondoCampo { get; init; }

    /// <summary>Línea de 1px que separa secciones. Reemplaza a todos los bordes 3D.</summary>
    public required Color Separador { get; init; }

    /// <summary>Texto principal.</summary>
    public required Color Texto { get; init; }

    /// <summary>Texto de apoyo: descripciones, barra de estado, etiquetas secundarias.</summary>
    public required Color TextoSecundario { get; init; }

    /// <summary>Fondo del botón secundario.</summary>
    public required Color BotonSecundario { get; init; }

    /// <summary>Fondo del botón secundario con el mouse encima, y del ítem del lateral bajo el mouse.</summary>
    public required Color BotonSecundarioHover { get; init; }

    /// <summary>Borde de campos de texto, combos y botones secundarios.</summary>
    public required Color BordeCampo { get; init; }

    /// <summary>
    /// Verde de "salió bien" para los mensajes de estado. No es el acento: el acento es identidad de
    /// marca y estos tres son significado, así que el tema azul también tiene su verde de éxito.
    /// </summary>
    public required Color Exito { get; init; }

    /// <summary>Rojo de error para los mensajes de estado.</summary>
    public required Color Error { get; init; }

    /// <summary>Ámbar de advertencia para los mensajes de estado.</summary>
    public required Color Advertencia { get; init; }

    /// <summary>Color de los mensajes informativos. Es el propio acento del tema.</summary>
    public Color Informacion => Acento;

    /// <summary>
    /// Texto sobre el acento. Es blanco en los cuatro temas: los acentos están elegidos para que el
    /// blanco quede legible encima, y por eso el botón primario va en negrita.
    /// </summary>
    public Color TextoSobreAcento => Color.White;
}
