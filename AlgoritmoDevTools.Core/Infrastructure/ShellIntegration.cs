using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Primitivas compartidas para integrarse con el menú contextual del explorador: dónde está el
/// ejecutable, cómo avisarle al explorador que el registro cambió, y el menú clásico de Windows 11.
///
/// Vive en Core porque la usan dos tools (el Convertidor a Markdown, que registra verbos por
/// extensión de archivo, y el Secrets Manager, que registra un submenú en el fondo de carpeta).
/// Todo se escribe en HKEY_CURRENT_USER: aplica sólo a este usuario y no pide permisos de
/// administrador.
/// </summary>
public static class ShellIntegration
{
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNF_IDLIST = 0x0000;

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    /// <summary>
    /// En un publish de archivo único el ensamblado se extrae a una carpeta temporal, así que
    /// Assembly.Location viene vacío. ProcessPath devuelve el .exe real, que es lo que hay que
    /// registrar.
    /// </summary>
    public static string? RutaDelEjecutable() => Environment.ProcessPath;

    /// <summary>
    /// Sin esto el menú contextual puede tardar en reflejar el cambio hasta reiniciar el explorador.
    /// </summary>
    public static void AvisarAlExplorador()
        => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

    // ---------------------------------------------------------------------
    // Menú clásico de Windows 11
    // ---------------------------------------------------------------------

    // Windows 11 manda los verbos clásicos del registro (los que usan estas tools y cualquier .reg)
    // a "Mostrar más opciones". Registrar este CLSID vacío desactiva el menú nuevo y devuelve el
    // clásico completo, donde la opción aparece directo. Es el mismo CLSID que usa el propio
    // explorador para el menú moderno.
    private const string CLSID_MENU_NUEVO_RAIZ = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
    private const string CLSID_MENU_NUEVO = CLSID_MENU_NUEVO_RAIZ + @"\InprocServer32";

    /// <summary>
    /// True si Windows 11 o posterior. En Windows 10 el menú clásico ya es el default y no hay nada
    /// que tocar. La build 22000 es la primera de Windows 11.
    /// </summary>
    public static bool EsWindows11OPosterior()
        => Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

    public static bool MenuClasicoActivado()
    {
        using var clave = Registry.CurrentUser.OpenSubKey(CLSID_MENU_NUEVO);
        return clave is not null;
    }

    /// <summary>
    /// Devuelve el menú contextual clásico de Windows 10. Afecta a TODO el sistema, no sólo a estas
    /// opciones: el que lo active lo tiene que saber. Requiere reiniciar el explorador.
    /// </summary>
    public static string? TryActivarMenuClasico()
    {
        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(CLSID_MENU_NUEVO);
            if (clave is null) return "no se pudo crear la clave del registro.";

            // El valor vacío es lo que desactiva el menú nuevo: le dice al explorador que la DLL
            // que lo dibuja está en ninguna parte.
            clave.SetValue(null, string.Empty);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static string? TryDesactivarMenuClasico()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(CLSID_MENU_NUEVO_RAIZ, throwOnMissingSubKey: false);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Reinicia el explorador para que el cambio de menú tome efecto. Windows lo vuelve a levantar
    /// solo.
    /// </summary>
    public static void ReiniciarExplorador()
    {
        foreach (var proceso in System.Diagnostics.Process.GetProcessesByName("explorer"))
        {
            try
            {
                proceso.Kill();
            }
            catch
            {
                // Si no se puede matar uno, se sigue con los demás.
            }
        }
    }
}
