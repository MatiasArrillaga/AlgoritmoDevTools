using System.Diagnostics;

using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>
/// Arranca tye desde la aplicación.
///
/// <para>
/// <b>Va en su propia ventana de consola, no adentro.</b> La aplicación no tiene forma de mostrar
/// la salida de un proceso en vivo, y construirla sólo para esto sería caro: tye ya escribe sus
/// logs en una consola y en Seq, que es donde uno los busca. Como además queda suelto y no colgado
/// de la aplicación, cerrar DevTools no se lleva puesto el entorno.
/// </para>
///
/// <para>
/// Por lo mismo no se usa <see cref="ProcessRunner"/>: ése espera a que el proceso termine, y tye
/// no termina — se queda corriendo hasta que lo frenan.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public sealed class LanzadorDeTye
{
    /// <summary>
    /// Dos instancias de tye con los mismos servicios se pelean por los puertos y se pisan los
    /// contenedores. Antes de arrancar hay que saber si ya hay una.
    /// </summary>
    public static bool HayTyeCorriendo() => Process.GetProcessesByName("tye").Length > 0;

    /// <summary>
    /// Cierra las ventanas de consola de tye que hayan quedado abiertas, y devuelve cuántas cerró.
    ///
    /// <para>
    /// <b>Por qué hace falta.</b> El frenado se pide por la API del panel (<c>DELETE /api/v1/control</c>),
    /// que baja los servicios pero no siempre termina el proceso de tye: la consola se queda abierta
    /// mostrando el último log, el usuario la ve viva y no sabe si el entorno bajó o no. Como la barra
    /// ya esperó a que no quede ningún servicio, llegado este punto no hay nada que esa consola esté
    /// haciendo.
    /// </para>
    ///
    /// <para>
    /// <b>Primero por las buenas.</b> <c>CloseMainWindow</c> le manda el cierre a la ventana y deja que
    /// tye haga su salida ordenada; sólo si en dos segundos no se fue se lo mata con todo su árbol.
    /// Al revés —matar de entrada— se pierde la limpieza propia de tye y quedan más huérfanos para el
    /// <see cref="LimpiadorDelEntorno"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Se cierran todas.</b> Cuando se suma un servicio en caliente hay dos instancias de tye
    /// corriendo. El frenado baja los dos paneles, así que ninguna de las dos consolas tiene por qué
    /// seguir.
    /// </para>
    /// </summary>
    public static int CerrarLasConsolas()
    {
        var cerradas = 0;

        foreach (var proceso in ConsolasAbiertas())
        {
            using (proceso)
            {
                try
                {
                    if (proceso.HasExited) continue;

                    if (!proceso.CloseMainWindow() || !proceso.WaitForExit(2000))
                    {
                        proceso.Kill(entireProcessTree: true);
                        proceso.WaitForExit(2000);
                    }

                    cerradas++;
                }
                catch (Exception)
                {
                    // Terminó solo entre que lo enumeramos y lo quisimos cerrar, o es de otra sesión
                    // de Windows y no tenemos permiso: en los dos casos no hay nada que hacer.
                }
            }
        }

        ConfiguracionDevTools.Compartida.PidDeTye = null;
        ConfiguracionDevTools.Compartida.Guardar();

        return cerradas;
    }

    /// <summary>
    /// Los procesos de tye a cerrar: el que lanzamos nosotros —anotado al levantar— y, por las
    /// dudas, cualquier otro que se llame así.
    ///
    /// <para>
    /// El PID anotado está porque el nombre del proceso no es una garantía: <c>tye</c> se instala
    /// como herramienta global de dotnet y basta con que esa resolución cambie —un shim, un
    /// <c>dotnet tye</c>— para que <see cref="Process.GetProcessesByName(string)"/> no encuentre
    /// nada y la consola quede abierta sin que nadie se entere.
    /// </para>
    /// </summary>
    private static IEnumerable<Process> ConsolasAbiertas()
    {
        var vistos = new HashSet<int>();

        if (ConfiguracionDevTools.Compartida.PidDeTye is int anotado)
        {
            Process? elNuestro = null;
            try
            {
                elNuestro = Process.GetProcessById(anotado);
            }
            catch (ArgumentException)
            {
                // Ya no existe: terminó solo, o la máquina se reinició desde la última vez.
            }

            if (elNuestro is not null && vistos.Add(anotado)) yield return elNuestro;
        }

        foreach (var proceso in Process.GetProcessesByName("tye"))
        {
            if (vistos.Add(proceso.Id)) yield return proceso;
            else proceso.Dispose();
        }
    }

    /// <summary>
    /// Levanta un yaml de tye y devuelve el proceso de la consola, o null si no se pudo arrancar.
    /// </summary>
    /// <param name="rutaDelYaml">Ruta del yaml, relativa a la raíz de AlgoritmoCore o absoluta.</param>
    /// <param name="puertoDelPanel">Dónde abre el panel. El principal usa el 8000.</param>
    /// <param name="sinCompilar">
    /// Con <c>--no-build</c>. Es obligatorio cuando ya hay otro tye arriba: compilar vuelve a generar
    /// las bibliotecas compartidas y eso mata los procesos del entorno que ya estaba corriendo.
    /// </param>
    public Process? Levantar(string rutaDelYaml, int puertoDelPanel, bool sinCompilar)
    {
        var raiz = RutasDeTrabajo.AlgoritmoCore;
        if (!Directory.Exists(raiz)) return null;

        var argumentos = $"run \"{rutaDelYaml}\" --watch --port {puertoDelPanel}";
        if (sinCompilar) argumentos += " --no-build";

        var inicio = new ProcessStartInfo
        {
            FileName = "tye",
            Arguments = argumentos,
            WorkingDirectory = raiz,
            // Con esto Windows le abre su propia ventana y el proceso queda independiente del nuestro.
            UseShellExecute = true
        };

        Process? proceso;

        try
        {
            proceso = Process.Start(inicio);
        }
        catch (Exception)
        {
            // Tye se instala como herramienta global de dotnet; si no está, no hay nada que lanzar.
            return null;
        }

        // Fuera del try de arriba a propósito: el PID es un dato auxiliar para poder cerrarle la
        // consola al frenar, y el archivo de configuración lo escriben varios procesos. Si el
        // guardado falla y eso devolviera null, la pantalla diría que tye no arrancó con tye ya
        // corriendo, y encima sin barra.
        try
        {
            if (proceso is not null)
            {
                ConfiguracionDevTools.Compartida.PidDeTye = proceso.Id;
                ConfiguracionDevTools.Compartida.Guardar();
            }
        }
        catch (Exception)
        {
            // Sin el PID anotado el cierre de la consola cae al barrido por nombre.
        }

        return proceso;
    }
}
