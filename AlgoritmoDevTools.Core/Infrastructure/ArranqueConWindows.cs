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
    /// Qué dice la clave Run sobre esta entrada.
    ///
    /// Son tres estados y no dos porque la pregunta "¿arranca con Windows?" y la pregunta "¿arranca
    /// <b>este</b> ejecutable?" tienen respuestas distintas, y confundirlas hace perder la entrada
    /// buena: al abrir el Shell desde <c>bin\Debug</c>, una entrada que apunta al publicado se vería
    /// apagada, y prenderla la pisaría con la de la carpeta de compilación.
    /// </summary>
    public enum EstadoDelArranque
    {
        /// <summary>No hay entrada: no arranca con la sesión.</summary>
        Apagado,

        /// <summary>Hay entrada y levanta el ejecutable que está corriendo ahora.</summary>
        ApuntaAlQueCorre,

        /// <summary>Hay entrada, pero levanta otro binario. <see cref="ArranqueRegistrado.RutaRegistrada"/> dice cuál.</summary>
        ApuntaAOtro
    }

    /// <param name="Estado">En cuál de los tres casos está.</param>
    /// <param name="RutaRegistrada">Lo que tiene escrito la clave Run, o null si no hay entrada.</param>
    public sealed record ArranqueRegistrado(EstadoDelArranque Estado, string? RutaRegistrada)
    {
        /// <summary>Si arranca con la sesión, sea este ejecutable o cualquier otro.</summary>
        public bool EstaPuesto => Estado is not EstadoDelArranque.Apagado;
    }

    /// <summary>
    /// Lee la entrada sin interpretarla: devuelve si está, y si está, si es ésta o es otra.
    /// </summary>
    public static ArranqueRegistrado Leer(string nombre)
    {
        using var clave = Registry.CurrentUser.OpenSubKey(RUTA_RUN);

        if (clave?.GetValue(nombre) is not string valor || string.IsNullOrWhiteSpace(valor))
        {
            return new ArranqueRegistrado(EstadoDelArranque.Apagado, null);
        }

        var exe = ShellIntegration.RutaDelEjecutable();

        var estado = exe is not null && valor.Contains(exe, StringComparison.OrdinalIgnoreCase)
            ? EstadoDelArranque.ApuntaAlQueCorre
            : EstadoDelArranque.ApuntaAOtro;

        return new ArranqueRegistrado(estado, valor);
    }

    /// <summary>
    /// Si la ruta sale de una carpeta de compilación —<c>bin\Debug</c>, <c>bin\x64\Release</c>—.
    ///
    /// Importa porque un residente que corre de ahí mantiene tomados los DLL del proyecto y
    /// cualquier compilación de la solución muere con <c>MSB3027</c>, sin que el error nombre al
    /// culpable.
    ///
    /// El <c>publish</c> queda afuera aunque viva bajo <c>bin\Release</c>: es un ejecutable único y
    /// autocontenido, no toma los DLL del proyecto y es justamente la ubicación recomendada.
    /// </summary>
    public static bool EsRutaDeCompilacion(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return false;

        var partes = ruta.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var bin = Array.FindIndex(partes, parte => string.Equals(parte, "bin", StringComparison.OrdinalIgnoreCase));
        if (bin < 0) return false;

        var despuesDeBin = partes.Skip(bin + 1).ToArray();
        var esPublish = despuesDeBin.Any(parte => string.Equals(parte, "publish", StringComparison.OrdinalIgnoreCase));

        if (esPublish) return false;

        return despuesDeBin.Any(parte =>
            string.Equals(parte, "Debug", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(parte, "Release", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Devuelve null si salió bien, o el mensaje de error.
    ///
    /// Se niega a registrar un ejecutable que corre desde una carpeta de compilación, porque es la
    /// forma de perder sin aviso una entrada que ya apuntaba al publicado. Quien de verdad lo quiera
    /// lo pide con <paramref name="aunqueCorraDesdeCompilacion"/>, pero que sea una decisión y no un
    /// clic distraído en el toggle con el Shell abierto desde Visual Studio.
    /// </summary>
    /// <param name="aunqueCorraDesdeCompilacion">True cuando el usuario ya fue advertido y confirmó.</param>
    public static string? TryActivar(string nombre, string argumentos = "", bool aunqueCorraDesdeCompilacion = false)
    {
        var exe = ShellIntegration.RutaDelEjecutable();
        if (exe is null) return "no se pudo determinar la ruta del ejecutable.";

        if (!aunqueCorraDesdeCompilacion && EsRutaDeCompilacion(exe)) return "este ejecutable corre desde la carpeta de compilación.";

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
