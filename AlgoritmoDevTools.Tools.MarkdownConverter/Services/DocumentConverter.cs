using System.Text;
using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Services;

/// <summary>
/// Tema del HTML de lectura. El Markdown no lleva estilos: el tema aplica al HTML que se genera
/// aparte para leer el documento comodo.
/// </summary>
public enum HtmlTheme
{
    Claro,
    Oscuro
}

/// <summary>
/// Que hacer con cada documento. <paramref name="Html"/> en null no genera HTML.
/// </summary>
public sealed record ConversionOptions(bool QuitarTachado, HtmlTheme? Html)
{
    public static ConversionOptions Default => new(QuitarTachado: true, Html: null);
}

/// <summary>
/// Resultado de convertir un archivo. Sigue el patron TryXxx del resto de la suite: nunca tira,
/// el error viaja en <see cref="Error"/>.
/// </summary>
public sealed record ConversionResult(
    string SourceName,
    string? OutputPath,
    string? HtmlPath,
    long OutputBytes,
    int TrimmedPercent,
    int StrikeRemoved,
    string? Error,
    bool Skipped)
{
    public static ConversionResult Ok(string sourceName, string outputPath, string? htmlPath, long bytes, int trimmed, int strike)
        => new(sourceName, outputPath, htmlPath, bytes, trimmed, strike, null, false);

    public static ConversionResult Fail(string sourceName, string error)
        => new(sourceName, null, null, 0, 0, 0, error, false);

    public static ConversionResult Skip(string sourceName, string motivo)
        => new(sourceName, null, null, 0, 0, 0, motivo, true);
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
        ".docx", ".odt", ".rtf", ".pptx", ".xlsx", ".html", ".htm", ".epub", ".ipynb", ".csv", ".org", ".rst", ".tex",
        ".md", ".markdown"
    };

    // Un .md que entra no se puede convertir a .md: el destino seria el mismo archivo. Para estos
    // la salida es el HTML de lectura y el original queda intacto.
    private static readonly HashSet<string> YaSonMarkdown = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown"
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

    // Texto tachado: en las ERS marca requisitos que se descartaron. Sacarlo no solo ahorra
    // contexto, evita que el asistente implemente algo que ya no va.
    private static readonly Regex Tachado = new(@"~~(.+?)~~", RegexOptions.Compiled);

    public bool PandocDisponible => PandocPath is not null;

    public string? PandocPath { get; } = PandocRunner.Locate();

    public IReadOnlyCollection<string> ExtensionesSoportadas => Soportados;

    public ConversionResult Convert(string sourcePath, ConversionOptions options, CancellationToken cancellationToken)
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

        // Un .md ya es Markdown: lo unico que se puede hacer con el es la vista HTML, y el archivo
        // original no se toca.
        if (YaSonMarkdown.Contains(extension))
        {
            if (options.Html is null)
            {
                return ConversionResult.Skip(nombre, "ya es Markdown: elegí un tema en 'HTML para leer' si querés la vista para leerlo.");
            }

            var html = GenerarHtml(sourcePath, carpeta, options.Html.Value, cancellationToken, out var errorSoloHtml);
            if (html is null)
            {
                return ConversionResult.Fail(nombre, errorSoloHtml ?? "no se pudo generar el HTML.");
            }

            // El "resultado" es el HTML: es lo unico que se genero.
            return ConversionResult.Ok(nombre, html, null, new FileInfo(html).Length, 0, 0);
        }

        var destino = Path.Combine(carpeta, Path.GetFileNameWithoutExtension(sourcePath) + ".md");

        // Las imagenes se extraen a una carpeta temporal y despues se mueven a media\<archivo>\.
        // No se puede pedirle a pandoc que las ponga ahi directamente: le pega adelante la ruta
        // interna del documento (word/media/), asi que "media/ERS" termina dando media/ERS/media/.
        var temporal = "." + Guid.NewGuid().ToString("N")[..8];

        var argumentos = new[]
        {
            sourcePath,
            "-t", "gfm",
            "-o", destino,
            "--wrap=none",
            // Las revisiones de Word se aceptan: lo insertado queda, lo borrado no vuelve.
            "--track-changes=accept",
            "--extract-media=" + temporal
        };

        var error = PandocRunner.TryRun(PandocPath, argumentos, carpeta, cancellationToken);
        if (error is not null)
        {
            LimpiarTemporal(carpeta, temporal);
            return ConversionResult.Fail(nombre, error);
        }

        if (!File.Exists(destino))
        {
            LimpiarTemporal(carpeta, temporal);
            return ConversionResult.Fail(nombre, "pandoc no genero el archivo de salida.");
        }

        AcomodarImagenes(carpeta, temporal, destino);

        var antes = new FileInfo(destino).Length;
        var tachadosQuitados = Limpiar(destino, options.QuitarTachado);
        var despues = new FileInfo(destino).Length;

        var recorte = antes > 0 && despues < antes ? (int)((antes - despues) * 100 / antes) : 0;

        string? htmlPath = null;
        if (options.Html is not null)
        {
            htmlPath = GenerarHtml(destino, carpeta, options.Html.Value, cancellationToken, out var errorHtml);
            if (htmlPath is null)
            {
                return ConversionResult.Fail(nombre, "el .md salio bien pero el HTML fallo: " + errorHtml);
            }
        }

        return ConversionResult.Ok(nombre, destino, htmlPath, despues, recorte, tachadosQuitados);
    }

    /// <summary>
    /// Saca del Markdown lo que Word arrastra y no aporta al leerlo: el indice de contenido, el
    /// alto y ancho de cada imagen y, si se pide, el texto tachado. Devuelve cuantos fragmentos
    /// tachados se quitaron.
    /// </summary>
    private static int Limpiar(string path, bool quitarTachado)
    {
        var lineas = File.ReadAllLines(path);
        var salida = new List<string>(lineas.Length);
        var tachadosQuitados = 0;

        foreach (var linea in lineas)
        {
            if (IndiceDeWord.IsMatch(linea.Trim())) continue;

            var limpia = ImagenHtml.Replace(linea, m => "![](" + NormalizarRuta(m.Groups[1].Value) + ")");

            if (quitarTachado && limpia.Contains("~~", StringComparison.Ordinal))
            {
                limpia = Tachado.Replace(limpia, _ =>
                {
                    tachadosQuitados++;
                    return string.Empty;
                });

                // Si la linea era un parrafo entero tachado, ahora quedo vacia y se descarta.
                if (string.IsNullOrWhiteSpace(limpia)) continue;
            }

            // Las lineas en blanco se dejan de a una, para no arrastrar el hueco que deja el
            // indice al salir.
            var vacia = string.IsNullOrWhiteSpace(limpia);
            var anteriorVacia = salida.Count > 0 && string.IsNullOrWhiteSpace(salida[^1]);
            if (vacia && (anteriorVacia || salida.Count == 0)) continue;

            salida.Add(limpia);
        }

        File.WriteAllLines(path, salida);
        return tachadosQuitados;
    }

    /// <summary>
    /// Pandoc devuelve las rutas de las imagenes como "./media/x.png". El "./" no aporta y ensucia
    /// el diff cuando el documento se versiona.
    /// </summary>
    private static string NormalizarRuta(string ruta)
    {
        var normalizada = ruta.Replace('\\', '/');
        return normalizada.StartsWith("./", StringComparison.Ordinal) ? normalizada[2..] : normalizada;
    }

    /// <summary>
    /// Mueve las imagenes de la carpeta temporal a media\&lt;archivo&gt;\ y reescribe las rutas del
    /// Markdown. La subcarpeta por documento evita que dos conversiones en la misma carpeta se
    /// pisen las imagenes entre si: pandoc las numera image1, image2... arrancando de uno en cada
    /// documento. Devuelve el nombre de la subcarpeta, o null si el documento no traia imagenes.
    /// </summary>
    private static string? AcomodarImagenes(string carpeta, string temporal, string markdownPath)
    {
        var origen = Path.Combine(carpeta, temporal, "media");
        if (!Directory.Exists(origen))
        {
            LimpiarTemporal(carpeta, temporal);
            return null;
        }

        var subcarpeta = NombreDeCarpetaSeguro(Path.GetFileNameWithoutExtension(markdownPath));
        var destino = Path.Combine(carpeta, "media", subcarpeta);

        try
        {
            Directory.CreateDirectory(destino);

            foreach (var archivo in Directory.GetFiles(origen))
            {
                var final = Path.Combine(destino, Path.GetFileName(archivo));
                File.Move(archivo, final, overwrite: true);
            }

            // Las rutas quedaron apuntando a la carpeta temporal.
            var texto = File.ReadAllText(markdownPath);
            texto = texto.Replace($"{temporal}/media/", $"media/{subcarpeta}/")
                         .Replace($"{temporal}\\media\\", $"media/{subcarpeta}/");
            File.WriteAllText(markdownPath, texto, new UTF8Encoding(false));

            return subcarpeta;
        }
        catch (IOException)
        {
            // Si no se pudieron mover, mejor dejar el .md apuntando a la temporal que romper los
            // links: la conversion sirve igual y el problema es visible.
            return null;
        }
        finally
        {
            LimpiarTemporal(carpeta, temporal);
        }
    }

    private static void LimpiarTemporal(string carpeta, string temporal)
    {
        var ruta = Path.Combine(carpeta, temporal);

        try
        {
            if (Directory.Exists(ruta)) Directory.Delete(ruta, recursive: true);
        }
        catch (IOException)
        {
            // Queda una carpeta oculta de mas; no vale abortar la conversion por eso.
        }
    }

    /// <summary>
    /// Los espacios y los parentesis rompen los links de Markdown, asi que el nombre del documento
    /// no se puede usar tal cual como carpeta. Se deja solo lo que es seguro en una ruta relativa.
    /// </summary>
    private static string NombreDeCarpetaSeguro(string nombre)
    {
        var limpio = new StringBuilder(nombre.Length);
        var ultimoFueGuion = false;

        foreach (var c in nombre)
        {
            if (char.IsLetterOrDigit(c) && c < 128)
            {
                limpio.Append(c);
                ultimoFueGuion = false;
            }
            else if (!ultimoFueGuion && limpio.Length > 0)
            {
                limpio.Append('-');
                ultimoFueGuion = true;
            }
        }

        var resultado = limpio.ToString().Trim('-');
        if (resultado.Length > 50) resultado = resultado[..50].TrimEnd('-');

        return resultado.Length > 0 ? resultado : "documento";
    }

    /// <summary>
    /// Genera un HTML de lectura a partir del .md ya limpio, con el CSS del tema embebido. Se parte
    /// del .md y no del original para que el HTML herede la misma limpieza.
    /// </summary>
    private string? GenerarHtml(string markdownPath, string carpeta, HtmlTheme theme, CancellationToken cancellationToken, out string? error)
    {
        var htmlPath = Path.ChangeExtension(markdownPath, ".html");
        var cssPath = Path.Combine(Path.GetTempPath(), $"devtools-md-{theme}.html");

        File.WriteAllText(cssPath, Css(theme), new UTF8Encoding(false));

        var argumentos = new[]
        {
            markdownPath,
            "-f", "gfm",
            "-t", "html",
            "-s",
            "-o", htmlPath,
            "--metadata", "title=" + Path.GetFileNameWithoutExtension(markdownPath),
            "--include-in-header=" + cssPath
        };

        error = PandocRunner.TryRun(PandocPath!, argumentos, carpeta, cancellationToken);
        return error is null && File.Exists(htmlPath) ? htmlPath : null;
    }

    private static string Css(HtmlTheme theme) => theme == HtmlTheme.Oscuro
        ? """
          <style>
            :root { color-scheme: dark; }
            body { background:#1e1e1e; color:#d4d4d4; font-family:'Segoe UI',system-ui,sans-serif;
                   line-height:1.6; max-width:52em; margin:2rem auto; padding:0 1.5rem; }
            h1,h2,h3,h4 { color:#e7e7e7; border-bottom:1px solid #3c3c3c; padding-bottom:.2em; margin-top:1.8em; }
            a { color:#4daafc; }
            code { background:#2d2d2d; color:#ce9178; padding:.15em .35em; border-radius:3px; font-family:Consolas,monospace; }
            pre { background:#252526; border:1px solid #3c3c3c; border-radius:4px; padding:1em; overflow-x:auto; }
            pre code { background:none; color:#d4d4d4; }
            table { border-collapse:collapse; width:100%; margin:1em 0; }
            th,td { border:1px solid #3c3c3c; padding:.5em .7em; text-align:left; }
            th { background:#2d2d2d; }
            blockquote { border-left:3px solid #4daafc; margin:1em 0; padding:.2em 1em; color:#b0b0b0; }
            img { max-width:100%; border-radius:3px; }
            del, s { color:#7a7a7a; }
            hr { border:none; border-top:1px solid #3c3c3c; }
          </style>
          """
        : """
          <style>
            :root { color-scheme: light; }
            body { background:#ffffff; color:#24292f; font-family:'Segoe UI',system-ui,sans-serif;
                   line-height:1.6; max-width:52em; margin:2rem auto; padding:0 1.5rem; }
            h1,h2,h3,h4 { color:#1f2328; border-bottom:1px solid #d8dee4; padding-bottom:.2em; margin-top:1.8em; }
            a { color:#0969da; }
            code { background:#f3f4f6; color:#953800; padding:.15em .35em; border-radius:3px; font-family:Consolas,monospace; }
            pre { background:#f6f8fa; border:1px solid #d8dee4; border-radius:4px; padding:1em; overflow-x:auto; }
            pre code { background:none; color:#24292f; }
            table { border-collapse:collapse; width:100%; margin:1em 0; }
            th,td { border:1px solid #d8dee4; padding:.5em .7em; text-align:left; }
            th { background:#f6f8fa; }
            blockquote { border-left:3px solid #0969da; margin:1em 0; padding:.2em 1em; color:#57606a; }
            img { max-width:100%; border-radius:3px; }
            del, s { color:#8c959f; }
            hr { border:none; border-top:1px solid #d8dee4; }
          </style>
          """;
}
