using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Core.Theming;

/// <summary>
/// Pinta un árbol de controles con los colores de un tema. Es el único lugar donde se decide qué
/// color le toca a cada tipo de control: las vistas no fijan colores, sólo declaran el rol de los
/// controles que se salen de lo común mediante la propiedad Tag del diseñador.
///
/// Se aplica sobre la raíz (el formulario, la vista de una herramienta o un diálogo) y baja en
/// cascada. Es idempotente: todos los colores se asignan de forma absoluta, así que volver a
/// aplicarlo al cambiar de tema deja el mismo resultado que aplicarlo por primera vez.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Core/Theming/README.referencia-tecnica-funcional-temas.md.
/// </summary>
public static class AplicadorDeTema
{
    /// <summary>Botón de la acción principal de la pantalla: va en el color de marca y en negrita.</summary>
    public const string RolPrimario = "primario";

    /// <summary>Panel o control que forma parte de la franja lateral, con su fondo propio.</summary>
    public const string RolLateral = "lateral";

    /// <summary>Texto de apoyo: descripciones, estados, ayudas. Va en el gris secundario.</summary>
    public const string RolSutil = "sutil";

    /// <summary>Línea separadora de 1px. El control se pinta entero del color del separador.</summary>
    public const string RolSeparador = "separador";

    /// <summary>
    /// Aplica el tema a un control y a todo lo que cuelga de él.
    /// </summary>
    /// <param name="raiz">Formulario, vista o diálogo a pintar.</param>
    /// <param name="tema">Paleta a aplicar.</param>
    public static void Aplicar(Control raiz, Tema tema)
    {
        ArgumentNullException.ThrowIfNull(raiz);
        ArgumentNullException.ThrowIfNull(tema);

        raiz.SuspendLayout();
        try
        {
            Pintar(raiz, tema);
        }
        finally
        {
            raiz.ResumeLayout(performLayout: true);
            raiz.Invalidate(invalidateChildren: true);
        }
    }

    private static void Pintar(Control control, Tema tema)
    {
        AplicarSegunTipo(control, tema);

        foreach (Control hijo in control.Controls)
        {
            Pintar(hijo, tema);
        }
    }

    private static void AplicarSegunTipo(Control control, Tema tema)
    {
        var rol = control.Tag as string;

        if (string.Equals(rol, RolSeparador, StringComparison.OrdinalIgnoreCase))
        {
            control.BackColor = tema.Separador;
            return;
        }

        switch (control)
        {
            case Button boton:
                PintarBoton(boton, tema, EsPrimario(rol));
                break;

            case CheckBox chk:
                chk.FlatStyle = FlatStyle.Flat;
                chk.FlatAppearance.BorderSize = 0;
                chk.BackColor = Color.Transparent;
                chk.ForeColor = tema.Texto;
                break;

            case RadioButton radio:
                radio.FlatStyle = FlatStyle.Flat;
                radio.FlatAppearance.BorderSize = 0;
                radio.BackColor = Color.Transparent;
                radio.ForeColor = tema.Texto;
                break;

            // LinkLabel va antes que Label porque hereda de él: al revés nunca entraría.
            case LinkLabel vinculo:
                vinculo.BackColor = Color.Transparent;
                vinculo.ForeColor = tema.Texto;
                vinculo.LinkColor = tema.Acento;
                vinculo.ActiveLinkColor = tema.AcentoHover;
                vinculo.VisitedLinkColor = tema.Acento;
                break;

            case Label etiqueta:
                etiqueta.BackColor = Color.Transparent;
                etiqueta.ForeColor = EsSutil(rol) ? tema.TextoSecundario : tema.Texto;
                break;

            case RichTextBox texto:
                texto.BorderStyle = BorderStyle.FixedSingle;
                texto.BackColor = tema.FondoCampo;
                texto.ForeColor = tema.Texto;
                break;

            case TextBox campo:
                campo.BorderStyle = BorderStyle.FixedSingle;
                campo.BackColor = tema.FondoCampo;
                campo.ForeColor = tema.Texto;
                break;

            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = tema.FondoCampo;
                combo.ForeColor = tema.Texto;
                break;

            case ListBox lista:
                lista.BorderStyle = BorderStyle.None;
                lista.BackColor = tema.FondoCampo;
                lista.ForeColor = tema.Texto;
                break;

            case ListView listView:
                listView.BorderStyle = BorderStyle.None;
                listView.BackColor = EsLateral(rol) ? tema.FondoLateral : tema.FondoPrincipal;
                listView.ForeColor = tema.Texto;
                break;

            case TreeView arbol:
                arbol.BorderStyle = BorderStyle.None;
                arbol.BackColor = tema.FondoPrincipal;
                arbol.ForeColor = tema.Texto;
                arbol.LineColor = tema.Separador;
                break;

            case Splitter divisor:
                divisor.BackColor = tema.Separador;
                break;

            case StatusStrip barraEstado:
                PintarBarraDeEstado(barraEstado, tema);
                break;

            case GroupBox grupo:
                grupo.BackColor = Color.Transparent;
                grupo.ForeColor = tema.TextoSecundario;
                break;

            case Panel or TableLayoutPanel or FlowLayoutPanel or UserControl or Form:
                control.BackColor = EsLateral(rol) ? tema.FondoLateral : tema.FondoPrincipal;
                control.ForeColor = tema.Texto;
                break;

            default:
                control.ForeColor = tema.Texto;
                break;
        }
    }

