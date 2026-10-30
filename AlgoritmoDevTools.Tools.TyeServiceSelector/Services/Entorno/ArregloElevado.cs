using System.ComponentModel;
using System.Diagnostics;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>Cómo terminó un arreglo que necesitaba permisos de administrador.</summary>
public enum ResultadoDelArreglo
{
    Hecho,

    /// <summary>El usuario dijo que no en el cartel de Windows. No es un error.</summary>
    Cancelado,

    Fallo
}

/// <summary>
/// Corre un arreglo que necesita una consola de administrador.
///
/// <para>
/// Existe por un caso concreto: cuando Windows le reserva a Hyper-V un tramo de puertos que pisa
/// los que tienen fijados los contenedores de Dapr, éstos dejan de arrancar y la única salida es
/// reiniciar <c>winnat</c>, que sin elevar devuelve «Error de sistema 5. Acceso denegado».
/// </para>
///
/// <para>
/// Pedir la elevación acá y no mandar al usuario a abrir una consola es la diferencia entre
/// «placement está abajo» y destrabarlo con un clic. La aplicación ya se eleva así para la limpieza
/// de disco.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public static class ArregloElevado
{
    public static ResultadoDelArreglo Ejecutar(string comando)
    {
        var inicio = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{comando}\"",
            // Las dos van juntas: la elevación la pide el shell de Windows, que necesita
            // UseShellExecute, y eso impide redirigir la salida. Lo que importa acá es el resultado
            // en el sistema, no lo que el comando haya escrito.
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var proceso = Process.Start(inicio);
            if (proceso is null) return ResultadoDelArreglo.Fallo;

            proceso.WaitForExit();
            return proceso.ExitCode == 0 ? ResultadoDelArreglo.Hecho : ResultadoDelArreglo.Fallo;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // 1223 es el usuario cancelando el cartel de Windows: decidió que no, y está bien.
            return ResultadoDelArreglo.Cancelado;
        }
        catch (Exception)
        {
            return ResultadoDelArreglo.Fallo;
        }
    }
}
