using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.CommandsMaker;
using AlgoritmoDevTools.Tools.MarkdownConverter;
using AlgoritmoDevTools.Tools.ModelDriftChecker;
using AlgoritmoDevTools.Tools.SecretsManager;
using AlgoritmoDevTools.Tools.TyeServiceSelector;

namespace AlgoritmoDevTools.Shell;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Con archivos por linea de comandos (menu contextual del explorador) se convierte y se
        // sale, sin abrir la ventana: abrir el Shell entero para un archivo es pesado y molesto.
        var archivosDeEntrada = args.Where(File.Exists).ToArray();
        if (archivosDeEntrada.Length > 0)
        {
            ConversionSinVentana.Ejecutar(archivosDeEntrada);
            return;
        }

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
}
