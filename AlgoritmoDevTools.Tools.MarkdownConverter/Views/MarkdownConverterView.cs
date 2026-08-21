using AlgoritmoDevTools.Tools.MarkdownConverter.Services;
using System.Diagnostics;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Views;

public partial class MarkdownConverterView : UserControl
{
    private const string TITULO = "Convertidor a Markdown";

    private readonly DocumentConverter _converter;
    private CancellationTokenSource? _cts;
    private string? _ultimaCarpeta;

    public MarkdownConverterView(DocumentConverter converter)
    {
        _converter = converter;
        InitializeComponent();
        SetupTooltips();
        SetupDragAndDrop();
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 10_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(DropPanel, "Arrastrá uno o varios documentos. El .md queda al lado del original.");
        tips.SetToolTip(ElegirBtn, "Abre el explorador para elegir los documentos a convertir.");
        tips.SetToolTip(DetenerBtn, "Corta la conversión en curso. Los archivos ya convertidos quedan.");
        tips.SetToolTip(AbrirCarpetaBtn, "Abre la carpeta del último archivo convertido.");
        tips.SetToolTip(OutputTxt, "Resultado de cada archivo: tamaño del .md y cuánto relleno de Word se descartó.");
    }

    // El drop se engancha en el panel y en su label: si sólo se enganchara en el panel, soltar
    // encima del texto no dispararía nada porque el label se come el evento.
    private void SetupDragAndDrop()
    {
        foreach (Control control in new Control[] { this, DropPanel, DropLbl })
        {
            control.DragEnter += OnDragEnter;
            control.DragDrop += OnDragDrop;
        }
    }

    private void MarkdownConverterView_Load(object? sender, EventArgs e)
    {
        FormatosLbl.Text = "Formatos: " + string.Join("  ", _converter.ExtensionesSoportadas);

        if (!_converter.PandocDisponible)
        {
            PandocLbl.Text = "pandoc: no encontrado";
            StatusLbl.Text = "Falta pandoc.";
            Escribir("No se encontró pandoc, que es lo que hace la conversión.");
            Escribir("Instalalo con:  winget install --id JohnMacFarlane.Pandoc -e");
            Escribir("Después volvé a entrar a esta herramienta.");
            DropPanel.Enabled = false;
            ElegirBtn.Enabled = false;
            return;
        }

        PandocLbl.Text = "pandoc: " + _converter.PandocPath;
        Escribir("Las imágenes se extraen a una carpeta 'media' al lado del archivo.");
        Escribir(string.Empty);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        var hayArchivos = e.Data is not null && e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effect = hayArchivos && ElegirBtn.Enabled ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] archivos)
        {
            await ConvertirAsync(archivos);
        }
    }

    private async void ElegirBtn_Click(object? sender, EventArgs e)
    {
        var filtro = string.Join(";", _converter.ExtensionesSoportadas.Select(ext => "*" + ext));

        using var dialog = new OpenFileDialog
        {
            Title = "Elegí los documentos a convertir",
            Multiselect = true,
            Filter = $"Documentos convertibles ({filtro})|{filtro}|Todos los archivos (*.*)|*.*"
        };

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
        {
            await ConvertirAsync(dialog.FileNames);
        }
    }

    private void DetenerBtn_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        StatusLbl.Text = "Cortando...";
    }

    private void AbrirCarpetaBtn_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_ultimaCarpeta) || !Directory.Exists(_ultimaCarpeta)) return;

        Process.Start(new ProcessStartInfo(_ultimaCarpeta) { UseShellExecute = true });
    }

    private async Task ConvertirAsync(IReadOnlyList<string> rutas)
    {
        if (_cts is not null) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        ElegirBtn.Enabled = false;
        DropPanel.Enabled = false;
        DetenerBtn.Enabled = true;

        var convertidos = 0;

        try
        {
            foreach (var ruta in rutas)
            {
                if (token.IsCancellationRequested) break;

                if (Directory.Exists(ruta))
                {
                    Escribir($"SALTEADO  {Path.GetFileName(ruta)}  ->  es una carpeta, arrastrá archivos");
                    continue;
                }

                if (!File.Exists(ruta)) continue;

                StatusLbl.Text = "Convirtiendo " + Path.GetFileName(ruta) + "...";

                ConversionResult resultado;
                try
                {
                    resultado = await Task.Run(() => _converter.Convert(ruta, token), token);
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
                    AbrirCarpetaBtn.Enabled = !string.IsNullOrEmpty(_ultimaCarpeta);
                }
            }

            StatusLbl.Text = token.IsCancellationRequested
                ? $"Cortado. {convertidos} archivo(s) convertido(s)."
                : $"Listo. {convertidos} archivo(s) convertido(s).";
            Escribir(string.Empty);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            ElegirBtn.Enabled = true;
            DropPanel.Enabled = true;
            DetenerBtn.Enabled = false;
        }
    }

    private static string Describir(ConversionResult resultado)
    {
        if (resultado.Skipped)
        {
            return $"SALTEADO  {resultado.SourceName}  ->  {resultado.Error}";
        }

        if (resultado.Error is not null)
        {
            return $"ERROR     {resultado.SourceName}  ->  {resultado.Error}";
        }

        var peso = (resultado.OutputBytes / 1024.0).ToString("0.0") + " KB";
        var recorte = resultado.TrimmedPercent > 0
            ? $", {resultado.TrimmedPercent}% menos de relleno"
            : string.Empty;

        return $"OK        {Path.GetFileName(resultado.OutputPath!)}  ({peso}{recorte})";
    }

    private void Escribir(string linea) => OutputTxt.AppendText(linea + Environment.NewLine);
}
