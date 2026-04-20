using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.ModelDriftChecker.Services;
using AlgoritmoDevTools.Tools.ModelDriftChecker.Views;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.ModelDriftChecker;

public sealed class ModelDriftCheckerTool : ITool
{
    private static readonly Image? _icon =
        IconLoader.LoadEmbedded(typeof(ModelDriftCheckerTool).Assembly, "icon.ico");

    public string Id => "ModelDriftChecker";
    public string DisplayName => "Schema Change Detector";
    public string Description => "Detecta si los commits traídos desde la última migración requieren correr migraciones.";
    public Image? Icon => _icon;

    public UserControl CreateView()
    {
        var storage = new ToolStorage(Id);
        var baselines = new BaselineRepository(storage);
        return new ModelDriftCheckerView(baselines);
    }
}
