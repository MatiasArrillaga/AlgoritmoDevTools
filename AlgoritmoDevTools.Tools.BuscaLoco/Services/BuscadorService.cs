using System.Diagnostics;
using System.Text.RegularExpressions;
using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Tools.BuscaLoco.Model;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>Lo que dejó una búsqueda terminada.</summary>
/// <param name="Archivos">Los archivos con sus coincidencias, en el orden en que aparecieron.</param>
/// <param name="ArchivosMirados">Cuántos se abrieron o se miraron por nombre.</param>
/// <param name="ArchivosNoEvaluados">
/// Los que no se pudieron mirar: tomados por otro proceso, demasiado grandes, o los que agotaron el
/// tiempo de la expresión regular. Se informan en lugar de desaparecer.
/// </param>
/// <param name="Coincidencias">El total, que no es la cantidad de archivos.</param>
/// <param name="Duracion">Cuánto tardó, para mostrarlo al lado del conteo.</param>
public sealed record ResultadoDeBusqueda(
    IReadOnlyList<ArchivoConHallazgos> Archivos,
    int ArchivosMirados,
    int ArchivosNoEvaluados,
    int Coincidencias,
    TimeSpan Duracion);

/// <summary>
/// Busca por nombre, por contenido, o por los dos a la vez.
///
/// Recorre con <see cref="RecorridoSeguro"/> del Core, que es el que aguanta los permisos, las
/// rutas largas y los junctions. La poda que le pasa es <b>de velocidad</b> —<c>bin</c>, <c>obj</c>,
/// <c>.git</c>, <c>node_modules</c>— y no de seguridad: por eso el usuario la puede apagar, al
/// revés de la lista negra del Disk Cleaner.
///
/// Los resultados se informan <b>por lotes</b> mientras el recorrido sigue. Un recorrido grande
/// tarda, y una lista que recién aparece al final se siente colgada; pero avisar de a una
/// coincidencia haría repintar la pantalla miles de veces y sería peor que no mostrar nada.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public sealed class BuscadorService
{
    /// <summary>Las carpetas que no se miran salvo que el usuario pida lo contrario.</summary>
    public static IReadOnlyList<string> CarpetasDeRuido { get; } = ["bin", "obj", ".git", "node_modules"];

    /// <summary>
    /// Cada cuánto se vuelcan los resultados acumulados. Es lo que separa «se ve avanzar» de
    /// «repinta en cada coincidencia»; el número se ajusta midiendo, no de entrada.
    /// </summary>
    private static readonly TimeSpan CadaCuantoSeAvisa = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// Si el criterio alcanza para buscar, y si no por qué.
    ///
    /// Es lo que mira la pantalla para dejar el botón habilitado o no. Vive acá y no en el manejador
    /// del clic: si la validación estuviera en el handler, el botón se vería habilitado y no haría
    /// nada al tocarlo, que es el peor de los dos mundos.
    /// </summary>
    public static bool SePuedeBuscar(CriterioDeBusqueda criterio, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(criterio.Raiz))
        {
            error = "Elegí una carpeta donde buscar.";
            return false;
        }

        if (!criterio.TieneQueBuscar)
        {
            error = "Escribí qué buscar: un texto, un nombre de archivo, o los dos.";
            return false;
        }

        if (FiltroDeNombre.Crear(criterio.Nombre, criterio.NombreEsExpresion, criterio.DistinguirMayusculas, out var errorDelNombre) is null)
        {
            error = errorDelNombre;
            return false;
        }

        if (criterio.MiraElContenido
            && CoincidenciaEnTexto.Crear(criterio.Texto, criterio.TextoEsExpresion, criterio.DistinguirMayusculas, criterio.PalabraEntera, out var errorDelTexto) is null)
        {
            error = errorDelTexto;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Recorre y devuelve lo encontrado.
    /// </summary>
    /// <param name="criterio">Qué se busca y dónde.</param>
    /// <param name="avance">
    /// Recibe los lotes mientras el recorrido sigue. Se dispara desde los hilos del recorrido, así
    /// que quien lo escuche tiene que volver al de la interfaz antes de tocarla.
    /// </param>
    /// <param name="estaEnLaRuta">
    /// Filtro extra por carpeta, para los presets que apuntan a una parte del árbol —como las
    /// Unidades Funcionales—. Nulo no filtra nada.
    /// </param>
    public async Task<ResultadoDeBusqueda> BuscarAsync(
        CriterioDeBusqueda criterio,
        IProgress<IReadOnlyList<ArchivoConHallazgos>>? avance = null,
        Func<string, bool>? estaEnLaRuta = null,
        CancellationToken cancelacion = default)
    {
        var reloj = Stopwatch.StartNew();

        var filtro = FiltroDeNombre.Crear(criterio.Nombre, criterio.NombreEsExpresion, criterio.DistinguirMayusculas, out _)
                     ?? FiltroDeNombre.Crear(string.Empty, false, false, out _)!;

        var coincidencia = criterio.MiraElContenido
            ? CoincidenciaEnTexto.Crear(criterio.Texto, criterio.TextoEsExpresion, criterio.DistinguirMayusculas, criterio.PalabraEntera, out _)
            : null;

        var encontrados = new List<ArchivoConHallazgos>();
        var pendientesDeAvisar = new List<ArchivoConHallazgos>();
        var candado = new Lock();

        var mirados = 0;
        var noEvaluados = 0;
        var ultimoAviso = reloj.Elapsed;

        var archivos = RecorridoSeguro.BuscarArchivos(
            criterio.Raiz,
            archivo => filtro.Coincide(archivo.Name) && (estaEnLaRuta is null || estaEnLaRuta(archivo.DirectoryName ?? string.Empty)),
            ArmarPoda(criterio),
            cancelacion);

        var opciones = new ParallelOptions
        {
            CancellationToken = cancelacion,

            // Acotado a propósito: el cuello de botella es el disco, y largar un hilo por archivo
            // sobre un repositorio entero deja la máquina inusable sin terminar antes.
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Parallel.ForEachAsync(archivos, opciones, (ruta, token) =>
        {
            var resultado = Revisar(ruta, coincidencia, token);

            lock (candado)
            {
                mirados++;
                if (resultado.NoSePudoEvaluar) noEvaluados++;

                if (resultado.Archivo is not null)
                {
                    encontrados.Add(resultado.Archivo);
                    pendientesDeAvisar.Add(resultado.Archivo);
                }

                // Se avisa por tiempo y no por cantidad: con pocos resultados muy separados, esperar
                // a juntar N dejaría la pantalla quieta igual.
                if (avance is not null && pendientesDeAvisar.Count > 0 && reloj.Elapsed - ultimoAviso >= CadaCuantoSeAvisa)
                {
                    avance.Report([.. pendientesDeAvisar]);
                    pendientesDeAvisar.Clear();
                    ultimoAviso = reloj.Elapsed;
                }
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        if (avance is not null && pendientesDeAvisar.Count > 0) avance.Report([.. pendientesDeAvisar]);

        return new ResultadoDeBusqueda(
            encontrados,
            mirados,
            noEvaluados,
            encontrados.Sum(a => a.Hallazgos.Count),
            reloj.Elapsed);
    }

    /// <summary>
    /// Qué ramas no se recorren.
    ///
    /// Con las subcarpetas apagadas no se entra en ninguna que no sea la raíz, que es la forma de
    /// decir «sólo esta carpeta» sin que el recorrido necesite saber de profundidades.
    /// </summary>
    private static Func<string, bool> ArmarPoda(CriterioDeBusqueda criterio)
    {
        var raiz = Normalizar(criterio.Raiz);

        return carpeta =>
        {
            var limpia = Normalizar(RecorridoSeguro.Mostrable(carpeta));

            if (criterio.SaltearRuido && EsRuido(limpia)) return true;

            return !criterio.Subcarpetas && !string.Equals(limpia, raiz, StringComparison.OrdinalIgnoreCase);
        };
    }

    private static bool EsRuido(string carpeta) =>
        CarpetasDeRuido.Contains(Path.GetFileName(carpeta), StringComparer.OrdinalIgnoreCase);

    private static string Normalizar(string ruta)
    {
        try
        {
            return Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar);
        }
        catch
        {
            return ruta.TrimEnd(Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    /// Mira un archivo. Sin texto a buscar alcanza con que su nombre haya pasado el filtro: el
    /// archivo mismo es el resultado, y no hace falta abrirlo.
    /// </summary>
    private static (ArchivoConHallazgos? Archivo, bool NoSePudoEvaluar) Revisar(
        string ruta,
        CoincidenciaEnTexto? coincidencia,
        CancellationToken cancelacion)
    {
        if (coincidencia is null) return (new ArchivoConHallazgos(ruta, []), false);

        var lectura = LectorDeArchivos.Leer(ruta);

        if (!lectura.SePudoLeer)
        {
            // Un binario no es un problema: es lo que se quería descartar. Los otros motivos sí se
            // cuentan, porque son archivos que el usuario esperaba que se miraran.
            return (null, lectura.Descarte != MotivoDeDescarte.EsBinario);
        }

        var hallazgos = new List<Hallazgo>();

        try
        {
            foreach (var (numero, texto) in LectorDeArchivos.Lineas(lectura.Texto!))
            {
                cancelacion.ThrowIfCancellationRequested();

                foreach (var (desde, largo) in coincidencia.Buscar(texto))
                {
                    hallazgos.Add(new Hallazgo(ruta, numero, texto, desde, largo));
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // La expresión se quedó pensando en este archivo. Se lo informa y el recorrido sigue:
            // un archivo raro no puede arruinar un resultado de siete mil.
            return (null, true);
        }

        return hallazgos.Count == 0 ? (null, false) : (new ArchivoConHallazgos(ruta, hallazgos), false);
    }
}
