using AlgoritmoDevTools.Integrations.VisualStudio.Comandos;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace AlgoritmoDevTools.Integrations.VisualStudio;

/// <summary>
/// Punto de entrada de la extensión y definición del menú, que cuelga del clic derecho de la
/// <b>Consola del Administrador de paquetes</b>:
///
/// <code>
/// Commands Maker ▸
///     Elegir dominio...
///     ────────────────
///     Add-Migration (Cereales)
///     Remove-Migration (Cereales)
///     Update-Database (Cereales)
/// </code>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Integrations.VisualStudio/README.referencia-tecnica-funcional-menu-visual-studio.md
/// </summary>
[VisualStudioContribution]
internal class ExtensionEntrypoint : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "AlgoritmoDevTools.VisualStudio.5c9e1b7a-3d42-4f18-9a6c-0b2e8d71f403",
            version: this.ExtensionAssemblyVersion,
            publisherName: "Algoritmo",
            displayName: "Algoritmo DevTools",
            description: "Los comandos de migración del Commands Maker, en el clic derecho de la Consola del Administrador de paquetes."),
    };
}

/// <summary>
/// Los menús se declaran como propiedades estáticas, al revés que los comandos: el atributo va en la
/// propiedad y no en la clase.
///
/// <para>
/// <b>Dónde se ancla y por qué ahí.</b> El destino de estos comandos es la Package Manager Console:
/// lo que generan se pega ahí. Pero <b>la consola no tiene menú contextual propio</b> — está
/// hosteada sobre un <c>IVsTextView</c> (ver <c>PowerConsoleToolWindow.VsTextView</c> en
/// NuGet.Client), así que muestra el menú del editor. Por eso el anclaje es al grupo de edición del
/// editor y no a nada de NuGet: no hay nada de NuGet a lo que anclarse.
/// </para>
///
/// <para>
/// <b>Consecuencia: el menú sale también en el clic derecho de los archivos de código.</b> No es un
/// descuido ni se puede evitar — la consola muestra <b>el mismo</b> menú que el editor, el que trae
/// Run Test(s), las refactorizaciones y los breakpoints, no uno parecido. Separarlos con
/// <c>VisibleWhen</c> ya se intentó y no funciona; el porqué está en
/// <see cref="ComandoDelCommandsMaker"/>, con una advertencia para no reintentarlo.
/// </para>
/// </summary>
internal static class MenusDeDevTools
{
    /// <summary>
    /// Identificador del menú principal del shell de Visual Studio (<c>guidSHLMainMenu</c> en
    /// <c>vsshlids.h</c>), dueño de los menús contextuales del IDE.
    /// </summary>
    private static readonly Guid MenuPrincipalDelShell = new("d309f791-903f-11d0-9efc-00a0c911004f");

    /// <summary>
    /// <c>IDM_VS_CTXT_CODEWIN</c>: el <b>menú</b> contextual del editor de código, que es el que
    /// dibuja también la consola.
    ///
    /// Ojo con la diferencia, que es la que define dónde termina cayendo el ítem: los ids de
    /// <c>vsshlids.h</c> que empiezan con <c>IDM_</c> son <b>menús</b> y los que empiezan con
    /// <c>IDG_</c> son <b>grupos</b> —las secciones separadas por líneas de adentro de un menú—.
    /// Colgarse de un grupo ajeno mete el ítem <i>dentro</i> de esa sección: anclado a
    /// <c>IDG_VS_CODEWIN_TEXTEDIT</c> (0x01C0) salía entre Cortar y Copiar, y anclado al 518 del
    /// ejemplo oficial —que es <c>IDG_VS_CTXT_PROJECT_BUILD</c>— salía entre Compilar y Recompilar.
    ///
    /// Para tener sección propia hay que aportar un grupo propio y colgarlo del MENÚ, que es lo que
    /// hace <see cref="GrupoDeDevTools"/>.
    /// </summary>
    private const uint MENU_DEL_EDITOR = 0x040D;

    /// <summary>
    /// Grupo propio dentro del menú del editor. Existe sólo para que "Commands Maker" quede en su
    /// propia sección, con línea separadora arriba y abajo, como "Acciones rápidas y
    /// refactorizaciones", en vez de mezclado con los ítems de otra sección.
    /// </summary>
    [VisualStudioContribution]
    public static CommandGroupConfiguration GrupoDeDevTools =>
        // La prioridad decide a qué altura del menú cae la sección, y es el único número a tocar
        // para moverla. Visual Studio no publica las de sus propios grupos, así que se ajusta por
        // ensayo; lo medido hasta ahora, de abajo hacia arriba:
        //
        //     0x0600  ->  último de todo, debajo de "Anotación"
        //     0x0200  ->  cuarto bloque, entre "Ejecutar pruebas" y "Punto de interrupción"
        //     0x0150  ->  el actual: pegado a "Acciones rápidas", que es donde se lo quiere
        new(GroupPlacement.VsctParent(MenuPrincipalDelShell, id: MENU_DEL_EDITOR, priority: 0x0150))
        {
            Children = [GroupChild.Menu(MenuCommandsMaker)],
        };

    /// <summary>
    /// El menú no declara <c>Placements</c>: lo ubica <see cref="GrupoDeDevTools"/>, que es quien
    /// le da la sección propia.
    ///
    /// Tampoco lleva icono, y no por una decisión de estilo: <c>MenuConfiguration</c> no expone
    /// <c>Icon</c>. Sus únicas propiedades son <c>Children</c>, <c>DisplayName</c>, <c>Id</c>,
    /// <c>Placements</c>, <c>TooltipText</c> y <c>Type</c>.
    /// </summary>
    [VisualStudioContribution]
    public static MenuConfiguration MenuCommandsMaker => new("Commands Maker")
    {
        Children =
        [
            MenuChild.Command<ElegirDominioCommand>(),
            MenuChild.Separator,
            MenuChild.Command<AddMigrationCommand>(),
            MenuChild.Command<RemoveMigrationCommand>(),
            MenuChild.Command<UpdateDatabaseCommand>(),
        ],
    };
}
