namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Cerrar la aplicación y volver a abrirla, que es lo único que hace falta para empezar a usar una
/// versión recién instalada.
///
/// La interfaz HTML no puede hacerlo por su cuenta: el ejecutable y la ventana son de quien la
/// hospeda. Lo resuelve el Shell, igual que los diálogos de Windows.
/// </summary>
public interface IReinicioDeLaAplicacion
{
    /// <summary>
    /// Arranca el ejecutable de nuevo y cierra esta ventana. No vuelve: lo que siga escrito después
    /// de llamarla puede no llegar a correr.
    /// </summary>
    void Reiniciar();
}
