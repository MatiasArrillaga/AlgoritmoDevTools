namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>
/// Quién sabe gobernar el entorno local: leer qué está corriendo, frenarlo, reiniciar un servicio
/// o bajarlo.
///
/// <para>
/// <b>Existe para que la interfaz no sepa que hoy abajo hay tye.</b> El equipo planea reemplazar
/// tye por el entorno de Docker Compose que ya vive en <c>Dev/Docker/Compose</c>. Cuando eso pase,
/// se agrega un proveedor nuevo y la barra, el pre-vuelo y el resto siguen igual. Sin esta costura,
/// la migración obligaría a reescribir todo lo que mira el entorno.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public interface IProveedorDeEntorno
{
    /// <summary>Qué hay corriendo ahora mismo. Devuelve vacío si no hay nada.</summary>
    Task<EntornoCompleto> ObtenerEstadoAsync(CancellationToken cancelacion = default);

    /// <summary>Baja el entorno entero. No limpia lo que queda colgado: de eso se ocupa el limpiador.</summary>
    Task FrenarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Reinicia un servicio sin tocar a los demás. Devuelve false si el servicio no se puede
    /// reiniciar — por ejemplo un sidecar, que no es un proyecto.
    /// </summary>
    Task<bool> ReiniciarServicioAsync(string nombre, CancellationToken cancelacion = default);

    /// <summary>
    /// Reinicia todos de una. Es lo que hay que hacer después de cambiar la base de datos, porque
    /// cada servicio la lee al arrancar y los que ya están arriba siguen con la anterior.
    /// </summary>
    Task ReiniciarTodoAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Baja un servicio y deja el resto en pie. Devuelve false si no se pudo, normalmente porque el
    /// proceso ya no estaba.
    /// </summary>
    Task<bool> BajarServicioAsync(string nombre, CancellationToken cancelacion = default);

    /// <summary>
    /// Suma al entorno vivo un servicio que no estaba, sin reiniciar nada de lo que ya corre.
    /// Devuelve false si no se pudo preparar o arrancar.
    /// </summary>
    Task<bool> SumarServicioAsync(string nombre, CancellationToken cancelacion = default);

    /// <summary>
    /// Saca del entorno un servicio que se había sumado. No es lo mismo que bajarlo: bajarlo lo
    /// deja apagado pero adentro, y sacarlo lo quita junto con lo que se levantó para él.
    /// </summary>
    Task<bool> SacarServicioAsync(string nombre, CancellationToken cancelacion = default);
}
