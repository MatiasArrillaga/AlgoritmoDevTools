using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Tools.MarkdownConverter.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class MarkdownConverterPagina : IDisposable
{
    [Inject]
    private DocumentConverter Convertidor { get; set; } = default!;

    [Inject]
    private IDialogosDelSistema Dialogos { get; set; } = default!;

    [Inject]
    private IArchivosArrastrados Arrastre { get; set; } = default!;

    [Inject]
    private IZonaDeArrastre ZonaDeArrastre { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

    private const string HtmlNo = "no";
    private const string HtmlClaro = "claro";
    private const string HtmlOscuro = "oscuro";

    private readonly StringBuilder _registro = new();

    private CancellationTokenSource? _cancelacion;
    private bool _hayPandoc;
    private bool _convirtiendo;
    private bool _quitarTachado = true;
    private bool _menuInstalado;
    private string _html = HtmlNo;
    private string? _ultimaCarpeta;
    private string _estadoMenu = string.Empty;
    private string _claseEstadoMenu = "estado-neutro";

    protected override void OnInitialized()
    {
        Arrastre.Soltados += AlSoltarArchivos;

        _hayPandoc = Convertidor.PandocDisponible;
        ActualizarEstadoDelMenu();

        if (!_hayPandoc)
        {
            Informar("Falta pandoc.", "estado-error");
            return;
        }

        // La franja donde se sueltan los documentos la dibuja la ventana, fuera del navegador: es
        // la única superficie que recibe las rutas en disco. Se arma acá y aparece sola cuando el
        // usuario empieza a arrastrar algo sobre la ventana.
        ZonaDeArrastre.Armar("Soltá acá los documentos a convertir");
    }

    /// <summary>
    /// Lo que explica el signo de pregunta del título: qué deja la conversión, qué sabe convertir y
    /// con qué pandoc. Son datos de consulta —se miran una vez y no vuelven a hacer falta—, así que
    /// viven acá y no ocupando lugar arriba de la pantalla.
    /// </summary>
    private string TextoDeAyuda
    {
        get
        {
            var formatos = string.Join(" ", Convertidor.ExtensionesSoportadas);
            var donde = _hayPandoc ? $" Usa el pandoc de {Convertidor.PandocPath}." : string.Empty;

            return $"El .md y sus imágenes quedan al lado del documento original. Convierte: {formatos}.{donde}";
        }
    }

    private ConversionOptions OpcionesActuales()
    {
        HtmlTheme? tema = _html switch
        {
            HtmlClaro => HtmlTheme.Claro,
            HtmlOscuro => HtmlTheme.Oscuro,
            _ => null
        };

        return new ConversionOptions(_quitarTachado, tema);
    }

    private async Task ElegirYConvertir()
    {
        var extensiones = string.Join(";", Convertidor.ExtensionesSoportadas.Select(ext => "*" + ext));
        var filtro = $"Documentos convertibles ({extensiones})|{extensiones}|Todos los archivos (*.*)|*.*";

        var rutas = await Dialogos.ElegirArchivos("Elegí los documentos a convertir", filtro, _ultimaCarpeta);
        if (rutas.Count == 0) return;

        await Convertir(rutas);
    }

    private async Task Convertir(IReadOnlyList<string> rutas)
    {
        if (_cancelacion is not null) return;

        _cancelacion = new CancellationTokenSource();
        var token = _cancelacion.Token;
        var opciones = OpcionesActuales();
        var convertidos = 0;

        _convirtiendo = true;

        try
        {
            foreach (var ruta in rutas)
            {
                if (token.IsCancellationRequested) break;

                if (Directory.Exists(ruta))
                {
                    Escribir($"SALTEADO  {Path.GetFileName(ruta)}  ->  es una carpeta");
                    continue;
                }

                if (!File.Exists(ruta)) continue;

                Informar($"Convirtiendo {Path.GetFileName(ruta)}...", "estado-neutro");
                StateHasChanged();

                ConversionResult resultado;
                try
                {
                    resultado = await Task.Run(() => Convertidor.Convert(ruta, opciones, token), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                Escribir(Describir(resultado));

                if (resultado.OutputPath is not null)
                {
                    convertidos++;
                    _ultimaCarpeta = Path.GetDirectoryName(resultado.OutputPath);
                }
            }

            Informar(
                token.IsCancellationRequested
                    ? $"Cortado. {convertidos} archivo(s) convertido(s)."
                    : $"Listo. {convertidos} archivo(s) convertido(s).",
                convertidos > 0 ? "estado-exito" : "estado-neutro");

            Escribir(string.Empty);
        }
        finally
        {
            _cancelacion.Dispose();
            _cancelacion = null;
            _convirtiendo = false;
        }
    }

    private void Detener()
    {
        _cancelacion?.Cancel();
        Informar("Cortando...", "estado-advertencia");
    }

    private void AbrirCarpeta()
    {
        if (!string.IsNullOrEmpty(_ultimaCarpeta)) Dialogos.AbrirCarpeta(_ultimaCarpeta);
    }

    private static string Describir(ConversionResult resultado)
    {
        if (resultado.Skipped) return $"SALTEADO  {resultado.SourceName}  ->  {resultado.Error}";
        if (resultado.Error is not null) return $"ERROR     {resultado.SourceName}  ->  {resultado.Error}";

        var detalle = new List<string> { (resultado.OutputBytes / 1024.0).ToString("0.0") + " KB" };

        if (resultado.TrimmedPercent > 0) detalle.Add($"{resultado.TrimmedPercent}% menos de relleno");
        if (resultado.StrikeRemoved > 0) detalle.Add($"{resultado.StrikeRemoved} tachado(s) fuera");
        if (resultado.HtmlPath is not null) detalle.Add("+ " + Path.GetFileName(resultado.HtmlPath));

        return $"OK        {Path.GetFileName(resultado.OutputPath!)}  ({string.Join(", ", detalle)})";
    }

    // --- Menú contextual ----------------------------------------------------

    private void InstalarMenu()
    {
        var error = MenuContextual.TryInstalar(Convertidor.ExtensionesSoportadas);
        if (error is not null) EstadoMenu($"No se pudo agregar al menú: {error}", "estado-error");
        ActualizarEstadoDelMenu();
    }

    private void QuitarMenu()
    {
        var error = MenuContextual.TryDesinstalar(Convertidor.ExtensionesSoportadas);
        if (error is not null) EstadoMenu($"No se pudo quitar del menú: {error}", "estado-error");
        ActualizarEstadoDelMenu();
    }

    private void ActualizarEstadoDelMenu()
    {
        _menuInstalado = MenuContextual.EstaInstalado(Convertidor.ExtensionesSoportadas);

        if (!_menuInstalado)
        {
            EstadoMenu("Menú contextual: no instalado.", "estado-neutro");
            return;
        }

        var usados = MenuContextualDevTools.ContarItems();
        EstadoMenu($"Menú contextual: {usados}/{MenuContextualDevTools.LimiteDeItems} ítems.",
            usados >= MenuContextualDevTools.LimiteDeItems ? "estado-error" : "estado-exito");
    }

    private void Escribir(string linea) => _registro.AppendLine(linea);

    private void Informar(string texto, string clase) => Barra.Informar(texto, clase);

    private void EstadoMenu(string texto, string clase)
    {
        _estadoMenu = texto;
        _claseEstadoMenu = clase;
    }

    /// <summary>
    /// Lo que se suelta sobre la ventana llega acá con su ruta real. Se ignora mientras hay una
    /// conversión en curso: el convertidor procesa una tanda por vez.
    /// </summary>
    private async void AlSoltarArchivos(object? sender, IReadOnlyList<string> rutas)
    {
        if (!_hayPandoc || _convirtiendo) return;

        await InvokeAsync(() => Convertir(rutas));
    }

    public void Dispose()
    {
        Barra.Limpiar();
        Arrastre.Soltados -= AlSoltarArchivos;
        ZonaDeArrastre.Desarmar();
        _cancelacion?.Cancel();
        _cancelacion?.Dispose();
    }
}
