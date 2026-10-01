using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.SecretsManager.Dialogs;
using AlgoritmoDevTools.Tools.SecretsManager.Services;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Ejecuta las acciones de secretos que llegan por línea de comandos desde el menú contextual del
/// explorador, sin abrir la ventana del Shell.
///
/// A diferencia de la conversión a Markdown, acá SIEMPRE se confirma antes y se avisa después.
/// El motivo es que estas acciones son destructivas: <c>ApplySecrets</c> corre
/// <c>dotnet user-secrets clear</c> antes de reescribir, así que un clic por error en el menú deja
/// el proyecto sin secretos. Crear un .md al lado del original no tiene esa consecuencia.
/// </summary>
internal static class SecretosSinVentana
{
    private const string TITULO = "Secretos de SoftCerealCore";

    /// <summary>Mismo storage que usa <c>SecretsManagerTool</c>: las conexiones son las mismas.</summary>
    private static SavedConnectionsRepository AbrirRepositorio() => new(new ToolStorage("Shared"));

    public static void Restaurar()
    {
        if (!Confirmar(
                "Se van a restaurar los user-secrets desde:\n\n" +
                $"    secrets\\{Constantes.DefaultConectionDataFile}\n\n" +
                "Los secretos actuales se reemplazan. ¿Seguir?"))
            return;

        Ejecutar(() =>
        {
            SecretService.Shared.RestoreSecretsFromFile();
            return "Secretos restaurados.";
        });
    }

    public static void Aplicar(int idConexion)
    {
        var repositorio = AbrirRepositorio();
        var conexion = repositorio.GetById(idConexion);

        if (conexion is null)
        {
            Avisar(
                "La conexión que apunta este ítem del menú ya no existe.\n\n" +
                "Abrí el Secrets Manager y volvé a generar el menú contextual.",
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(conexion.BaseEfectiva))
        {
            Avisar("Esa conexión no tiene ninguna base registrada.", MessageBoxIcon.Warning);
            return;
        }

        if (!Confirmar(
                "Los user-secrets Development y DAPR van a apuntar a:\n\n" +
                $"    Servidor:  {conexion.Server}\n" +
                $"    Base:      {conexion.BaseEfectiva}\n\n" +
                "¿Seguir?"))
            return;

        Ejecutar(() =>
        {
            AplicadorDeSecretos.Aplicar(conexion, conexion.BaseEfectiva, repositorio);
            return $"Secretos apuntando a {conexion.Server} - {conexion.BaseEfectiva}.";
        });
    }

    public static void Elegir()
    {
        var repositorio = AbrirRepositorio();

        using var dialogo = new ElegirBaseDialog(repositorio);
        if (dialogo.ShowDialog() != DialogResult.OK) return;
        if (dialogo.Conexion is null) return;

        var conexion = dialogo.Conexion;
        var baseElegida = dialogo.Base;

        Ejecutar(() =>
        {
            AplicadorDeSecretos.Aplicar(conexion, baseElegida, repositorio);
            return $"Secretos apuntando a {conexion.Server} - {baseElegida}.";
        });
    }

    /// <summary>Corre la acción y muestra el resultado, sea el mensaje de éxito o el error.</summary>
    private static void Ejecutar(Func<string> accion)
    {
        try
        {
            var mensaje = accion();
            Avisar(mensaje, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Avisar($"No se pudieron aplicar los secretos:\n\n{ex.Message}", MessageBoxIcon.Error);
        }
    }

    private static bool Confirmar(string mensaje)
        => MessageBox.Show(mensaje, TITULO, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    private static void Avisar(string mensaje, MessageBoxIcon icono)
        => MessageBox.Show(mensaje, TITULO, MessageBoxButtons.OK, icono);
}
