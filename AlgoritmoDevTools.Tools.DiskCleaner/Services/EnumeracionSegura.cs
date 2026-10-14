namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Lo que mide una carpeta. Nunca tira: una carpeta sin permiso es un dato, no una excepción, y en
/// medio disco van a aparecer varias.
/// </summary>
/// <param name="Bytes">Lo que suman los archivos alcanzados.</param>
/// <param name="Archivos">Cuántos se contaron.</param>
/// <param name="Inaccesibles">Carpetas que no se pudieron abrir. Su contenido no está en el total.</param>
public sealed record Medicion(long Bytes, int Archivos, int Inaccesibles)
{
    public static readonly Medicion Vacia = new(0, 0, 0);
}

/// <summary>
/// El único camino por el que el escáner toca el disco.
///
/// Recorre a mano en lugar de usar <c>RecurseSubdirectories</c>, y no es por gusto: esa opción
/// enumera todo y deja filtrar después, con lo cual una carpeta protegida igual se recorrería
/// —"el escáner ni las mira" dejaría de ser cierto— y una carpeta enorme que alguien protegió se
/// pagaría igual en tiempo. Recorriendo a mano se poda la rama entera y el salto de los junctions
/// queda escrito donde se puede leer.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public static class EnumeracionSegura
{
    /// <summary>
    /// Suma lo que ocupa todo lo que cuelga de la ruta.
    ///
    /// Una ruta protegida mide cero: para la herramienta esa carpeta no existe, y devolver su
    /// tamaño sería el primer paso para ofrecerla.
    /// </summary>
    public static Medicion Medir(string raiz, CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(raiz) || RutasProtegidas.EstaProtegida(raiz)) return Medicion.Vacia;

        long bytes = 0;
        var archivos = 0;
        var inaccesibles = 0;

        foreach (var carpeta in Recorrer(raiz, cancelacion))
        {
            try
            {
                foreach (var archivo in carpeta.EnumerateFiles())
                {
                    // Un symlink a un archivo pesa lo que pesa su destino, y ese destino ya se
                    // cuenta donde vive de verdad.
                    if (EsEnlace(archivo.Attributes)) continue;

                    bytes += archivo.Length;
                    archivos++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                inaccesibles++;
            }
        }

        return new Medicion(bytes, archivos, inaccesibles);
    }

    /// <summary>
    /// Las carpetas que se llaman de alguna de esas maneras, en cualquier nivel bajo la raíz.
    ///
    /// No entra en lo que encuentra: una <c>obj</c> adentro de otra <c>obj</c> ya está contada por
    /// la de afuera, y devolver las dos haría que el total se sumara dos veces.
    /// </summary>
    public static IReadOnlyList<string> BuscarCarpetas(
        string raiz,
        IReadOnlyCollection<string> nombres,
        CancellationToken cancelacion = default)
    {
        var encontradas = new List<string>();
        if (string.IsNullOrWhiteSpace(raiz) || nombres.Count == 0) return encontradas;

        foreach (var carpeta in Recorrer(raiz, cancelacion, noEntrarSi: c => EsAlguna(c, nombres)))
        {
            if (EsAlguna(carpeta, nombres)) encontradas.Add(Mostrable(carpeta.FullName));
        }

        return encontradas;
    }

    private static bool EsAlguna(DirectoryInfo carpeta, IReadOnlyCollection<string> nombres) =>
        nombres.Any(n => string.Equals(carpeta.Name, n, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Recorre en profundidad con una pila propia y no con recursión: un árbol hondo haría
    /// desbordar la del proceso, y acá se recorren repositorios enteros.
    ///
    /// Lo que no se puede abrir se saltea en silencio y el recorrido sigue. Es la diferencia con un
    /// <c>Get-ChildItem -Recurse</c>, que muere en la primera carpeta sin permiso y pierde todo lo
    /// que venía después.
    /// </summary>
    private static IEnumerable<DirectoryInfo> Recorrer(
        string raiz,
        CancellationToken cancelacion,
        Func<DirectoryInfo, bool>? noEntrarSi = null)
    {
        var pendientes = new Stack<DirectoryInfo>();

        DirectoryInfo inicial;
        try
        {
            inicial = new DirectoryInfo(ParaRutaLarga(raiz));
            if (!inicial.Exists) yield break;
        }
        catch
        {
            yield break;
        }

        pendientes.Push(inicial);

        while (pendientes.Count > 0)
        {
            cancelacion.ThrowIfCancellationRequested();

            var actual = pendientes.Pop();
            yield return actual;

            if (noEntrarSi is not null && noEntrarSi(actual)) continue;

            DirectoryInfo[] hijas;
            try
            {
                hijas = actual.GetDirectories();
            }
            catch
            {
                continue;
            }

            foreach (var hija in hijas)
            {
                // Un junction o un symlink apunta a algo que ya vive en otro lado: seguirlo cuenta
                // dos veces lo mismo y, si apunta hacia arriba, el recorrido no termina nunca.
                if (EsEnlace(hija.Attributes)) continue;
                if (RutasProtegidas.EstaProtegida(hija.FullName)) continue;

                pendientes.Push(hija);
            }
        }
    }

    private static bool EsEnlace(FileAttributes atributos) =>
        (atributos & FileAttributes.ReparsePoint) != 0;

    /// <summary>
    /// Antepone el prefijo de rutas largas.
    ///
    /// El repositorio tiene carpetas que rozan el límite de 260 caracteres, y sin el prefijo
    /// cualquiera que lo pase falla con un "no se encuentra la ruta" que no se parece en nada a su
    /// causa. El prefijo exige la ruta ya resuelta: con <c>..</c> adentro o con barras al revés no
    /// funciona, porque desactiva la normalización que Windows hace en el camino.
    /// </summary>
    private static string ParaRutaLarga(string ruta)
    {
        if (ruta.StartsWith(@"\\?\", StringComparison.Ordinal)) return ruta;

        var completa = Path.GetFullPath(ruta);

        return completa.StartsWith(@"\\", StringComparison.Ordinal)
            ? @"\\?\UNC\" + completa[2..]
            : @"\\?\" + completa;
    }

    /// <summary>
    /// Saca el prefijo para lo que ve el usuario y para lo que se guarda. Una ruta con
    /// <c>\\?\</c> adelante en pantalla no la reconoce nadie.
    /// </summary>
    private static string Mostrable(string ruta)
    {
        if (ruta.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) return @"\\" + ruta[8..];
        if (ruta.StartsWith(@"\\?\", StringComparison.Ordinal)) return ruta[4..];
        return ruta;
    }
}
