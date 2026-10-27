using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>Deja en el portapapeles un <c>remove-migration -force</c> para el dominio activo.</summary>
[VisualStudioContribution]
internal sealed class RemoveMigrationCommand : ComandoDeMigracion
{
    public RemoveMigrationCommand(VisualStudioExtensibility extensibility) : base(extensibility) { }

    public override CommandConfiguration CommandConfiguration => new("Remove-Migration");

    protected override string Nombre => "Remove-Migration";

    protected override string Verbo => VerbosDelShell.Remove;
}
