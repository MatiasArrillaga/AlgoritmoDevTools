using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Services;

/// <summary>
/// Resultado de convertir un archivo. Sigue el patron TryXxx del resto de la suite: nunca tira,
/// el error viaja en <see cref="Error"/>.
/// </summary>
public sealed record ConversionResult(string SourceName, string? OutputPath, long OutputBytes, int TrimmedPercent, string? Error, bool Skipped)
{
    public static ConversionResult Ok(string sourceName, string outputPath, long bytes, int trimmed)
        => new(sourceName, outputPath, bytes, trimmed, null, false);

    public static ConversionResult Fail(string sourceName, string error)
        => new(sourceName, null, 0, 0, error, false);

    public static ConversionResult Skip(string sourceName, string motivo)
        => new(sourceName, null, 0, 0, motivo, true);
}

/// <summary>
/// Convierte documentos a Markdown para poder pasarlos a un asistente sin gastar contexto de mas.
/// El .md queda al lado del original y las imagenes en una carpeta "media", asi el documento se
/// puede leer por secciones y se abren solo las imagenes que hagan falta.
/// </summary>
public sealed class DocumentConverter
{
    // Formatos que pandoc lee. Sale de `pandoc --list-input-formats` (3.10). Los formatos viejos
    // de Office no estan y se avisan aparte.
    private static readonly HashSet<string> Soportados = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".odt", ".rtf", ".pptx", ".xlsx", ".html", ".htm", ".epub", ".ipynb", ".csv", ".org", ".rst", ".tex"
    };

    private static readonly HashSet<string> FormatosViejos = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".ppt", ".xls", ".xlsm"
    };

    // El indice del Word queda como un link con el numero de pagina anidado adentro:
    //   [Introduccion [1](#introduccion)](#introduccion)
    // Se exige el numero de pagina en el patron a proposito: asi nunca se borra un link que el
    // autor escribio a mano.
    private static readonly Regex IndiceDeWord = new(@"^\[.*\[\d+\]\([^)]*\).*\]\([^)]*\)$", RegexOptions.Compiled);

    // Las imagenes con medidas no se pueden escribir en Markdown puro, asi que pandoc las deja como
    // HTML con los estilos adentro. La ruta es lo unico que hace falta.
    private static readonly Regex ImagenHtml = new(@"<img\s+src=""([^""]+)""[^>]*?/?>", RegexOptions.Compiled);

    public bool PandocDisponible => PandocPath is not null;

    public string? PandocPath { get; } = PandocRunner.Locate();

    public IReadOnlyCollection<string> ExtensionesSoportadas => Soportados;

    public ConversionResult Convert(string sourcePath, CancellationToken cancellationToken)
    {
        var nombre = Path.GetFileName(sourcePath);

        if (PandocPath is null)
        {
            return ConversionResult.Fail(nombre, "No se encontro pandoc.");
        }

        var extension = Path.GetExtension(sourcePath);

        if (FormatosViejos.Contains(extension))
        {
            return ConversionResult.Skip(nombre, $"pandoc no lee {extension}: guardalo como .docx, .pptx o .xlsx desde Office y convertilo de nuevo.");
        }

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ConversionResult.Skip(nombre, "el PDF no se puede convertir: conseguí el .docx original.");
        }

        if (!Soportados.Contains(extension))
        {
            return ConversionResult.Skip(nombre, $"formato no soportado ({extension}).");
        }

        var carpeta = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrEmpty(carpeta))
        {
            return ConversionResult.Fail(nombre, "No se pudo determinar la carpeta del archivo.");
        }

        var destino = Path.Combine(carpeta, Path.GetFileNameWithoutExtension(sourcePath) + ".md");

        var argumentos = new[]
        {
            sourcePath,
            "-t", "gfm",
            "-o", destino,
            "--wrap=none",
            // Relativo y no absoluto: asi el .md queda portable y las rutas de las imagenes no
            // arrastran el nombre de usuario de quien convirtio.
            "--extract-media=media"
        };

        var error = PandocRunner.TryRun(PandocPath, argumentos, carpeta, cancellationToken);
        if (error is not null)
        {
            return ConversionResult.Fail(nombre, error);
        }

        if (!File.Exists(destino))
        {
            return ConversionResult.Fail(nombre, "pandoc no genero el archivo de salida.");
        }

        var antes = new FileInfo(destino).Length;
        Limpiar(destino);
        var despues = new FileInfo(destino).Length;

        var recorte = antes > 0 && despues < antes ? (int)((antes - despues) * 100 / antes) : 0;
        return ConversionResult.Ok(nombre, destino, despues, recorte);
    }

    /// <summary>
    /// Saca del Markdown lo que Word arrastra y no aporta al leerlo: el indice de contenido y el
    /// alto y ancho de cada imagen. En una ERS tipica eso es cerca de un tercio del archivo.
    /// </summary>
    private static void Limpiar(string path)
    {
        var lineas = File.ReadAllLines(path);
        var salida = new List<string>(lineas.Length);

        foreach (var linea in lineas)
        {
            if (IndiceDeWord.IsMatch(linea.Trim())) continue;

            var limpia = ImagenHtml.Replace(linea, m => "![](" + m.Groups[1].Value.Replace('\\', '/') + ")");

            // Al sacar el indice quedan sus lineas en blanco: se dejan de a una, para no arrastrar
            // un hueco de veinte lineas donde antes estaba el indice.
            var vacia = string.IsNullOrWhiteSpace(limpia);
            var anteriorVacia = salida.Count > 0 && string.IsNullOrWhiteSpace(salida[^1]);
            if (vacia && (anteriorVacia || salida.Count == 0)) continue;

            salida.Add(limpia);
        }

        File.WriteAllLines(path, salida);
    }
}
