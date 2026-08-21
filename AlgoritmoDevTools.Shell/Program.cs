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
    static void Main()
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

        Application.Run(new MainForm(tools));
    }
}
