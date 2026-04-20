using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.CommandsMaker.Views;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.CommandsMaker;

public sealed class CommandsMakerTool : ITool
{
    private static readonly Image? _icon =
        IconLoader.LoadEmbedded(typeof(CommandsMakerTool).Assembly, "icon.ico");

    public string Id => "CommandsMaker";
    public string DisplayName => "Commands Maker";
    public string Description => "Genera comandos de migraciones EF Core (add, remove, update) por dominio.";
    public Image? Icon => _icon;

    public UserControl CreateView()
    {
        var storage = new ToolStorage(Id);
        var repository = new DomainRepository(storage);
        return new CommandsMakerView(repository);
    }
}
