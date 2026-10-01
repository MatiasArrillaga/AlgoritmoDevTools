using AlgoritmoDevTools.Core.Infrastructure;
using Microsoft.Win32;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Services;

/// <summary>
/// Agrega o quita "Convertir a Markdown" del menu contextual del explorador.
///
/// Las primitivas comunes (ruta del exe, aviso al explorador, menu clasico de Windows 11) viven en
/// <see cref="ShellIntegration"/>, que comparte con el menu del Secrets Manager.
///
/// Escribe en HKEY_CURRENT_USER y no en HKEY_CLASSES_ROOT: aplica solo a este usuario y por lo
/// tanto NO necesita permisos de administrador. Usa SystemFileAssociations, que es la clave prevista
/// para agregar verbos a una extension sin tocar el ProgID (o sea, sin pisar la asociacion de Word
/// o de Excel, que ademas cambia segun lo que este instalado en cada maquina).
/// </summary>
public static class MenuContextual
{
    private const string VERBO = "ConvertirAMarkdown";
    private const string TEXTO_DEL_MENU = "Convertir a Markdown";
    private const string BASE_CLASSES = @"Software\Classes\SystemFileAssociations";

    /// <summary>
    /// True si el verbo ya esta registrado para la primera extension de la lista. Alcanza con esa:
    /// se instalan y se quitan todas juntas.
    /// </summary>
    public static bool EstaInstalado(IEnumerable<string> extensiones)
    {
        var primera = extensiones.FirstOrDefault();
        if (primera is null) return false;

        using var clave = Registry.CurrentUser.OpenSubKey(RutaDelVerbo(primera));
        return clave is not null;
    }

    /// <summary>
    /// Registra el verbo para cada extension, apuntando al ejecutable que esta corriendo. Devuelve
    /// null si salio bien, o el mensaje de error.
    /// </summary>
    public static string? TryInstalar(IEnumerable<string> extensiones)
    {
        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return "no se pudo determinar la ruta del ejecutable.";

        try
        {
            foreach (var extension in extensiones)
            {
                using var verbo = Registry.CurrentUser.CreateSubKey(RutaDelVerbo(extension));
                if (verbo is null) return $"no se pudo crear la clave para {extension}.";

                verbo.SetValue(null, TEXTO_DEL_MENU);
                verbo.SetValue("Icon", exe + ",0");

                using var comando = verbo.CreateSubKey("command");
                comando?.SetValue(null, $"\"{exe}\" \"%1\"");
            }

            ShellIntegration.AvisarAlExplorador();
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return "el registro no permitio la escritura (revisar politicas del equipo).";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Quita el verbo de todas las extensiones. Devuelve null si salio bien, o el mensaje de error.
    /// </summary>
    public static string? TryDesinstalar(IEnumerable<string> extensiones)
    {
        try
        {
            foreach (var extension in extensiones)
            {
                Registry.CurrentUser.DeleteSubKeyTree(RutaDelVerbo(extension), throwOnMissingSubKey: false);
            }

            ShellIntegration.AvisarAlExplorador();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string RutaDelVerbo(string extension)
        => $@"{BASE_CLASSES}\{extension}\shell\{VERBO}";
}
