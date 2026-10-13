using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.UI.Servicios;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Cierra la ventana y abre el ejecutable de nuevo, para empezar a usar una versión recién
/// instalada sin que el usuario tenga que cerrarla a mano.
/// </summary>
public sealed class ReinicioDeLaAplicacion : IReinicioDeLaAplicacion
{
    private readonly Form _ventana;

    public ReinicioDeLaAplicacion(Form ventana) => _ventana = ventana;

    /// <summary>
    /// El cierre queda para el siguiente ciclo del bucle de mensajes, por lo mismo que los diálogos
    /// —está contado en <see cref="DialogosDelSistema"/>—: esto se llama desde un clic que el visor
    /// todavía está entregando, y destruir el navegador adentro de su propio aviso cierra el proceso
    /// de mala manera. Con <c>BeginInvoke</c> el clic termina primero y recién después se cierra.
    ///
    /// Primero se arranca y después se cierra, no al revés: cerrar primero termina este proceso y la
    /// línea siguiente no llega a correr nunca.
    /// </summary>
    public void Reiniciar() => _ventana.BeginInvoke(() =>
    {
        Actualizador.Relanzar();
        Application.Exit();
    });
}
