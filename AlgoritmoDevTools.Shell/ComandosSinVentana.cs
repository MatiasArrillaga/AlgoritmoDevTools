using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.CommandsMaker.Dialogs;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Copia al portapapeles un comando de migración pedido desde el menú contextual del explorador,
/// sin abrir la ventana del Shell.
///
/// Acá no se confirma nada ni se avisa el resultado, al revés que en
/// <see cref="SecretosSinVentana"/>: copiar al portapapeles no toca el proyecto ni la base, y el
/// comando ya queda listo para pegar. Las únicas ventanas que aparecen son la que pide el nombre
/// de la migración en Add-Migration, la de elegir dominio, y los errores.
/// </summary>
internal static class ComandosSinVentana
{
    private const string TITULO = "Commands Maker";

    /// <summary>
    /// Mismo storage y mismo repositorio que usa <c>CommandsMakerTool</c>: los dominios son los
    /// mismos. Va calificado porque hay dos clases <c>DomainRepository</c> duplicadas en el repo
    /// (ésta y una en <c>Tools.CommandsMaker.Services</c>) y el nombre suelto es ambiguo.
    /// </summary>
    private static Integrations.SoftCerealCore.DomainRepository AbrirRepositorio()
        => new(new ToolStorage("CommandsMaker"));

    public static void Add(string dominio)
    {
        var sugerido = GeneradorDeComandos.NombreDeMigracionSugerido(dominio);
        var nombre = InputDialog.Show("Nombre de la migración:", TITULO, sugerido)?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) return;

        Copiar(GeneradorDeComandos.AddMigration(dominio, nombre), dominio);
    }

    public static void Remove(string dominio)
        => Copiar(GeneradorDeComandos.RemoveMigration(dominio), dominio);

    public static void Update(string dominio)
        => Copiar(GeneradorDeComandos.UpdateDatabase(dominio), dominio);

    /// <summary>
    /// Cambia el dominio activo del menú. Es el primer paso del flujo: con 16 ítems de presupuesto
    /// no hay forma de listar los 13 dominios por 3 comandos, así que el menú trabaja sobre uno y
    /// esto es lo que lo cambia.
    /// </summary>
    public static void Elegir()
    {
        var repositorio = AbrirRepositorio();
        var dominios = repositorio.GetAll();

        if (dominios.Count == 0)
        {
            MessageBox.Show("No hay dominios cargados. Agregá al menos uno en la pantalla del Commands Maker.",
                TITULO, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialogo = new ElegirDominioDialog(dominios, repositorio.GetUltimoDominio());
        if (dialogo.ShowDialog() != DialogResult.OK) return;

        // Elegir no ejecuta ningún comando: deja el dominio activo y reescribe el menú, para que
        // los tres comandos de abajo pasen a ser los de ese dominio.
        repositorio.RegistrarUso(dialogo.Dominio);
        MenuContextualComandos.Sincronizar(repositorio.GetAll(), repositorio.GetRecientes());
    }

    /// <summary>
    /// Deja el comando en el portapapeles y recuerda el dominio, para que el menú lo muestre
    /// arriba de todo la próxima vez.
    ///
    /// El éxito no se avisa, igual que en la conversión a Markdown: el comando ya está en el
    /// portapapeles y el siguiente paso es pegarlo, así que un cartel sólo agrega un clic. Sí se
    /// avisa el error, que de otro modo pasaría desapercibido y dejaría pegar el comando anterior
    /// creyendo que es el nuevo.
    /// </summary>
    private static void Copiar(string comando, string dominio)
    {
        try
        {
            // copy: true es lo que deja el texto en el portapapeles DESPUÉS de que este proceso
            // termina. Sin eso, el comando se perdería al salir, que es lo que pasa siempre acá:
            // el exe arranca, copia y se cierra enseguida.
            Clipboard.SetDataObject(comando, copy: true);

            var repositorio = AbrirRepositorio();
            repositorio.RegistrarUso(dominio);
            MenuContextualComandos.Sincronizar(repositorio.GetAll(), repositorio.GetRecientes());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo copiar el comando:\n\n{ex.Message}",
                TITULO, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
