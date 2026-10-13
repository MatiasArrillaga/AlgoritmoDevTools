using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using Microsoft.Win32;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using AlgoritmoDevTools.UI.Servicios;
using System.Diagnostics;
using System.Drawing;

namespace AlgoritmoDevTools.Shell.Widget;

/// <summary>
/// Widget de bandeja: el mismo exe corriendo con <c>--widget</c>, sin ventana principal.
///
/// Muestra contra qué server y base está apuntando el proyecto, y deja a mano restaurar y
/// reapuntar secretos y copiar comandos de migración. Es el mismo trabajo que hace el menú
/// contextual del explorador, pero acá <b>no hay límite de 16 ítems</b>: el menú lo dibuja
/// WinForms y no el registro, así que puede listar los 13 dominios con sus tres comandos y todas
/// las conexiones guardadas.
///
/// <para>
/// <b>Ojo al desarrollar:</b> si el widget corre desde <c>bin\Debug</c>, mantiene los DLL tomados
/// y cualquier <c>dotnet build</c> de la solución falla con MSB3027. Conviene correrlo desde el
/// publish (<c>dotnet publish -c Release</c>, que genera un exe único) y no desde la carpeta de
/// compilación. Por eso el menú tiene "Salir" bien a mano.
/// </para>
///
/// El estado se relee al abrir el menú y después de cada acción, nunca por polling:
/// <see cref="SecretService.RefreshSecrets"/> levanta PowerShell y tarda uno o dos segundos.
/// </summary>
internal sealed class WidgetContext : ApplicationContext
{
    private const string TITULO = "Algoritmo DevTools";

    private readonly NotifyIcon _icono;
    private readonly ArranqueDelWidget _arranque = new(Program.VerboWidget);
    private readonly SecretService _secretos = SecretService.Shared;
    private readonly SavedConnectionsRepository _conexiones = new(new ToolStorage("Shared"));
    private readonly DomainRepository _dominios = new(new ToolStorage("CommandsMaker"));

    /// <summary>
    /// Dominio sobre el que trabajan los tres comandos. Es un campo y no una variable capturada
    /// porque elegir otro no rearma el menú: lo cambia en vivo, con el menú abierto.
    /// </summary>
    private string _dominioActivo = string.Empty;

    /// <summary>Ítems que hay que retocar cuando cambia el dominio sin cerrar el menú.</summary>
    private ToolStripMenuItem? _itemDominio;
    private readonly List<(ToolStripItem Item, string Formato)> _itemsDeComando = new();

    /// <summary>
    /// Se enciende al tocar un dominio y se apaga en cuanto termina de procesarse el clic. Mientras
    /// está encendido, los <c>Closing</c> de los tres niveles de menú cancelan el cierre.
    /// </summary>
    private bool _manteniendoAbierto;

    public WidgetContext()
    {
        _icono = new NotifyIcon
        {
            Icon = IconoDeLaBandeja(),
            Text = TITULO,
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };

        // El menú se arma en Opening y no una sola vez: las conexiones, los dominios y el secreto
        // activo cambian por fuera del widget (desde el Shell o desde el menú del explorador).
        _icono.ContextMenuStrip.Opening += (_, _) => ArmarMenu();
        _icono.ContextMenuStrip.Closing += CancelarCierreAlElegirDominio;
        _icono.DoubleClick += (_, _) => AbrirShell();
        SystemEvents.UserPreferenceChanged += AlCambiarElTemaDeWindows;

        RefrescarEstado();
    }

    // --- Menú ---------------------------------------------------------------

