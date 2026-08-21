using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Core.UI;
using AlgoritmoDevTools.Tools.MarkdownConverter.Services;
using AlgoritmoDevTools.Tools.MarkdownConverter.Views;
using System.Drawing;

namespace AlgoritmoDevTools.Tools.MarkdownConverter;

public sealed class MarkdownConverterTool : ITool
{
    private static readonly Image? _icon =
        IconLoader.LoadEmbedded(typeof(MarkdownConverterTool).Assembly, "icon.ico");

    public string Id => "MarkdownConverter";
    public string DisplayName => "Convertidor a Markdown";
    public string Description => "Convierte ERS, relevamientos y planes (.docx, .html, .pptx) a Markdown para pasarlos a un asistente sin gastar contexto de mas.";
    public Image? Icon => _icon;

    // No usa ToolStorage: el .md se escribe al lado del original, asi que no hay nada que recordar
    // entre sesiones.
    public UserControl CreateView() => new MarkdownConverterView(new DocumentConverter());
}
