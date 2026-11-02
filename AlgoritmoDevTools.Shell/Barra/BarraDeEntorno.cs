using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Paginas;
using AlgoritmoDevTools.UI.Servicios;

using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace AlgoritmoDevTools.Shell.Barra;

/// <summary>
/// La ventana de la barra del entorno local: una franja chica, siempre encima, que dice qué está
/// corriendo y deja frenarlo, reiniciarlo o cambiarle la base sin ir a la consola.
///
/// <para>
/// <b>Adentro es HTML, como el resto de la aplicación.</b> El contenido es un componente de Blazor
/// con el mismo <c>app.css</c> y las mismas variables de tema, así que se ve como todo lo demás sin
/// repintar nada a mano. Esta clase es sólo la ventana: el marco que no tiene, dónde se ubica, cómo
/// se arrastra y cómo se esconde.
/// </para>
///
/// <para>
/// <b>Por qué sin marco.</b> El documento de temas desaconseja sacarlo porque se pierden el
/// redimensionado, el acople y el menú del ícono. Eso vale para una ventana de trabajo; ésta no se
/// maximiza ni cambia de alto a mano, y lo que hay que reponer —arrastrar y estirar— son unas pocas
/// líneas.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public sealed class BarraDeEntorno : Form, IVentanaDeLaBarra
{
    private const int AnchoPorDefecto = 920;
    private const int AnchoMinimo = 620;
    private const int AnchoMaximo = 1400;
    private const int Radio = 12;

    /// <summary>
    /// Franja del borde derecho que queda fuera del visor, para que el mouse la agarre y Windows
    /// pueda estirar la ventana. Si el visor llegara hasta el borde, se quedaría con el mouse.
    /// </summary>
    private const int BordeParaEstirar = 6;

    /// <summary>
    /// El ancho de la pestaña que queda cuando la barra está colapsada. Sale de las medidas del
    /// icono: los 14 px de margen lateral que usa la barra a cada lado, más los 22 de la caja del
    /// icono. Con menos, el visor lo encogía para hacerlo entrar y la pestaña mostraba un icono más
    /// chico que el de la barra, justo lo que no tiene que pasar.
    ///
    /// <para>
    /// <b>El alto no cambia</b>: la pestaña queda tan alta como la barra. Achicarla también de alto
    /// hacía un salto raro a la vista; conservándolo, lo único que se percibe es que la barra se
    /// encoge hacia la izquierda y el icono cambia de color.
    /// </para>
    /// </summary>
    private const int AnchoDeLaPestania = 50;

    /// <summary>
    /// El ancho de la pestaña en píxeles de pantalla.
    ///
    /// <para>
    /// <b>La constante está en las unidades del HTML</b>, que es donde se definen el margen y el
    /// tamaño del icono, y la ventana se mide en píxeles reales. Sin convertir, en una pantalla al
    /// 125% la pestaña queda más angosta que su propio contenido: el icono se come el margen de la
    /// derecha y queda descentrado. El alto no necesita esto porque sale de <c>Size</c>, que ya
    /// viene en píxeles reales.
    /// </para>
    /// </summary>
    private int AnchoDeLaPestaniaEnPantalla => (int)Math.Round(AnchoDeLaPestania * DeviceDpi / 96.0);

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private readonly System.Windows.Forms.Timer _vigilante = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer _siempreEncima = new() { Interval = 3000 };
    private readonly VisorEmbebido _visor;

    private bool _colapsada;

    /// <summary>
    /// Desplegada por un clic en la pestaña y no clavada: se contrae sola en cuanto el mouse se va.
    /// Es el auto-ocultamiento de los paneles de Windows, y por eso el pin muestra si está clavada.
    /// </summary>
    private bool _temporal;

    /// <summary>
    /// Dónde está la pestaña, que siempre vive pegada a un lateral de la pantalla. La barra se
    /// despliega desde ahí hacia adentro, así que el borde que toca la pantalla es el mismo en los
    /// dos estados.
    /// </summary>
    private Point _pestania;

    /// <summary>Contra qué borde de la pantalla se apoya la pestaña.</summary>
    private enum Lado { Izquierda, Derecha, Arriba }

    /// <summary>
    /// Contra qué borde está apoyada, o null si no se apoya en ninguno. Define hacia dónde se
    /// despliega la barra y qué esquinas van rectas: las que dan contra la pantalla no se
    /// redondean.
    ///
    /// <para>
    /// <b>Null mientras flota.</b> Recién abierta, la barra está donde la dejaron la última vez y
    /// no se apoya en ningún borde: se dibuja con las de abajo redondeadas y las de arriba rectas,
    /// igual que apoyada arriba. Sin el nullable arrancaba con el valor por defecto del enum
    /// —izquierda— y se dibujaba con los dos vértices de ese lado rectos, apoyada contra nada.
    /// </para>
    /// </summary>
    private Lado? _lado;

    private Size _tamanoDesplegado;

    public BarraDeEntorno()
    {
        Text = "Entorno local";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(AnchoGuardado(), BarraPagina.AltoDeLaBarra);
        MinimumSize = new Size(AnchoMinimo, BarraPagina.AltoDeLaBarra);
        MaximumSize = new Size(AnchoMaximo, 600);
        BackColor = _temas.Actual.FondoPrincipal;

        // El visor deja libre la franja derecha: es lo único que el hit-test de la ventana puede ver.
        Padding = new Padding(0, 0, BordeParaEstirar, 0);

        var servicios = new ServiceCollection();
        servicios.AddWindowsFormsBlazorWebView();
        servicios.AddSingleton<IVentanaDeLaBarra>(this);
#if DEBUG
        servicios.AddBlazorWebViewDeveloperTools();
#endif

        _visor = new VisorEmbebido
        {
            Dock = DockStyle.Fill,
            HostPage = PaginaDeInicio(),
            Services = servicios.BuildServiceProvider()
        };
        _visor.RootComponents.Add<RaizDeLaBarra>("#app");
        Controls.Add(_visor);

        PrepararElArrastre();

        Ubicar();
        Redondear();

        _vigilante.Tick += (_, _) => VigilarElMouse();
        _siempreEncima.Tick += (_, _) => AsegurarQueEsteEncima();
        _siempreEncima.Start();
    }

    /// <summary>
    /// La misma página que la ventana principal: monta otro componente en el mismo <c>#app</c>, así
    /// no hace falta un segundo archivo ni tocar el empaquetado del contenido web. La ruta va
    /// absoluta porque el visor la resuelve contra el directorio actual, que no siempre es el del
    /// ejecutable.
    /// </summary>
    private static string PaginaDeInicio() => Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

    // --- Lo que la barra le pide a la ventana --------------------------------

    public bool EstaReplegada => _colapsada;

    public bool EsTemporal => _temporal;

    public bool ApoyadaALaDerecha => _lado == Lado.Derecha;

    public event EventHandler? AspectoCambiado;

    public event EventHandler? FrenadoPedido;

    public void AjustarAlto(int alto)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AjustarAlto(alto));
            return;
        }

        // Desplegar algo con la barra colapsada no tendría dónde mostrarse. Queda clavada: se
        // pidió ver algo, no echarle un vistazo.
        if (_colapsada) Desplegar(temporal: false);

        ClientSize = new Size(ClientSize.Width, alto);
        Redondear();
    }

    public void Cerrar()
    {
        if (InvokeRequired) BeginInvoke(Close);
        else Close();
    }

    /// <summary>
    /// La barra se colapsa sobre su extremo izquierdo y se despliega hacia la derecha desde ahí,
    /// sin moverse de lugar.
    ///
    /// <para>
    /// <b>Antes se escondía contra el borde de la pantalla más cercano</b>, con tres variantes, y
    /// volvía al pasarle el mouse por encima. Eso dejaba la pestaña siempre en un borde y, si
    /// molestaba justo ahí, no había forma de correrla: pasarle el mouse para agarrarla la
    /// desplegaba. Colapsándose en el lugar, mover la barra es mover la pestaña, y se fueron con
    /// ello los tres bordes, el centrado, el vigilante del mouse y el asomar.
    /// </para>
    /// </summary>
    public void AlternarElRepliegue()
    {
        if (InvokeRequired)
        {
            BeginInvoke(AlternarElRepliegue);
            return;
        }

        // Clavada, el pin la despincha y se va a la pestaña. Despinchada —desplegada por un clic,
        // de las que se contraen solas—, el pin la clava y se queda. Es el orden de los paneles
        // acoplables de Windows.
        if (_colapsada) Desplegar(temporal: false);
        else if (_temporal) Clavar();
        else Colapsar();
    }

    private void Colapsar()
    {
        _tamanoDesplegado = Size;
        PegarLaPestaniaAlLateral();
        _colapsada = true;
        _temporal = false;
        _vigilante.Stop();
        MostrarLaSolapa();
        Recordar();
    }

    /// <param name="temporal">
    /// True cuando la desplegó un clic en la pestaña: se contrae en cuanto el mouse se va. False
    /// cuando se la clavó con el pin o la pidió alguien de afuera, y entonces se queda.
    /// </param>
    private void Desplegar(bool temporal)
    {
        _colapsada = false;
        _temporal = temporal;
        MostrarLaBarra();
        Recordar();

        if (temporal) _vigilante.Start();
        else _vigilante.Stop();
    }

    /// <summary>
    /// Deja la pestaña contra el borde más cercano de los tres —los dos laterales y el de arriba—,
    /// conservando el otro eje. Siempre pegada a un margen: suelta en medio de la pantalla es un
    /// recuadro flotando sin relación con nada, y además el despliegue necesita saber hacia qué
    /// lado crecer.
    /// </summary>
    private void PegarLaPestaniaAlLateral()
    {
        var pantalla = Screen.FromControl(this).WorkingArea;

        var aIzquierda = Location.X - pantalla.Left;
        var aDerecha = pantalla.Right - Right;
        var arriba = Location.Y - pantalla.Top;

        _lado = arriba <= aIzquierda && arriba <= aDerecha ? Lado.Arriba
            : aIzquierda <= aDerecha ? Lado.Izquierda
            : Lado.Derecha;

        _pestania = _lado switch
        {
            Lado.Izquierda => new Point(pantalla.Left, Location.Y),
            Lado.Derecha => new Point(pantalla.Right - AnchoDeLaPestaniaEnPantalla, Location.Y),
            _ => new Point(Location.X, pantalla.Top)
        };
    }

    /// <summary>La deja fija: deja de mirarse el mouse y el pin pasa a mostrarse clavado.</summary>
    private void Clavar()
    {
        _temporal = false;
        _vigilante.Stop();
        AspectoCambiado?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Mientras está desplegada sin clavar, se contrae en cuanto el mouse sale de la ventana.
    ///
    /// <para>
    /// El mouse se vigila con un reloj y no con <c>MouseEnter</c>/<c>MouseLeave</c>: el visor ocupa
    /// toda la ventana y se queda con esos eventos, así que entrar a él contaría como salir de la
    /// ventana y la barra se contraería con el mouse encima.
    /// </para>
    /// </summary>
    private void VigilarElMouse()
    {
        if (_colapsada || !_temporal) return;
        if (Bounds.Contains(Cursor.Position)) return;

        Colapsar();
    }

    // --- Arrastrar y estirar sin marco ---------------------------------------

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, IntPtr parametro, IntPtr otro);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr ventana, IntPtr insertarDespuesDe, int x, int y, int ancho, int alto, uint opciones);

    private const int AlApretarElMarco = 0x00A1;
    private const int DondeEstaElMouse = 0x0084;

    /// <summary>
    /// El mensaje con el que otro proceso le pide a la barra que se muestre. Es 0 si Windows no lo
    /// pudo registrar, y en ese caso no se compara contra él: 0 es <c>WM_NULL</c> y llega todo el
    /// tiempo.
    /// </summary>
    private static readonly uint Mostrate = ArranqueDeLaBarra.MensajeParaMostrarse();

    /// <summary>El mensaje con el que otro proceso le pide a la barra que frene el entorno.</summary>
    private static readonly uint Frenate = ArranqueDeLaBarra.MensajeParaFrenar();
    private const int EnLaBarraDeTitulo = 2;
    private const int EnElBordeDerecho = 11;

    private static readonly IntPtr SiempreEncima = new(-1);
    private const uint SinMover = 0x0002;
    private const uint SinRedimensionar = 0x0001;
    private const uint SinActivar = 0x0010;

    /// <summary>
    /// El arrastre lo dispara la página: el visor ocupa la ventana y se queda con el mouse, así que
    /// el hit-test de acá nunca vería la zona de puntos. La página avisa por el mismo camino que usa
    /// la franja de archivos arrastrados, y entonces se le pide a Windows que mueva la ventana como
    /// si la hubieran agarrado de la barra de título.
    /// </summary>
    private void PrepararElArrastre() => _visor.BlazorWebViewInitialized += (_, _) =>
    {
        _visor.WebView.CoreWebView2.WebMessageReceived += (_, mensaje) =>
        {
            if (mensaje.TryGetWebMessageAsString() != "barra:mover") return;

            var antesDeArrastrar = Location;

            ReleaseCapture();
            SendMessage(Handle, AlApretarElMarco, EnLaBarraDeTitulo, IntPtr.Zero);

            // Al soltar, la ventana puede haber quedado afuera de la pantalla.
            BeginInvoke(() =>
            {
                // Colapsada, un arrastre que no movió nada es un clic, y el clic despliega. Se
                // decide acá y no con un onclick en la página porque el arrastre lo maneja Windows
                // con su propio bucle modal: se queda con el botón del mouse y no hay garantía de
                // que el clic llegue después al visor.
                // Sin movimiento fue un clic sobre el icono, y el icono alterna: colapsada la
                // despliega, desplegada la vuelve a cerrar. Clavada no, porque para eso está el
                // pin: clavarla es justamente pedir que no se cierre sola ni de un clic al pasar.
                if (Location == antesDeArrastrar)
                {
                    if (_colapsada) Desplegar(temporal: true);
                    else if (_temporal) Colapsar();

                    return;
                }

                if (_colapsada)
                {
                    // Arrastrada: vuelve a pegarse al borde, que puede ser otro.
                    PegarLaPestaniaAlLateral();
                    MostrarLaSolapa();
                    Recordar();
                    return;
                }

                Location = AcotarALaPantalla(Location);
                _pestania = Location;

                // Movida a mano, la barra vuelve a flotar: deja de apoyarse donde estaba y
                // recupera las cuatro esquinas redondeadas. El apoyo se decide de nuevo al
                // colapsar, con el borde que le haya quedado más cerca.
                _lado = null;
                Redondear();

                Recordar();
            });
        };
    };

    protected override void WndProc(ref Message mensaje)
    {
        base.WndProc(ref mensaje);

        if (Mostrate != 0 && mensaje.Msg == Mostrate)
        {
            Mostrarse();
            return;
        }

        if (Frenate != 0 && mensaje.Msg == Frenate)
        {
            FrenadoPedido?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (mensaje.Msg != DondeEstaElMouse || _colapsada) return;

        var enPantalla = new Point(mensaje.LParam.ToInt32() & 0xFFFF, (mensaje.LParam.ToInt32() >> 16) & 0xFFFF);
        if (PointToClient(enPantalla).X >= ClientSize.Width - BordeParaEstirar) mensaje.Result = EnElBordeDerecho;
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);

        if (_colapsada) return;

        Location = AcotarALaPantalla(Location);
        _pestania = Location;
        Recordar();
        Redondear();
    }

    // --- Siempre a la vista --------------------------------------------------

    /// <summary>
    /// Alguien de afuera —la pantalla del Selector al levantar, el menú de la bandeja— pide que la
    /// barra se vea. Si está colapsada se despliega: dejarla así y sólo ponerla encima deja a la
    /// vista una pestaña de 44 px, que es como no mostrar nada.
    /// </summary>
    private void Mostrarse()
    {
        if (InvokeRequired)
        {
            BeginInvoke(Mostrarse);
            return;
        }

        if (_colapsada) Desplegar(temporal: false);
        AsegurarQueEsteEncima();
    }

    /// <summary>
    /// <c>TopMost</c> solo no alcanza: otra ventana que también se declare siempre encima —y Visual
    /// Studio tiene varias— puede quedar arriba, y desde ahí la barra queda tapada aunque la
    /// propiedad siga en true. Reponerlo cada tanto la devuelve al frente sin robarle el foco.
    /// </summary>
    private void AsegurarQueEsteEncima()
    {
        if (!IsHandleCreated || IsDisposed) return;

        TopMost = true;
        SetWindowPos(Handle, SiempreEncima, 0, 0, 0, 0, SinMover | SinRedimensionar | SinActivar);
    }

    // --- Colapsarse sobre el extremo izquierdo -------------------------------

    /// <summary>
    /// Colapsa la ventana a la pestaña, en el mismo punto donde empieza la barra: sin el mínimo de
    /// ancho, que no la dejaría bajar del de la barra, y sin la franja para estirar, que se comería
    /// parte del icono.
    /// </summary>
    private void MostrarLaSolapa()
    {
        MinimumSize = Size.Empty;
        Padding = Padding.Empty;
        Bounds = new Rectangle(_pestania, new Size(AnchoDeLaPestaniaEnPantalla, _tamanoDesplegado.Height));
        Redondear();
        AspectoCambiado?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Despliega la barra hacia la derecha desde donde está la pestaña, que es su mismo origen.
    /// </summary>
    private void MostrarLaBarra()
    {
        MinimumSize = new Size(AnchoMinimo, BarraPagina.AltoDeLaBarra);
        Padding = new Padding(0, 0, BordeParaEstirar, 0);
        // Apoyada a la derecha se despliega hacia la izquierda: el borde que toca la pantalla es
        // el mismo antes y después, y la barra crece hacia adentro. En los otros dos lados crece
        // hacia la derecha desde donde está la pestaña.
        var x = _lado == Lado.Derecha
            ? _pestania.X + AnchoDeLaPestaniaEnPantalla - _tamanoDesplegado.Width
            : _pestania.X;

        Bounds = new Rectangle(new Point(x, _pestania.Y), _tamanoDesplegado);
        Redondear();
        AspectoCambiado?.Invoke(this, EventArgs.Empty);
    }

    // --- Dónde y de qué tamaño -----------------------------------------------

    private void Ubicar()
    {
        var pantalla = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        var configuracion = ConfiguracionDevTools.Compartida;

        var guardada = configuracion.PosicionDeLaBarra?.Split(',');
        Location = guardada is { Length: 2 }
            && int.TryParse(guardada[0], out var x)
            && int.TryParse(guardada[1], out var y)
                ? AcotarALaPantalla(new Point(x, y))
                : new Point(pantalla.Right - ClientSize.Width - 24, pantalla.Top + 24);

        // Siempre nace a la vista: la barra se abre cuando se levanta el entorno, que es justo
        // cuando hay algo para mirar. Esconderla es una decisión del momento y dura lo que dura
        // la sesión; recordarla haría que arrancara escondida y pareciera que no abrió.
        _pestania = Location;
    }

    /// <summary>
    /// La deja dentro de la pantalla: una ventana sin marco se puede arrastrar hasta sacarla, y ahí
    /// no hay forma de recuperarla —no está en la barra de tareas ni se la alcanza con el mouse—.
    /// El repliegue es la única excepción, y ése no pasa por acá.
    /// </summary>
    private Point AcotarALaPantalla(Point donde)
    {
        var pantalla = Screen.FromPoint(donde).WorkingArea;

        return new Point(
            Math.Clamp(donde.X, pantalla.Left, Math.Max(pantalla.Left, pantalla.Right - Width)),
            Math.Clamp(donde.Y, pantalla.Top, Math.Max(pantalla.Top, pantalla.Bottom - Height)));
    }

    private void Recordar()
    {
        var configuracion = ConfiguracionDevTools.Compartida;
        var posicion = _colapsada ? _pestania : Location;
        var ancho = _colapsada ? _tamanoDesplegado.Width : ClientSize.Width;

        configuracion.PosicionDeLaBarra = $"{posicion.X},{posicion.Y}";
        configuracion.AnchoDeLaBarra = ancho;
        configuracion.Guardar();
    }

    private static int AnchoGuardado()
    {
        var guardado = ConfiguracionDevTools.Compartida.AnchoDeLaBarra;

        return guardado is int ancho && ancho >= AnchoMinimo && ancho <= AnchoMaximo
            ? ancho
            : AnchoPorDefecto;
    }

    /// <summary>
    /// Redondea las esquinas que no tocan la pantalla y deja rectas las del borde de apoyo. No se
    /// usa <see cref="MenuDelTema.Contorno"/> porque redondea las cuatro, y un vértice redondeado
    /// contra el borde deja ver el escritorio por el hueco: se nota el recorte.
    /// </summary>
    private void Redondear()
    {
        var area = new Rectangle(Point.Empty, Size);
        var diametro = Radio * 2;
        var derecha = area.Right - 1;
        var abajo = area.Bottom - 1;

        // Qué esquinas van redondeadas, en orden horario desde la de arriba a la izquierda. Sin
        // apoyo se comporta como apoyada arriba: las de abajo redondeadas y las de arriba rectas,
        // que es el aspecto de la barra flotando.
        var (supIzq, supDer, infDer, infIzq) = _lado switch
        {
            Lado.Derecha => (true, false, false, true),
            Lado.Izquierda => (false, true, true, false),
            _ => (false, false, true, true)
        };

        using var contorno = new GraphicsPath();

        if (supIzq) contorno.AddArc(area.X, area.Y, diametro, diametro, 180, 90);
        else contorno.AddLine(area.X, area.Y, area.X + 1, area.Y);

        if (supDer) contorno.AddArc(derecha - diametro, area.Y, diametro, diametro, 270, 90);
        else contorno.AddLine(derecha, area.Y, derecha, area.Y + 1);

        if (infDer) contorno.AddArc(derecha - diametro, abajo - diametro, diametro, diametro, 0, 90);
        else contorno.AddLine(derecha, abajo, derecha - 1, abajo);

        if (infIzq) contorno.AddArc(area.X, abajo - diametro, diametro, diametro, 90, 90);
        else contorno.AddLine(area.X, abajo, area.X, abajo - 1);

        contorno.CloseFigure();
        Region = new Region(contorno);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vigilante.Dispose();
            _siempreEncima.Dispose();
        }

        base.Dispose(disposing);
    }
}
