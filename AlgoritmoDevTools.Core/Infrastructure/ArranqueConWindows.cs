using Microsoft.Win32;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Pone o saca el ejecutable de los programas que arrancan con la sesión del usuario.
///
/// Escribe en <c>HKEY_CURRENT_USER\...\Run</c>: aplica sólo a este usuario y no pide permisos de
/// administrador, igual que el menú contextual.
/// </summary>
public static class ArranqueConWindows
{
    private const string RUTA_RUN = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// True si la entrada existe Y apunta al ejecutable que está corriendo ahora. La segunda parte
    /// importa: si se publicó el exe en otra carpeta, la entrada vieja sigue levantando el binario
    /// anterior y conviene mostrarlo como "no activado" para que se regenere.
    /// </summary>
    public static bool EstaActivado(string nombre)
    {
        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return false;

        using var clave = Registry.CurrentUser.OpenSubKey(RUTA_RUN);
        return clave?.GetValue(nombre) is string valor && valor.Contains(exe, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public static string? TryActivar(string nombre, string argumentos = "")
    {
        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return "no se pudo determinar la ruta del ejecutable.";

        try
        {
            using var clave = Registry.CurrentUser.CreateSubKey(RUTA_RUN);
            if (clave is null) return "no se pudo abrir la clave del registro.";

            clave.SetValue(nombre, $"\"{exe}\" {argumentos}".TrimEnd());
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static string? TryDesactivar(string nombre)
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(RUTA_RUN, writable: true);
            clave?.DeleteValue(nombre, throwOnMissingValue: false);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
