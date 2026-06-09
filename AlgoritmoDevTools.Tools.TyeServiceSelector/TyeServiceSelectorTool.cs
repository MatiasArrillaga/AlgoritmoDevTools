using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Views;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector;

public sealed class TyeServiceSelectorTool : ITool
{
    private static readonly Image? _icon =
        IconLoader.LoadEmbedded(typeof(TyeServiceSelectorTool).Assembly, "icon.ico");

    public string Id => "TyeServiceSelector";
    public string DisplayName => "Selector de Servicios (Tye)";
    public string Description => "Elegí qué microservicios levantar y generá un tye.devtools.yaml para correr con tye run --watch.";
    public Image? Icon => _icon;

    public UserControl CreateView()
    {
        var profiles = new ProfileRepository(new ToolStorage(Id));
        return new TyeServiceSelectorView(profiles);
    }
}
