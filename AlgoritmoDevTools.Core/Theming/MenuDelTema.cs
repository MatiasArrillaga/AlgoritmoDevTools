using System.Drawing.Drawing2D;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Le da a los menús de Windows Forms la misma estética plana que el resto de la aplicación: fondo y
/// texto del tema elegido, esquinas redondeadas, separadores de 1px y nada de degradados.
///
/// Windows dibuja el menú del ícono de la bandeja con el estilo clásico, que no se parece en nada a
/// la aplicación y encima ignora el tema: en modo oscuro sigue saliendo gris claro.
///
/// Son tres cosas que no se pueden pedir de otra forma:
/// <list type="bullet">
/// <item>el pintado, con un <see cref="ToolStripRenderer"/> propio;</item>
/// <item>las esquinas redondeadas, recortando la ventana con una región, porque un menú no tiene
/// borde configurable;</item>
/// <item>el alto de cada ítem, que de fábrica sale apretado contra el texto.</item>
/// </list>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public static class MenuDelTema
{
    /// <summary>Radio de las esquinas. El mismo que usan los bloques redondeados de la interfaz.</summary>
    private const int Radio = 8;

    /// <summary>Lo que se le suma al alto de fábrica de cada ítem para que respire.</summary>
    private const int AlturaExtraDelItem = 12;

    /// <summary>Aire a los costados del texto, el mismo para todos los ítems del menú.</summary>
    private const int MargenDelTexto = 12;

    /// <summary>
    /// Aplica el tema vigente a un menú y a todos los submenús que cuelguen de él.
    ///
    /// Se llama cada vez que el menú se arma, no una sola vez al crearlo: los submenús se rehacen
    /// con el menú y el tema puede haber cambiado desde la ventana mientras tanto.
    /// </summary>
    public static void Aplicar(ToolStripDropDown menu)
    {
        var tema = ServicioDeTemas.Instancia.Actual;

        // El renderizador es de toda la aplicación: cada submenú es una ventana aparte, y pedírselo
        // uno por uno obligaría a acordarse de los que todavía no existen.
        ToolStripManager.Renderer = new RenderizadorDeMenu(tema);

        Preparar(menu, tema);
    }

    private static void Preparar(ToolStripDropDown menu, Tema tema)
    {
        menu.BackColor = tema.FondoPrincipal;
        menu.ForeColor = tema.Texto;
        menu.Padding = new Padding(4);

        if (menu is ToolStripDropDownMenu desplegable)
        {
            // Sin la banda de íconos de la izquierda, que acá no tiene nada que mostrar.
            desplegable.ShowImageMargin = false;

            // La del tilde sólo si alguno lo usa. Es la que corre todo el texto a la derecha, y en
            // un menú donde no hay nada que tildar deja a los renglones empezando en el aire.
            desplegable.ShowCheckMargin = menu.Items
                .OfType<ToolStripMenuItem>()
                .Any(item => item.Checked || item.CheckOnClick);
        }

        Redondear(menu);

        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripSeparator) continue;

            item.AutoSize = false;
            item.Height = item.GetPreferredSize(Size.Empty).Height + AlturaExtraDelItem;
            item.Width = menu.Width - menu.Padding.Horizontal;

            // Todos los renglones arrancan en la misma columna y con el texto al medio del alto,
            // incluidos los que sólo muestran un dato: alineados de a uno, saltan a la vista.
            item.TextAlign = ContentAlignment.MiddleLeft;
            item.Padding = new Padding(MargenDelTexto, 0, MargenDelTexto, 0);

            // Un submenú recién existe cuando se lo abre, así que se lo prepara en ese momento.
            if (item is ToolStripMenuItem conHijos && conHijos.HasDropDownItems)
            {
                conHijos.DropDownOpening -= AlAbrirSubmenu;
                conHijos.DropDownOpening += AlAbrirSubmenu;
            }
        }
    }

    private static void AlAbrirSubmenu(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            Preparar(item.DropDown, ServicioDeTemas.Instancia.Actual);
        }
    }

    /// <summary>
    /// Recorta la ventana del menú con las esquinas redondeadas. Hay que rehacerlo cada vez que
    /// cambia de tamaño: la región se mide en píxeles y no acompaña sola.
    /// </summary>
    private static void Redondear(ToolStripDropDown menu)
    {
        menu.Resize -= AlCambiarDeTamanio;
        menu.Resize += AlCambiarDeTamanio;
        AplicarRegion(menu);
    }

    private static void AlCambiarDeTamanio(object? sender, EventArgs e)
    {
        if (sender is ToolStripDropDown menu) AplicarRegion(menu);
    }

    private static void AplicarRegion(ToolStripDropDown menu)
    {
        if (menu.Width <= Radio * 2 || menu.Height <= Radio * 2) return;

        using var contorno = Contorno(new Rectangle(Point.Empty, menu.Size), Radio);
        menu.Region = new Region(contorno);
    }

    /// <summary>Rectángulo con las cuatro esquinas redondeadas.</summary>
    internal static GraphicsPath Contorno(Rectangle area, int radio)
    {
        var diametro = radio * 2;
        var camino = new GraphicsPath();

        camino.AddArc(area.X, area.Y, diametro, diametro, 180, 90);
        camino.AddArc(area.Right - diametro - 1, area.Y, diametro, diametro, 270, 90);
        camino.AddArc(area.Right - diametro - 1, area.Bottom - diametro - 1, diametro, diametro, 0, 90);
        camino.AddArc(area.X, area.Bottom - diametro - 1, diametro, diametro, 90, 90);
        camino.CloseFigure();

        return camino;
    }
}

