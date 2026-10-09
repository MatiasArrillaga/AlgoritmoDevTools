namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Muestra y oculta la franja donde se sueltan archivos.
///
/// Esa franja no es HTML: la dibuja la ventana con Windows Forms, por fuera del navegador. Es la
/// única forma de recibir las rutas en disco, porque el motor de Edge cubre la ventana entera, no
/// cede el arrastre al formulario y nunca entrega la ruta de lo que se suelta — sólo su contenido.
///
/// La pinta con los colores del tema vigente, así que se lee como una pieza más de la pantalla.
/// </summary>
public interface IZonaDeArrastre
{
    /// <summary>
    /// Deja la franja lista, pero sin mostrarla: aparece recién cuando se arrastra algo sobre la
    /// ventana y se va cuando el arrastre termina. La llama la pantalla que acepta archivos.
    /// </summary>
    /// <param name="mensaje">Lo que se lee cuando no hay nada encima.</param>
    void Armar(string mensaje);

    /// <summary>La desarma y la esconde. La llama la pantalla al dejar de estar visible.</summary>
    void Desarmar();
}
