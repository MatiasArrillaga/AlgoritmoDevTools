using AlgoritmoDevTools.Tools.BuscaLoco.Model;
using AlgoritmoDevTools.Tools.BuscaLoco.Services;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;

namespace AlgoritmoDevTools.UI.Paginas;

/// <summary>
/// La pantalla de Busca Loco.
///
/// No guarda el criterio ni lo encontrado: eso vive en <see cref="SesionDeBusqueda"/>, que sobrevive
/// a cambiar de herramienta. Acá sólo está lo que es de la vista —si la ayuda está abierta— y los
/// armadores de lo que se dibuja.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public partial class BuscaLocoPagina : IDisposable
{
    /// <summary>
    /// Los patrones que sirven en este repositorio, no ejemplos genéricos de sintaxis.
    ///
    /// Escriben su patrón en el campo al tocarlos: es la diferencia entre una tabla de sintaxis
    /// —que nadie lee— y algo que deja salir andando. Se toca el que se parece y después se retoca.
    /// </summary>
    private static readonly (string Patron, string QueEncuentra)[] EjemplosDeExpresiones =
    [
        ("TipoCotizacion(DTO)?", "La entidad y su DTO a la vez"),
        (@"CreateMap<\w+,\s*\w+DTO>", "Los mapeos escritos a mano"),
        (@"class \w+Factory\b", "Todas las factories"),
        ("TODO|HACK|FIXME", "Cualquiera de esas tres"),
        (@"^\s*//.*contrase", "Comentarios que la nombran")
    ];

    [Inject] private SesionDeBusqueda Sesion { get; set; } = default!;
    [Inject] private BarraDeEstado Barra { get; set; } = default!;
    [Inject] private IDialogosDelSistema Dialogos { get; set; } = default!;

    private bool _ayudaAbierta;

    /// <summary>Si lo último que escribió esta pantalla en la barra fue el aviso de la guarda.</summary>
    private bool _avisoDeGuardaPuesto;

    private IReadOnlyList<ArchivoConHallazgos> Encontrados => Sesion.Encontrados;

    private bool Buscando => Sesion.Buscando;

    private string Raiz => Sesion.Criterio.Raiz;

    private bool PresetsAplican => PresetsDelStack.AplicanEn(Raiz);

    /// <summary>
    /// Si el criterio alcanza para buscar. Sale del servicio y no de un <c>if</c> en el manejador
    /// del clic: con la validación en el handler, el botón se vería habilitado y no haría nada.
    /// </summary>
    private bool SePuedeBuscar => BuscadorService.SePuedeBuscar(Sesion.Criterio, out _);

    /// <summary>El error de la expresión del texto, para el panel de ayuda.</summary>
    private string? ErrorDelPatron
    {
        get
        {
            if (!Sesion.Criterio.TextoEsExpresion || string.IsNullOrEmpty(Sesion.Criterio.Texto)) return null;

            CoincidenciaEnTexto.Crear(Sesion.Criterio.Texto, true, Sesion.Criterio.DistinguirMayusculas, false, out var error);
            return error;
        }
    }

    // El criterio es un record inmutable, así que cada cambio lo reemplaza con `with`. Las
    // propiedades de acá son el puente: la vista las bindea y ellas reescriben el criterio entero.

    private string Texto
    {
        get => Sesion.Criterio.Texto;
        set => Cambiar(Sesion.Criterio with { Texto = value });
    }

    private string Nombre
    {
        get => Sesion.Criterio.Nombre;
        set => Cambiar(Sesion.Criterio with { Nombre = value });
    }

    private bool TextoEsExpresion => Sesion.Criterio.TextoEsExpresion;

    private bool NombreEsExpresion => Sesion.Criterio.NombreEsExpresion;

    private bool Subcarpetas => Sesion.Criterio.Subcarpetas;

    private bool DistinguirMayusculas => Sesion.Criterio.DistinguirMayusculas;

    private bool PalabraEntera => Sesion.Criterio.PalabraEntera;

    private bool SaltearRuido => Sesion.Criterio.SaltearRuido;

    protected override void OnInitialized()
    {
        Sesion.Cambio += AlCambiarLaSesion;
        RevisarLaGuarda();
    }

    public void Dispose()
    {
        Sesion.Cambio -= AlCambiarLaSesion;

        // Si hay una búsqueda corriendo, su aviso es justamente lo que tiene que quedar a la vista
        // desde otra herramienta.
        if (!Sesion.Buscando) Barra.Limpiar();
    }

    /// <summary>
    /// La sesión avisa desde los hilos del recorrido, así que hay que volver al de la interfaz antes
    /// de repintar. Traga <see cref="ObjectDisposedException"/> por la ventana que queda entre el
    /// aviso y la desuscripción.
    /// </summary>
    private async void AlCambiarLaSesion()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void Cambiar(CriterioDeBusqueda criterio)
    {
        Sesion.Criterio = criterio;
        RevisarLaGuarda();
    }

    /// <summary>
    /// Deja dicho en la barra qué falta para poder buscar.
    ///
    /// Un botón gris sin explicación es peor que un error: el usuario ve que no puede y no sabe qué
    /// le falta. Y va a la barra de estado, que es el canal del estado actual de la pantalla, no a
    /// un renglón adentro de la vista.
    /// </summary>
    private void RevisarLaGuarda()
    {
        if (!BuscadorService.SePuedeBuscar(Sesion.Criterio, out var error))
        {
            Barra.Informar(error!, "estado-advertencia");
            _avisoDeGuardaPuesto = true;
            return;
        }

        // Sólo se limpia lo que puso esta guarda: el resultado de una búsqueda anterior tiene que
        // seguir ahí mientras valga.
        if (_avisoDeGuardaPuesto)
        {
            Barra.Limpiar();
            _avisoDeGuardaPuesto = false;
        }
    }

    private void Buscar()
    {
        _avisoDeGuardaPuesto = false;
        Sesion.Buscar();
    }

    private void Cancelar() => Sesion.Cancelar();

    private void AlternarAyuda() => _ayudaAbierta = !_ayudaAbierta;

    private void AlternarExpresionDelTexto()
    {
        var esExpresion = !TextoEsExpresion;

        // Palabra entera se apaga al pasar a expresión: se implementa envolviendo el patrón en
        // \b…\b, y eso sobre una expresión arbitraria le cambia el significado sin avisar.
        Cambiar(Sesion.Criterio with
        {
            TextoEsExpresion = esExpresion,
            PalabraEntera = esExpresion ? false : PalabraEntera
        });

        if (!esExpresion) _ayudaAbierta = false;
    }

    private void AlternarExpresionDelNombre() =>
        Cambiar(Sesion.Criterio with { NombreEsExpresion = !NombreEsExpresion });

    private void AlternarSubcarpetas(ChangeEventArgs e) =>
        Cambiar(Sesion.Criterio with { Subcarpetas = e.Value is true });

    private void AlternarMayusculas(ChangeEventArgs e) =>
        Cambiar(Sesion.Criterio with { DistinguirMayusculas = e.Value is true });

    private void AlternarPalabraEntera(ChangeEventArgs e) =>
        Cambiar(Sesion.Criterio with { PalabraEntera = e.Value is true });

    private void AlternarRuido(ChangeEventArgs e) =>
        Cambiar(Sesion.Criterio with { SaltearRuido = e.Value is true });

    /// <summary>
    /// El preset escribe su patrón en el campo en lugar de aplicarlo por abajo: así se ve qué hizo
    /// y se puede retocar.
    /// </summary>
    private void UsarPreset(PresetDelStack preset)
    {
        Cambiar(Sesion.Criterio with { Nombre = preset.PatronDeNombre, NombreEsExpresion = false });
        Sesion.CarpetaDelPreset = preset.CarpetaEnLaRuta;
    }

    private void UsarEjemplo(string patron) => Texto = patron;

    /// <summary>
    /// El diálogo de Windows va con <c>await</c> por obligación: abrirlo en el acto desde un clic
    /// que el visor todavía está entregando cierra la aplicación entera, sin excepción ni log.
    /// </summary>
    private async Task ElegirCarpeta()
    {
        var elegida = await Dialogos.ElegirCarpeta("Dónde buscar", Raiz);
        if (elegida is null) return;

        Cambiar(Sesion.Criterio with { Raiz = elegida });
        Sesion.Olvidar();
    }

    private string Resumen()
    {
        if (Buscando) return $"{Sesion.Coincidencias} hasta ahora…";
        if (Sesion.Resultado is null) return string.Empty;

        var r = Sesion.Resultado;
        var texto = $"{r.Coincidencias} {(r.Coincidencias == 1 ? "coincidencia" : "coincidencias")} " +
                    $"en {r.Archivos.Count} {(r.Archivos.Count == 1 ? "archivo" : "archivos")} · " +
                    $"{r.ArchivosMirados:N0} archivos recorridos en {r.Duracion.TotalSeconds:N1} s";

        // Los que no se pudieron mirar se dicen, no desaparecen: son archivos que el usuario
        // esperaba que se miraran.
        return r.ArchivosNoEvaluados > 0 ? $"{texto} · {r.ArchivosNoEvaluados} sin poder mirar" : texto;
    }

    private static string Cuenta(ArchivoConHallazgos archivo) =>
        archivo.Hallazgos.Count switch
        {
            0 => "por nombre",
            1 => "1 coincidencia",
            var n => $"{n} coincidencias"
        };

    /// <summary>
    /// Las coincidencias agrupadas por línea: dos hits en el mismo renglón son un renglón con dos
    /// resaltados, no dos renglones repetidos.
    /// </summary>
    private static IEnumerable<LineaDeResultado> PorLinea(ArchivoConHallazgos archivo) =>
        archivo.Hallazgos
            .GroupBy(h => h.Linea)
            .Select(g => new LineaDeResultado(g.Key, g.First().Texto, [.. g.OrderBy(h => h.Desde)]));

    /// <summary>
    /// Parte la línea en los trozos que van resaltados y los que no.
    ///
    /// Se arma con los rangos que guardó la búsqueda y no volviendo a correr la expresión: con una
    /// expresión cara, hacerlo al dibujar sería pagar dos veces lo mismo.
    /// </summary>
    private static IEnumerable<(string Texto, bool EsCoincidencia)> Segmentos(LineaDeResultado linea)
    {
        var desde = 0;

        foreach (var hallazgo in linea.Coincidencias)
        {
            // Dos coincidencias solapadas dejarían un trozo de largo negativo: se saltea la segunda.
            if (hallazgo.Desde < desde) continue;

            if (hallazgo.Desde > desde)
            {
                yield return (linea.Texto[desde..hallazgo.Desde], false);
            }

            var hasta = Math.Min(hallazgo.Desde + hallazgo.Largo, linea.Texto.Length);
            yield return (linea.Texto[hallazgo.Desde..hasta], true);

            desde = hasta;
        }

        if (desde < linea.Texto.Length)
        {
            yield return (linea.Texto[desde..], false);
        }
    }

    /// <summary>Una línea del archivo con todas sus coincidencias, ya ordenadas.</summary>
    private sealed record LineaDeResultado(int Numero, string Texto, IReadOnlyList<Hallazgo> Coincidencias);
}
