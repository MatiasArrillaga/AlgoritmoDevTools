using System.Text.RegularExpressions;

using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services;

public sealed record TyeService(string Name, bool Enabled);

/// <summary>
/// Lee la lista de servicios del <c>tye.yaml</c> de AlgoritmoCore y genera un archivo
/// derivado (<see cref="GeneratedFileName"/>) donde los servicios no seleccionados quedan
/// comentados. El master nunca se modifica: se corre con <c>tye run tye.devtools.yaml --watch</c>.
///
/// El toggle es por comentarios de línea (prefijo "# ") para que sea reversible y preserve
/// todo el formato del yaml — no se usa un parser YAML que reformatearía y perdería comentarios.
///
/// <para>
/// <b>Cada servicio arrastra su sidecar Dapr.</b> Hasta el commit <c>0dd570fa5</c> de
/// AlgoritmoCore ("Corregir autenticación y arranque de Dapr", 2026-10-02) los sidecars los
/// derivaba Tye solo: la extensión <c>dapr</c> declaraba el mapa de puertos y le generaba uno a
/// cada servicio de la lista, así que comentar el servicio alcanzaba. Esa extensión se eliminó y
/// ahora los sidecars son servicios top-level propios (<c>produccion-dapr</c>, que ejecuta
/// <c>Utils/Tye/Start-DevelopmentDapr.ps1</c>): Tye no los deriva de nada, los levanta porque
/// están escritos en la lista. Por eso acá se emparejan por nombre (<c>X</c> con
/// <c>X</c><see cref="DaprSuffix"/>) y se comentan juntos. Las dos mitades sueltas fallan:
/// un servicio sin sidecar no puede invocar a nadie, y un sidecar sin servicio queda esperando
/// para siempre una app que no va a arrancar.
/// </para>
/// <para>
/// Los sidecars no se ofrecen en la UI: no son una elección del desarrollador sino un detalle de
/// implementación del servicio que acompañan.
/// </para>
/// </summary>
public static class TyeServiceToggler
{
    public const string GeneratedFileName = "tye.devtools.yaml";
    public const string RunCommand = "tye run " + GeneratedFileName + " --watch";

    /// <summary>Sufijo que identifica al sidecar Dapr de otro servicio de la misma lista.</summary>
    public const string DaprSuffix = "-dapr";

    /// <summary>Raíz de AlgoritmoCore, según la carpeta elegida en Configuraciones.</summary>
    public static string AlgoritmoCoreRoot => RutasDeTrabajo.AlgoritmoCore;

    public static string MasterYamlPath => Path.Combine(AlgoritmoCoreRoot, "tye.yaml");
    public static string GeneratedYamlPath => Path.Combine(AlgoritmoCoreRoot, GeneratedFileName);

    // "- name: cereales" en la columna 0 (sobre la línea ya descomentada).
    // El ancla "^" sin TrimStart es lo que distingue un servicio de una variable de entorno:
    // las entradas de "env:" son también "- name: X" pero indentadas ("  - name: ASPNETCORE_URLS").
    private static readonly Regex ServiceNameRegex =
        new(@"^-\s*name:\s*(\S+)", RegexOptions.Compiled);

    /// <summary>
    /// Lista los servicios seleccionables desde el master (fuente de la lista completa, sin los
    /// sidecars) y superpone el estado activo/inactivo desde el archivo generado si ya existe.
    /// Servicios nuevos en el master que todavía no están en el generado se consideran activos.
    /// </summary>
    public static IReadOnlyList<TyeService> ReadServices()
    {
        if (!File.Exists(MasterYamlPath))
            return Array.Empty<TyeService>();

        var masterNames = FindServiceBlocks(File.ReadAllLines(MasterYamlPath))
            .Select(b => b.Name)
            .ToList();
        var declarados = Declarados(masterNames);

        Dictionary<string, bool>? state = null;
        if (File.Exists(GeneratedYamlPath))
            state = FindServiceBlocks(File.ReadAllLines(GeneratedYamlPath))
                .ToDictionary(b => b.Name, b => b.Enabled);

        return masterNames
            .Where(name => !EsSidecar(name, declarados))
            .Select(name =>
            {
                bool enabled = state is null || !state.TryGetValue(name, out var on) || on;
                return new TyeService(name, enabled);
            })
            .ToList();
    }

    /// <summary>
    /// Copia el master comentando los servicios que <paramref name="selection"/> da por apagados
    /// —cada uno junto con su sidecar Dapr— y escribe el archivo generado. Servicios ausentes en
    /// el diccionario quedan activos.
    /// </summary>
    public static void GenerateSelection(IReadOnlyDictionary<string, bool> selection)
    {
        if (!File.Exists(MasterYamlPath))
            throw new FileNotFoundException($"No se encontró el master en {MasterYamlPath}.");

        var lines = File.ReadAllLines(MasterYamlPath);

        // Comment/Uncomment no cambia la cantidad de líneas, así que los índices de los bloques
        // (calculados sobre el master original) siguen siendo válidos.
        var blocks = FindServiceBlocks(lines);
        var declarados = Declarados(blocks.Select(b => b.Name));

        foreach (var b in blocks)
        {
            // El sidecar sigue la decisión de su servicio, no la propia: nunca se ofreció en la UI.
            var principal = ServicioPrincipal(b.Name, declarados);
            bool enabled = !selection.TryGetValue(principal, out var on) || on;

            // Tocar solo lo que cambia de estado. Un bloque que ya viene del master como
            // corresponde se copia tal cual, y así sobreviven los comentarios escritos a mano que
            // caen dentro de él (p. ej. "# Cada proceso prepara el estado persistente antes de
            // ejecutar su sidecar." justo antes de cereales-dapr): descomentar el bloque entero
            // los convertiría en yaml inválido.
            if (enabled == b.Enabled) continue;

            for (int i = b.Start; i < b.End; i++)
                lines[i] = enabled ? Uncomment(lines[i]) : Comment(lines[i]);
        }

        File.WriteAllLines(GeneratedYamlPath, lines);
    }

    // --- Servicios y sus sidecars -------------------------------------------

    private static HashSet<string> Declarados(IEnumerable<string> names)
        => new(names, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Servicio cuya decisión manda sobre <paramref name="name"/>: el propio, salvo que sea el
    /// sidecar de otro servicio de la lista, caso en el que manda ese otro.
    /// </summary>
    private static string ServicioPrincipal(string name, IReadOnlySet<string> declarados)
        => EsSidecar(name, declarados) ? name[..^DaprSuffix.Length] : name;

    /// <summary>
    /// Un servicio es sidecar si se llama "X-dapr" y "X" también está declarado. La condición
    /// sobre "X" es lo que evita esconder un sidecar huérfano: sin servicio que lo arrastre, no
    /// habría forma de apagarlo desde la UI.
    /// </summary>
    private static bool EsSidecar(string name, IReadOnlySet<string> declarados)
        => name.Length > DaprSuffix.Length
           && name.EndsWith(DaprSuffix, StringComparison.OrdinalIgnoreCase)
           && declarados.Contains(name[..^DaprSuffix.Length]);

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

            // Sin TrimStart: solo los items en la columna 0 son servicios. Las líneas hijas
            // indentadas ("  - name: SOFTCEREALCORE_PROCESS_CULTURE" dentro de "env:") quedan
            // absorbidas por el bloque del servicio en curso, que es lo que corresponde.
            var m = ServiceNameRegex.Match(decommented);
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
