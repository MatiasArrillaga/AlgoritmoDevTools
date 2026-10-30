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
    /// El lado de la solapa que queda a la vista cuando está escondida contra un borde: el de un
    /// botón de la barra más su margen, así la flecha se ve como uno más. Es lo que recibe el mouse
    /// para traerla de vuelta.
    /// </summary>
    private const int LadoDeLaSolapa = 44;

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private readonly System.Windows.Forms.Timer _vigilante = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer _siempreEncima = new() { Interval = 3000 };
    private readonly VisorEmbebido _visor;

    private string? _replegadaEn;
    private bool _asomando;

    /// <summary>
    /// Dónde vuelve a la vista mientras está escondida. Al replegarse deja de ser el lugar donde
    /// estaba y pasa a ser el centro del borde: ver <see cref="PosicionPegadaAlBorde"/>.
    /// </summary>
    private Point _posicionDesplegada;

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

    public bool EstaReplegada => _replegadaEn is not null;

    public string? SolapaContra => _asomando ? null : _replegadaEn;

    public event EventHandler? AspectoCambiado;

    public void AjustarAlto(int alto)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AjustarAlto(alto));
            return;
        }

        // Desplegar algo con la barra escondida no tendría dónde mostrarse.
        if (_replegadaEn is not null) AlternarElRepliegue();

        ClientSize = new Size(ClientSize.Width, alto);
        Redondear();
    }

    public void Cerrar()
    {
        if (InvokeRequired) BeginInvoke(Close);
        else Close();
    }

    /// <summary>
    /// El botón de esconder fija la decisión: con la barra a la vista la esconde contra el borde más
    /// cercano, y estando escondida la deja desplegada para siempre —centrada en ese borde, igual
    /// que cuando asoma—. Pasarle el mouse sólo la hace asomar mientras esté encima.
    /// </summary>
    public void AlternarElRepliegue()
    {
        if (InvokeRequired)
        {
            BeginInvoke(AlternarElRepliegue);
            return;
        }

        if (_replegadaEn is null)
        {
            Replegar(BordeMasCercano());
            return;
        }

        _replegadaEn = null;
        _asomando = false;
        _vigilante.Stop();
        MostrarLaBarra(_posicionDesplegada);
        Recordar();
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

            ReleaseCapture();
            SendMessage(Handle, AlApretarElMarco, EnLaBarraDeTitulo, IntPtr.Zero);

            // Al soltar, la ventana puede haber quedado afuera de la pantalla o pegada a un borde.
            BeginInvoke(() =>
            {
                Location = AcotarALaPantalla(Location);
                Recordar();
                ReplegarSiQuedoPegada();
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

        if (mensaje.Msg != DondeEstaElMouse || SolapaContra is not null) return;

        var enPantalla = new Point(mensaje.LParam.ToInt32() & 0xFFFF, (mensaje.LParam.ToInt32() >> 16) & 0xFFFF);
        if (PointToClient(enPantalla).X >= ClientSize.Width - BordeParaEstirar) mensaje.Result = EnElBordeDerecho;
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);

        if (_replegadaEn is not null) return;

        Location = AcotarALaPantalla(Location);
        Recordar();
        Redondear();
    }

    // --- Siempre a la vista --------------------------------------------------

    /// <summary>
    /// Alguien de afuera —la pantalla del Selector al levantar, el menú de la bandeja— pide que la
    /// barra se vea. Si está escondida se despliega: dejarla replegada y sólo ponerla encima deja a
    /// la vista una solapa de 44 px perdida en el borde, que es como no mostrar nada.
    /// </summary>
    private void Mostrarse()
    {
        if (InvokeRequired)
        {
            BeginInvoke(Mostrarse);
            return;
        }

        if (_replegadaEn is not null) AlternarElRepliegue();
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

    // --- Esconderse contra un borde ------------------------------------------

    /// <summary>
    /// El mouse se vigila con un reloj y no con <c>MouseEnter</c>/<c>MouseLeave</c>: el visor se
    /// queda con esos eventos, así que entrar a él contaría como salir de la ventana y la barra se
    /// escondería con el mouse encima.
    /// </summary>
    private void VigilarElMouse()
    {
        if (_replegadaEn is null) return;

        if (Bounds.Contains(Cursor.Position)) Asomar();
        else Esconder();
    }

    private void Asomar()
    {
        if (_asomando || _replegadaEn is null) return;

        _asomando = true;
        MostrarLaBarra(_posicionDesplegada);
        AsegurarQueEsteEncima();
    }

    private void Esconder()
    {
        if (!_asomando || _replegadaEn is null) return;

        _asomando = false;
        MostrarLaSolapa();
    }

    private void Replegar(string borde)
    {
        _posicionDesplegada = PosicionPegadaAlBorde(borde);
        _tamanoDesplegado = Size;
        _replegadaEn = borde;
        _asomando = false;
        MostrarLaSolapa();
        _vigilante.Start();
        Recordar();
    }

    /// <summary>
    /// Escondida, la ventana se achica a la solapa: sin el mínimo de ancho, que no la dejaría bajar
    /// del de la barra, y sin la franja para estirar, que se comería la mitad de la flecha.
    /// </summary>
    private void MostrarLaSolapa()
    {
        MinimumSize = Size.Empty;
        Padding = Padding.Empty;
        Bounds = LugarDeLaSolapa(_replegadaEn!);
        Redondear();
        AspectoCambiado?.Invoke(this, EventArgs.Empty);
    }

    private void MostrarLaBarra(Point donde)
    {
        MinimumSize = new Size(AnchoMinimo, BarraPagina.AltoDeLaBarra);
        Padding = new Padding(0, 0, BordeParaEstirar, 0);
        Bounds = new Rectangle(donde, _tamanoDesplegado);
        Redondear();
        AspectoCambiado?.Invoke(this, EventArgs.Empty);
    }

    private void ReplegarSiQuedoPegada()
    {
        const int Pegada = 12;

        // Contra el borde del escritorio entero y no el de esta pantalla: si no, arrastrarla de un
        // monitor al otro la esconde a mitad de camino y nunca llega.
        var escritorio = SystemInformation.VirtualScreen;

        if (Location.X <= escritorio.Left + Pegada) Replegar("Izquierda");
        else if (Right >= escritorio.Right - Pegada) Replegar("Derecha");
        else if (Location.Y <= escritorio.Top + Pegada) Replegar("Arriba");
    }

    /// <summary>
    /// La solapa queda donde termina la barra, que es de donde va a salir: a los costados, a la
    /// altura de la franja; arriba, sobre su extremo derecho, que es donde está el botón de
    /// esconder. Con la barra centrada eso cae en el medio del borde superior, tirado a la derecha.
    /// </summary>
    private Rectangle LugarDeLaSolapa(string borde)
    {
        var pantalla = PantallaDeLaBarra();
        var alturaDeLaFranja = _posicionDesplegada.Y + (BarraPagina.AltoDeLaBarra - LadoDeLaSolapa) / 2;

        var donde = borde switch
        {
            "Izquierda" => new Point(pantalla.Left, alturaDeLaFranja),
            "Derecha" => new Point(pantalla.Right - LadoDeLaSolapa, alturaDeLaFranja),
            _ => new Point(_posicionDesplegada.X + _tamanoDesplegado.Width - LadoDeLaSolapa, pantalla.Top)
        };

        return new Rectangle(donde, new Size(LadoDeLaSolapa, LadoDeLaSolapa));
    }

    /// <summary>
    /// Dónde queda la barra cada vez que vuelve a la vista: contra el borde por el que se escondió
    /// y centrada en el otro eje. Se calcula una sola vez, al replegarse.
    ///
    /// <para>
    /// No vuelve a donde estaba porque esconderla es decir que ahí molesta: traerla de vuelta al
    /// mismo lugar es traer de vuelta el estorbo, y encima tapando lo que el usuario haya puesto
    /// ahí mientras no estaba.
    /// </para>
    ///
    /// <para>
    /// Que sea una sola posición es lo que mantiene coherentes las tres cosas que la leen: la
    /// solapa se ubica a partir de ella, el mouse encima la hace asomar ahí —cayendo debajo de la
    /// solapa, así el cursor sigue adentro y no se esconde al instante— y la flecha la fija en ese
    /// mismo lugar. Antes el hover la pegaba al borde y la flecha la mandaba de vuelta a donde
    /// estaba, así que fijarla la hacía saltar.
    /// </para>
    /// </summary>
    private Point PosicionPegadaAlBorde(string borde)
    {
        var pantalla = Screen.FromControl(this).WorkingArea;

        // El eje que no es el del borde va al centro. Dejarlo donde estaba hace que la barra
        // reaparezca en medio del escritorio, que es de donde se la quiso sacar; mandarlo al
        // extremo la pega contra la esquina, lejos de donde uno la busca. Centrada, la solapa cae
        // en el medio del borde —tirada a la derecha, que es donde termina la barra—.
        var centradaX = pantalla.Left + (pantalla.Width - Width) / 2;
        var centradaY = pantalla.Top + (pantalla.Height - Height) / 2;

        return borde switch
        {
            "Izquierda" => new Point(pantalla.Left, centradaY),
            "Derecha" => new Point(pantalla.Right - Width, centradaY),
            _ => new Point(centradaX, pantalla.Top)
        };
    }

    private Rectangle PantallaDeLaBarra() => Screen.FromPoint(_posicionDesplegada).WorkingArea;

    private string BordeMasCercano()
    {
        var pantalla = Screen.FromControl(this).WorkingArea;

        var aIzquierda = Location.X - pantalla.Left;
        var aDerecha = pantalla.Right - Right;
        var arriba = Location.Y - pantalla.Top;

        if (arriba <= aIzquierda && arriba <= aDerecha) return "Arriba";
        return aIzquierda <= aDerecha ? "Izquierda" : "Derecha";
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
        _posicionDesplegada = Location;
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
        var posicion = _replegadaEn is null ? Location : _posicionDesplegada;
        var ancho = _replegadaEn is null ? ClientSize.Width : _tamanoDesplegado.Width;

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

    private void Redondear()
    {
        using var contorno = MenuDelTema.Contorno(new Rectangle(Point.Empty, Size), Radio);
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
