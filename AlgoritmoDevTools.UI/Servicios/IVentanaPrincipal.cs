namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// La ventana que hospeda la interfaz, para lo poco que la pantalla necesita pedirle.
///
/// Igual que los diálogos del sistema y el reinicio: la interfaz HTML no sabe nada de la ventana
/// —es del Shell— y para esto tiene que pedírselo a quien la tiene.
/// </summary>
public interface IVentanaPrincipal
{
    /// <summary>
    /// La manda a la barra de tareas. Se usa después de levantar el entorno: la ventana ya cumplió
    /// y lo que sigue pasa en la consola de tye y en la barra flotante, así que estorba.
    /// </summary>
    void Minimizar();
}
