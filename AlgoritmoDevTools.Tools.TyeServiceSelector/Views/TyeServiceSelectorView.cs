using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Views;

public partial class TyeServiceSelectorView : UserControl
{
    private const string SinPerfil = "(sin perfil)";

    /// <summary>Servicio que siempre queda tildado y no se puede destildar.</summary>
    private const string ServicioObligatorio = "seguridad";

    /// <summary>Cantidad de columnas en las que se reparte la grilla de servicios.</summary>
    private const int ColumnasServicios = 3;

    private const int AltoFilaServicio = 30;

    private readonly ProfileRepository _profiles;

    /// <summary>Un CheckBox por servicio, en el mismo orden en que vienen del tye.yaml. El nombre
    /// real del servicio va en el <c>Tag</c>, porque el <c>Text</c> puede llevar decoración.</summary>
    private readonly List<CheckBox> _serviceChecks = new();

    /// <summary>Se cuelga del contenedor de componentes de la vista para que se libere con ella.</summary>
    private ToolTip _tips = null!;

    private bool _suppressProfileEvent;

    public TyeServiceSelectorView(ProfileRepository profiles)
    {
        _profiles = profiles;
        InitializeComponent();
        SetupTooltips();
    }

    private void SetupTooltips()
    {
        _tips = new ToolTip(components) { AutoPopDelay = 12_000, InitialDelay = 400, ReshowDelay = 200 };

        _tips.SetToolTip(RefrescarBtn, "Vuelve a leer el tye.yaml y refleja qué servicios están activos según el archivo generado.");
        _tips.SetToolTip(MarcarTodosBtn, "Tilda todos los servicios.");
        _tips.SetToolTip(DesmarcarTodosBtn, "Destilda todos los servicios.");
        _tips.SetToolTip(ServicesPanel, $"Tildá los microservicios que querés levantar. '{ServicioObligatorio}' siempre queda activo. El master tye.yaml no se modifica.");
        _tips.SetToolTip(ProfilesCombo, "Elegí un perfil guardado para tildar automáticamente sus servicios.");
        _tips.SetToolTip(GuardarPerfilBtn, "Guarda la selección actual como un perfil con nombre (o sobrescribe el existente).");
        _tips.SetToolTip(EliminarPerfilBtn, "Elimina el perfil seleccionado.");
        _tips.SetToolTip(GenerarBtn, $"Genera {TyeServiceToggler.GeneratedFileName} en la raíz de AlgoritmoCore con los servicios tildados activos y el resto comentados.");
        _tips.SetToolTip(CopiarComandoBtn, $"Copia al portapapeles: {TyeServiceToggler.RunCommand}");
    }

    private void TyeServiceSelectorView_Load(object? sender, EventArgs e)
    {
        PathLbl.Text =
            $"Master (solo lectura): {TyeServiceToggler.MasterYamlPath}\r\n" +
            $"Genera: {TyeServiceToggler.GeneratedYamlPath}";
        LoadServices();
        LoadProfiles();
    }

    private void LoadServices()
    {
        if (!File.Exists(TyeServiceToggler.MasterYamlPath))
        {
            RenderServices(Array.Empty<TyeService>());
            SetStatus($"No se encontró el master en {TyeServiceToggler.MasterYamlPath}.", Color.Firebrick);
            return;
        }

        var services = TyeServiceToggler.ReadServices();
        RenderServices(services);

        if (services.Count == 0)
        {
            SetStatus("No se encontraron servicios en la lista 'services:' del tye.yaml.", Color.DarkOrange);
            return;
        }

        int activos = services.Count(s => s.Enabled);
        bool hayGenerado = File.Exists(TyeServiceToggler.GeneratedYamlPath);
        SetStatus(
            hayGenerado
                ? $"{services.Count} servicios — {activos} activos (según {TyeServiceToggler.GeneratedFileName})."
                : $"{services.Count} servicios — todos activos (todavía no generaste {TyeServiceToggler.GeneratedFileName}).",
            Color.Gray);
    }

