using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>
/// Cambia el dominio sobre el que trabajan los otros tres. No ejecuta ninguna migración: abre el
/// diálogo de DevTools, y al cerrarse los tres ítems de abajo pasan a decir el dominio nuevo.
///
/// Va primero en el menú porque es el paso que habilita a los demás, igual que en el explorador.
/// </summary>
[VisualStudioContribution]
internal sealed class ElegirDominioCommand : ComandoDelCommandsMaker
{
    public ElegirDominioCommand(VisualStudioExtensibility extensibility) : base(extensibility) { }

    public override CommandConfiguration CommandConfiguration => new("Elegir dominio...");

    public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        => EjecutarEnDevToolsAsync(_ => VerbosDelShell.Elegir, cancellationToken);
}
