namespace AlgoritmoDevTools.UI;

/// <summary>
/// Una herramienta tal como la muestra el panel lateral.
/// </summary>
/// <param name="Id">Debe coincidir con el Id de la tool de Windows Forms: con esa clave se guarda
/// la última herramienta usada y el almacenamiento propio de cada una.</param>
/// <param name="Nombre">Nombre en el panel lateral.</param>
/// <param name="Descripcion">Texto de la franja superior.</param>
/// <param name="IconoSvg">Ícono en línea, de Bootstrap Icons (ver <see cref="IconosSvg"/>).</param>
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

        // Model Drift Checker: oculto del panel, igual que su tarjeta en el inicio. La página
        // (Paginas.ModelDriftCheckerPagina) y su ícono siguen en el proyecto; para volver a
        // mostrarlo alcanza con reponer acá su HerramientaUI, entre el Secrets Manager y el Selector.

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
            typeof(Paginas.DiskCleanerPagina))
    };
}

/// <summary>
/// Los íconos del panel lateral. Son SVG en línea y no archivos: al ir en línea toman el color del
/// tema sin depender de ningún archivo externo.
///
/// <para>
/// Salen de Bootstrap Icons (MIT): todos comparten la grilla de 16x16 y el mismo peso de silueta,
/// que es lo que hace que se lean como un juego y no como íconos sueltos. Llevan <c>ico-bi</c>
/// porque son siluetas rellenas y no trazos: el CSS usa esa clase para pintarlos con
/// <c>fill</c> en lugar de <c>stroke</c>. La única excepción es <see cref="Drift"/>, trazado a
/// mano, que quedó así porque su herramienta está oculta del panel.
/// </para>
/// </summary>
public static class IconosSvg
{
    /// <summary>Disco (device-hdd), para el Disk Cleaner.</summary>
    public const string Limpieza =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M12 2.5a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0m0 11a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0m-7.5.5a.5.5 0 1 0 0-1 .5.5 0 0 0 0 1M5 2.5a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0M8 8a1 1 0 1 0 0-2 1 1 0 0 0 0 2"/><path d="M12 7a4 4 0 0 1-3.937 4c-.537.813-1.02 1.515-1.181 1.677a1.102 1.102 0 0 1-1.56-1.559c.1-.098.396-.314.795-.588A4 4 0 0 1 8 3a4 4 0 0 1 4 4m-1 0a3 3 0 1 0-3.891 2.865c.667-.44 1.396-.91 1.955-1.268.224-.144.483.115.34.34l-.62.96A3 3 0 0 0 11 7"/><path d="M2 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1z"/></svg>""";

    /// <summary>Llave (key), para el Secrets Manager.</summary>
    public const string Secretos =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M0 8a4 4 0 0 1 7.465-2H14a.5.5 0 0 1 .354.146l1.5 1.5a.5.5 0 0 1 0 .708l-1.5 1.5a.5.5 0 0 1-.708 0L13 9.207l-.646.647a.5.5 0 0 1-.708 0L11 9.207l-.646.647a.5.5 0 0 1-.708 0L9 9.207l-.646.647A.5.5 0 0 1 8 10h-.535A4 4 0 0 1 0 8m4-3a3 3 0 1 0 2.712 4.285A.5.5 0 0 1 7.163 9h.63l.853-.854a.5.5 0 0 1 .708 0l.646.647.646-.647a.5.5 0 0 1 .708 0l.646.647.646-.647a.5.5 0 0 1 .708 0l.646.647.793-.793-1-1h-6.63a.5.5 0 0 1-.451-.285A3 3 0 0 0 4 5"/><path d="M4 8a1 1 0 1 1-2 0 1 1 0 0 1 2 0"/></svg>""";

    /// <summary>Rack (server), para el Selector de Servicios.</summary>
    public const string Servicios =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M4.5 5a.5.5 0 1 0 0-1 .5.5 0 0 0 0 1M3 4.5a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0m2 7a.5.5 0 1 1-1 0 .5.5 0 0 1 1 0m-2.5.5a.5.5 0 1 0 0-1 .5.5 0 0 0 0 1"/><path d="M2 2a2 2 0 0 0-2 2v1a2 2 0 0 0 2 2h1v2H2a2 2 0 0 0-2 2v1a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-1a2 2 0 0 0-2-2h-1V7h1a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2zm13 2v1a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1m0 7v1a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1v-1a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1m-3-4v2H4V7z"/></svg>""";

    /// <summary>Terminal, para el Commands Maker.</summary>
    public const string Comandos =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M6 9a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 0 1h-3A.5.5 0 0 1 6 9M3.854 4.146a.5.5 0 1 0-.708.708L4.793 6.5 3.146 8.146a.5.5 0 1 0 .708.708l2-2a.5.5 0 0 0 0-.708z"/><path d="M2 1a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V3a2 2 0 0 0-2-2zm12 1a1 1 0 0 1 1 1v10a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z"/></svg>""";

    /// <summary>Markdown, para el Markdown Converter.</summary>
    public const string Documento =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M14 3a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1zM2 2a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2z"/><path fill-rule="evenodd" d="M9.146 8.146a.5.5 0 0 1 .708 0L11.5 9.793l1.646-1.647a.5.5 0 0 1 .708.708l-2 2a.5.5 0 0 1-.708 0l-2-2a.5.5 0 0 1 0-.708"/><path fill-rule="evenodd" d="M11.5 5a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-1 0v-4a.5.5 0 0 1 .5-.5"/><path d="M3.56 11V7.01h.056l1.428 3.239h.774l1.42-3.24h.056V11h1.073V5.001h-1.2l-1.71 3.894h-.039l-1.71-3.894H2.5V11z"/></svg>""";

    /// <summary>Trazado a mano: la herramienta está oculta del panel.</summary>
    public const string Drift =
        """<svg viewBox="0 0 16 16"><circle cx="4.5" cy="4" r="1.8"/><circle cx="11.5" cy="12" r="1.8"/><path d="M4.5 5.8v4.4a1.8 1.8 0 0 0 1.8 1.8h3.4M11.5 10.2V5.8A1.8 1.8 0 0 0 9.7 4H6.3"/></svg>""";

    /// <summary>Casa (house-door): el inicio, con el resumen de lo que hay configurado.</summary>
    public const string Home =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4.5a.5.5 0 0 0 .5-.5v-4h2v4a.5.5 0 0 0 .5.5H14a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L13 5.793V2.5a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1.293zM2.5 14V7.707l5.5-5.5 5.5 5.5V14H10v-4a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 0-.5.5v4z"/></svg>""";

    /// <summary>Engranaje (gear), para Configuraciones.</summary>
    public const string Configuraciones =
        """<svg class="ico-bi" viewBox="0 0 16 16"><path d="M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492M5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0"/><path d="M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.377l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115z"/></svg>""";
}
