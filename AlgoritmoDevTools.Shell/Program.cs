using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.CommandsMaker;
using AlgoritmoDevTools.Tools.SecretsManager;

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
            new SecretsManagerTool()
        };

        Application.Run(new MainForm(tools));
    }
}
