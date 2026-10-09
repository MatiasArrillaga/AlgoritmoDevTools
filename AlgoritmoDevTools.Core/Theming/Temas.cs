using System.Drawing;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Catálogo de los cuatro temas que ofrece Configuraciones. Son dos familias —verde y azul— cada
/// una con su variante clara y su variante oscura.
///
/// Los acentos están calibrados para que el texto blanco encima se lea igual de bien en los cuatro:
/// los dos claros rondan 3,4:1 de contraste y los dos oscuros también, así que ninguno pesa más que
/// otro en pantalla. Los grises no son neutros puros: llevan un sesgo hacia el acento de su familia,
/// que es lo que evita que se lean como el gris de sistema que viene por defecto.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public static class Temas
{
    public const string IdVerdeEsperanza = "verde-esperanza";
    public const string IdAzulIlusion = "azul-ilusion";
    public const string IdVerdeEsperanzaOscuro = "verde-esperanza-oscuro";
    public const string IdAzulIlusionOscuro = "azul-ilusion-oscuro";

    public static Tema VerdeEsperanza { get; } = new()
    {
        Id = IdVerdeEsperanza,
        Nombre = "Verde Esperanza",
        EsOscuro = false,
        Acento = Color.FromArgb(0, 160, 94),
        AcentoHover = Color.FromArgb(15, 180, 109),
        AcentoPresionado = Color.FromArgb(0, 128, 73),
        FondoLateral = Color.FromArgb(244, 247, 245),
        FondoPrincipal = Color.FromArgb(255, 255, 255),
        FondoCampo = Color.FromArgb(255, 255, 255),
        Separador = Color.FromArgb(223, 230, 225),
        Texto = Color.FromArgb(40, 48, 44),
        TextoSecundario = Color.FromArgb(107, 122, 114),
        BotonSecundario = Color.FromArgb(239, 243, 240),
        BotonSecundarioHover = Color.FromArgb(227, 234, 229),
        BordeCampo = Color.FromArgb(214, 222, 217),
        Exito = Color.FromArgb(30, 126, 52),
        Error = Color.FromArgb(179, 38, 30),
        Advertencia = Color.FromArgb(166, 77, 8)
    };

    public static Tema AzulIlusion { get; } = new()
    {
        Id = IdAzulIlusion,
        Nombre = "Azul Ilusión",
        EsOscuro = false,
        Acento = Color.FromArgb(14, 142, 233),
        AcentoHover = Color.FromArgb(42, 157, 244),
        AcentoPresionado = Color.FromArgb(11, 114, 186),
        FondoLateral = Color.FromArgb(245, 246, 248),
        FondoPrincipal = Color.FromArgb(255, 255, 255),
        FondoCampo = Color.FromArgb(255, 255, 255),
        Separador = Color.FromArgb(225, 228, 232),
        Texto = Color.FromArgb(43, 49, 55),
        TextoSecundario = Color.FromArgb(110, 123, 133),
        BotonSecundario = Color.FromArgb(240, 242, 245),
        BotonSecundarioHover = Color.FromArgb(228, 232, 236),
        BordeCampo = Color.FromArgb(216, 221, 227),
        Exito = Color.FromArgb(30, 126, 52),
        Error = Color.FromArgb(179, 38, 30),
        Advertencia = Color.FromArgb(166, 77, 8)
    };

    public static Tema VerdeEsperanzaOscuro { get; } = new()
    {
        Id = IdVerdeEsperanzaOscuro,
        Nombre = "Verde Esperanza Oscuro",
        EsOscuro = true,
        Acento = Color.FromArgb(14, 156, 143),
        AcentoHover = Color.FromArgb(25, 178, 163),
        AcentoPresionado = Color.FromArgb(9, 122, 112),
        FondoLateral = Color.FromArgb(22, 28, 27),
        FondoPrincipal = Color.FromArgb(29, 36, 35),
        FondoCampo = Color.FromArgb(37, 45, 44),
        Separador = Color.FromArgb(43, 52, 50),
        Texto = Color.FromArgb(229, 237, 235),
        TextoSecundario = Color.FromArgb(150, 168, 165),
        BotonSecundario = Color.FromArgb(37, 45, 44),
        BotonSecundarioHover = Color.FromArgb(48, 58, 56),
        BordeCampo = Color.FromArgb(57, 68, 66),
        Exito = Color.FromArgb(92, 201, 141),
        Error = Color.FromArgb(240, 117, 107),
        Advertencia = Color.FromArgb(224, 161, 82)
    };

    public static Tema AzulIlusionOscuro { get; } = new()
    {
        Id = IdAzulIlusionOscuro,
        Nombre = "Azul Ilusión Oscuro",
        EsOscuro = true,
        Acento = Color.FromArgb(74, 147, 190),
        AcentoHover = Color.FromArgb(92, 163, 204),
        AcentoPresionado = Color.FromArgb(60, 124, 162),
        FondoLateral = Color.FromArgb(22, 26, 31),
        FondoPrincipal = Color.FromArgb(29, 34, 40),
        FondoCampo = Color.FromArgb(37, 43, 51),
        Separador = Color.FromArgb(43, 50, 58),
        Texto = Color.FromArgb(229, 235, 242),
        TextoSecundario = Color.FromArgb(150, 163, 176),
        BotonSecundario = Color.FromArgb(37, 43, 51),
        BotonSecundarioHover = Color.FromArgb(48, 56, 65),
        BordeCampo = Color.FromArgb(56, 65, 76),
        Exito = Color.FromArgb(92, 201, 141),
        Error = Color.FromArgb(240, 117, 107),
        Advertencia = Color.FromArgb(224, 161, 82)
    };

    /// <summary>Los temas en el orden en que se muestran en Configuraciones.</summary>
    public static IReadOnlyList<Tema> Todos { get; } = new[]
    {
        VerdeEsperanza,
        AzulIlusion,
        VerdeEsperanzaOscuro,
        AzulIlusionOscuro
    };

    public static Tema PorDefecto => VerdeEsperanza;

    /// <summary>
    /// Busca un tema por su Id. Cae en el tema por defecto si el Id no existe, que es lo que pasa
    /// cuando se renombra un tema o cuando el archivo de configuración viene de una versión anterior.
    /// </summary>
    public static Tema Resolver(string? id) =>
        Todos.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)) ?? PorDefecto;
}
