using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace AlgoritmoDevTools.Tools.MarkdownConverter.Services;

/// <summary>
/// Agrega o quita "Convertir a Markdown" del menu contextual del explorador.
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

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNF_IDLIST = 0x0000;

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

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
        var exe = RutaDelEjecutable();
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

            AvisarAlExplorador();
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

            AvisarAlExplorador();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string RutaDelVerbo(string extension)
        => $@"{BASE_CLASSES}\{extension}\shell\{VERBO}";

    /// <summary>
    /// En un publish de archivo unico el ensamblado se extrae a una carpeta temporal, asi que
    /// Assembly.Location viene vacio. ProcessPath devuelve el .exe real, que es lo que hay que
    /// registrar.
    /// </summary>
    private static string? RutaDelEjecutable() => Environment.ProcessPath;

    /// <summary>
    /// Sin esto el menu contextual puede tardar en reflejar el cambio hasta reiniciar el explorador.
    /// </summary>
    private static void AvisarAlExplorador()
        => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
}
