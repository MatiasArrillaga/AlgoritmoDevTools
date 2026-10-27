using Microsoft.VisualStudio.Extensibility;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>
/// Los tres que generan un comando de EF Core sobre el dominio activo: el texto del ítem lleva el
/// dominio entre paréntesis, igual que en el explorador.
///
/// El texto se arma en <see cref="InitializeAsync"/> y no en la configuración del comando porque
/// Visual Studio lee esa configuración del manifiesto, antes de cargar la extensión: ahí el dominio
/// todavía no se puede consultar. Lo que queda compilado es el nombre pelado («Add-Migration») y
/// recién al cargar se le agrega el dominio.
/// </summary>
internal abstract class ComandoDeMigracion : ComandoDelCommandsMaker
{
    protected ComandoDeMigracion(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
    }

    /// <summary>El comando de EF Core, tal como se muestra: <c>Add-Migration</c>.</summary>
    protected abstract string Nombre { get; }

    /// <summary>Con qué se relanza DevTools. Uno de <see cref="VerbosDelShell"/>.</summary>
    protected abstract string Verbo { get; }

    public override Task InitializeAsync(CancellationToken cancellationToken)
    {
        MenuDelExplorador.Cambio += ActualizarTexto;
        ActualizarTexto();
        return Task.CompletedTask;
    }

    public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        => EjecutarEnDevToolsAsync(entrada => $"{Verbo} {entrada.Dominio}", cancellationToken);

    /// <summary>
    /// Sin el menú del explorador instalado queda el nombre pelado: el ítem igual se muestra, y al
    /// usarlo explica qué falta. Es preferible a esconderlo, que no daría ninguna pista.
    /// </summary>
    private void ActualizarTexto()
    {
        var dominio = MenuDelExplorador.Leer()?.Dominio;
        this.DisplayName = dominio is null ? Nombre : $"{Nombre} ({dominio})";
    }
}
