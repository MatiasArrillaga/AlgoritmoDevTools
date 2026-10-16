using Microsoft.AspNetCore.Components;

namespace AlgoritmoDevTools.UI.Componentes;

public partial class TituloDeGrupo
{
    /// <summary>Nombre de la sección.</summary>
    [Parameter, EditorRequired]
    public required string Texto { get; set; }

    /// <summary>Qué hace la sección. Sin esto, el signo de pregunta no aparece.</summary>
    [Parameter]
    public string? Ayuda { get; set; }

    /// <summary>
    /// Un ícono en línea delante del texto, para las secciones donde el estado se lee de un vistazo
    /// y repetirlo en cada fila sería decir lo mismo muchas veces. SVG del mismo juego que los
    /// demás: 16x16, trazo, sin relleno. El color lo pone quien lo usa, con <see cref="ClaseDelIcono"/>.
    /// </summary>
    [Parameter]
    public string? Icono { get; set; }

    /// <summary>Clase del ícono, para darle el color que corresponda a su significado.</summary>
    [Parameter]
    public string? ClaseDelIcono { get; set; }

    /// <summary>
    /// Un dato corto a la derecha del título: un total, una cuenta. Lo que resume la sección sin
    /// tener que abrirla.
    /// </summary>
    [Parameter]
    public string? Detalle { get; set; }

    /// <summary>
    /// Botones que acompañan al título, a la derecha del todo: la acción que es de la sección y no
    /// de la pantalla, como exportar lo que la sección muestra.
    ///
    /// <b>No tiene efecto en un encabezado plegable.</b> Ahí el título entero es un botón, y un
    /// botón adentro de otro no es HTML válido: el navegador lo saca del medio y el clic termina
    /// plegando la sección en lugar de ejecutar la acción.
    /// </summary>
    [Parameter]
    public RenderFragment? Acciones { get; set; }

    /// <summary>Si la sección está plegada. Sólo tiene efecto junto con <see cref="AlAlternar"/>.</summary>
    [Parameter]
    public bool Contraido { get; set; }

    /// <summary>
    /// Avisa que se pidió plegar o desplegar, con el estado nuevo. Mientras no se pase, el
    /// encabezado es un párrafo común y no hay nada que plegar.
    ///
    /// Qué se contrae lo decide quien lo usa: este componente es sólo el encabezado.
    /// </summary>
    [Parameter]
    public EventCallback<bool> AlAlternar { get; set; }
}
