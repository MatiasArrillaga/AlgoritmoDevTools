using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Una ruta que no se pudo borrar, con el motivo y con quién la tiene tomada si se pudo averiguar.
/// </summary>
public sealed record FalloDeBorrado(string Ruta, string Motivo, IReadOnlyList<ProcesoBloqueante> Quien);

/// <summary>
/// Cómo le fue a un ítem.
/// </summary>
/// <param name="BytesLiberados">
/// Sumados antes de borrar, ruta por ruta, y contados sólo si esa ruta se borró. <b>Nunca sale de
/// restar el espacio libre</b>: dos lecturas separadas por minutos dieron 6,9 y 40 GB sin que nadie
/// tocara el disco, y un borrado de 19,7 GB marcó un delta de 41,07.
/// </param>
public sealed record ResultadoPorItem(
    ItemLimpieza Item,
    long BytesLiberados,
    int Borradas,
    IReadOnlyList<FalloDeBorrado> Fallos);

/// <summary>
/// Cómo le fue a la limpieza entera.
/// </summary>
/// <param name="ParaElevar">Lo que no se intentó porque necesita permiso de administrador.</param>
/// <param name="Cancelado">
/// Si se cortó por pedido del usuario. Lo que figura en <paramref name="Items"/> igual se borró:
/// cancelar detiene lo que falta, no deshace lo hecho.
/// </param>
public sealed record ResultadoDeLimpieza(
    IReadOnlyList<ResultadoPorItem> Items,
    IReadOnlyList<ItemDelPlan> ParaElevar,
    bool Cancelado = false)
{
    public long BytesLiberados => Items.Sum(i => i.BytesLiberados);
    public int Borradas => Items.Sum(i => i.Borradas);
    public int Fallidas => Items.Sum(i => i.Fallos.Count);
}

