using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>
/// Cambia el dominio sobre el que trabajan los otros tres. No ejecuta ninguna migración: abre el
/// diálogo de DevTools, y al cerrarse los tres ítems de abajo pasan a decir el dominio nuevo.
///
/// <para>
/// <b>El ítem dice cuál es el dominio activo</b> —"Dominio: Cereales"— en vez de un "Elegir
/// dominio..." que no informa nada. Es el mismo encabezado que usa el selector del widget, donde
/// también se lee de un vistazo sobre qué dominio se está trabajando antes de abrir nada.
/// </para>
///
/// Va primero en el menú porque es el paso que habilita a los demás, igual que en el explorador.
/// </summary>
[VisualStudioContribution]
internal sealed class ElegirDominioCommand : ComandoDelCommandsMaker
{
    /// <summary>
    /// Lo que se ve mientras no se pudo averiguar el dominio, que pasa cuando el menú del
    /// explorador no está instalado. Es el texto de siempre: sin dominio que mostrar, al menos
    /// dice qué hace.
    /// </summary>
    private const string SIN_DOMINIO = "Elegir dominio...";

    public ElegirDominioCommand(VisualStudioExtensibility extensibility) : base(extensibility) { }

    public override CommandConfiguration CommandConfiguration => new(SIN_DOMINIO);

    public override Task InitializeAsync(CancellationToken cancellationToken)
    {
        MenuDelExplorador.Cambio += ActualizarTexto;
        ActualizarTexto();
        return Task.CompletedTask;
    }

    public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        => EjecutarEnDevToolsAsync(_ => VerbosDelShell.Elegir, cancellationToken);

    /// <summary>
    /// Igual que en los tres comandos de migración, el texto se arma en ejecución y no en la
    /// configuración: Visual Studio lee esa configuración del manifiesto, antes de cargar la
    /// extensión, y ahí el dominio todavía no se puede consultar.
    /// </summary>
    private void ActualizarTexto()
    {
        var dominio = MenuDelExplorador.Leer()?.Dominio;
        this.DisplayName = dominio is null ? SIN_DOMINIO : $"Dominio: {dominio}";
    }
}
