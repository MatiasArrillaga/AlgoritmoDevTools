namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Lo que la barra necesita pedirle a la ventana que la hospeda.
///
/// La parte de adentro de la barra es HTML y no sabe nada de ventanas: el alto, el repliegue y el
/// cierre los maneja el host de Windows Forms, que es el que tiene el marco —o la falta de él—.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public interface IVentanaDeLaBarra
{
    /// <summary>
    /// Ajusta el alto de la ventana al del contenido. Lo llama la barra cuando despliega o cierra
    /// el panel de abajo: el HTML no puede agrandar su propia ventana.
    /// </summary>
    void AjustarAlto(int alto);

    /// <summary>
    /// Si está colapsada: la ventana es sólo la pestaña, y la barra dibuja únicamente su icono.
    /// </summary>
    bool EstaReplegada { get; }

    /// <summary>
    /// Si está desplegada pero sin clavar, de las que se contraen en cuanto el mouse se va. La
    /// barra lo usa para dibujar el pin: inclinado mientras no está clavada, derecho cuando sí.
    /// </summary>
    bool EsTemporal { get; }

    /// <summary>
    /// Si la pestaña se apoya contra el lateral derecho. La barra lo usa para llevar la zona de
    /// agarre a ese lado: así el icono queda en el mismo punto desplegada que en pestaña, y al
    /// colapsar no salta de una punta a la otra.
    /// </summary>
    bool ApoyadaALaDerecha { get; }

    /// <summary>Avisa que la ventana pasó de barra a solapa o al revés, para que la barra se redibuje.</summary>
    event EventHandler? AspectoCambiado;

    /// <summary>
    /// Avisa que alguien de afuera pidió frenar el entorno —el Selector, al cancelar un levantado—.
    /// El puente va en este sentido porque frenar lo sabe hacer la barra, no la ventana.
    /// </summary>
    event EventHandler? FrenadoPedido;

    /// <summary>
    /// Colapsa la barra sobre su extremo izquierdo, o la despliega hacia la derecha si ya estaba
    /// colapsada. El origen es el mismo en los dos casos, así que no se mueve de lugar.
    /// </summary>
    void AlternarElRepliegue();

    /// <summary>Cierra la barra. Se usa cuando el entorno terminó de frenarse.</summary>
    void Cerrar();
}
