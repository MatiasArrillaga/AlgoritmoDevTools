namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Avisa cuando se arrastran archivos sobre la ventana, con sus rutas reales en disco.
///
/// Existe porque el motor de Edge no entrega la ruta de un archivo soltado: por seguridad el
/// contenido web sólo recibe los bytes. La ventana que lo hospeda sí la recibe, así que el arrastre
/// se resuelve del lado de Windows Forms y llega a los componentes por acá.
/// </summary>
public interface IArchivosArrastrados
{
    /// <summary>Se dispara al soltar, con las rutas de lo que se soltó.</summary>
    event EventHandler<IReadOnlyList<string>>? Soltados;

    /// <summary>
    /// Se dispara al entrar y al salir del área de la ventana con archivos encima. Sirve para
    /// resaltar la zona donde hay que soltar mientras dura el arrastre.
    /// </summary>
    event EventHandler<bool>? ArrastreEnCurso;
}
