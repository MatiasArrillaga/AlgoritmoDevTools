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

        var tools = new ITool[]
        {
            new CommandsMakerTool(),
            new SecretsManagerTool(),
            new ModelDriftCheckerTool(),
            new TyeServiceSelectorTool(),
            new MarkdownConverterTool()
        };

        // Los argumentos llegan cuando el .exe se invoca desde el menu contextual del explorador.
        // Se filtran los que sean archivos existentes: el resto se ignora en silencio.
        var archivos = args.Where(File.Exists).ToArray();

        Application.Run(new MainForm(tools, archivos));
    }
}
