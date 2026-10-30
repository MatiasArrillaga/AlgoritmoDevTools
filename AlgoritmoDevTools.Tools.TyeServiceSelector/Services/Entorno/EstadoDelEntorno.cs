namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>
/// Un servicio tal como lo reporta el panel de tye, ya interpretado.
///
/// <para>
/// <b>El estado vive en la réplica, no en el servicio.</b> El panel devuelve cada servicio con un
/// diccionario de réplicas, y un servicio caído no trae ninguna: queda con la lista vacía y con el
/// contador de reinicios en lo que haya llegado. Por eso <see cref="Arriba"/> se pregunta por el
/// estado y no por la existencia del servicio — el servicio existe igual.
/// </para>
/// </summary>
/// <param name="Nombre">El app-id, como figura en el <c>tye.yaml</c>.</param>
/// <param name="Estado">Lo que dice la réplica (<c>ready</c>, <c>stopped</c>…) o vacío si no hay.</param>
/// <param name="Pid">Proceso de la réplica. Es lo que se mata para bajar el servicio.</param>
/// <param name="Puertos">Los que la réplica tiene publicados.</param>
/// <param name="Reinicios">Cuántas veces tye lo vio caer.</param>
/// <param name="ProyectoCsproj">El <c>.csproj</c>, cuando es un proyecto .NET. Es a lo que se le
/// hace <c>touch</c> para reiniciarlo, así que sin esto el servicio no se puede reiniciar.</param>
/// <param name="Tipo">Cómo lo corre tye: <c>project</c>, <c>executable</c>, <c>container</c> o
/// <c>external</c>.</param>
/// <param name="PuertoDelPanel">De qué instancia de tye vino. Define a quién se le pide frenarlo.</param>
/// <remarks>
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </remarks>
public sealed record ServicioDelEntorno(
    string Nombre,
    string Estado,
    int? Pid,
    IReadOnlyList<int> Puertos,
    int Reinicios,
    string? ProyectoCsproj,
    string Tipo,
    int PuertoDelPanel)
{
    /// <summary>Lo que el panel escribe cuando la réplica terminó de arrancar.</summary>
    public const string Listo = "ready";

    public bool Arriba => string.Equals(Estado, Listo, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Un sidecar Dapr. No se ofrecen como acción propia: siguen la suerte del servicio que
    /// acompañan, igual que en la lista de casillas del Selector.
    /// </summary>
    public bool EsSidecar => Nombre.EndsWith(TyeServiceToggler.DaprSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <c>ui</c> está declarado <c>external</c>: tye no lo corre, sólo le reserva el puerto. No
    /// tiene réplica ni puede tenerla, así que mostrarlo como caído sería mentir.
    /// </summary>
    public bool LoCorreTye => !string.Equals(Tipo, "external", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Un contenedor de infraestructura, como el <c>seq</c> que levanta la extensión del yaml. No
    /// es un microservicio del dominio: nadie lo baja ni lo suma a mano, y en la lista sólo ocupa
    /// un lugar que le hace falta a los que sí se tocan.
    /// </summary>
    public bool EsInfraestructura => string.Equals(Tipo, "container", StringComparison.OrdinalIgnoreCase);

    /// <summary>Se puede reiniciar con un <c>touch</c> sólo si tye sabe de qué proyecto viene.</summary>
    public bool SePuedeReiniciar => !string.IsNullOrWhiteSpace(ProyectoCsproj);
}

/// <summary>Lo que reporta una instancia de tye: la del entorno principal o la de un agregado.</summary>
/// <param name="PuertoDelPanel">Dónde contestó. El principal es el 8000.</param>
/// <param name="Servicios">Todo lo que esa instancia levantó, sidecars incluidos.</param>
public sealed record EstadoDelEntorno(int PuertoDelPanel, IReadOnlyList<ServicioDelEntorno> Servicios);

/// <summary>
/// El entorno como lo ve el desarrollador: un solo conjunto de servicios, aunque por debajo sean
/// varias instancias de tye.
///
/// <para>
/// Hay más de un panel porque sumar un dominio al entorno vivo exige levantarlo con su propio
/// <c>tye run</c>, y cada uno necesita su puerto. Eso es plomería: el dominio conserva los puertos
/// del maestro y los demás servicios lo encuentran por app-id sin enterarse de nada. Acá se vuelven
/// a juntar para que la interfaz muestre un entorno y no tres.
/// </para>
/// </summary>
public sealed record EntornoCompleto(IReadOnlyList<EstadoDelEntorno> Paneles)
{
    public static EntornoCompleto Vacio { get; } = new(Array.Empty<EstadoDelEntorno>());

    public bool Vivo => Paneles.Count > 0;

    /// <summary>El panel del entorno principal, si está. Es el único que puede faltar con otros presentes.</summary>
    public const int PuertoPrincipal = 8000;

    /// <summary>Todos los servicios de todos los paneles, sin los sidecars ni los externos.</summary>
    public IReadOnlyList<ServicioDelEntorno> Servicios => Paneles
        .SelectMany(p => p.Servicios)
        .Where(s => !s.EsSidecar && s.LoCorreTye && !s.EsInfraestructura)
        .OrderBy(s => s.Nombre, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public int Listos => Servicios.Count(s => s.Arriba);

    /// <summary>
    /// Los que vinieron de un tye agregado. Se bajan frenando su propia instancia, que se lleva
    /// además el sidecar y la red; los del principal sólo se pueden bajar matando el proceso.
    /// </summary>
    public IReadOnlyList<ServicioDelEntorno> Agregados => Servicios
        .Where(s => s.PuertoDelPanel != PuertoPrincipal)
        .ToList();

    public ServicioDelEntorno? Buscar(string nombre) => Paneles
        .SelectMany(p => p.Servicios)
        .FirstOrDefault(s => string.Equals(s.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
}
