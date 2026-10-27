using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>
/// Deja en el portapapeles un <c>add-migration</c> para el dominio activo, con un nombre de
/// migración que el Shell arma solo a partir de la fecha y hora. No pregunta nada.
/// </summary>
[VisualStudioContribution]
internal sealed class AddMigrationCommand : ComandoDeMigracion
{
    public AddMigrationCommand(VisualStudioExtensibility extensibility) : base(extensibility) { }

    public override CommandConfiguration CommandConfiguration => new("Add-Migration");

    protected override string Nombre => "Add-Migration";

    protected override string Verbo => VerbosDelShell.Add;
}
