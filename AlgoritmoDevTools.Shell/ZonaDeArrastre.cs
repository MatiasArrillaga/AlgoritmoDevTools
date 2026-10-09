using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.UI.Servicios;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Franja donde se sueltan archivos, dibujada con Windows Forms por encima del navegador.
///
/// Existe porque el motor de Edge no entrega la ruta en disco de lo que se suelta y tampoco cede el
/// arrastre al formulario: con el navegador ocupando toda la ventana, no queda superficie que pueda
/// recibirlo. Esta franja es esa superficie.
///
/// Se pinta a mano con la paleta del tema vigente para que no se note que es de otra tecnología:
/// mismo fondo de campo, mismo borde punteado y la misma tipografía que la interfaz HTML.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md.
/// </summary>
public sealed class ZonaDeArrastre : Panel, IZonaDeArrastre
{
    private const int AlturaNormal = 68;

    /// <summary>
    /// Cuánto se espera antes de esconder la franja. El aviso de "salió" llega por dos caminos que
    /// se pisan —el del navegador y el de la propia franja— y sin esta espera la franja parpadea:
    /// al entrar el cursor en ella, el navegador deja de ver el arrastre y pide esconderla, con lo
    /// que el cursor vuelve a quedar sobre el navegador y el ciclo se repite.
    /// </summary>
    private const int MilisegundosAntesDeEsconder = 180;

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private readonly ArchivosArrastrados _arrastre;
    private readonly System.Windows.Forms.Timer _apagado;

    private string _mensaje = string.Empty;
    private bool _armada;
    private bool _enLaVentana;
    private bool _encima;

    public ZonaDeArrastre(ArchivosArrastrados arrastre)
    {
        _arrastre = arrastre;

        Height = AlturaNormal;
        Visible = false;
        AllowDrop = true;
        Padding = new Padding(24, 8, 24, 8);
        Font = new Font("Segoe UI", 9.75f);
        DoubleBuffered = true;

        DragEnter += AlEntrar;
        DragOver += AlEntrar;
        DragLeave += (_, _) => { _encima = false; PedirApagado(); };
        DragDrop += AlSoltar;

        _apagado = new System.Windows.Forms.Timer { Interval = MilisegundosAntesDeEsconder };
        _apagado.Tick += (_, _) => { _apagado.Stop(); Refrescar(); };

        _temas.TemaCambiado += (_, _) => Invalidate();
    }

    /// <summary>Alto de la barra de estado de la interfaz HTML, para apoyarse justo encima.</summary>
    private int _alturaDeLaBarraDeEstado = 28;

    public void Armar(string mensaje)
    {
        if (IsHandleCreated && InvokeRequired)
        {
            BeginInvoke(() => Armar(mensaje));
            return;
        }

        _mensaje = mensaje;
        _armada = true;
    }

    public void Desarmar()
    {
        if (IsHandleCreated && InvokeRequired)
        {
            BeginInvoke(Desarmar);
            return;
        }

        _armada = false;
        _enLaVentana = false;
        _encima = false;
        _apagado.Stop();
        Refrescar();
    }

    /// <summary>
    /// Informa que hay o dejó de haber un arrastre sobre la ventana. Lo avisa el navegador, que es
    /// el único que lo ve mientras el cursor está sobre su área.
    /// </summary>
    public void SeguirArrastre(bool enCurso)
    {
        if (IsHandleCreated && InvokeRequired)
        {
            BeginInvoke(() => SeguirArrastre(enCurso));
            return;
        }

        _enLaVentana = enCurso;

        if (enCurso)
        {
            _apagado.Stop();
            Refrescar();
            return;
        }

        PedirApagado();
    }

    /// <summary>
    /// Posiciona la franja apoyada sobre la barra de estado, por encima del navegador. No usa Dock
    /// a propósito: acoplada al borde empujaría el contenido cada vez que aparece, y como aparece y
    /// desaparece con el arrastre, la pantalla entera saltaría.
    /// </summary>
    /// <param name="areaDisponible">Área del formulario donde se acomoda.</param>
    /// <param name="alturaDeLaBarraDeEstado">
    /// Alto en píxeles de pantalla de la barra de estado de la interfaz HTML. Nulo conserva el
    /// último conocido; depende del escalado de Windows, así que lo informa la propia página.
    /// </param>
    public void Reposicionar(Size areaDisponible, int? alturaDeLaBarraDeEstado = null)
    {
        if (alturaDeLaBarraDeEstado is > 0) _alturaDeLaBarraDeEstado = alturaDeLaBarraDeEstado.Value;

        var alto = Math.Min(AlturaNormal, Math.Max(0, areaDisponible.Height - _alturaDeLaBarraDeEstado));
        var arriba = Math.Max(0, areaDisponible.Height - _alturaDeLaBarraDeEstado - alto);

        Bounds = new Rectangle(0, arriba, areaDisponible.Width, alto);
    }

    private void PedirApagado()
    {
        _apagado.Stop();
        _apagado.Start();
    }

    /// <summary>
    /// La franja se ve si está armada y hay un arrastre, lo vea el navegador o lo vea ella misma.
    /// Que alcance con cualquiera de los dos es lo que evita el parpadeo.
    /// </summary>
    private void Refrescar()
    {
        var visible = _armada && (_enLaVentana || _encima);
        if (Visible == visible) return;

        if (!visible) _encima = false;

        Visible = visible;
        if (visible) BringToFront();
        Invalidate();
    }

    private void AlEntrar(object? sender, DragEventArgs e)
    {
        var hayArchivos = e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        e.Effect = hayArchivos ? DragDropEffects.Copy : DragDropEffects.None;

        if (!hayArchivos) return;

        _apagado.Stop();

        if (_encima) return;

        _encima = true;
        Invalidate();
    }

    private void AlSoltar(object? sender, DragEventArgs e)
    {
        _encima = false;
        _enLaVentana = false;
        _apagado.Stop();
        Refrescar();

        if (e.Data?.GetData(DataFormats.FileDrop) is string[] rutas && rutas.Length > 0)
        {
            _arrastre.NotificarSoltados(rutas);
        }
    }

    /// <summary>
    /// Replica la zona de soltar de la interfaz HTML: el recuadro punteado se vuelve continuo y toma
    /// el color de acento mientras hay archivos encima.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var tema = _temas.Actual;
        var g = e.Graphics;

        g.Clear(tema.FondoPrincipal);

        var marco = new Rectangle(
            Padding.Left,
            Padding.Top,
            Math.Max(0, Width - Padding.Horizontal - 1),
            Math.Max(0, Height - Padding.Vertical - 1));

        using (var fondo = new SolidBrush(tema.FondoCampo))
        {
            g.FillRectangle(fondo, marco);
        }

        using (var borde = new Pen(_encima ? tema.Acento : tema.BordeCampo))
        {
            borde.DashStyle = _encima
                ? System.Drawing.Drawing2D.DashStyle.Solid
                : System.Drawing.Drawing2D.DashStyle.Dash;
            g.DrawRectangle(borde, marco);
        }

        TextRenderer.DrawText(
            g,
            _mensaje,
            Font,
            marco,
            _encima ? tema.Acento : tema.TextoSecundario,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _apagado.Dispose();
        base.Dispose(disposing);
    }
}
