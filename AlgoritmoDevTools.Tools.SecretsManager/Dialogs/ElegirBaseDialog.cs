using AlgoritmoDevTools.Integrations.SoftCerealCore;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.SecretsManager.Dialogs;

/// <summary>
/// Elige conexión y base para apuntar los secretos, consultando las bases en vivo contra el
/// servidor.
///
/// Existe por el menú contextual: ese menú es estático y sólo puede ofrecer la última base usada de
/// cada conexión (ver <see cref="Services.MenuContextualSecretos"/>). Este diálogo es la salida para
/// cambiar a cualquier otra base sin abrir el Shell entero.
/// </summary>
public partial class ElegirBaseDialog : Form
{
    private readonly SavedConnectionsRepository _savedConnections;

    /// <summary>Conexión elegida. Sólo tiene valor cuando el diálogo devuelve OK.</summary>
    public SavedConnection? Conexion { get; private set; }

    /// <summary>Base elegida. Sólo tiene valor cuando el diálogo devuelve OK.</summary>
    public string Base { get; private set; } = string.Empty;

    public ElegirBaseDialog(SavedConnectionsRepository savedConnections)
    {
        _savedConnections = savedConnections;
        InitializeComponent();
    }

    private void ElegirBaseDialog_Load(object? sender, EventArgs e)
    {
        var conexiones = _savedConnections.GetAll();
        if (conexiones.Count == 0)
        {
            SetEstado("No hay conexiones guardadas. Creá una desde el Secrets Manager.", Color.Firebrick);
            ConexionesCmb.Enabled = false;
            BasesCmb.Enabled = false;
            AplicarBtn.Enabled = false;
            return;
        }

        ConexionesCmb.DataSource = conexiones;
    }

    private async void ConexionesCmb_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (ConexionesCmb.SelectedItem is not SavedConnection conexion) return;

        BasesCmb.Enabled = false;
        AplicarBtn.Enabled = false;
        BasesCmb.DataSource = new List<string> { "Cargando..." };
        SetEstado($"Consultando las bases de {conexion.Server}...", Color.Gray);

        var resultado = await Task.Run(() => SQLService.TryGetDatabases(
            new SQLService.ConnectionData(conexion.Server, conexion.User, conexion.Password, conexion.UseIntegratedSecurity)));

        if (!resultado.IsSuccess || resultado.Databases.Count == 0)
        {
            // Sin conexión al servidor queda la última base conocida, que es mejor que nada:
            // permite reapuntar los secretos aunque el server esté caído en este momento.
            var ultima = conexion.BaseEfectiva;
            BasesCmb.DataSource = string.IsNullOrWhiteSpace(ultima) ? new List<string>() : new List<string> { ultima };
            AplicarBtn.Enabled = !string.IsNullOrWhiteSpace(ultima);
            SetEstado(
                resultado.IsSuccess
                    ? "El servidor no devolvió bases de datos."
                    : $"No se pudo listar las bases: {resultado.Error}",
                Color.DarkOrange);
            return;
        }

        BasesCmb.DataSource = resultado.Databases.ToList();
        if (resultado.Databases.Contains(conexion.BaseEfectiva))
            BasesCmb.SelectedItem = conexion.BaseEfectiva;

        BasesCmb.Enabled = true;
        AplicarBtn.Enabled = true;
        SetEstado($"{resultado.Databases.Count} base(s) disponibles.", Color.Gray);
    }

    private void AplicarBtn_Click(object? sender, EventArgs e)
    {
        if (ConexionesCmb.SelectedItem is not SavedConnection conexion) return;
        if (BasesCmb.SelectedItem is not string baseElegida || string.IsNullOrWhiteSpace(baseElegida)) return;

        Conexion = conexion;
        Base = baseElegida;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetEstado(string texto, Color color)
    {
        EstadoLbl.Text = texto;
        EstadoLbl.ForeColor = color;
    }
}
