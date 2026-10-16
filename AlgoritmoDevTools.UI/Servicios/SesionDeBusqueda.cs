using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Tools.BuscaLoco.Model;
using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// El criterio, lo encontrado y la búsqueda en curso, fuera de la pantalla que los muestra.
///
/// Vive mientras vive la aplicación, y no mientras se está mirando Busca Loco, por lo mismo que
/// <see cref="SesionDeLimpieza"/>, que ya pagó las dos:
///
/// <list type="bullet">
/// <item>
/// <b>Cambiar de herramienta durante la búsqueda cerraba la aplicación.</b> El recorrido es un
/// controlador asincrónico largo; al irse, Blazor desarma el componente y su <c>Dispose</c>
/// cancelaba <b>y liberaba</b> el <see cref="CancellationTokenSource"/> en el acto, con el
/// recorrido todavía en vuelo. Al volver del <c>await</c> se repintaba un componente que ya no
/// existía, y en el visor una excepción que nadie atrapa cierra la ventana entera.
/// </item>
/// <item>
/// <b>Cambiar de herramienta después la perdía</b>, y había que volver a buscar para ver lo mismo.
/// </item>
/// </list>
///
/// Acá el <see cref="CancellationTokenSource"/> se libera cuando la operación terminó de verdad, en
/// el <c>finally</c> de quien la lanzó, que es el único momento en que nadie más lo está usando.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public sealed class SesionDeBusqueda
{
    private readonly BuscadorService _buscador;
    private readonly BarraDeEstado _barra;
    private readonly Avisos _avisos;

    private CancellationTokenSource? _cancelacion;

    public SesionDeBusqueda(BuscadorService buscador, BarraDeEstado barra, Avisos avisos)
    {
        _buscador = buscador;
        _barra = barra;
        _avisos = avisos;

        Criterio = CriterioInicial();
    }

    /// <summary>
    /// Lo que el usuario pidió. Vive acá y no en la pantalla porque también tiene que sobrevivir a
    /// irse a otra herramienta: volver y encontrar los campos vacíos sería perder el trabajo de
    /// haberlos llenado.
    /// </summary>
    public CriterioDeBusqueda Criterio { get; set; }

    /// <summary>
    /// La carpeta que el preset elegido exige que esté en la ruta, cuando elige una.
    ///
    /// Va aparte del criterio y no adentro porque no es algo que el usuario escriba: lo pone el
    /// atajo de «Unidades Funcionales», que filtra por dónde está el archivo y no por cómo se llama.
    /// </summary>
    public string? CarpetaDelPreset { get; set; }

    /// <summary>Lo encontrado hasta ahora. Se va llenando mientras el recorrido sigue.</summary>
    public List<ArchivoConHallazgos> Encontrados { get; private set; } = [];

    /// <summary>Los números de la última búsqueda terminada, o null si no terminó ninguna.</summary>
    public ResultadoDeBusqueda? Resultado { get; private set; }

    /// <summary>Si hay una búsqueda corriendo, se esté mirando la pantalla o no.</summary>
    public bool Buscando { get; private set; }

    /// <summary>
    /// Cambió algo que la pantalla muestra. Se dispara desde los hilos del recorrido, así que quien
    /// escuche tiene que volver al suyo antes de tocar la interfaz.
    /// </summary>
    public event Action? Cambio;

    /// <summary>Cuántas coincidencias hay, que no es la cantidad de archivos.</summary>
    public int Coincidencias => Encontrados.Sum(a => a.Hallazgos.Count);

    /// <summary>
    /// Lanza la búsqueda y vuelve en el acto. Si ya hay una corriendo no hace nada: dos recorridos
    /// del mismo árbol tardan más que uno y se pisan en el mismo resultado.
    /// </summary>
    public void Buscar()
    {
        if (Buscando) return;
        if (!BuscadorService.SePuedeBuscar(Criterio, out var error))
        {
            _barra.Informar(error ?? "No se puede buscar con eso.", "estado-advertencia");
            Cambio?.Invoke();
            return;
        }

        var cancelacion = new CancellationTokenSource();

        _cancelacion = cancelacion;
        Buscando = true;
        Encontrados = [];
        Resultado = null;

        _barra.Informar("Buscando…", "estado-neutro");
        Cambio?.Invoke();

        // Sin await y sin guardar la tarea: lo que hay que saber de ella está en las propiedades de
        // acá, y lo que falle lo informa ella misma por la barra.
        _ = Correr(cancelacion);
    }

    private async Task Correr(CancellationTokenSource cancelacion)
    {
        try
        {
            var avance = new AvanceDeLaBusqueda(lote =>
            {
                Encontrados.AddRange(lote);
                Cambio?.Invoke();
            });

            var carpeta = CarpetaDelPreset;

            Func<string, bool>? estaEnLaRuta = string.IsNullOrWhiteSpace(carpeta)
                ? null
                : ruta => ruta.Contains(Path.DirectorySeparatorChar + carpeta + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                          || ruta.EndsWith(Path.DirectorySeparatorChar + carpeta, StringComparison.OrdinalIgnoreCase);

            Resultado = await _buscador
                .BuscarAsync(Criterio, avance, estaEnLaRuta, cancelacion.Token)
                .ConfigureAwait(false);

            // Con el nombre de la herramienta adelante porque el aviso puede llegar estando en otra
            // pantalla, y ahí solo "31 coincidencias" no dice de qué habla.
            Avisar(
                $"Busca Loco: {Resultado.Coincidencias} {(Resultado.Coincidencias == 1 ? "coincidencia" : "coincidencias")} " +
                $"en {Resultado.Archivos.Count} {(Resultado.Archivos.Count == 1 ? "archivo" : "archivos")}.",
                Resultado.Coincidencias > 0 ? "estado-exito" : "estado-neutro");
        }
        catch (OperationCanceledException)
        {
            // Cancelar es algo que acaba de hacer el usuario, mirando: no hace falta una pastilla.
            _barra.Informar("Búsqueda cancelada.", "estado-neutro");
        }
        catch (Exception ex)
        {
            Avisar($"Busca Loco: no se pudo completar la búsqueda. {ex.Message}", "estado-error");
        }
        finally
        {
            Buscando = false;
            _cancelacion = null;

            // Recién acá: liberarlo mientras el recorrido todavía lo usa es lo que rompía al cambiar
            // de herramienta en el medio.
            cancelacion.Dispose();

            Cambio?.Invoke();
        }
    }

    /// <summary>
    /// Deja el aviso en los dos lados, porque sirven para cosas distintas: en la barra queda
    /// mientras la pantalla valga, y la pastilla aparece aunque el usuario esté en otra herramienta.
    /// </summary>
    private void Avisar(string texto, string clase)
    {
        _barra.Informar(texto, clase);
        _avisos.Mostrar(texto, clase);
    }

    /// <summary>Corta el recorrido. Lo que ya se encontró queda a la vista.</summary>
    public void Cancelar() => _cancelacion?.Cancel();

    /// <summary>Descarta lo encontrado, sin tocar el criterio.</summary>
    public void Olvidar()
    {
        Encontrados = [];
        Resultado = null;
        Cambio?.Invoke();
    }

    /// <summary>
    /// Con qué arranca la pantalla la primera vez: apuntada a la carpeta de repositorios, mirando
    /// subcarpetas y salteando el ruido, que es lo que uno quiere el 90 % de las veces.
    /// </summary>
    private static CriterioDeBusqueda CriterioInicial() => new(
        Raiz: RutasDeTrabajo.CarpetaDeRepositorios,
        Texto: string.Empty,
        Nombre: string.Empty,
        TextoEsExpresion: false,
        NombreEsExpresion: false,
        Subcarpetas: true,
        DistinguirMayusculas: false,
        PalabraEntera: false,
        SaltearRuido: true);

    /// <summary>
    /// Recibe los lotes del recorrido.
    ///
    /// No se usa <see cref="Progress{T}"/> porque esa clase vuelve al contexto de sincronización de
    /// quien la creó, y acá quien la crea es un servicio del arranque, no la pantalla. Avisar
    /// directo y que cada pantalla vuelva a su hilo es lo que ya hace el resto del armazón.
    /// </summary>
    private sealed class AvanceDeLaBusqueda(Action<IReadOnlyList<ArchivoConHallazgos>> alAvanzar)
        : IProgress<IReadOnlyList<ArchivoConHallazgos>>
    {
        public void Report(IReadOnlyList<ArchivoConHallazgos> value) => alAvanzar(value);
    }
}
