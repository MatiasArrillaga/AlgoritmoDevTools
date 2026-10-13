namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Qué se lleva el archivo que se exporta, herramienta por herramienta y elemento por elemento.
///
/// Existe porque exportar todo rara vez es lo que uno quiere: las conexiones de producción no van al
/// que recién entra, y un perfil de Tye armado para una prueba puntual no le sirve a nadie más. La
/// pantalla la dibuja como un árbol y marca todo de entrada, salvo las contraseñas.
///
/// **Para sumar una herramienta** alcanza con agregar acá su lista, llenarla en
/// <see cref="TransferenciaDeConfiguracion.PrepararSeleccion"/>, leerla en <c>Armar</c> y colgar una
/// rama más en la pantalla. Las cuatro cosas en un solo lugar cada una.
/// </summary>
public sealed class SeleccionDeExportacion
{
    /// <summary>
    /// Arranca apagada siempre. Las contraseñas están guardadas en texto plano: con ellas adentro,
    /// el archivo es una credencial, y eso lo decide quien exporta cada vez y no un valor recordado.
    /// </summary>
    public bool Contrasenas { get; set; }

    public List<Seleccionable<ConexionExportada>> Conexiones { get; init; } = [];

    public List<Seleccionable<string>> Dominios { get; init; } = [];

    public List<Seleccionable<PerfilExportado>> PerfilesDeTye { get; init; } = [];

    /// <summary>Sin nada marcado no hay archivo que escribir, y el botón de exportar queda apagado.</summary>
    public bool NoHayNadaMarcado =>
        !Conexiones.Any(elemento => elemento.Marcado) &&
        !Dominios.Any(elemento => elemento.Marcado) &&
        !PerfilesDeTye.Any(elemento => elemento.Marcado);
}

/// <summary>Un elemento del árbol: el dato, con qué nombre se muestra y si está marcado.</summary>
/// <typeparam name="T">Lo que se va a exportar si queda marcado.</typeparam>
public sealed class Seleccionable<T>
{
    public required T Valor { get; init; }

    /// <summary>Cómo se lo nombra en el árbol. Tiene que alcanzar para distinguirlo de sus hermanos.</summary>
    public required string Etiqueta { get; init; }

    public bool Marcado { get; set; } = true;
}