/// <summary>
/// Pinta los menús con los colores del tema. Hereda del profesional por el layout —dónde va el
/// texto, el tilde y la flecha— y le reemplaza todo lo que dibuja.
/// </summary>
internal sealed class RenderizadorDeMenu : ToolStripProfessionalRenderer
{
    private readonly Tema _tema;

    public RenderizadorDeMenu(Tema tema) : base(new SinColoresPropios(tema))
    {
        _tema = tema;

        // El borde redondeado lo dibuja OnRenderToolStripBorder: el de fábrica sale recto y asoma
        // por fuera del recorte.
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var fondo = new SolidBrush(_tema.FondoPrincipal);
        e.Graphics.FillRectangle(fondo, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is not ToolStripDropDown) return;

        // Sobre el recorte de la región, el borde queda dentado si no se suaviza.
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var area = new Rectangle(Point.Empty, e.ToolStrip.Size);
        using var contorno = MenuDelTema.Contorno(area, 8);
        using var lapiz = new Pen(_tema.Separador);
        e.Graphics.DrawPath(lapiz, contorno);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        // Un ítem deshabilitado no es una opción: muestra un dato, y resaltarlo al pasar por encima
        // promete algo que no va a pasar.
        if (!e.Item.Selected || !e.Item.Enabled) return;

        var area = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var contorno = MenuDelTema.Contorno(area, 5);
        using var relleno = new SolidBrush(_tema.BotonSecundarioHover);
        e.Graphics.FillPath(relleno, contorno);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? _tema.Texto : _tema.TextoSecundario;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var lapiz = new Pen(_tema.Separador);
        e.Graphics.DrawLine(lapiz, 8, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // El tilde a mano, con el acento: el de fábrica viene con su recuadro y su degradado.
        var area = e.ImageRectangle;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var lapiz = new Pen(_tema.Acento, 1.8f);
        var izquierda = area.Left + area.Width * 0.22f;
        var medio = area.Left + area.Width * 0.42f;
        var derecha = area.Left + area.Width * 0.78f;
        var arriba = area.Top + area.Height * 0.30f;
        var centro = area.Top + area.Height * 0.52f;
        var abajo = area.Top + area.Height * 0.70f;

        e.Graphics.DrawLines(lapiz,
        [
            new PointF(izquierda, centro),
            new PointF(medio, abajo),
            new PointF(derecha, arriba)
        ]);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == false ? _tema.TextoSecundario : _tema.Texto;
        base.OnRenderArrow(e);
    }

    /// <summary>
    /// La tabla de colores del renderizador profesional. Va en blanco porque todo lo que se ve lo
    /// pinta el renderizador: lo único que queda de ella es el fondo del margen del tilde, que si no
    /// sale gris y corta el fondo del menú con una banda vertical.
    /// </summary>
    private sealed class SinColoresPropios : ProfessionalColorTable
    {
        private readonly Tema _tema;

        public SinColoresPropios(Tema tema)
        {
            _tema = tema;
            UseSystemColors = false;
        }

        public override Color ImageMarginGradientBegin => _tema.FondoPrincipal;
        public override Color ImageMarginGradientMiddle => _tema.FondoPrincipal;
        public override Color ImageMarginGradientEnd => _tema.FondoPrincipal;
        public override Color ToolStripDropDownBackground => _tema.FondoPrincipal;
        public override Color MenuBorder => _tema.Separador;
        public override Color MenuItemBorder => _tema.BotonSecundarioHover;
        public override Color MenuItemSelected => _tema.BotonSecundarioHover;
        public override Color SeparatorDark => _tema.Separador;
        public override Color SeparatorLight => _tema.Separador;
    }
}
