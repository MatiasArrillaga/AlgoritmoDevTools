using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;
using AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;
using AlgoritmoDevTools.Tools.MarkdownConverter.Services;
using AlgoritmoDevTools.Tools.ModelDriftChecker.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Ventana principal con la interfaz en HTML. Es el mismo ejecutable de siempre: lo único que cambia
/// es que el contenido lo dibuja el motor de Edge —vía WebView2— en lugar de los controles de
/// Windows Forms. Los servicios, el almacenamiento y la integración con los secretos no se tocan.
///
/// Convive con <see cref="MainForm"/> mientras dure la migración: se abre con el verbo --html y así
/// se pueden comparar las dos sin tener que elegir de entrada.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public sealed class MainFormBlazor : Form
{
    private readonly ArchivosArrastrados _arrastre = new();
    private readonly ZonaDeArrastre _zonaDeArrastre;

    public MainFormBlazor()
    {
        _zonaDeArrastre = new ZonaDeArrastre(_arrastre);

        Text = "Algoritmo DevTools";

        // El alto sale de lo que necesita el Inicio, que es la pantalla mas alta: con el aviso de
        // version arriba pide 553, y la ventana suma la barra de titulo, asi que con 640 entra sin
        // scroll y sobra margen. Mas no conviene: el area util de una pantalla de 1366x768 son 728
        // y la ventana ya mide 679.
        //
        // Lo que no entre con este alto se resuelve adentro de la pantalla y no agrandando la
        // ventana: el bloque largo de cada una —la salida, la lista de casillas, el listado del
        // Disk Cleaner— se achica contra el lugar que queda y scrollea solo. Quien hace eso es la
        // regla ".contenido > .salida, ..." de AlgoritmoDevTools.UI/wwwroot/app.css, que explica
        // por que el tope no puede estar en vh.
        ClientSize = new Size(1460, 640);
        MinimumSize = new Size(1180, 600);
        StartPosition = FormStartPosition.CenterScreen;

        using (var iconStream = typeof(MainFormBlazor).Assembly.GetManifestResourceStream("app.ico"))
        {
            if (iconStream is not null) Icon = new Icon(iconStream);
        }

        var servicios = new ServiceCollection();
        servicios.AddWindowsFormsBlazorWebView();
        RegistrarServiciosDeHerramientas(servicios);
        servicios.AddSingleton<IDialogosDelSistema>(new DialogosDelSistema(this));
        servicios.AddSingleton<IReinicioDeLaAplicacion>(new ReinicioDeLaAplicacion(this));
        servicios.AddSingleton<IArchivosArrastrados>(_arrastre);
        servicios.AddSingleton<IZonaDeArrastre>(_zonaDeArrastre);
        servicios.AddSingleton<BarraDeEstado>();

        // Aparte de la barra: la barra muestra el estado de la pantalla en la que estas, y otra
        // pantalla lo pisa. Lo que termina mientras hacias otra cosa necesita algo que no se borre
        // solo por haber cambiado de herramienta.
        servicios.AddSingleton<Avisos>();
#if DEBUG
        // Habilita el clic derecho con "Inspeccionar", que es la única forma cómoda de depurar el
        // HTML. En Release queda fuera para que no aparezca en la máquina de nadie más.
        servicios.AddBlazorWebViewDeveloperTools();
#endif

        var vista = new VisorEmbebido
        {
            Dock = DockStyle.Fill,
            HostPage = PaginaDeInicio(),
            Services = servicios.BuildServiceProvider()
        };
        vista.RootComponents.Add<Raiz>("#app");

        Controls.Add(vista);

        // La franja va por encima del navegador, apoyada sobre la barra de estado. Superpuesta y no
        // acoplada a un borde: como aparece y desaparece con cada arrastre, acoplada empujaría el
        // contenido y la pantalla entera saltaría.
        Controls.Add(_zonaDeArrastre);
        _zonaDeArrastre.BringToFront();
        _zonaDeArrastre.Reposicionar(ClientSize);
        Resize += (_, _) => _zonaDeArrastre.Reposicionar(ClientSize);

        PrepararArrastre(vista);

        // La barra de título la dibuja Windows y no acompaña al tema por su cuenta: en modo oscuro
        // se queda blanca, como un marco pegado a una ventana que no es suya.
        _temas.TemaCambiado += AlCambiarElTema;
    }

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;

    /// <summary>
    /// Al crearse la ventana, antes de que se vea: pintarla después deja ver el cambio de color.
    /// </summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        BarraDeTitulo.Pintar(Handle, _temas.Actual);
    }

    private void AlCambiarElTema(object? sender, EventArgs e) => BarraDeTitulo.Pintar(Handle, _temas.Actual);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _temas.TemaCambiado -= AlCambiarElTema;
        base.Dispose(disposing);
    }

    /// <summary>
    /// La página con la que arranca el visor.
    ///
    /// No se lee del disco —la contesta <see cref="ContenidoWebEmbebido"/> desde adentro del
    /// ejecutable— pero la ruta igual tiene que ser completa: el visor la resuelve contra el
    /// directorio actual, que no tiene por qué ser el del ejecutable. El menú contextual del
    /// explorador, sin ir más lejos, arranca este mismo exe parado en la carpeta donde se hizo clic.
    /// </summary>
    private static string PaginaDeInicio() => Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

    /// <summary>
    /// Engancha el aviso de arrastre que manda la página.
    ///
    /// El navegador queda con el arrastre habilitado a propósito, aunque no sirva para recibir
    /// archivos: así es él quien detecta que hay algo encima de la ventana —el formulario no puede,
    /// porque el navegador lo tapa entero— y avisa por <c>postMessage</c>. Con ese aviso aparece la
    /// franja, que está fuera de su área y sí recibe las rutas al soltar.
    /// </summary>
    private void PrepararArrastre(BlazorWebView vista)
    {
        vista.WebView.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess) return;

            vista.WebView.CoreWebView2.WebMessageReceived += (_, mensaje) =>
            {
                var texto = mensaje.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(texto)) return;

                // "arrastre:entra:28" — el alto de la barra de estado viaja con el aviso porque
                // depende del escalado de pantalla, y es lo que decide dónde se apoya la franja.
                var partes = texto.Split(':');
                if (partes.Length < 2 || partes[0] != "arrastre") return;

                if (partes.Length >= 3 && int.TryParse(partes[2], out var altoBarra))
                {
                    _zonaDeArrastre.Reposicionar(ClientSize, altoBarra);
                }

                _zonaDeArrastre.SeguirArrastre(partes[1] == "entra");
            };
        };
    }

    /// <summary>
    /// Registra los servicios de cada herramienta, con el mismo almacenamiento que usan en la
    /// ventana clásica: la clave de <see cref="ToolStorage"/> es la que aísla los datos de cada una,
    /// así que cambiarla acá dejaría a la herramienta sin su historial.
    ///
    /// Secrets Manager comparte el almacenamiento "Shared" con el resto porque sus conexiones las
    /// consumen también el menú contextual y las acciones sin ventana.
    /// </summary>
    private static void RegistrarServiciosDeHerramientas(IServiceCollection servicios)
    {
        servicios.AddSingleton(new BaselineRepository(new ToolStorage("ModelDriftChecker")));
        servicios.AddSingleton(new DomainRepository(new ToolStorage("CommandsMaker")));
        servicios.AddSingleton(new SavedConnectionsRepository(new ToolStorage("Shared")));
        servicios.AddSingleton(new ProfileRepository(new ToolStorage("TyeServiceSelector")));
        servicios.AddSingleton(new PerfilRepository(new ToolStorage("DiskCleaner")));
        // El detector de logs necesita las conexiones guardadas, que son las de la clave "Shared".
        servicios.AddSingleton(sp => new EscaneoService(
            CatalogoDeDetectores.Todos(sp.GetRequiredService<SavedConnectionsRepository>())));
        servicios.AddSingleton<LimpiezaService>();
        servicios.AddSingleton<EjecutorDeLimpieza>();
        servicios.AddSingleton<ElevacionService>();

        // Singleton y no por pantalla: es lo que deja seguir analizando despues de cambiar de
        // herramienta, y encontrar lo analizado al volver.
        servicios.AddSingleton<SesionDeLimpieza>();
        servicios.AddSingleton(new DocumentConverter());

        // Los dos toman los repositorios de arriba: uno para armar el archivo que se exporta, el
        // otro para saber con qué datos escribir cada menú del explorador.
        servicios.AddSingleton<TransferenciaDeConfiguracion>();
        servicios.AddSingleton<MenusContextuales>();

        // El verbo lo pone el Shell, que es el que conoce su propia línea de comandos.
        servicios.AddSingleton(new ArranqueDelWidget(Program.VerboWidget));
        servicios.AddSingleton<ServicioDeActualizaciones>();
    }
}
