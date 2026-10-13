using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.CommandsMaker;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using AlgoritmoDevTools.Tools.MarkdownConverter;
using AlgoritmoDevTools.Tools.ModelDriftChecker;
using AlgoritmoDevTools.Tools.SecretsManager;
using AlgoritmoDevTools.Tools.SecretsManager.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector;
using AlgoritmoDevTools.UI.Servicios;

namespace AlgoritmoDevTools.Shell;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // El menu contextual del explorador invoca este mismo exe con argumentos. Se resuelve la
        // accion y se sale sin abrir la ventana: levantar el Shell entero para una accion suelta es
        // pesado y molesto.
        if (EjecutarAccionDirecta(args)) return;

        // La ventana HTML es la de siempre a partir de ahora. La clasica sigue disponible con
        // --clasico mientras las herramientas terminan de migrar, porque todavia viven ahi.
        Application.Run(new MainFormBlazor());
    }

    /// <summary>Arma las herramientas que se alojan en la ventana clasica.</summary>
    private static ITool[] CrearHerramientas() =>
    [
        new CommandsMakerTool(),
        new SecretsManagerTool(),
        new ModelDriftCheckerTool(),
        new TyeServiceSelectorTool(),
        new MarkdownConverterTool()
    ];

    /// <summary>
    /// Resuelve los argumentos del menu contextual. Devuelve true si se ejecuto una accion y el
    /// proceso tiene que terminar sin abrir la ventana.
    ///
    /// Los verbos de secretos van primero y son explicitos; los archivos quedan al final porque el
    /// Convertidor a Markdown no registra un verbo propio en la linea de comandos: le llega el
    /// archivo directo como "%1".
    /// </summary>
    /// <summary>Levanta el widget de bandeja en vez de la ventana principal.</summary>
    public const string VerboWidget = "--widget";

    /// <summary>
    /// Abre la ventana clasica de Windows Forms. Sigue siendo la unica que tiene las herramientas
    /// completas mientras cada una no tenga su pantalla en HTML.
    /// </summary>
    public const string VerboClasico = "--clasico";

    /// <summary>
    /// Abre la ventana HTML. Es lo que hace el arranque sin argumentos; queda como verbo explicito
    /// para los accesos directos que ya lo usan.
    /// </summary>
    public const string VerboHtml = "--html";

    private static bool EjecutarAccionDirecta(string[] args)
    {
        if (args.Length == 0) return false;

        switch (args[0])
        {
            case VerboWidget:
                EjecutarWidget();
                return true;

            case VerboHtml:
                Application.Run(new MainFormBlazor());
                return true;

            case VerboClasico:
                Application.Run(new MainForm(CrearHerramientas()));
                return true;

            case MenuContextualSecretos.VerboRestaurar:
                SecretosSinVentana.Restaurar();
                return true;

            case MenuContextualSecretos.VerboAplicar:
                if (args.Length > 1 && int.TryParse(args[1], out var idConexion))
                    SecretosSinVentana.Aplicar(idConexion);
                else
                    MessageBox.Show("Falta el identificador de la conexion. Volve a generar el menu contextual desde el Secrets Manager.",
                        "Secretos de SoftCerealCore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;

            case MenuContextualSecretos.VerboElegir:
                SecretosSinVentana.Elegir();
                return true;

            case MenuContextualComandos.VerboAdd:
                EjecutarPorDominio(args, ComandosSinVentana.Add);
                return true;

            case MenuContextualComandos.VerboRemove:
                EjecutarPorDominio(args, ComandosSinVentana.Remove);
                return true;

            case MenuContextualComandos.VerboUpdate:
                EjecutarPorDominio(args, ComandosSinVentana.Update);
                return true;

            case MenuContextualComandos.VerboElegir:
                ComandosSinVentana.Elegir();
                return true;
        }

        var archivosDeEntrada = args.Where(File.Exists).ToArray();
        if (archivosDeEntrada.Length == 0) return false;

        ConversionSinVentana.Ejecutar(archivosDeEntrada);
        return true;
    }

    /// <summary>
    /// Corre el widget de bandeja, uno solo por usuario. Si ya hay uno, no abre un segundo icono:
    /// avisa y sale, porque dos widgets haciendo lo mismo solo confunden.
    /// </summary>
    private static void EjecutarWidget()
    {
        using var mutex = new Mutex(initiallyOwned: true, ArranqueDelWidget.MutexDeInstanciaUnica, out var esElPrimero);
        if (!esElPrimero)
        {
            MessageBox.Show("El widget ya esta corriendo: miralo en la bandeja, al lado del reloj.",
                "Algoritmo DevTools", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new Widget.WidgetContext());

        // El mutex se libera al salir del using, no antes: mientras el widget corre, tiene que
        // seguir tomado para que el proximo intento lo vea.
        GC.KeepAlive(mutex);
    }

    /// <summary>
    /// Los verbos del Commands Maker llevan el dominio como segundo argumento. Si falta, el menu
    /// quedo escrito por una version vieja o a mano: lo unico util es decir que se regenere.
    /// </summary>
    private static void EjecutarPorDominio(string[] args, Action<string> accion)
    {
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            accion(args[1]);
            return;
        }

        MessageBox.Show(
            "Falta el dominio. Volve a generar el menu contextual desde el Commands Maker.",
            "Commands Maker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
