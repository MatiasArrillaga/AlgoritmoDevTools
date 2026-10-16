namespace AlgoritmoDevTools.Core.Infrastructure;

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
/// Recorrer un árbol de carpetas sin que lo tire un permiso, una ruta larga o un junction.
///
/// Nació adentro del Disk Cleaner, como <c>EnumeracionSegura</c>, y subió acá cuando una segunda
/// herramienta —Busca Loco— necesitó lo mismo. La diferencia con aquel es la única que importa:
/// <b>la poda es un parámetro</b>, no una lista cableada.
///
/// Son dos preguntas que se parecen y no son la misma:
///
/// <list type="bullet">
/// <item>
/// El <b>Disk Cleaner</b> poda las rutas protegidas porque va a <b>borrar</b>: la configuración de
/// usuario no se ofrece, y la garantía es que el escáner «ni las mira».
/// </item>
/// <item>
/// <b>Busca Loco</b> sólo <b>lee</b>. Para él esa lista no corresponde —buscar texto en el
/// <c>UserSettings.xml</c> de SSMS es inofensivo y es justo lo que uno quiere cuando algo anda
/// mal— y heredarla haría que devuelva cero resultados donde el texto está, sin decir por qué.
/// Lo que poda es ruido (<c>bin</c>, <c>obj</c>, <c>.git</c>), y por eso se puede apagar.
/// </item>
/// </list>
///
/// Por eso <paramref name="noEntrarEn"/> viaja en cada llamada: cablear cualquiera de las dos acá
/// le daría a la otra un comportamiento que no pidió.
///
/// Recorre a mano en lugar de usar <c>RecurseSubdirectories</c>, y no es por gusto: esa opción
/// enumera todo y deja filtrar después, con lo cual una carpeta podada igual se recorrería y una
/// carpeta enorme que alguien excluyó se pagaría igual en tiempo. Recorriendo a mano se poda la
/// rama entera y el salto de los junctions queda escrito donde se puede leer.
/// </summary>
public static class RecorridoSeguro
{
    /// <summary>
    /// Suma lo que ocupa todo lo que cuelga de la ruta.
    ///
    /// Una ruta podada mide cero: para quien la excluyó esa carpeta no existe, y devolver su tamaño
    /// sería el primer paso para ofrecerla.
    /// </summary>
    /// <param name="noEntrarEn">
    /// Qué ramas no se recorren. Nulo recorre todo: quien quiera podar, lo dice.
    /// </param>
    public static Medicion Medir(
        string raiz,
        Func<string, bool>? noEntrarEn = null,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(raiz)) return Medicion.Vacia;
        if (noEntrarEn is not null && noEntrarEn(raiz)) return Medicion.Vacia;

        long bytes = 0;
        var archivos = 0;
        var inaccesibles = 0;

        foreach (var carpeta in Recorrer(raiz, cancelacion, noEntrarEn))
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
        Func<string, bool>? noEntrarEn = null,
        CancellationToken cancelacion = default)
    {
        var encontradas = new List<string>();
        if (string.IsNullOrWhiteSpace(raiz) || nombres.Count == 0) return encontradas;

        bool NoSeguir(DirectoryInfo c) =>
            EsAlguna(c, nombres) || (noEntrarEn is not null && noEntrarEn(c.FullName));

        foreach (var carpeta in Recorrer(raiz, cancelacion, noEntrarEn, NoSeguir))
        {
            if (EsAlguna(carpeta, nombres))
            {
                encontradas.Add(Mostrable(carpeta.FullName));
            }
        }

        return encontradas;
    }

    /// <summary>
    /// Los archivos que cuelgan de la raíz y pasan el filtro, a medida que aparecen.
    ///
    /// Devuelve perezosamente a propósito: quien busca adentro de miles de archivos tiene que poder
    /// mostrar los primeros resultados sin esperar a que termine el recorrido entero.
    /// </summary>
    /// <param name="coincide">
    /// Qué archivos interesan. Nulo los devuelve todos. Recibe el <see cref="FileInfo"/> entero
    /// porque filtrar por tamaño o por fecha es tan normal como filtrar por nombre.
    /// </param>
    public static IEnumerable<string> BuscarArchivos(
        string raiz,
        Func<FileInfo, bool>? coincide = null,
        Func<string, bool>? noEntrarEn = null,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(raiz)) yield break;
        if (noEntrarEn is not null && noEntrarEn(raiz)) yield break;

        foreach (var carpeta in Recorrer(raiz, cancelacion, noEntrarEn))
        {
            // La lista se arma adentro del try y se recorre afuera: no se puede devolver con yield
            // desde un bloque que atrapa excepciones, y una carpeta ilegible no puede cortar nada.
            List<FileInfo> archivos;
            try
            {
                archivos = [.. carpeta.EnumerateFiles()];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                continue;
            }

            foreach (var archivo in archivos)
            {
                cancelacion.ThrowIfCancellationRequested();

                if (EsEnlace(archivo.Attributes)) continue;
                if (coincide is not null && !coincide(archivo)) continue;

                yield return Mostrable(archivo.FullName);
            }
        }
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
        Func<string, bool>? noEntrarEn,
        Func<DirectoryInfo, bool>? noSeguir = null)
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

            if (noSeguir is not null && noSeguir(actual)) continue;

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
                if (noEntrarEn is not null && noEntrarEn(hija.FullName)) continue;

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
    public static string ParaRutaLarga(string ruta)
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
    public static string Mostrable(string ruta)
    {
        if (ruta.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) return @"\\" + ruta[8..];
        if (ruta.StartsWith(@"\\?\", StringComparison.Ordinal)) return ruta[4..];
        return ruta;
    }
}
