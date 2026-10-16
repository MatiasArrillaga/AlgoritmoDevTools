namespace AlgoritmoDevTools.UI;

/// <summary>
/// Una herramienta tal como la muestra el panel lateral.
/// </summary>
/// <param name="Id">Debe coincidir con el Id de la tool de Windows Forms: con esa clave se guarda
/// la última herramienta usada y el almacenamiento propio de cada una.</param>
/// <param name="Nombre">Nombre en el panel lateral.</param>
/// <param name="Descripcion">Texto de la franja superior.</param>
/// <param name="IconoSvg">Ícono en línea, del juego de 16x16 con trazo de 1,6.</param>
/// <param name="Componente">Componente que dibuja la herramienta. Mientras es null, se muestra el
/// marcador de pendiente y la herramienta se sigue usando desde la ventana clásica.</param>
public sealed record HerramientaUI(
    string Id,
    string Nombre,
    string Descripcion,
    string IconoSvg,
    Type? Componente = null);

/// <summary>
/// Las herramientas que ofrece el panel lateral, en orden.
///
/// Por ahora es una lista fija y no sale de las implementaciones de ITool: esas devuelven un
/// UserControl de Windows Forms, que no sirve para renderizar en HTML. A medida que cada herramienta
/// tenga su componente, este catálogo pasa a apuntar a ese componente.
/// </summary>
public static class CatalogoDeHerramientas
{
    /// <summary>
    /// Las herramientas en el orden en que se ven. Es el mismo con el que la ventana clásica las
    /// registra en <c>Program.Main</c>: quien viene usando el DevTools las busca donde siempre
    /// estuvieron, y no hay motivo para reordenarlas al cambiar de interfaz.
    /// </summary>
    public static IReadOnlyList<HerramientaUI> Todas { get; } = new[]
    {
        new HerramientaUI(
            "CommandsMaker",
            "Commands Maker",
            "Genera los comandos de migraciones para el dominio elegido.",
            IconosSvg.Comandos,
            typeof(Paginas.CommandsMakerPagina)),

        new HerramientaUI(
            "SecretsManager",
            "Secrets Manager",
            "Administra las cadenas de conexión y los secretos de cada microservicio.",
            IconosSvg.Secretos,
            typeof(Paginas.SecretsManagerPagina)),

        new HerramientaUI(
            "ModelDriftChecker",
            "Model Drift Checker",
            "Compara el modelo de dominio contra el baseline para detectar migraciones pendientes.",
            IconosSvg.Drift,
            typeof(Paginas.ModelDriftCheckerPagina)),

        new HerramientaUI(
            "TyeServiceSelector",
            "Tye Service Selector",
            "Elige qué microservicios levanta tye.yaml en el entorno local.",
            IconosSvg.Servicios,
            typeof(Paginas.TyeServiceSelectorPagina)),

        new HerramientaUI(
            "MarkdownConverter",
            "Markdown Converter",
            "Convierte documentos entre Markdown y los formatos de entrega.",
            IconosSvg.Documento,
            typeof(Paginas.MarkdownConverterPagina)),

        new HerramientaUI(
            "DiskCleaner",
            "Disk Cleaner",
            "Muestra qué ocupa el disco, explica el riesgo de cada cosa y limpia sólo lo que elijas.",
            IconosSvg.Limpieza,
            typeof(Paginas.DiskCleanerPagina)),

        new HerramientaUI(
            "BuscaLoco",
            "Busca Loco",
            "Encuentra una palabra adentro de los archivos de una carpeta y sus subcarpetas, o encuentra archivos por su nombre.",
            IconosSvg.Busqueda)
    };
}

/// <summary>
/// Los íconos del panel lateral. Son SVG en línea y no archivos: todos comparten la misma grilla de
/// 16x16 y el mismo trazo de 1,6 sin relleno, que es lo que hace que se lean como un juego y no como
/// seis íconos sueltos. Al ir en línea, además, toman el color del tema por currentColor.
/// </summary>
public static class IconosSvg
{
    public const string Limpieza =
        """<svg viewBox="0 0 16 16"><path d="M3 6h10l-1 7H4L3 6z"/><path d="M6 6V3h4v3"/></svg>""";

    public const string Secretos =
        """<svg viewBox="0 0 16 16"><circle cx="5.5" cy="6.5" r="2.5"/><path d="M7.8 7.6 13 12.8M11 11l1.4-1.4"/></svg>""";

    public const string Servicios =
        """<svg viewBox="0 0 16 16"><rect x="2.5" y="3" width="11" height="4"/><rect x="2.5" y="9" width="11" height="4"/></svg>""";

    public const string Comandos =
        """<svg viewBox="0 0 16 16"><rect x="2" y="3" width="12" height="10"/><path d="m5 7 2 1.8L5 10.5M8.5 10.5H11"/></svg>""";

    public const string Documento =
        """<svg viewBox="0 0 16 16"><path d="M9 2H4v12h8V5L9 2z"/><path d="M9 2v3h3"/></svg>""";

    public const string Drift =
        """<svg viewBox="0 0 16 16"><circle cx="4.5" cy="4" r="1.8"/><circle cx="11.5" cy="12" r="1.8"/><path d="M4.5 5.8v4.4a1.8 1.8 0 0 0 1.8 1.8h3.4M11.5 10.2V5.8A1.8 1.8 0 0 0 9.7 4H6.3"/></svg>""";

    /// <summary>
    /// Lupa con dos renglones adentro, no la lupa de siempre: el ícono de Secretos ya es un círculo
    /// con una línea diagonal saliendo, y a 16 píxeles en una franja de 60 los dos se leen como la
    /// misma mancha. Los renglones además dicen «texto», que es la mitad de lo que hace.
    /// </summary>
    public const string Busqueda =
        """<svg viewBox="0 0 16 16"><circle cx="6.8" cy="6.8" r="4.3"/><path d="m10 10 4.2 4.2"/><path d="M5 5.8h3.6M5 7.9h2.3"/></svg>""";

    /// <summary>Casa: el inicio, con el resumen de lo que hay configurado.</summary>
    public const string Home =
        """<svg viewBox="0 0 16 16"><path d="M2.5 7 8 2.5 13.5 7v6a.9.9 0 0 1-.9.9H3.4a.9.9 0 0 1-.9-.9V7z"/><path d="M6.4 13.9V9.3h3.2v4.6"/></svg>""";

    public const string Configuraciones =
        """<svg viewBox="0 0 16 16"><circle cx="8" cy="8" r="2.2"/><path d="M8 1.6v1.8M8 12.6v1.8M14.4 8h-1.8M3.4 8H1.6M12.5 3.5l-1.3 1.3M4.8 11.2l-1.3 1.3M12.5 12.5l-1.3-1.3M4.8 4.8 3.5 3.5"/></svg>""";
}
