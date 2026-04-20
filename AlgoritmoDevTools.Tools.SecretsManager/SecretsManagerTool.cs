using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.SecretsManager.Views;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.SecretsManager;

public sealed class SecretsManagerTool : ITool
{
    private static readonly Image? _icon =
        IconLoader.LoadEmbedded(typeof(SecretsManagerTool).Assembly, "icon.ico");

    public string Id => "SecretsManager";
    public string DisplayName => "Secrets Manager";
    public string Description => "Gestiona los user-secrets de conexión a SQL de SoftCerealCore.";
    public Image? Icon => _icon;

    public UserControl CreateView()
    {
        var storage = new ToolStorage("Shared");
        var savedConnections = new SavedConnectionsRepository(storage);
        return new SecretsManagerView(savedConnections);
    }
}
