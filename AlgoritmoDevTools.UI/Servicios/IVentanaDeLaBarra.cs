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

    /// <summary>Si está escondida contra un borde. La barra lo usa para saber hacia dónde apunta su flecha.</summary>
    bool EstaReplegada { get; }

    /// <summary>
    /// El borde contra el que está escondida mientras la ventana es sólo la solapa, o null cuando se
    /// ve la barra entera —a la vista o asomando con el mouse encima—. La barra lo usa para dibujar
    /// únicamente la flecha y para saber hacia dónde apunta.
    /// </summary>
    string? SolapaContra { get; }

    /// <summary>Avisa que la ventana pasó de barra a solapa o al revés, para que la barra se redibuje.</summary>
    event EventHandler? AspectoCambiado;

    /// <summary>Esconde la barra contra el borde más cercano, o la devuelve si ya estaba escondida.</summary>
    void AlternarElRepliegue();

    /// <summary>Cierra la barra. Se usa cuando el entorno terminó de frenarse.</summary>
    void Cerrar();
}
