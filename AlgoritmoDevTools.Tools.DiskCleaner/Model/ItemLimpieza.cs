namespace AlgoritmoDevTools.Tools.DiskCleaner.Model;

/// <summary>
/// Qué se pierde al borrar un ítem.
///
/// El orden importa y no es decorativo: a partir de <see cref="Alto"/> la pantalla exige una
/// confirmación escrita y aparte, así que comparar por mayor o igual es la regla.
/// </summary>
public enum NivelRiesgo
{
    Ninguno,
    Bajo,
    Medio,
    Alto,
    Irreversible
}

/// <summary>
/// Dónde se agrupa el ítem en la grilla.
///
/// No hay un tercer grupo a propósito: lo que no se debe borrar no se muestra, porque un grupo
/// "nunca borrar" es una tentación —alguien lee la ruta y la borra por fuera de la herramienta—.
/// Por eso el semáforo tiene seguro y con criterio, y nunca un "no".
/// </summary>
public enum GrupoLimpieza
{
    Seguro,
    ConCriterio
}

/// <summary>
/// Un proceso que tiene tomado algo del ítem.
///
/// Se reporta para que decida el usuario: la herramienta no mata procesos para destrabar un
/// archivo. El nombre del proceso es lo que convierte un "acceso denegado" en algo accionable.
/// </summary>
public sealed record ProcesoBloqueante(int Pid, string Nombre, string? RutaTomada);

/// <summary>
/// Lo que se ofrece cuando la respuesta correcta no es borrar.
///
/// Existe por la caché del VS Installer: ocupa 14,45 GB y se muestra, pero borrarla a mano se llevó
/// una vez el inventario de productos instalados y no se pudo recuperar. La vía soportada es la
/// opción del propio instalador, así que el ítem explica y lleva hasta ahí.
/// </summary>
/// <param name="Texto">Lo que dice el botón.</param>
/// <param name="Destino">Qué se abre.</param>
public sealed record AccionDerivada(string Texto, string Destino);

/// <summary>
/// Un ítem del catálogo: algo que ocupa espacio, con qué es, qué se pierde al tocarlo y qué se
/// puede hacer al respecto.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed record ItemLimpieza
{
    /// <summary>
    /// Identifica al ítem entre corridas. <b>Es estable y no se renombra</b>: es la clave con la que
    /// el perfil del usuario recuerda qué ignorar, así que cambiarlo hace que el ítem reaparezca
    /// tildado en todas las máquinas del equipo.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Lo que ve el usuario en la grilla.</summary>
    public required string Ubicacion { get; init; }

    public long Bytes { get; init; }

    public GrupoLimpieza Grupo { get; init; }

    public NivelRiesgo Riesgo { get; init; }

    /// <summary>Qué es.</summary>
    public required string Descripcion { get; init; }

    /// <summary>Qué pasa si lo borrás.</summary>
    public required string Consecuencia { get; init; }

    /// <summary>Cómo se vuelve atrás. En <c>null</c> cuando no se vuelve.</summary>
    public string? ComoSeRevierte { get; init; }

    /// <summary>
    /// Si borrarlo necesita elevación. De todo lo que hay en el catálogo son pocos, y por eso la
    /// elevación es bajo demanda y queda un solo pedido de permisos al final.
    /// </summary>
    public bool RequiereAdmin { get; init; }

    /// <summary>
    /// Sale del grupo y no se asigna a mano: el invariante "lo seguro viene tildado" deja de ser
    /// algo que haya que recordar en cada detector.
    /// </summary>
    public bool TildadoPorDefecto => Grupo == GrupoLimpieza.Seguro;

    /// <summary>Lo que se borraría. Vacío cuando el ítem deriva en lugar de borrar.</summary>
    public IReadOnlyList<string> Rutas { get; init; } = [];

    /// <summary>Quién tiene tomado algo de <see cref="Rutas"/>, si alguien lo tiene.</summary>
    public IReadOnlyList<ProcesoBloqueante> Bloqueantes { get; init; } = [];

    /// <summary>
    /// Por qué este ítem no se puede tocar ahora, cuando la razón no es un proceso.
    ///
    /// Nace del log con una transacción abierta: achicarlo no recuperaría nada y puede trabarse, así
    /// que se muestra con el motivo en lugar de desaparecer —si desapareciera, el usuario buscaría
    /// esos GB por su cuenta— y en lugar de ofrecerse —si se ofreciera, fallaría—.
    /// </summary>
    public string? MotivoParaNoTocarlo { get; init; }

    /// <summary>
    /// Se puede elegir para limpiar: borra y no hay un motivo en contra.
    ///
    /// <b><see cref="Bloqueantes"/> no entra en la cuenta a propósito.</b> Un proceso en ejecución
    /// advierte, no impide: en una máquina de desarrollo casi siempre hay un compilador vivo, y
    /// tomarlo como impedimento dejaría el ítem más grande del grupo seguro apagado prácticamente
    /// siempre. De 446 carpetas fallaron 3, y reintentar resolvió 2: el costo de intentarlo es un
    /// reintento, no un desastre.
    ///
    /// Lo que sí impide es <see cref="MotivoParaNoTocarlo"/>, que es donde un detector pone las
    /// razones por las que algo no se puede tocar ahora.
    /// </summary>
    public bool SePuedeElegir => SeBorra && MotivoParaNoTocarlo is null;

    /// <summary>
    /// En <c>null</c> el ítem se borra y lo que se borra es <see cref="Rutas"/>. Con valor, el ítem
    /// <b>no borra nada</b>: se muestra, se explica y ofrece la acción que resuelve el caso.
    /// </summary>
    public AccionDerivada? Derivacion { get; init; }

    /// <summary>
    /// Un ítem que deriva no entra en la limpieza: no se tilda, no va al dry-run y no se le pide
    /// confirmación.
    /// </summary>
    public bool SeBorra => Derivacion is null;
}

/// <summary>
/// Un ítem tal como se le ofrece al usuario: el ítem detectado más el tilde que le corresponde en
/// esta máquina, que sale del perfil guardado o, si nunca se tocó, de su grupo.
///
/// Los que el usuario marcó como "no ofrecer nunca" no llegan hasta acá: desaparecen del resultado
/// en lugar de aparecer destildados.
/// </summary>
public sealed record ItemOfrecido(ItemLimpieza Item, bool Tildado);
