using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.CommandsMaker.Views;

public partial class CommandsMakerView : UserControl
{
    private readonly DomainRepository _repository;

    public CommandsMakerView(DomainRepository repository)
    {
        _repository = repository;
        InitializeComponent();
        cmbDominios.DataSource = _repository.GetAll();

        // Arranca en el último dominio con el que se generó un comando, que es el mismo que el
        // menú contextual muestra arriba de todo.
        var ultimo = _repository.GetUltimoDominio();
        if (!string.IsNullOrWhiteSpace(ultimo) && cmbDominios.Items.Contains(ultimo))
            cmbDominios.SelectedItem = ultimo;

        migrationName.Text = GeneradorDeComandos.NombreDeMigracionSugerido(cmbDominios.Text);
        SetupTooltips();
        VisibleChanged += VistaVisibleChanged;
        ActualizarEstadoDelMenu();
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 10_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(cmbDominios, "Dominio actual. Se usa para armar los comandos. Compartido con Schema Change Detector.");
        tips.SetToolTip(addDomain, "Agrega un dominio nuevo a la lista.");
        tips.SetToolTip(removeDomain, "Elimina el dominio seleccionado.");
        tips.SetToolTip(migrationName, "Nombre de la migración (editable). Se sustituye en el comando add-migration.");
        tips.SetToolTip(checkBox1, "Si está activo, antepone '[Dominio].' al nombre de la migración.");
        tips.SetToolTip(bAdd, "Copia al clipboard el comando 'add-migration' con el nombre y la connection string actual, listo para pegarlo en PM Console.");
        tips.SetToolTip(bRemove, "Copia al clipboard el comando 'remove-migration -force', listo para pegarlo en PM Console.");
        tips.SetToolTip(bUpdate, "Copia al clipboard el comando 'update-database' con la connection string del secreto Development.");
        tips.SetToolTip(MenuAgregarBtn, "Agrega DevTools > Commands Maker al clic derecho sobre el fondo de cualquier carpeta: los tres comandos del dominio actual, y el resto bajo 'Otro dominio'. Va en HKEY_CURRENT_USER: no necesita permisos de administrador.");
        tips.SetToolTip(MenuQuitarBtn, "Saca el grupo Commands Maker del clic derecho. La raíz DevTools se borra sólo si no quedó ninguna otra tool colgando.");
        tips.SetToolTip(MenuEstadoLbl, "El menú es estático: el dominio destacado queda clavado al escribirlo. Se regenera solo cuando agregás, borrás o usás un dominio.");
        tips.SetToolTip(rtbText, "Último comando generado. Ya está copiado en el clipboard.");
    }

    private void CastCommand(string command)
    {
        Clipboard.SetData(DataFormats.Text, command);
        rtbText.Text = command;

        // El dominio usado es el que el menú contextual muestra arriba de todo.
        _repository.RegistrarUso(cmbDominios.Text);
        SincronizarMenu();
    }

    // --- Menú contextual del explorador -------------------------------------

    /// <summary>
    /// Refresca el cartel cada vez que la pantalla vuelve a mostrarse. Hace falta porque las vistas
    /// quedan cacheadas en el Shell: sin esto, el cartel sigue mostrando lo que habia cuando se
    /// abrio, y el presupuesto de 16 items es COMPARTIDO, asi que lo que haga la otra tool cambia
    /// lo que esta pantalla deberia estar diciendo.
    /// </summary>
    private void VistaVisibleChanged(object? sender, EventArgs e)
    {
        if (Visible) ActualizarEstadoDelMenu();
    }

