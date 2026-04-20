using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.SecretsManager.Views;
using System.Drawing;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.SecretsManager;

public sealed class SecretsManagerTool : ITool
{
    public string Id => "SecretsManager";
    public string DisplayName => "Secrets Manager";
    public string Description => "Gestiona los user-secrets de conexión a SQL de SoftCerealCore.";
    public Image? Icon => null;

    public UserControl CreateView() => new SecretsManagerView();
}
