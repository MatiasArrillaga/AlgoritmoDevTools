using AlgoritmoDevTools.Core.Abstractions;

namespace AlgoritmoDevTools.Shell;

public partial class MainForm : Form
{
    private readonly IReadOnlyList<ITool> _tools;
    private readonly Dictionary<string, UserControl> _viewCache = new();
    private UserControl? _currentView;

    public MainForm(IEnumerable<ITool> tools)
    {
        InitializeComponent();
        _tools = tools.ToList();

        foreach (var tool in _tools)
        {
            toolsList.Items.Add(tool.DisplayName);
        }

        if (_tools.Count > 0)
            toolsList.SelectedIndex = 0;
    }

    private void toolsList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var index = toolsList.SelectedIndex;
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
}
