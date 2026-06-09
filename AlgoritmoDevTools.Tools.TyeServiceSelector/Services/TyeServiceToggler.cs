using System.Text.RegularExpressions;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services;

public sealed record TyeService(string Name, bool Enabled);

/// <summary>
/// Lee la lista de servicios del <c>tye.yaml</c> de AlgoritmoCore y genera un archivo
/// derivado (<see cref="GeneratedFileName"/>) donde los servicios no seleccionados quedan
/// comentados. El master nunca se modifica: se corre con <c>tye run tye.devtools.yaml --watch</c>.
///
/// El toggle es por comentarios de línea (prefijo "# ") para que sea reversible y preserve
/// todo el formato del yaml — no se usa un parser YAML que reformatearía y perdería comentarios.
/// Se togglea tanto el bloque en la lista <c>services:</c> como la entrada del servicio en la
/// extensión <c>dapr</c>, para que el archivo quede consistente.
/// </summary>
public static class TyeServiceToggler
{
    public const string GeneratedFileName = "tye.devtools.yaml";
    public const string RunCommand = "tye run " + GeneratedFileName + " --watch";

    public static string AlgoritmoCoreRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "source", "repos", "AlgoritmoCore");

    public static string MasterYamlPath => Path.Combine(AlgoritmoCoreRoot, "tye.yaml");
    public static string GeneratedYamlPath => Path.Combine(AlgoritmoCoreRoot, GeneratedFileName);

    // "- name: cereales" (sobre la línea ya descomentada y sin indentación)
    private static readonly Regex ServiceNameRegex =
        new(@"^-\s*name:\s*(\S+)", RegexOptions.Compiled);

    // Entrada de servicio en la extensión dapr: "cereales:" (clave sin valor a continuación).
    // Las líneas hijas "grpc-port: 42735" no matchean porque tienen valor después del ':'.
    private static readonly Regex DaprEntryRegex =
        new(@"^([A-Za-z][\w-]*):\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Lista los servicios desde el master (fuente de la lista completa) y superpone el estado
    /// activo/inactivo desde el archivo generado si ya existe. Servicios nuevos en el master que
    /// todavía no están en el generado se consideran activos.
    /// </summary>
    public static IReadOnlyList<TyeService> ReadServices()
    {
        if (!File.Exists(MasterYamlPath))
            return Array.Empty<TyeService>();

        var masterNames = FindServiceBlocks(File.ReadAllLines(MasterYamlPath))
            .Select(b => b.Name)
            .ToList();

        Dictionary<string, bool>? state = null;
        if (File.Exists(GeneratedYamlPath))
            state = FindServiceBlocks(File.ReadAllLines(GeneratedYamlPath))
                .ToDictionary(b => b.Name, b => b.Enabled);

        return masterNames
            .Select(name =>
            {
                bool enabled = state is null || !state.TryGetValue(name, out var on) || on;
                return new TyeService(name, enabled);
            })
            .ToList();
    }

    /// <summary>
    /// Copia el master comentando/descomentando cada servicio según <paramref name="selection"/>
    /// (en la lista services: y en la extensión dapr) y escribe el archivo generado.
    /// Servicios ausentes en el diccionario quedan activos.
    /// </summary>
    public static void GenerateSelection(IReadOnlyDictionary<string, bool> selection)
    {
        if (!File.Exists(MasterYamlPath))
            throw new FileNotFoundException($"No se encontró el master en {MasterYamlPath}.");

        var lines = File.ReadAllLines(MasterYamlPath);

        // Comment/Uncomment no cambia la cantidad de líneas, así que los índices de ambos
        // conjuntos de bloques (calculados sobre el master original) siguen siendo válidos.
        var serviceBlocks = FindServiceBlocks(lines);
        var daprBlocks = FindDaprServiceBlocks(lines);

        foreach (var b in serviceBlocks.Concat(daprBlocks))
        {
            bool enabled = !selection.TryGetValue(b.Name, out var on) || on;
            for (int i = b.Start; i < b.End; i++)
                lines[i] = enabled ? Uncomment(lines[i]) : Comment(lines[i]);
        }

        File.WriteAllLines(GeneratedYamlPath, lines);
    }

    // --- Parseo de bloques --------------------------------------------------

    /// <summary>Bloques de la lista top-level <c>services:</c> (cada "- name: X" y sus líneas hijas).</summary>
    private static List<(string Name, int Start, int End, bool Enabled)> FindServiceBlocks(string[] lines)
    {
        var blocks = new List<(string, int, int, bool)>();
        var (regionStart, regionEnd) = FindTopLevelServicesRegion(lines);
        if (regionStart < 0) return blocks;

        int? curStart = null;
        string? curName = null;
        bool curEnabled = false;

        for (int i = regionStart; i < regionEnd; i++)
        {
            var (decommented, wasCommented) = Decomment(lines[i]);
            var m = ServiceNameRegex.Match(decommented.TrimStart());
            if (!m.Success) continue;

            if (curStart is not null)
                blocks.Add((curName!, curStart.Value, i, curEnabled));

            curStart = i;
            curName = m.Groups[1].Value;
            curEnabled = !wasCommented;
        }

        if (curStart is not null)
            blocks.Add((curName!, curStart.Value, regionEnd, curEnabled));

        return blocks;
    }

    /// <summary>Entradas de servicio dentro del bloque <c>services:</c> de la extensión dapr.</summary>
    private static List<(string Name, int Start, int End, bool Enabled)> FindDaprServiceBlocks(string[] lines)
    {
        var blocks = new List<(string, int, int, bool)>();

        // El "services:" de dapr está indentado (el top-level está en la columna 0).
        int header = -1, headerIndent = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed == "services:" && lines[i].Length > trimmed.Length)
            {
                header = i;
                headerIndent = lines[i].Length - trimmed.Length;
                break;
            }
        }
        if (header < 0) return blocks;

        int? curStart = null;
        string? curName = null;
        bool curEnabled = false;
        int regionEnd = lines.Length;

        for (int i = header + 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var (decommented, wasCommented) = Decomment(lines[i]);
            int indent = LeadingWhitespace(decommented);

            // Dedent hasta el nivel del header (o menos) cierra el bloque dapr.
            if (indent <= headerIndent)
            {
                regionEnd = i;
                break;
            }

            var m = DaprEntryRegex.Match(decommented.TrimStart());
            if (!m.Success) continue;

            if (curStart is not null)
                blocks.Add((curName!, curStart.Value, i, curEnabled));

            curStart = i;
            curName = m.Groups[1].Value;
            curEnabled = !wasCommented;
        }

        if (curStart is not null)
            blocks.Add((curName!, curStart.Value, regionEnd, curEnabled));

        return blocks;
    }

    /// <summary>Rango [start, end) de la lista top-level <c>services:</c> (columna 0).</summary>
    private static (int Start, int End) FindTopLevelServicesRegion(string[] lines)
    {
        int start = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"^services:\s*$"))
            {
                start = i + 1;
                break;
            }
        }
        if (start < 0) return (-1, -1);

        int end = lines.Length;
        for (int i = start; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Una nueva clave top-level (columna 0, no item de lista, no comentario) cierra la región.
            if (!char.IsWhiteSpace(line[0]) && line[0] != '-' && line[0] != '#'
                && Regex.IsMatch(line, @"^[A-Za-z_][\w-]*:"))
            {
                end = i;
                break;
            }
        }
        return (start, end);
    }

    // --- Comment helpers ----------------------------------------------------

    /// <summary>Comenta la línea con "# " preservando la indentación. Idempotente.</summary>
    private static string Comment(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return line;
        int i = LeadingWhitespace(line);
        string leading = line[..i];
        string rest = line[i..];
        return rest.StartsWith("#") ? line : leading + "# " + rest;
    }

    /// <summary>Quita el prefijo de comentario si lo tiene. Idempotente.</summary>
    private static string Uncomment(string line)
    {
        var (text, wasCommented) = Decomment(line);
        return wasCommented ? text : line;
    }

    /// <summary>Devuelve la línea sin el prefijo "# " (preservando indentación) y si estaba comentada.</summary>
    private static (string Text, bool WasCommented) Decomment(string line)
    {
        int i = LeadingWhitespace(line);
        string leading = line[..i];
        string rest = line[i..];
        if (rest.StartsWith("# ")) return (leading + rest[2..], true);
        if (rest.StartsWith("#")) return (leading + rest[1..], true);
        return (line, false);
    }

    private static int LeadingWhitespace(string s)
    {
        int i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        return i;
    }
}
