using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.CommandsMaker;
using AlgoritmoDevTools.Tools.MarkdownConverter;
using AlgoritmoDevTools.Tools.ModelDriftChecker;
using AlgoritmoDevTools.Tools.SecretsManager;
using AlgoritmoDevTools.Tools.SecretsManager.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector;

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

        var tools = new ITool[]
        {
            new CommandsMakerTool(),
            new SecretsManagerTool(),
            new ModelDriftCheckerTool(),
            new TyeServiceSelectorTool(),
            new MarkdownConverterTool()
        };

        Application.Run(new MainForm(tools));
    }

    /// <summary>
    /// Resuelve los argumentos del menu contextual. Devuelve true si se ejecuto una accion y el
    /// proceso tiene que terminar sin abrir la ventana.
    ///
    /// Los verbos de secretos van primero y son explicitos; los archivos quedan al final porque el
    /// Convertidor a Markdown no registra un verbo propio en la linea de comandos: le llega el
    /// archivo directo como "%1".
    /// </summary>
    private static bool EjecutarAccionDirecta(string[] args)
    {
        if (args.Length == 0) return false;

        switch (args[0])
        {
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
        }

        var archivosDeEntrada = args.Where(File.Exists).ToArray();
        if (archivosDeEntrada.Length == 0) return false;

        ConversionSinVentana.Ejecutar(archivosDeEntrada);
        return true;
    }
}
