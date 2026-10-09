using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Shell.Views;

namespace AlgoritmoDevTools.Shell;

public partial class MainForm : Form
{
    private readonly IReadOnlyList<ITool> _tools;
    private readonly Dictionary<string, UserControl> _viewCache = new();
    private readonly SecretService _secrets = SecretService.Shared;
    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private UserControl? _currentView;
    private ConfiguracionesView? _configuracionesView;

    /// <summary>Índice de la fila que tiene el mouse encima, para dibujarle el resaltado.</summary>
    private int _indiceBajoElMouse = -1;

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

        PrepararListaDeHerramientas();

        _secrets.SecretsChanged += OnSecretsChanged;
        _temas.TemaCambiado += OnTemaCambiado;
        _temas.AplicarA(this);

        SeleccionarHerramientaInicial();
    }

    /// <summary>
    /// El resaltado de la fila elegida lo dibujamos nosotros: el ListView, librado a Windows, la
    /// pinta del azul del sistema, que no tiene nada que ver con el tema elegido.
    /// </summary>
    private void PrepararListaDeHerramientas()
    {
        toolsList.OwnerDraw = true;
        toolsList.DrawColumnHeader += (_, e) => e.DrawDefault = false;
        toolsList.DrawSubItem += (_, e) => e.DrawDefault = false;
        toolsList.DrawItem += toolsList_DrawItem;
        toolsList.MouseMove += toolsList_MouseMove;
        toolsList.MouseLeave += toolsList_MouseLeave;
    }

    /// <summary>Abre la primera herramienta de la lista.</summary>
    private void SeleccionarHerramientaInicial()
    {
        if (_tools.Count == 0) return;

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

        MarcarConfiguracionesActivo(false);

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
        _temas.AplicarA(view);
        contentPanel.Controls.Add(view);
        _currentView = view;
    }

    private void settingsButton_Click(object? sender, EventArgs e) => MostrarConfiguraciones();

    /// <summary>
    /// Trae la pantalla de Configuraciones al panel central. La lista de herramientas se deselecciona
    /// porque ninguna está abierta: el resaltado tiene que estar en el botón del pie.
    /// </summary>
    private void MostrarConfiguraciones()
    {
        _configuracionesView ??= new ConfiguracionesView();

        toolsList.SelectedIndices.Clear();
        descriptionLabel.Text = "Ajusta la apariencia y el comportamiento de Algoritmo DevTools.";

        if (_currentView != null)
        {
            contentPanel.Controls.Remove(_currentView);
            _currentView = null;
        }

        _temas.AplicarA(_configuracionesView);
        contentPanel.Controls.Add(_configuracionesView);
        _currentView = _configuracionesView;
        MarcarConfiguracionesActivo(true);
    }

    /// <summary>Pinta el botón del pie como la fila activa del lateral cuando Configuraciones está abierta.</summary>
    private void MarcarConfiguracionesActivo(bool activo)
    {
        var tema = _temas.Actual;
        settingsButton.BackColor = activo ? tema.Acento : tema.FondoLateral;
        settingsButton.ForeColor = activo ? tema.TextoSobreAcento : tema.Texto;
        settingsButton.FlatAppearance.BorderSize = 0;
        settingsButton.FlatAppearance.MouseOverBackColor = activo ? tema.AcentoHover : tema.BotonSecundarioHover;
        settingsButton.FlatAppearance.MouseDownBackColor = activo ? tema.AcentoPresionado : tema.BordeCampo;
    }

    /// <summary>
    /// Repinta todo con el tema nuevo: la ventana, la vista abierta y las que quedaron en caché, que
    /// están fuera del árbol de controles y por eso no las alcanza el repintado del formulario.
    /// </summary>
    private void OnTemaCambiado(object? sender, EventArgs e)
    {
        _temas.AplicarA(this);

        foreach (var vista in _viewCache.Values)
        {
            _temas.AplicarA(vista);
        }

        if (_configuracionesView is not null) _temas.AplicarA(_configuracionesView);

        MarcarConfiguracionesActivo(_currentView is ConfiguracionesView);
        toolsList.Invalidate();
    }

    private void toolsList_DrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        var tema = _temas.Actual;
        var seleccionado = e.Item is not null && e.Item.Selected;
        var resaltado = !seleccionado && e.ItemIndex == _indiceBajoElMouse;

        var fondo = seleccionado ? tema.Acento
            : resaltado ? tema.BotonSecundarioHover
            : tema.FondoLateral;

        using (var pincel = new SolidBrush(fondo))
        {
            e.Graphics.FillRectangle(pincel, e.Bounds);
        }

        if (e.Item is null) return;

        var izquierda = e.Bounds.Left + 14;

        if (!string.IsNullOrEmpty(e.Item.ImageKey) && toolsImages.Images.ContainsKey(e.Item.ImageKey))
        {
            var icono = toolsImages.Images[e.Item.ImageKey];
            if (icono is not null)
            {
                var alto = Math.Min(20, e.Bounds.Height - 4);
                var y = e.Bounds.Top + (e.Bounds.Height - alto) / 2;
                e.Graphics.DrawImage(icono, izquierda, y, alto, alto);
                izquierda += alto + 10;
            }
        }

        var rectanguloTexto = new Rectangle(
            izquierda,
            e.Bounds.Top,
            Math.Max(0, e.Bounds.Right - izquierda - 8),
            e.Bounds.Height);

        TextRenderer.DrawText(
            e.Graphics,
            e.Item.Text,
            toolsList.Font,
            rectanguloTexto,
            seleccionado ? tema.TextoSobreAcento : tema.Texto,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private void toolsList_MouseMove(object? sender, MouseEventArgs e)
    {
        var item = toolsList.GetItemAt(e.X, e.Y);
        var indice = item?.Index ?? -1;
        if (indice == _indiceBajoElMouse) return;

        _indiceBajoElMouse = indice;
        toolsList.Invalidate();
    }

    private void toolsList_MouseLeave(object? sender, EventArgs e)
    {
        if (_indiceBajoElMouse == -1) return;

        _indiceBajoElMouse = -1;
        toolsList.Invalidate();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _secrets.SecretsChanged -= OnSecretsChanged;
        _temas.TemaCambiado -= OnTemaCambiado;
        base.OnFormClosed(e);
    }
}
