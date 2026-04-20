using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Integrations.SoftCerealCore;

namespace AlgoritmoDevTools.Shell;

public partial class MainForm : Form
{
    private readonly IReadOnlyList<ITool> _tools;
    private readonly Dictionary<string, UserControl> _viewCache = new();
    private readonly SecretService _secrets = SecretService.Shared;
    private UserControl? _currentView;

    public MainForm(IEnumerable<ITool> tools)
    {
        InitializeComponent();
        _tools = tools.ToList();

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

    private async void MainForm_Load(object? sender, EventArgs e)
    {
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
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _secrets.SecretsChanged -= OnSecretsChanged;
        base.OnFormClosed(e);
    }
}