    /// <summary>
    /// Arma la grilla de checkboxes repartiendo los servicios en <see cref="ColumnasServicios"/>
    /// columnas. El llenado es vertical (se completa la primera columna, después la segunda), que
    /// es como se lee una lista: el orden del tye.yaml se sigue leyendo de arriba hacia abajo.
    /// </summary>
    private void RenderServices(IReadOnlyList<TyeService> services)
    {
        ServicesPanel.SuspendLayout();
        try
        {
            ServicesPanel.Controls.Clear();
            foreach (var viejo in _serviceChecks)
                viejo.Dispose();
            _serviceChecks.Clear();

            ServicesPanel.ColumnStyles.Clear();
            ServicesPanel.RowStyles.Clear();

            int filas = Math.Max(1, (services.Count + ColumnasServicios - 1) / ColumnasServicios);
            ServicesPanel.ColumnCount = ColumnasServicios;

            // Una fila extra de relleno al final: con todas las filas en Absolute, el alto sobrante
            // del panel se lo lleva la última, y el CheckBox queda centrado en una celda alta
            // (el salto que se veía entre la anteúltima y la última fila). La de relleno se come
            // ese excedente y las de contenido quedan todas del mismo alto.
            ServicesPanel.RowCount = filas + 1;

            for (int c = 0; c < ColumnasServicios; c++)
                ServicesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / ColumnasServicios));
            for (int f = 0; f < filas; f++)
                ServicesPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, AltoFilaServicio));
            ServicesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            for (int i = 0; i < services.Count; i++)
            {
                var servicio = services[i];
                bool obligatorio = EsObligatorio(servicio.Name);

                var check = new CheckBox
                {
                    Tag = servicio.Name,
                    Text = obligatorio ? $"{servicio.Name}  (siempre activo)" : servicio.Name,
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    Margin = new Padding(8, 4, 8, 4),
                    Font = new Font("Segoe UI", 10F, obligatorio ? FontStyle.Bold : FontStyle.Regular),
                    Checked = servicio.Enabled || obligatorio,
                };

                // El handler se engancha después de fijar Checked para no disparar el veto al armar la grilla.
                check.CheckedChanged += ServiceCheck_CheckedChanged;
                _tips.SetToolTip(check, obligatorio
                    ? $"'{servicio.Name}' siempre se levanta: el resto de los microservicios no arranca sin él."
                    : $"Levantar el microservicio '{servicio.Name}'.");

                _serviceChecks.Add(check);
                ServicesPanel.Controls.Add(check, i / filas, i % filas);
            }
        }
        finally
        {
            ServicesPanel.ResumeLayout();
        }
    }

    /// <summary>Veta el destildado del servicio obligatorio (clicks, "Desmarcar todos", aplicar perfil).</summary>
    private void ServiceCheck_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is not CheckBox check) return;
        if (!check.Checked && EsObligatorio(NombreDe(check))) check.Checked = true;
    }

    private static string NombreDe(CheckBox check) => (string)check.Tag!;

    private static bool EsObligatorio(string serviceName)
        => string.Equals(serviceName, ServicioObligatorio, StringComparison.OrdinalIgnoreCase);

    private void RefrescarBtn_Click(object? sender, EventArgs e) => LoadServices();

    private void MarcarTodosBtn_Click(object? sender, EventArgs e) => SetAllChecked(true);

    private void DesmarcarTodosBtn_Click(object? sender, EventArgs e) => SetAllChecked(false);

    private void SetAllChecked(bool value)
    {
        foreach (var check in _serviceChecks)
            check.Checked = value || EsObligatorio(NombreDe(check));
    }

    // --- Perfiles -----------------------------------------------------------

    private void LoadProfiles(string? selectName = null)
    {
        _suppressProfileEvent = true;
        try
        {
            ProfilesCombo.Items.Clear();
            ProfilesCombo.Items.Add(SinPerfil);
            foreach (var name in _profiles.GetProfileNames())
                ProfilesCombo.Items.Add(name);

            int idx = selectName is null ? 0 : ProfilesCombo.Items.IndexOf(selectName);
            ProfilesCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally
        {
            _suppressProfileEvent = false;
        }
    }

    private void ProfilesCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressProfileEvent) return;

        var name = CurrentProfileName();
        if (name is null) return;

        var enabled = _profiles.GetProfileServices(name);
        if (enabled is null)
        {
            SetStatus($"El perfil '{name}' ya no existe.", Color.DarkOrange);
            return;
        }

        foreach (var check in _serviceChecks)
        {
            var servicio = NombreDe(check);
            check.Checked = enabled.Contains(servicio) || EsObligatorio(servicio);
        }

        int activos = _serviceChecks.Count(c => c.Checked);
        SetStatus($"Perfil '{name}' aplicado — {activos} servicio(s) tildado(s). Tocá 'Generar y guardar' para escribir el archivo.", Color.RoyalBlue);
    }

    private void GuardarPerfilBtn_Click(object? sender, EventArgs e)
    {
        var defaultName = CurrentProfileName() ?? string.Empty;
        var name = InputDialog.Show("Nombre del perfil:", "Guardar perfil", defaultName, FindForm())?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        if (string.Equals(name, SinPerfil, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show($"'{SinPerfil}' es un nombre reservado.", "Guardar perfil",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var enabled = GetCheckedServiceNames();
        if (enabled.Count == 0)
        {
            MessageBox.Show("No hay servicios tildados. Tildá al menos uno antes de guardar el perfil.", "Guardar perfil",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _profiles.SaveProfile(name, enabled);
        LoadProfiles(name);
        SetStatus($"Perfil '{name}' guardado con {enabled.Count} servicio(s).", Color.ForestGreen);
    }

    private void EliminarPerfilBtn_Click(object? sender, EventArgs e)
    {
        var name = CurrentProfileName();
        if (name is null)
        {
            MessageBox.Show("Elegí un perfil en el combo para eliminarlo.", "Eliminar perfil",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show($"¿Eliminar el perfil '{name}'?", "Eliminar perfil",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _profiles.DeleteProfile(name);
        LoadProfiles();
        SetStatus($"Perfil '{name}' eliminado.", Color.Gray);
    }

    /// <summary>Nombre del perfil seleccionado, o null si está en "(sin perfil)".</summary>
    private string? CurrentProfileName()
    {
        if (ProfilesCombo.SelectedItem is not string name || name == SinPerfil)
            return null;
        return name;
    }

    private List<string> GetCheckedServiceNames()
        => _serviceChecks.Where(c => c.Checked).Select(NombreDe).ToList();

    private async void GenerarBtn_Click(object? sender, EventArgs e)
    {
        var selection = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var check in _serviceChecks)
            selection[NombreDe(check)] = check.Checked;

        if (selection.Count == 0 || selection.Values.All(v => !v))
        {
            MessageBox.Show("Tildá al menos un servicio antes de generar.", "Selector de Servicios (Tye)",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        SetStatus("Generando archivo...", Color.Gray);
        try
        {
            await Task.Run(() => TyeServiceToggler.GenerateSelection(selection));
            int activos = selection.Values.Count(v => v);
            SetStatus(
                $"✅ {TyeServiceToggler.GeneratedFileName} generado — {activos} servicio(s) activo(s). Corré: {TyeServiceToggler.RunCommand}",
                Color.ForestGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"Error al generar: {ex.Message}", Color.Firebrick);
            MessageBox.Show(ex.Message, "Selector de Servicios (Tye)",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CopiarComandoBtn_Click(object? sender, EventArgs e)
    {
        Clipboard.SetText(TyeServiceToggler.RunCommand);
        SetStatus($"Comando copiado: {TyeServiceToggler.RunCommand}", Color.SeaGreen);
    }

    private void SetStatus(string text, Color color)
    {
        StatusLbl.Text = text;
        StatusLbl.ForeColor = color;
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        RefrescarBtn.Enabled = !busy;
        MarcarTodosBtn.Enabled = !busy;
        DesmarcarTodosBtn.Enabled = !busy;
        GenerarBtn.Enabled = !busy;
        CopiarComandoBtn.Enabled = !busy;
        ServicesPanel.Enabled = !busy;
        ProfilesCombo.Enabled = !busy;
        GuardarPerfilBtn.Enabled = !busy;
        EliminarPerfilBtn.Enabled = !busy;
    }
}