/// <summary>
/// La única clase de la herramienta que borra.
///
/// Está separada del dry-run a propósito: el que mira la previsualización tiene que poder confiar
/// en que eso no toca nada, y la forma de garantizarlo es que aquella clase no tenga ningún camino
/// hasta acá.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class EjecutorDeLimpieza
{
    /// <summary>
    /// Cuánto esperar antes del segundo intento. De 446 carpetas fallaron 3: dos por "el directorio
    /// no está vacío", porque Visual Studio las estaba recreando con sus builds de diseño mientras
    /// se las borraba. Las dos se borraron en el segundo intento.
    /// </summary>
    private static readonly TimeSpan EsperaAntesDeReintentar = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// Ejecuta lo que no necesita elevación y devuelve aparte lo que sí, para que se pida permiso
    /// una sola vez al final en lugar de una por ítem.
    /// </summary>
    /// <param name="progreso">
    /// Qué se está borrando en este momento. Borrar cientos de carpetas tarda lo suficiente como
    /// para que, sin esto, parezca que la aplicación se colgó.
    /// </param>
    public Task<ResultadoDeLimpieza> EjecutarSinElevarAsync(
        PlanDeLimpieza plan,
        IProgress<string>? progreso = null,
        CancellationToken cancelacion = default) =>
        Task.Run(() =>
        {
            var hechos = new List<ResultadoPorItem>();
            var paraElevar = new List<ItemDelPlan>();

            foreach (var entrada in plan.Items)
            {
                // Cortar y devolver lo hecho, en lugar de tirar. Cancelar a mitad de camino no
                // deshace nada: lo que ya se borró, se borró, y el usuario tiene que poder verlo.
                if (cancelacion.IsCancellationRequested) break;

                if (entrada.RequiereAdmin)
                {
                    paraElevar.Add(entrada);
                    continue;
                }

                hechos.Add(Ejecutar(entrada, progreso, cancelacion));
            }

            return new ResultadoDeLimpieza(hechos, paraElevar, cancelacion.IsCancellationRequested);
        }, cancelacion);

    private static ResultadoPorItem Ejecutar(
        ItemDelPlan entrada,
        IProgress<string>? progreso,
        CancellationToken cancelacion)
    {
        // El nombre del ítem y no el de la ruta: "Compilados de 428 proyectos" dice algo, y una
        // ruta de carpeta de salida pasa tan rápido que no se llega a leer. El contador es lo que
        // muestra que avanza.
        var (liberados, borradas, fallos) = BorrarRutas(
            entrada.Rutas,
            cancelacion,
            progreso is null ? null : (hechas, total) => progreso.Report(
                total <= 1
                    ? entrada.Item.Ubicacion
                    : $"{entrada.Item.Ubicacion} ({hechas} de {total})"));

        return new ResultadoPorItem(entrada.Item, liberados, borradas, fallos);
    }

    /// <summary>
    /// Borra una lista de rutas y cuenta qué pasó.
    ///
    /// Es público porque del otro lado del UAC no hay un <see cref="ItemLimpieza"/>: al helper
    /// elevado le llegan rutas y nada más. Las validaciones que importan están acá adentro, así que
    /// no se saltean por entrar por esta puerta.
    /// </summary>
    public static (long Liberados, int Borradas, IReadOnlyList<FalloDeBorrado> Fallos) BorrarRutas(
        IEnumerable<string> rutas,
        CancellationToken cancelacion = default,
        Action<int, int>? avisar = null)
    {
        long liberados = 0;
        var borradas = 0;
        var fallos = new List<FalloDeBorrado>();

        var todas = rutas as IReadOnlyList<string> ?? [.. rutas];
        var hechas = 0;

        foreach (var ruta in todas)
        {
            // Se corta entre rutas y no en medio de una: una carpeta a medio borrar es peor que
            // una carpeta entera borrada o una entera intacta.
            if (cancelacion.IsCancellationRequested) break;

            avisar?.Invoke(++hechas, todas.Count);

            // Última pregunta antes de tocar el disco. Que llegue hasta acá algo protegido sería un
            // error de otro lado, pero este es el único punto donde el error se vuelve irreversible.
            if (RutasProtegidas.EstaProtegida(ruta) || RutasProtegidas.ContieneAlgunaProtegida(ruta))
            {
                fallos.Add(new FalloDeBorrado(ruta, "Quedó afuera: adentro hay algo que no se toca.", []));
                continue;
            }

            // Medir antes y contarlo sólo si el borrado sale bien. Después ya no hay qué medir.
            var pesa = Pesar(ruta, cancelacion);

            var motivo = Borrar(ruta);
            if (motivo is null)
            {
                liberados += pesa;
                borradas++;
                continue;
            }

            // El segundo intento no es insistir por insistir: cubre el caso real de algo que está
            // siendo recreado justo mientras se lo borra.
            Thread.Sleep(EsperaAntesDeReintentar);
            motivo = Borrar(ruta);

            if (motivo is null)
            {
                liberados += pesa;
                borradas++;
                continue;
            }

            // Recién acá se pregunta quién lo tiene: es caro, y sólo vale la pena sobre lo que
            // efectivamente falló. Nunca se mata a nadie para destrabar un archivo — se informa y
            // decide el usuario.
            fallos.Add(new FalloDeBorrado(ruta, motivo, QuienLoTiene.De(ruta)));
        }

        return (liberados, borradas, fallos);
    }

    /// <summary>Devuelve <c>null</c> si salió bien, o el motivo si no.</summary>
    private static string? Borrar(string ruta)
    {
        try
        {
            if (Directory.Exists(ruta))
            {
                Directory.Delete(ParaRutaLarga(ruta), recursive: true);
                return null;
            }

            if (File.Exists(ruta))
            {
                File.Delete(ParaRutaLarga(ruta));
                return null;
            }

            // Ya no está: alguien lo borró entre la previsualización y ahora. No es un fallo.
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return "Sin permiso para borrarlo.";
        }
        catch (IOException ex)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static long Pesar(string ruta, CancellationToken cancelacion)
    {
        try
        {
            // Sin token: medir ya empezó y tarda poco. Cortarlo acá dejaría la ruta borrada sin
            // haberla contado, y el reporte diría que se liberó menos de lo que se liberó.
            return Directory.Exists(ruta)
                ? EnumeracionSegura.Medir(ruta).Bytes
                : new FileInfo(ruta).Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// El mismo prefijo que usa la enumeración. Sin él, una ruta de más de 260 caracteres falla con
    /// un "no se encuentra" que no se parece a su causa.
    /// </summary>
    private static string ParaRutaLarga(string ruta)
    {
        if (ruta.StartsWith(@"\\?\", StringComparison.Ordinal)) return ruta;

        var completa = Path.GetFullPath(ruta);

        return completa.StartsWith(@"\\", StringComparison.Ordinal)
            ? @"\\?\UNC\" + completa[2..]
            : @"\\?\" + completa;
    }
}