    private void ArmarMenu()
    {
        var menu = _icono.ContextMenuStrip!;
        menu.Items.Clear();

        menu.Items.Add(new ToolStripMenuItem(TextoDeConexionActual()) { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(ItemSecretos());
        menu.Items.Add(ItemComandos());
        menu.Items.Add(new ToolStripSeparator());

        var arranque = new ToolStripMenuItem("Iniciar con Windows")
        {
            Checked = _arranque.EstaActivado(),
            CheckOnClick = true,
        };
        arranque.Click += (_, _) => AlternarArranque(arranque.Checked);
        menu.Items.Add(arranque);

        menu.Items.Add("Abrir DevTools", null, (_, _) => AbrirShell());
        menu.Items.Add("Salir", null, (_, _) => Salir());

        // Al final de todo: hay que medir los ítems ya cargados, y el tema puede haber cambiado
        // desde la ventana desde la última vez que se abrió el menú.
        MenuDelTema.Aplicar(menu);
    }

    private ToolStripMenuItem ItemSecretos()
    {
        var raiz = new ToolStripMenuItem("Secretos");
        raiz.DropDownItems.Add("Restaurar secretos", null, (_, _) => EjecutarYRefrescar(SecretosSinVentana.Restaurar));

        var conexiones = _conexiones.GetAll().Where(c => !string.IsNullOrWhiteSpace(c.BaseEfectiva)).ToList();
        if (conexiones.Count > 0)
        {
            raiz.DropDownItems.Add(new ToolStripSeparator());
            foreach (var conexion in conexiones)
            {
                var id = conexion.Id;
                raiz.DropDownItems.Add(
                    MenuContextualSecretosTexto(conexion), null,
                    (_, _) => EjecutarYRefrescar(() => SecretosSinVentana.Aplicar(id)));
            }
        }

        raiz.DropDownItems.Add(new ToolStripSeparator());
        raiz.DropDownItems.Add("Elegir base...", null, (_, _) => EjecutarYRefrescar(SecretosSinVentana.Elegir));
        return raiz;
    }

    /// <summary>
    /// Mismo flujo que el menú contextual del explorador: se elige el dominio una vez y después
    /// los tres comandos quedan a un clic. La diferencia es que acá el dominio se elige en el mismo
    /// menú y no por diálogo — el límite de 16 ítems no rige, así que los 13 entran como submenú.
    /// </summary>
    private ToolStripMenuItem ItemComandos()
    {
        var raiz = new ToolStripMenuItem("Comandos de migración");
        var dominios = _dominios.GetAll();

        if (dominios.Count == 0)
        {
            raiz.DropDownItems.Add(new ToolStripMenuItem("(no hay dominios cargados)") { Enabled = false });
            return raiz;
        }

        _dominioActivo = _dominios.GetUltimoDominio() is string ultimo && dominios.Contains(ultimo)
            ? ultimo
            : dominios[0];

        var elegir = new ToolStripMenuItem($"Dominio: {_dominioActivo}");
        foreach (var dominio in dominios)
        {
            var d = dominio;
            elegir.DropDownItems.Add(new ToolStripMenuItem(d, null, (_, _) => CambiarDominio(d))
            {
                // La tilde evita tener que leer el encabezado para saber cuál está puesto.
                Checked = string.Equals(d, _dominioActivo, StringComparison.OrdinalIgnoreCase),
            });
        }

        // Elegir dominio no cierra el menú: la idea es elegir y después tocar el comando, sin
        // tener que volver a abrir todo.
        elegir.DropDown.Closing += CancelarCierreAlElegirDominio;
        elegir.DropDown.ItemClicked += (_, _) => MantenerAbiertoEsteClic();

        raiz.DropDown.Closing += CancelarCierreAlElegirDominio;
        _itemDominio = elegir;

        raiz.DropDownItems.Add(elegir);
        raiz.DropDownItems.Add(new ToolStripSeparator());

        // Los handlers leen _dominioActivo y no una variable capturada, porque el dominio puede
        // cambiar con este mismo menú abierto.
        _itemsDeComando.Clear();
        AgregarComando(raiz, "Add-Migration ({0})", () => ComandosSinVentana.Add(_dominioActivo));
        AgregarComando(raiz, "Remove-Migration ({0})", () => ComandosSinVentana.Remove(_dominioActivo));
        AgregarComando(raiz, "Update-Database ({0})", () => ComandosSinVentana.Update(_dominioActivo));

        return raiz;
    }

    private void AgregarComando(ToolStripMenuItem raiz, string formato, Action accion)
    {
        var item = raiz.DropDownItems.Add(string.Format(formato, _dominioActivo), null, (_, _) => EjecutarYRefrescar(accion));
        _itemsDeComando.Add((item, formato));
    }

    /// <summary>
    /// Deja el dominio como activo sin copiar ningún comando, igual que "Elegir dominio..." en el
    /// menú del explorador. Se resincroniza ese menú para que los dos muestren el mismo dominio.
    ///
    /// Como el menú queda abierto, los textos se actualizan en el momento en vez de rearmarlo:
    /// tocar <c>Items</c> de un menú que se está mostrando es pedir problemas.
    /// </summary>
    private void CambiarDominio(string dominio)
    {
        _dominioActivo = dominio;
        _dominios.RegistrarUso(dominio);
        MenuContextualComandos.Sincronizar(_dominios.GetAll(), _dominios.GetRecientes());

        if (_itemDominio is not null)
        {
            _itemDominio.Text = $"Dominio: {dominio}";
            foreach (var item in _itemDominio.DropDownItems.OfType<ToolStripMenuItem>())
                item.Checked = string.Equals(item.Text, dominio, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var (item, formato) in _itemsDeComando)
            item.Text = string.Format(formato, dominio);
    }

    /// <summary>
    /// Evita que elegir un dominio cierre el menú. El cierre se cancela en los tres niveles —el
    /// menú del icono, el de comandos y el de dominios— porque WinForms los cierra en cascada:
    /// cancelar sólo en el de dominios no alcanza, el padre se cierra igual y se lo lleva puesto.
    ///
    /// La bandera distingue este caso del clic en un comando, que sí tiene que cerrar: para cuando
    /// llega <c>Closing</c>, el evento no dice qué ítem se tocó.
    /// </summary>
    private void CancelarCierreAlElegirDominio(object? sender, ToolStripDropDownClosingEventArgs e)
    {
        if (_manteniendoAbierto && e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            e.Cancel = true;
    }

    /// <summary>
    /// Enciende la bandera y la apaga apenas se vacía la cola de mensajes, o sea después de los
    /// <c>Closing</c> de este clic. Así el próximo clic —que puede ser en un comando— vuelve a
    /// cerrar el menú como corresponde.
    /// </summary>
    private void MantenerAbiertoEsteClic()
    {
        _manteniendoAbierto = true;
        _icono.ContextMenuStrip?.BeginInvoke(new Action(() => _manteniendoAbierto = false));
    }

    private static string MenuContextualSecretosTexto(SavedConnection conexion)
        => Tools.SecretsManager.Services.MenuContextualSecretos.TextoDeConexion(conexion);

    // --- Estado -------------------------------------------------------------

    /// <summary>Lo que muestra el encabezado del menú y el tooltip del icono.</summary>
    private string TextoDeConexionActual()
    {
        var cs = _secretos.GetConnectionString(Constantes.SecretKeys.Development);
        if (cs is null) return "Secreto Development no configurado";

        var partes = ConnectionStringParser.Parse(cs);
        return $"Server: {partes.GetValueOrDefault("Server", "-")}   —   Base: {partes.GetValueOrDefault("Database", "-")}";
    }

    /// <summary>
    /// Relee los secretos en segundo plano y actualiza el tooltip. Va fuera del hilo de UI porque
    /// levanta PowerShell: hacerlo al abrir el menú lo dejaría colgado uno o dos segundos.
    /// </summary>
    private void RefrescarEstado()
    {
        Task.Run(() =>
        {
            try
            {
                _secretos.RefreshSecrets();
            }
            catch
            {
                // Sin secretos el widget sigue sirviendo: el encabezado lo va a decir.
            }
        }).ContinueWith(_ => ActualizarTooltip(), TaskScheduler.Default);
    }

    private void ActualizarTooltip()
    {
        // El tooltip de NotifyIcon se trunca a 63 caracteres: más largo que eso, Windows lo corta.
        var texto = $"{TITULO}\n{TextoDeConexionActual()}";
        _icono.Text = texto.Length <= 63 ? texto : texto[..63];
    }

    private void EjecutarYRefrescar(Action accion)
    {
        accion();
        RefrescarEstado();
    }

    // --- Acciones del propio widget -----------------------------------------

    private void AlternarArranque(bool activar)
    {
        var error = activar ? _arranque.Activar() : _arranque.Desactivar();

        if (error is not null)
        {
            MessageBox.Show($"No se pudo cambiar el inicio con Windows:\n\n{error}",
                TITULO, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AbrirShell()
    {
        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return;

        // Sin argumentos abre la ventana normal; el widget sigue corriendo aparte.
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
    }

    private void Salir()
    {
        _icono.Visible = false;
        ExitThread();
    }

    /// <summary>
    /// El ícono de la bandeja, en la versión que contrasta con la barra de tareas.
    ///
    /// Es el único lugar donde manda el tema de Windows y no el de DevTools: el ícono vive en SU
    /// barra, al lado del reloj, así que lo que tiene que contrastar es ese fondo. El de la
    /// aplicación a color no sirve acá —a 16 píxeles un engranaje con degradado es una mancha—, y
    /// por eso hay dos versiones de un solo trazo.
    /// </summary>
    private static Icon IconoDeLaBandeja()
    {
        var nombre = TemaDeWindows.BarraDeTareasOscura() ? "bandeja-oscura.ico" : "bandeja-clara.ico";

        using var stream = typeof(WidgetContext).Assembly.GetManifestResourceStream(nombre);
        return stream is not null ? new Icon(stream) : SystemIcons.Application;
    }

    /// <summary>
    /// Windows avisa cuando le cambian el tema. Sin esto el ícono se queda del color anterior hasta
    /// que alguien reinicie el widget, y sobre la barra contraria eso es un ícono que no se ve.
    /// </summary>
    private void AlCambiarElTemaDeWindows(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color)) return;

        var anterior = _icono.Icon;
        _icono.Icon = IconoDeLaBandeja();
        anterior?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= AlCambiarElTemaDeWindows;
            _icono.Dispose();
        }

        base.Dispose(disposing);
    }
}
