using System.Drawing.Drawing2D;
using AlgoritmoDevTools.Core.Theming;

namespace AlgoritmoDevTools.Shell.Views;

/// <summary>
/// Pantalla de Configuraciones del Shell. Hoy tiene dos secciones: la apariencia —las cuatro
/// tarjetas de tema— y el comportamiento de arranque.
///
/// La vista se arma por código en vez de por diseñador porque las tarjetas salen del catálogo de
/// temas: agregar un quinto tema no tiene que obligar a tocar esta pantalla.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public sealed class ConfiguracionesView : UserControl
{
    private const int AnchoTarjeta = 158;
    private const int AltoMuestra = 38;

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;
    private readonly List<TarjetaDeTema> _tarjetas = new();

    public ConfiguracionesView()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 20);
        AutoScroll = true;

        var contenido = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top
        };

        contenido.Controls.Add(Titulo("Configuraciones", 13f, FontStyle.Bold, new Padding(0, 0, 0, 2)));
        contenido.Controls.Add(Titulo("Se guardan por usuario y se aplican al instante.", 9.75f, FontStyle.Regular,
            new Padding(0, 0, 0, 14), AplicadorDeTema.RolSutil));

        contenido.Controls.Add(Encabezado("APARIENCIA"));
        contenido.Controls.Add(ConstruirGrillaDeTemas());

        Controls.Add(contenido);

        _temas.TemaCambiado += OnTemaCambiado;
        ActualizarSeleccion();
    }

    private FlowLayoutPanel ConstruirGrillaDeTemas()
    {
        var grilla = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 16),
            MaximumSize = new Size(AnchoTarjeta * 4 + 40, 0)
        };

        foreach (var tema in Temas.Todos)
        {
            var tarjeta = new TarjetaDeTema(tema);
            tarjeta.Elegido += (_, _) => _temas.Cambiar(tema);
            _tarjetas.Add(tarjeta);
            grilla.Controls.Add(tarjeta);
        }

        return grilla;
    }

    private static Label Titulo(string texto, float tamaño, FontStyle estilo, Padding margen, string? rol = null) =>
        new()
        {
            Text = texto,
            AutoSize = true,
            Font = new Font("Segoe UI", tamaño, estilo),
            Margin = margen,
            Tag = rol
        };

    private static Label Encabezado(string texto) =>
        new()
        {
            Text = texto,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
            Margin = new Padding(0, 0, 0, 8),
            Tag = AplicadorDeTema.RolSutil
        };

    private void OnTemaCambiado(object? sender, EventArgs e) => ActualizarSeleccion();

    private void ActualizarSeleccion()
    {
        foreach (var tarjeta in _tarjetas)
        {
            tarjeta.MarcarSeleccion(tarjeta.Tema.Id == _temas.Actual.Id, _temas.Actual);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _temas.TemaCambiado -= OnTemaCambiado;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Una tarjeta de la grilla de temas: la miniatura del tema, su nombre y el estado de elegido.
    ///
    /// La miniatura se dibuja con los colores del tema que representa, no con los del tema vigente:
    /// la gracia de la pantalla es poder comparar los cuatro antes de elegir.
    /// </summary>
    private sealed class TarjetaDeTema : Panel
    {
        private readonly PictureBox _muestra;
        private readonly RadioButton _opcion;

        public TarjetaDeTema(Tema tema)
        {
            Tema = tema;
            Width = AnchoTarjeta;
            Height = AltoMuestra + 42;
            Padding = new Padding(1);
            Margin = new Padding(0, 0, 10, 0);

            var interior = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };

            _opcion = new RadioButton
            {
                Text = tema.Nombre,
                Dock = DockStyle.Bottom,
                Height = 22,
                AutoSize = false,
                Font = new Font("Segoe UI", 8.25f)
            };
            _opcion.CheckedChanged += (_, _) =>
            {
                if (_opcion.Checked) Elegido?.Invoke(this, EventArgs.Empty);
            };

            _muestra = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = AltoMuestra,
                Image = DibujarMuestra(tema, AnchoTarjeta - 20, AltoMuestra),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Cursor = Cursors.Hand
            };
            _muestra.Click += (_, _) => _opcion.Checked = true;

            interior.Controls.Add(_opcion);
            interior.Controls.Add(_muestra);
            Controls.Add(interior);

            Click += (_, _) => _opcion.Checked = true;
            interior.Click += (_, _) => _opcion.Checked = true;
        }

        public Tema Tema { get; }

        public event EventHandler? Elegido;

        /// <summary>
        /// Marca la tarjeta elegida pintando su marco con el acento del tema vigente. Es el mismo
        /// recurso que usa el borde de los campos: un panel exterior de 1px que asoma por detrás.
        /// </summary>
        /// <param name="seleccionada">Si esta tarjeta es la del tema vigente.</param>
        /// <param name="temaVigente">Tema con el que se pinta el marco.</param>
        public void MarcarSeleccion(bool seleccionada, Tema temaVigente)
        {
            BackColor = seleccionada ? temaVigente.Acento : temaVigente.BordeCampo;
            Padding = new Padding(seleccionada ? 2 : 1);
            _opcion.Checked = seleccionada;
        }

        private static Bitmap DibujarMuestra(Tema tema, int ancho, int alto)
        {
            var bitmap = new Bitmap(ancho, alto);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.None;

            g.Clear(tema.FondoPrincipal);

            var anchoLateral = (int)(ancho * 0.3f);
            using (var lateral = new SolidBrush(tema.FondoLateral))
            {
                g.FillRectangle(lateral, 0, 0, anchoLateral, alto);
            }

            using (var separador = new Pen(tema.Separador))
            {
                g.DrawLine(separador, anchoLateral, 0, anchoLateral, alto);
            }

            using (var acento = new SolidBrush(tema.Acento))
            {
                g.FillRectangle(acento, anchoLateral + 7, alto - 14, (int)(ancho * 0.42f), 9);
            }

            using (var marco = new Pen(Color.FromArgb(48, 128, 128, 128)))
            {
                g.DrawRectangle(marco, 0, 0, ancho - 1, alto - 1);
            }

            return bitmap;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _muestra.Image?.Dispose();
            base.Dispose(disposing);
        }
    }
}
