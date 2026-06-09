using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Views;

public partial class TyeServiceSelectorView : UserControl
{
    private const string SinPerfil = "(sin perfil)";

    private readonly ProfileRepository _profiles;
    private bool _suppressProfileEvent;

    public TyeServiceSelectorView(ProfileRepository profiles)
    {
        _profiles = profiles;
        InitializeComponent();
        SetupTooltips();
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 12_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(RefrescarBtn, "Vuelve a leer el tye.yaml y refleja qué servicios están activos según el archivo generado.");
        tips.SetToolTip(MarcarTodosBtn, "Tilda todos los servicios.");
        tips.SetToolTip(DesmarcarTodosBtn, "Destilda todos los servicios.");
        tips.SetToolTip(ServicesList, "Tildá los microservicios que querés levantar. El master tye.yaml no se modifica.");
        tips.SetToolTip(ProfilesCombo, "Elegí un perfil guardado para tildar automáticamente sus servicios.");
        tips.SetToolTip(GuardarPerfilBtn, "Guarda la selección actual como un perfil con nombre (o sobrescribe el existente).");
        tips.SetToolTip(EliminarPerfilBtn, "Elimina el perfil seleccionado.");
        tips.SetToolTip(GenerarBtn, $"Genera {TyeServiceToggler.GeneratedFileName} en la raíz de AlgoritmoCore con los servicios tildados activos y el resto comentados.");
        tips.SetToolTip(CopiarComandoBtn, $"Copia al portapapeles: {TyeServiceToggler.RunCommand}");
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
        ServicesList.Items.Clear();

        if (!File.Exists(TyeServiceToggler.MasterYamlPath))
        {
            SetStatus($"No se encontró el master en {TyeServiceToggler.MasterYamlPath}.", Color.Firebrick);
            return;
        }

        var services = TyeServiceToggler.ReadServices();
        if (services.Count == 0)
        {
            SetStatus("No se encontraron servicios en la lista 'services:' del tye.yaml.", Color.DarkOrange);
            return;
        }

        foreach (var s in services)
            ServicesList.Items.Add(s.Name, s.Enabled);

        int activos = services.Count(s => s.Enabled);
        bool hayGenerado = File.Exists(TyeServiceToggler.GeneratedYamlPath);
        SetStatus(
            hayGenerado
                ? $"{services.Count} servicios — {activos} activos (según {TyeServiceToggler.GeneratedFileName})."
                : $"{services.Count} servicios — todos activos (todavía no generaste {TyeServiceToggler.GeneratedFileName}).",
            Color.Gray);
    }

    private void RefrescarBtn_Click(object? sender, EventArgs e) => LoadServices();

    private void MarcarTodosBtn_Click(object? sender, EventArgs e) => SetAllChecked(true);

    private void DesmarcarTodosBtn_Click(object? sender, EventArgs e) => SetAllChecked(false);

    private void SetAllChecked(bool value)
    {
        for (int i = 0; i < ServicesList.Items.Count; i++)
            ServicesList.SetItemChecked(i, value);
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

        for (int i = 0; i < ServicesList.Items.Count; i++)
            ServicesList.SetItemChecked(i, enabled.Contains((string)ServicesList.Items[i]));

        int activos = ServicesList.CheckedItems.Count;
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
    {
        var names = new List<string>();
        foreach (var item in ServicesList.CheckedItems)
            names.Add((string)item);
        return names;
    }

    private async void GenerarBtn_Click(object? sender, EventArgs e)
    {
        var selection = new Dictionary<string, bool>(StringComparer.Ordinal);
        for (int i = 0; i < ServicesList.Items.Count; i++)
            selection[(string)ServicesList.Items[i]] = ServicesList.GetItemChecked(i);

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
        ServicesList.Enabled = !busy;
        ProfilesCombo.Enabled = !busy;
        GuardarPerfilBtn.Enabled = !busy;
        EliminarPerfilBtn.Enabled = !busy;
    }
}
