using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Integrations.SoftCerealCore;

namespace AlgoritmoDevTools.Shell;

public partial class MainForm : Form
{
    private readonly IReadOnlyList<ITool> _tools;
    private readonly Dictionary<string, UserControl> _viewCache = new();
    private readonly SecretService _secrets = SecretService.Shared;
    private readonly IReadOnlyList<string>? _archivosDeEntrada;
    private IReadOnlyList<string>? _archivosPendientes;
    private UserControl? _currentView;

    public MainForm(IEnumerable<ITool> tools, IReadOnlyList<string>? archivosDeEntrada = null)
    {
        InitializeComponent();
        _tools = tools.ToList();
        _archivosDeEntrada = archivosDeEntrada;
        _archivosPendientes = archivosDeEntrada;

        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("app.ico"))
        {
            if (iconStream is not null) Icon = new System.Drawing.Icon(iconStream);
        }

        if (toolsList.Columns.Count == 0)
            toolsList.Columns.Add(string.Empty, toolsList.ClientSize.Width - 4);

        foreach (var tool in _tools)
        {
            if (tool.Icon is not null)
            {
                toolsImages.Images.Add(tool.Id, tool.Icon);
            }

            var item = new ListViewItem(tool.DisplayName);
            if (tool.Icon is not null)
                item.ImageKey = tool.Id;
            toolsList.Items.Add(item);
        }

        _secrets.SecretsChanged += OnSecretsChanged;

        if (_tools.Count > 0)
            toolsList.Items[0].Selected = true;
    }

    /// <summary>
    /// Indice de la primera tool que declare poder abrir los archivos recibidos, o null si no
    /// llegaron archivos o ninguna tool los reconoce.
    /// </summary>
    private int? IndiceDeLaToolParaLosArchivos()
    {
        if (_archivosDeEntrada is null || _archivosDeEntrada.Count == 0) return null;

        for (var i = 0; i < _tools.Count; i++)
        {
            if (_tools[i] is IFileTool fileTool && _archivosDeEntrada.Any(fileTool.CanOpen))
                return i;
        }

        return null;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        // La tool que abre los archivos se selecciona aca y no en el constructor: ahi el ListView
        // todavia no tiene handle y la seleccion se descarta, con lo cual al mostrarse la ventana
        // volveria a quedar la primera de la lista.
        var indice = IndiceDeLaToolParaLosArchivos();
        if (indice is not null)
            toolsList.Items[indice.Value].Selected = true;

        try
        {
            await Task.Run(() => _secrets.RefreshSecrets());
        }
        catch (Exception ex)
        {
            statusServerLabel.Text = "Server: -";
            statusDatabaseLabel.Text = "Base: -";
            statusMessageLabel.Text = $"No se pudieron leer los secretos: {ex.Message}";
        }
    }

    private void OnSecretsChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired) { BeginInvoke(new Action(UpdateStatusBar)); return; }
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        var cs = _secrets.GetConnectionString(Constantes.SecretKeys.Development);
        if (cs is null)
        {
            statusServerLabel.Text = "Server: -";
            statusDatabaseLabel.Text = "Base: -";
            statusMessageLabel.Text = "Secreto Development no configurado.";
            return;
        }

        var parts = ConnectionStringParser.Parse(cs);
        statusServerLabel.Text = $"Server: {parts.GetValueOrDefault("Server", "-")}";
        statusDatabaseLabel.Text = $"Base: {parts.GetValueOrDefault("Database", "-")}";
        statusMessageLabel.Text = string.Empty;
    }

    private void toolsList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (toolsList.SelectedIndices.Count == 0) return;
        var index = toolsList.SelectedIndices[0];
        if (index < 0 || index >= _tools.Count) return;

        var tool = _tools[index];

        descriptionLabel.Text = tool.Description;
        descriptionLabel.Update();

        if (_currentView != null)
        {
            contentPanel.Controls.Remove(_currentView);
            _currentView = null;
        }

        if (!_viewCache.TryGetValue(tool.Id, out var view))
        {
            UseWaitCursor = true;
            toolsList.Enabled = false;
            try
            {
                view = tool.CreateView();
                _viewCache[tool.Id] = view;
            }
            catch (Exception ex)
            {
                UseWaitCursor = false;
                toolsList.Enabled = true;
                MessageBox.Show($"Error al cargar '{tool.DisplayName}':\n{ex.Message}",
                    "Algoritmo DevTools", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                UseWaitCursor = false;
                toolsList.Enabled = true;
            }
        }

        view.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(view);
        _currentView = view;

        EntregarArchivosPendientes(tool, view);
    }

    /// <summary>
    /// Le pasa a la vista los archivos con los que se abrio el Shell, la primera vez que se crea la
    /// vista de una tool que sepa abrirlos. Despues los descarta, para que cambiar de tool no los
    /// vuelva a procesar.
    /// </summary>
    private void EntregarArchivosPendientes(ITool tool, UserControl view)
    {
        if (_archivosPendientes is null) return;
        if (tool is not IFileTool fileTool) return;
        if (view is not IFileReceiver receptor) return;
        if (!_archivosPendientes.Any(fileTool.CanOpen)) return;

        var archivos = _archivosPendientes;
        _archivosPendientes = null;
        receptor.ReceiveFiles(archivos);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _secrets.SecretsChanged -= OnSecretsChanged;
        base.OnFormClosed(e);
    }
}