    private void MenuAgregarBtn_Click(object? sender, EventArgs e)
    {
        var error = MenuContextualComandos.TryInstalar(_repository.GetAll(), _repository.GetRecientes());
        if (error is not null)
        {
            MessageBox.Show($"No se pudo agregar al menú: {error}",
                "Commands Maker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        ActualizarEstadoDelMenu();
    }

    private void MenuQuitarBtn_Click(object? sender, EventArgs e)
    {
        var error = MenuContextualComandos.TryDesinstalar();
        if (error is not null)
        {
            MessageBox.Show($"No se pudo quitar del menú: {error}",
                "Commands Maker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        ActualizarEstadoDelMenu();
    }

    /// <summary>
    /// Reescribe el menú si está instalado y refresca el cartel. Hace falta ante cualquier cambio
    /// en los dominios o en cuál es el último usado: el menú es estático, así que un dominio
    /// borrado seguiría apareciendo y el acceso directo de arriba apuntaría al anterior.
    /// </summary>
    private void SincronizarMenu()
    {
        MenuContextualComandos.Sincronizar(_repository.GetAll(), _repository.GetRecientes());
        ActualizarEstadoDelMenu();
    }

    private void ActualizarEstadoDelMenu()
    {
        var instalado = MenuContextualComandos.EstaInstalado();
        MenuAgregarBtn.Text = instalado ? "Regenerar menú" : "Agregar al menú";
        MenuQuitarBtn.Enabled = instalado;

        if (!instalado)
        {
            MenuEstadoLbl.Text = "Menú contextual: no instalado.";
            MenuEstadoLbl.ForeColor = Color.Gray;
            return;
        }

        // Se muestra el presupuesto porque es la unica pista de por que faltan opciones: pasarse
        // de 16 no da error, el explorador descarta el resto en silencio.
        var usados = MenuContextualDevTools.ContarItems();
        var destacado = _repository.GetUltimoDominio();
        var cual = string.IsNullOrWhiteSpace(destacado) ? string.Empty : $" — destaca {destacado}";

        MenuEstadoLbl.Text = $"Menú contextual: {usados}/{MenuContextualDevTools.LimiteDeItems} ítems{cual}.";
        MenuEstadoLbl.ForeColor = usados >= MenuContextualDevTools.LimiteDeItems ? Color.Firebrick : Color.ForestGreen;
    }

    private void bAdd_Click(object? sender, EventArgs e)
        => CastCommand(GeneradorDeComandos.AddMigration(cmbDominios.Text, migrationName.Text));

    private void bRemove_Click(object? sender, EventArgs e)
        => CastCommand(GeneradorDeComandos.RemoveMigration(cmbDominios.Text));

    private void bUpdate_Click(object? sender, EventArgs e)
        => CastCommand(GeneradorDeComandos.UpdateDatabase(cmbDominios.Text));

    private void addDomain_Click(object? sender, EventArgs e)
    {
        var input = InputDialog.Show("Ingrese el nombre de un dominio:", "Add Domain", owner: this.FindForm());
        if (!string.IsNullOrEmpty(input))
        {
            _repository.Add(input);
            cmbDominios.DataSource = _repository.GetAll();
            SincronizarMenu();
        }
    }

    private void removeDomain_Click(object? sender, EventArgs e)
    {
        var dominio = cmbDominios.Text;
        if (string.IsNullOrWhiteSpace(dominio))
        {
            MessageBox.Show("Seleccioná un dominio para eliminar.",
                "Commands Maker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show($"¿Eliminar dominio '{dominio}'?",
            "Commands Maker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _repository.Remove(dominio);
        cmbDominios.DataSource = _repository.GetAll();
        SincronizarMenu();
    }

    private void cmbDominios_SelectedIndexChanged(object? sender, EventArgs e)
        => migrationName.Text = ChangeMigrationName(checkBox1.Checked, migrationName.Text);

    private void checkBox1_CheckedChanged(object? sender, EventArgs e)
        => migrationName.Text = ChangeMigrationName(checkBox1.Checked, migrationName.Text);

    private string ChangeMigrationName(bool addDomines, string mName)
    {
        var domainDescription = addDomines ? $"[{cmbDominios.Text}]." : string.Empty;

        return mName.Contains('[', StringComparison.CurrentCulture)
            ? mName.Replace(mName.Substring(mName.IndexOf('['), mName.IndexOf('.') + 1), domainDescription)
            : domainDescription + mName;
    }

    private void cmbDominios_KeyDown(object? sender, KeyEventArgs e)
    {
        var dominio = cmbDominios.Text;
        switch (e.KeyCode)
        {
            case Keys.Delete:
                _repository.Remove(dominio);
                cmbDominios.DataSource = _repository.GetAll();
                MessageBox.Show($"Dominio '{dominio}' eliminado");
                break;

            case Keys.Return:
                if (!string.IsNullOrEmpty(dominio))
                {
                    _repository.Add(dominio);
                    cmbDominios.DataSource = _repository.GetAll();
                    MessageBox.Show($"Dominio '{dominio}' agregado");
                }
                break;
        }
    }
}