    private static void PintarBoton(Button boton, Tema tema, bool esPrimario)
    {
        boton.FlatStyle = FlatStyle.Flat;
        boton.UseVisualStyleBackColor = false;
        boton.Cursor = Cursors.Hand;

        if (esPrimario)
        {
            boton.BackColor = tema.Acento;
            boton.ForeColor = tema.TextoSobreAcento;
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.BorderColor = tema.Acento;
            boton.FlatAppearance.MouseOverBackColor = tema.AcentoHover;
            boton.FlatAppearance.MouseDownBackColor = tema.AcentoPresionado;

            // La negrita no es decoración: los cuatro acentos rondan 3,4:1 de contraste con el
            // blanco, y en regular la etiqueta se lee floja.
            if (!boton.Font.Bold) boton.Font = new Font(boton.Font, FontStyle.Bold);
            return;
        }

        boton.BackColor = tema.BotonSecundario;
        boton.ForeColor = tema.Texto;
        boton.FlatAppearance.BorderSize = 1;
        boton.FlatAppearance.BorderColor = tema.BordeCampo;
        boton.FlatAppearance.MouseOverBackColor = tema.BotonSecundarioHover;
        boton.FlatAppearance.MouseDownBackColor = tema.BordeCampo;

        if (boton.Font.Bold) boton.Font = new Font(boton.Font, FontStyle.Regular);
    }

    /// <summary>
    /// La barra de estado trae relieves <c>Etched</c> de la plantilla original de WinForms. Son el
    /// último resto del aspecto viejo, así que se los saca y la división queda por el color de fondo.
    /// </summary>
    private static void PintarBarraDeEstado(StatusStrip barra, Tema tema)
    {
        barra.BackColor = tema.FondoLateral;
        barra.ForeColor = tema.TextoSecundario;
        barra.SizingGrip = false;

        foreach (ToolStripItem item in barra.Items)
        {
            item.ForeColor = tema.TextoSecundario;
            item.BackColor = tema.FondoLateral;

            if (item is ToolStripStatusLabel etiqueta)
            {
                etiqueta.BorderSides = ToolStripStatusLabelBorderSides.None;
            }
        }
    }

    private static bool EsPrimario(string? rol) => string.Equals(rol, RolPrimario, StringComparison.OrdinalIgnoreCase);

    private static bool EsLateral(string? rol) => string.Equals(rol, RolLateral, StringComparison.OrdinalIgnoreCase);

    private static bool EsSutil(string? rol) => string.Equals(rol, RolSutil, StringComparison.OrdinalIgnoreCase);
}
