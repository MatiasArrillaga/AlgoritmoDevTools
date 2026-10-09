using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
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
        ClientSize = new Size(1460, 580);
        MinimumSize = new Size(1180, 560);
        StartPosition = FormStartPosition.CenterScreen;

        using (var iconStream = typeof(MainFormBlazor).Assembly.GetManifestResourceStream("app.ico"))
        {
            if (iconStream is not null) Icon = new Icon(iconStream);
        }

        var servicios = new ServiceCollection();
        servicios.AddWindowsFormsBlazorWebView();
        RegistrarServiciosDeHerramientas(servicios);
        servicios.AddSingleton<IDialogosDelSistema>(new DialogosDelSistema(this));
        servicios.AddSingleton<IArchivosArrastrados>(_arrastre);
        servicios.AddSingleton<IZonaDeArrastre>(_zonaDeArrastre);
        servicios.AddSingleton<BarraDeEstado>();
#if DEBUG
        // Habilita el clic derecho con "Inspeccionar", que es la única forma cómoda de depurar el
        // HTML. En Release queda fuera para que no aparezca en la máquina de nadie más.
        servicios.AddBlazorWebViewDeveloperTools();
#endif

        var vista = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = Path.Combine("wwwroot", "index.html"),
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
    }

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
        servicios.AddSingleton(new DocumentConverter());
    }
}
