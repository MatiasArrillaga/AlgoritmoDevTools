using System.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;

namespace AlgoritmoDevTools.Integrations.VisualStudio.Comandos;

/// <summary>
/// Lo común a los cuatro ítems: lanzar DevTools con un verbo y esperar a que termine.
///
/// <para>
/// <b>Por qué se lanza el ejecutable en vez de hacer el trabajo acá.</b> La extensión corre fuera
/// de devenv, en net8.0-windows, y no puede referenciar los proyectos de la solución
/// (net10.0-windows). Pero tampoco hace falta: el Shell ya resuelve estos verbos sin abrir ventana
/// —arma el comando, lo deja en el portapapeles y reescribe el menú del explorador—, así que acá no
/// se duplica nada. El día que cambie cómo se arma un comando de migración, cambia en un solo lado.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Integrations.VisualStudio/README.referencia-tecnica-funcional-menu-visual-studio.md
/// </summary>
internal abstract class ComandoDelCommandsMaker : Command
{
    // NO reintentar limitar estos comandos a la consola con VisibleWhen y el content type
    // "PackageConsole": ya se probó y NO funciona. ActivationConstraint.ClientContext con
    // ClientContextKey.Shell.ActiveEditorContentType mira el DOCUMENTO ACTIVO DEL EDITOR, no la
    // ventana que tiene el foco. Con el cursor dentro de la consola, el documento activo sigue
    // siendo el archivo abierto, así que el valor es el de ese archivo ("CSharp") y la condición
    // nunca da verdadera: el resultado es que los comandos no aparecen en NINGÚN lado, ni en la
    // consola ni en el editor, y sin ningún error que lo explique.
    //
    // No hay forma de separarlos: la consola muestra el MISMO menú contextual que el editor de
    // código, con tests, refactorizaciones y breakpoints incluidos. Ver la sección 5 del README.
    //
    // Y por la misma razón, NO reintentar que el comando se escriba solo en la consola con
    // context.GetActiveTextViewAsync() + Extensibility.Editor().EditAsync(): se probó en la 1.4.0 y
    // NO pega nada. Esa API devuelve la vista activa del EDITOR, que con el foco en la consola
    // sigue siendo el archivo abierto detrás. Son dos evidencias independientes de lo mismo: el
    // modelo fuera-de-proceso no ve la consola como editor. Ver la sección 8 del README, que
    // conserva el código por si algún día cambia.

    private const string FALTA_EL_MENU =
        "El Commands Maker todavía no está en el menú contextual de Windows.\n\n" +
        "Abrí Algoritmo DevTools, entrá al Commands Maker e instalá el menú contextual: " +
        "esta extensión lee de ahí el dominio activo y la ubicación del ejecutable.";

    protected ComandoDelCommandsMaker(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
    }

    /// <summary>
    /// Corre DevTools y espera. Se espera a propósito: los verbos del Commands Maker terminan
    /// dejando algo en el portapapeles o abriendo un diálogo modal, y hasta que el proceso no sale
    /// no se sabe si el dominio activo cambió.
    /// </summary>
    protected async Task EjecutarEnDevToolsAsync(Func<EntradaDelMenu, string> argumentos, CancellationToken cancellationToken)
    {
        var entrada = MenuDelExplorador.Leer();
        if (entrada is null)
        {
            await AvisarAsync(FALTA_EL_MENU, cancellationToken);
            return;
        }

        try
        {
            var arranque = new ProcessStartInfo(entrada.Ejecutable, argumentos(entrada)) { UseShellExecute = true };

            using var proceso = Process.Start(arranque);
            if (proceso is null)
            {
                await AvisarAsync("No se pudo iniciar Algoritmo DevTools.", cancellationToken);
                return;
            }

            await proceso.WaitForExitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await AvisarAsync($"No se pudo ejecutar la acción:\n\n{ex.Message}", cancellationToken);
            return;
        }

        // El dominio activo puede haber cambiado —seguro si fue "Elegir dominio", posiblemente si
        // fue uno de los tres, porque el Shell registra el uso—, así que los ítems rehacen su texto.
        MenuDelExplorador.NotificarCambio();
    }

    protected Task AvisarAsync(string mensaje, CancellationToken cancellationToken)
        => this.Extensibility.Shell().ShowPromptAsync(mensaje, PromptOptions.OK, cancellationToken);
}
