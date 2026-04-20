using AlgoritmoDevTools.Integrations.SoftCerealCore;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.SecretsManager.Dialogs;

public partial class ConnectionEditorDialog : Form
{
    private readonly SavedConnectionsRepository _savedConnections;
    private readonly SavedConnection? _existing;

    public SavedConnection Result { get; private set; } = new();

    public ConnectionEditorDialog(SavedConnectionsRepository savedConnections, SavedConnection? existing = null)
    {
        _savedConnections = savedConnections;
        _existing = existing;
        InitializeComponent();
        SetupTooltips();

        if (existing is not null)
        {
            Text = "Editar conexión";
            OkBtn.Text = "Guardar";
            IntegratedSecurityChk.Checked = existing.UseIntegratedSecurity;
            ServerTxt.Text = existing.Server;
            UserTxt.Text = existing.User;
            PasswordTxt.Text = existing.Password;
            if (existing.UseIntegratedSecurity)
            {
                UserLbl.Enabled = false;
                UserTxt.Enabled = false;
                PasswordLbl.Enabled = false;
                PasswordTxt.Enabled = false;
            }
        }
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 12_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(IntegratedSecurityChk, "Si está activo, usa tu usuario de Windows para conectarse (evita problemas de permisos a nivel SQL).");
        tips.SetToolTip(ServerTxt, "Nombre del server SQL. Ej: localhost,1433 o 10.1.85.92,1433.");
        tips.SetToolTip(UserTxt, "SQL Login. Deshabilitado cuando está activa la autenticación Windows.");
        tips.SetToolTip(PasswordTxt, "Contraseña del SQL Login. Se guarda en texto plano en %LOCALAPPDATA%/AlgoritmoDevTools/Shared/data.db.");
        tips.SetToolTip(TestBtn, "Abre una conexión de prueba con las credenciales ingresadas. No requiere una base específica.");
        tips.SetToolTip(OkBtn, "Guarda la conexión. Re-ejecuta automáticamente el test; si falla, no guarda.");
        tips.SetToolTip(CancelBtn2, "Descarta los cambios y cierra.");
    }

    private void IntegratedSecurityChk_CheckedChanged(object? sender, EventArgs e)
    {
        var integrated = IntegratedSecurityChk.Checked;
        UserLbl.Enabled = !integrated;
        UserTxt.Enabled = !integrated;
        PasswordLbl.Enabled = !integrated;
        PasswordTxt.Enabled = !integrated;

        if (integrated)
        {
            UserTxt.Text = string.Empty;
            PasswordTxt.Text = string.Empty;
        }

        ResetStatus();
    }

    private void CredentialChanged(object? sender, EventArgs e) => ResetStatus();

    private void ResetStatus()
    {
        StatusLbl.Text = string.Empty;
        StatusLbl.ForeColor = Color.Gray;
    }

    private async void TestBtn_Click(object? sender, EventArgs e)
    {
        await RunTestAsync();
    }

    /// <summary>
    /// Ejecuta el test y devuelve true si la conexión se estableció correctamente.
    /// </summary>
    private async Task<bool> RunTestAsync()
    {
        if (!AllCredentialsFilled())
        {
            SetStatus(IntegratedSecurityChk.Checked
                ? "Completá el Server."
                : "Completá Server, User Id y Password.",
                Color.Firebrick);
            return false;
        }

        SetStatus("Probando conexión...", Color.Gray);
        TestBtn.Enabled = false;
        OkBtn.Enabled = false;
        try
        {
            var args = BuildConnectionData();
            var error = await Task.Run(() => SQLService.TryTestConnection(args));
            if (error is null)
            {
                SetStatus("✓ Conexión establecida correctamente.", Color.ForestGreen);
                return true;
            }
            SetStatus($"✗ {error}", Color.Firebrick);
            return false;
        }
        finally
        {
            TestBtn.Enabled = true;
            OkBtn.Enabled = true;
        }
    }

    private bool AllCredentialsFilled()
    {
        if (string.IsNullOrWhiteSpace(ServerTxt.Text)) return false;
        if (IntegratedSecurityChk.Checked) return true;
        return !string.IsNullOrWhiteSpace(UserTxt.Text)
            && !string.IsNullOrWhiteSpace(PasswordTxt.Text);
    }

    private SQLService.ConnectionData BuildConnectionData()
        => new(
            server: ServerTxt.Text,
            user: UserTxt.Text,
            password: PasswordTxt.Text,
            useIntegratedSecurity: IntegratedSecurityChk.Checked);

    private void SetStatus(string text, Color color)
    {
        StatusLbl.Text = text;
        StatusLbl.ForeColor = color;
    }

    private async void OkBtn_Click(object? sender, EventArgs e)
    {
        if (!AllCredentialsFilled())
        {
            MessageBox.Show("Completá todos los campos antes de aceptar.",
                "Nueva conexión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        // Siempre re-valida la conexión antes de guardar.
        DialogResult = DialogResult.None;
        var ok = await RunTestAsync();
        if (!ok) return;

        var integrated = IntegratedSecurityChk.Checked;
        var user = integrated ? string.Empty : UserTxt.Text;

        // Al editar, el DataBase lo preservamos del registro original (el usuario lo modifica en la vista principal).
        var dataBase = _existing?.DataBase ?? string.Empty;

        if (_savedConnections.Exists(ServerTxt.Text, dataBase, user, excludeId: _existing?.Id))
        {
            MessageBox.Show("Ya existe una conexión guardada con esos datos.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = new SavedConnection
        {
            Id = _existing?.Id ?? 0,
            Server = ServerTxt.Text,
            DataBase = dataBase,
            User = user,
            Password = integrated ? string.Empty : PasswordTxt.Text,
            UseIntegratedSecurity = integrated
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
