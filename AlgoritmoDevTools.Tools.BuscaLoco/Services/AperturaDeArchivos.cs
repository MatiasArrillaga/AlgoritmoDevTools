using System.Diagnostics;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>
/// Abre un archivo encontrado, en la línea donde está lo que se buscó.
///
/// Hay dos caminos y cada uno resuelve algo distinto:
///
/// <list type="bullet">
/// <item>
/// <b>Abrir y caer en la línea</b> es lo que uno quiere al tocar un resultado de la línea 2104.
/// Sólo lo puede hacer un editor que acepte la línea por línea de comandos, y por eso se busca
/// Visual Studio Code.
/// </item>
/// <item>
/// <b>Abrir con…</b> muestra el diálogo de Windows para elegir con qué programa. Ahí la línea se
/// pierde —ese diálogo sólo abre el archivo—, así que es el camino de excepción y no el de todos
/// los días: preguntar en cada clic es fricción cuando uno abre diez resultados seguidos.
/// </item>
/// </list>
///
/// <b>Ninguno de los dos necesita diferirse.</b> La trampa de los diálogos modales que cierran la
/// aplicación es de las ventanas que corren en <i>nuestro</i> bucle de mensajes; acá los dos son un
/// proceso aparte, así que el visor nunca entra en reentrada. No hace falta pasarlos por
/// <c>IDialogosDelSistema</c>.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public static class AperturaDeArchivos
{
    /// <summary>
    /// Dónde se busca Visual Studio Code, en orden.
    ///
    /// Se apunta al <c>.exe</c> y no al <c>code.cmd</c> del PATH a propósito: el <c>.cmd</c>
    /// necesita que lo levante un intérprete, y eso abre una ventana de consola que parpadea.
    /// </summary>
    private static readonly string[] DondeEstaVsCode =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Microsoft VS Code\Code.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft VS Code\Code.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft VS Code\Code.exe")
    ];

    /// <summary>Si hay un editor capaz de abrir en una línea puntual.</summary>
    public static bool HayEditorQueEntiendeLaLinea => BuscarVsCode() is not null;

    /// <summary>
    /// Abre el archivo en esa línea si se puede, y si no lo abre con el programa asociado.
    ///
    /// Caer al asociado es una degradación y no una falla: el archivo igual se abre, sólo que al
    /// principio. Devuelve false únicamente cuando no se pudo abrir de ninguna forma.
    /// </summary>
    public static bool AbrirEnLaLinea(string ruta, int linea)
    {
        var vsCode = BuscarVsCode();

        if (vsCode is not null && Correr(vsCode, ["-g", $"{ruta}:{linea}"]))
        {
            return true;
        }

        return AbrirConElAsociado(ruta);
    }

    /// <summary>Abre el archivo con el programa que Windows tenga asociado a su extensión.</summary>
    public static bool AbrirConElAsociado(string ruta)
    {
        try
        {
            // UseShellExecute es lo que hace que Windows resuelva la asociación; sin él, Process
            // espera un ejecutable y un .cs no lo es.
            using var proceso = Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            return proceso is not null || File.Exists(ruta);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Muestra el diálogo «Abrir con» de Windows.
    ///
    /// La ruta va <b>sin comillas</b>: <c>OpenAs_RunDLL</c> toma todo lo que viene después de la
    /// coma como un solo argumento, y las comillas terminan formando parte del nombre que busca.
    /// </summary>
    public static bool AbrirCon(string ruta) =>
        Correr("rundll32.exe", [$"shell32.dll,OpenAs_RunDLL", ruta]);

    /// <summary>Abre el explorador con el archivo ya seleccionado.</summary>
    public static bool MostrarEnLaCarpeta(string ruta) =>
        Correr("explorer.exe", ["/select,", ruta]);

    private static string? BuscarVsCode() => DondeEstaVsCode.FirstOrDefault(File.Exists);

    private static bool Correr(string ejecutable, string[] argumentos)
    {
        try
        {
            var arranque = new ProcessStartInfo(ejecutable) { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argumento in argumentos)
            {
                arranque.ArgumentList.Add(argumento);
            }

            using var proceso = Process.Start(arranque);
            return proceso is not null;
        }
        catch
        {
            return false;
        }
    }
}
