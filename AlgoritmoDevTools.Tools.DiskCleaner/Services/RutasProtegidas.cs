namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Las rutas que el escáner no mira.
///
/// No son ítems destildados: no se enumeran, no se muestran y no se borran, bajo ninguna opción ni
/// grupo. El criterio que decide los casos nuevos es qué las regenera: se limpia lo que vuelve solo
/// —compilados, cachés de descarga, logs, temporales—, nunca lo que un humano configuró a mano.
///
/// La consulta vive acá y la hace <c>EnumeracionSegura</c>, que es el único camino por el que el
/// escáner toca el disco. Si cada detector tuviera que acordarse de preguntar, el detector número
/// doce se olvidaría y la protección no habría existido nunca.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public static class RutasProtegidas
{
    /// <summary>Un segmento que matchea cualquier nombre de carpeta.</summary>
    private const string CUALQUIERA = "*";

    private static readonly string[][] _reglas = ConstruirReglas();

    /// <summary>
    /// Las reglas en vigor, para verificarlas en las pruebas. <b>No es para mostrarlas</b>: nombrar
    /// lo que no se puede borrar es la tentación que el catálogo evita a propósito.
    /// </summary>
    public static IReadOnlyList<string> Todas { get; } =
        _reglas.Select(r => string.Join(Path.DirectorySeparatorChar, r)).ToArray();

    /// <summary>
    /// Si la ruta cae dentro de alguna de las carpetas protegidas, ella misma incluida.
    ///
    /// Ante cualquier duda contesta que sí: una ruta que no se puede ni interpretar no es una ruta
    /// que convenga borrar.
    /// </summary>
    public static bool EstaProtegida(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return true;

        string[] segmentos;
        try
        {
            segmentos = Segmentar(ruta);
        }
        catch
        {
            return true;
        }

        return _reglas.Any(regla => Empieza(segmentos, regla));
    }

    /// <summary>
    /// Si borrar esta ruta con todo lo que tiene adentro se llevaría puesta alguna de las
    /// protegidas.
    ///
    /// <see cref="EstaProtegida"/> contesta por la ruta misma y sirve mientras se recorre, pero un
    /// borrado recursivo no recorre nada: le pide al sistema que se lleve el árbol entero, y ahí la
    /// lista negra no tiene dónde intervenir. Esta es la última pregunta antes de eso.
    /// </summary>
    public static bool ContieneAlgunaProtegida(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return true;

        string[] segmentos;
        try
        {
            segmentos = Segmentar(ruta);
        }
        catch
        {
            return true;
        }

        // Al revés que EstaProtegida: ahí se pregunta si la ruta cae dentro de una regla; acá, si
        // alguna regla cae dentro de la ruta.
        return _reglas.Any(regla => Empieza(regla, segmentos));
    }

    /// <summary>
    /// Parte la ruta en segmentos comparables.
    ///
    /// Saca el prefijo de rutas largas antes de normalizar: las rutas llegan desde la enumeración
    /// con <c>\\?\</c> adelante, y comparar eso contra una lista escrita sin el prefijo no acierta
    /// nunca.
    /// </summary>
    private static string[] Segmentar(string ruta)
    {
        if (ruta.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
            ruta = @"\\" + ruta[8..];
        else if (ruta.StartsWith(@"\\?\", StringComparison.Ordinal))
            ruta = ruta[4..];

        return Path.GetFullPath(ruta)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(s => s.Length > 0)
            .ToArray();
    }

    /// <summary>
    /// Si <paramref name="larga"/> empieza con <paramref name="corta"/>, comparando segmento por
    /// segmento y no por prefijo de texto: <c>C:\Foo</c> no protege a <c>C:\FooBar</c>, que es la
    /// clase de error que deja pasar un <c>StartsWith</c>.
    ///
    /// El comodín vale de los dos lados a propósito. Se la llama en las dos direcciones —la ruta
    /// contra la regla, y la regla contra la ruta— y mirarlo de un solo lado haría que
    /// <c>…\VisualStudio\*\Settings</c> no se reconociera dentro de <c>…\VisualStudio\17.0_abc</c>.
    /// Es seguro porque un nombre de carpeta de Windows no puede contener un asterisco.
    /// </summary>
    private static bool Empieza(string[] larga, string[] corta)
    {
        if (larga.Length < corta.Length) return false;

        for (var i = 0; i < corta.Length; i++)
        {
            if (corta[i] == CUALQUIERA || larga[i] == CUALQUIERA) continue;
            if (!string.Equals(larga[i], corta[i], StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }

    private static string[][] ConstruirReglas()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var reglas = new List<string[]>();

        // El historial de conexiones de SSMS, los Servidores Registrados y las plantillas. Son
        // kilobytes y reconstruirlos es a mano, servidor por servidor.
        Agregar(reglas, roaming, "Microsoft", "SQL Server Management Studio");

        // Los user-secrets de los microservicios: los administra el Secrets Manager de esta suite.
        Agregar(reglas, roaming, "Microsoft", "UserSecrets");

        // Configuración de Visual Studio.
        Agregar(reglas, roaming, "Microsoft", "VisualStudio");

        // Sólo los ajustes de cada instancia, no la carpeta entera: ahí al lado hay cachés que algún
        // día pueden ser un ítem del catálogo.
        Agregar(reglas, local, "Microsoft", "VisualStudio", CUALQUIERA, "Settings");

        // El perfil de las herramientas de esta suite, el de DiskCleaner incluido. Borrarlo haría
        // que la herramienta se olvide de lo que el usuario eligió ignorar.
        Agregar(reglas, local, "AlgoritmoDevTools");

        return [.. reglas];
    }

    /// <summary>
    /// Una carpeta especial que el sistema no sepa resolver devuelve cadena vacía. Esa regla se
    /// omite en lugar de quedar convertida en una ruta relativa que proteja cualquier cosa.
    /// </summary>
    private static void Agregar(List<string[]> reglas, string raiz, params string[] segmentos)
    {
        if (string.IsNullOrWhiteSpace(raiz)) return;

        try
        {
            reglas.Add([.. Segmentar(raiz), .. segmentos]);
        }
        catch
        {
        }
    }
}
