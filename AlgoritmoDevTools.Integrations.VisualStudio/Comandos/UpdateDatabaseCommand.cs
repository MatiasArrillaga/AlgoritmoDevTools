using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>Deja en el portapapeles un <c>update-database</c> para el dominio activo.</summary>
[VisualStudioContribution]
internal sealed class UpdateDatabaseCommand : ComandoDeMigracion
{
    public UpdateDatabaseCommand(VisualStudioExtensibility extensibility) : base(extensibility) { }

    public override CommandConfiguration CommandConfiguration => new("Update-Database");

    protected override string Nombre => "Update-Database";

    protected override string Verbo => VerbosDelShell.Update;
}
