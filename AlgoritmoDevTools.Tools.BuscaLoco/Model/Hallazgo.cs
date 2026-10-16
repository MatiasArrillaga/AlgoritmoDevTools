namespace AlgoritmoDevTools.Tools.BuscaLoco.Model;

/// <summary>
/// Una coincidencia: en qué archivo, en qué línea y dónde exactamente adentro de esa línea.
///
/// El rango es lo que pinta el resaltado en la pantalla. Se guarda al encontrar la coincidencia y
/// no se vuelve a calcular al dibujar: con una expresión regular, volver a correrla sobre cada
/// línea visible sería hacer dos veces el trabajo caro.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
/// <param name="Ruta">
/// La ruta como la lee una persona. Nunca lleva el prefijo <c>\\?\</c> de rutas largas: con él
/// puesto no la reconoce nadie, y además es lo que se guarda en el historial y lo que se le pasa al
/// editor.
/// </param>
/// <param name="Linea">Número de línea, empezando en 1, que es como las cuenta cualquier editor.</param>
/// <param name="Texto">La línea entera, para mostrarla con su contexto alrededor de lo encontrado.</param>
/// <param name="Desde">Dónde empieza la coincidencia dentro de <paramref name="Texto"/>.</param>
/// <param name="Largo">Cuánto mide la coincidencia.</param>
public sealed record Hallazgo(
    string Ruta,
    int Linea,
    string Texto,
    int Desde,
    int Largo)
{
    /// <summary>
    /// Cómo se exporta a texto plano: es el formato que entienden las herramientas de la terminal y
    /// el que se pega en un ticket.
    /// </summary>
    public string ComoLinea() => $"{Ruta}:{Linea}:{Texto.Trim()}";
}

/// <summary>
/// Un archivo con todo lo que se encontró adentro.
///
/// Los resultados se agrupan por archivo y no se muestran como una lista plana: treinta
/// coincidencias en el mismo archivo son un resultado, no treinta.
/// </summary>
/// <param name="Ruta">La ruta completa, mostrable.</param>
/// <param name="Hallazgos">Las coincidencias, en el orden en que aparecen en el archivo.</param>
public sealed record ArchivoConHallazgos(string Ruta, IReadOnlyList<Hallazgo> Hallazgos)
{
    /// <summary>El nombre solo, que es lo que encabeza el grupo en la pantalla.</summary>
    public string Nombre => Path.GetFileName(Ruta);

    /// <summary>La carpeta, que va atenuada al lado del nombre.</summary>
    public string Carpeta => Path.GetDirectoryName(Ruta) ?? string.Empty;
}
