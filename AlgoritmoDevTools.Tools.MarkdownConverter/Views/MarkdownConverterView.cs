using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.MarkdownConverter.Services;
using System.Diagnostics;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Views;

public partial class MarkdownConverterView : UserControl, IFileReceiver
{
    private const string TITULO = "Convertidor a Markdown";

    private readonly DocumentConverter _converter;
    private CancellationTokenSource? _cts;
    private string? _ultimaCarpeta;
    private IReadOnlyList<string>? _pendientes;
    private bool _yaCargo;

    public MarkdownConverterView(DocumentConverter converter)
    {
        _converter = converter;
        InitializeComponent();
        SetupOpciones();
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
        tips.SetToolTip(TachadoChk, "En las ERS el texto tachado son requisitos que se descartaron. Sacarlo ahorra contexto y evita que el asistente implemente algo que ya no va.");
        tips.SetToolTip(HtmlCmb, "Genera además un .html con estilos para leer el documento cómodo. El .md no lleva estilos: el tema aplica sólo al HTML.");
        tips.SetToolTip(MenuAgregarBtn, "Agrega 'Convertir a Markdown' al clic derecho del explorador, para todos los formatos soportados. Va en HKEY_CURRENT_USER: no necesita permisos de administrador.");
        tips.SetToolTip(MenuQuitarBtn, "Saca la opción del clic derecho y deja el registro como estaba.");
        tips.SetToolTip(OutputTxt, "Resultado de cada archivo: tamaño del .md y cuánto relleno de Word se descartó.");
    }

    private void SetupOpciones()
    {
        HtmlCmb.Items.AddRange(new object[] { "no generar", "tema claro", "tema oscuro" });
        HtmlCmb.SelectedIndex = 0;
    }

    private ConversionOptions OpcionesActuales()
    {
        HtmlTheme? tema = HtmlCmb.SelectedIndex switch
        {
            1 => HtmlTheme.Claro,
            2 => HtmlTheme.Oscuro,
            _ => null
        };

        return new ConversionOptions(TachadoChk.Checked, tema);
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
        _yaCargo = true;
        FormatosLbl.Text = "Formatos: " + string.Join("  ", _converter.ExtensionesSoportadas);
        ActualizarEstadoDelMenu();

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

        // Los archivos que llegaron del explorador se convierten recién acá: en el constructor la
        // vista todavía no está en pantalla y el log no se vería.
        if (_pendientes is not null)
        {
            var deEntrada = _pendientes;
            _pendientes = null;
            BeginInvoke(new Action(async () => await ConvertirAsync(deEntrada)));
        }
    }

    /// <summary>
    /// Recibe los archivos con los que se abrió el Shell (menú contextual del explorador).
    /// El Shell puede llamar a esto antes o después del Load de la vista según cuándo agregue el
    /// control al panel, así que se cubren los dos casos: si la vista ya está en pantalla se
    /// convierte de una, y si todavía no, queda pendiente para el Load.
    /// </summary>
    public void ReceiveFiles(IReadOnlyList<string> filePaths)
    {
        if (filePaths.Count == 0) return;

        if (_yaCargo && IsHandleCreated)
        {
            BeginInvoke(new Action(async () => await ConvertirAsync(filePaths)));
            return;
        }

        _pendientes = filePaths;
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

    private void MenuAgregarBtn_Click(object? sender, EventArgs e)
    {
        var error = MenuContextual.TryInstalar(_converter.ExtensionesSoportadas);

        if (error is not null)
        {
            MessageBox.Show($"No se pudo agregar al menú contextual:\n{error}",
                TITULO, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            Escribir($"Agregado al menú contextual para {_converter.ExtensionesSoportadas.Count} extensiones.");
            Escribir("Probalo: clic derecho sobre un documento → Convertir a Markdown.");
            Escribir(string.Empty);
        }

        ActualizarEstadoDelMenu();
    }

    private void MenuQuitarBtn_Click(object? sender, EventArgs e)
    {
        var error = MenuContextual.TryDesinstalar(_converter.ExtensionesSoportadas);

        if (error is not null)
        {
            MessageBox.Show($"No se pudo quitar del menú contextual:\n{error}",
                TITULO, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            Escribir("Quitado del menú contextual.");
            Escribir(string.Empty);
        }

        ActualizarEstadoDelMenu();
    }

    /// <summary>
    /// Habilita un botón u otro según si el verbo ya está registrado. Las dos operaciones son
    /// idempotentes igual — el registro sobreescribe y el borrado no falla si no existe —, pero
    /// deshabilitar el que no corresponde deja claro en qué estado está.
    /// </summary>
    private void ActualizarEstadoDelMenu()
    {
        var instalado = MenuContextual.EstaInstalado(_converter.ExtensionesSoportadas);

        MenuAgregarBtn.Enabled = !instalado;
        MenuQuitarBtn.Enabled = instalado;
        MenuEstadoLbl.Text = instalado
            ? "En el menú contextual: sí, clic derecho sobre un documento y aparece."
            : "En el menú contextual: no.";
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

        var opciones = OpcionesActuales();

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
                    resultado = await Task.Run(() => _converter.Convert(ruta, opciones, token), token);
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

        var detalle = new List<string> { (resultado.OutputBytes / 1024.0).ToString("0.0") + " KB" };

        if (resultado.TrimmedPercent > 0)
        {
            detalle.Add($"{resultado.TrimmedPercent}% menos de relleno");
        }

        if (resultado.StrikeRemoved > 0)
        {
            detalle.Add($"{resultado.StrikeRemoved} tachado(s) fuera");
        }

        if (resultado.HtmlPath is not null)
        {
            detalle.Add("+ " + Path.GetFileName(resultado.HtmlPath));
        }

        return $"OK        {Path.GetFileName(resultado.OutputPath!)}  ({string.Join(", ", detalle)})";
    }

    private void Escribir(string linea) => OutputTxt.AppendText(linea + Environment.NewLine);
}
