using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Trae la última versión publicada en GitHub y reemplaza el ejecutable con ella.
///
/// El repositorio es público, así que la consulta no lleva credenciales de ningún tipo: nada que
/// embeber en el ejecutable ni que repartirle al equipo.
///
/// Se actualiza a sí mismo, que es lo que lo hace delicado: un ejecutable en uso no se puede
/// sobrescribir, pero Windows sí deja **renombrarlo**. Esa es toda la maniobra —correrse de nombre y
/// dejar al nuevo en su lugar— y es lo que permite actualizar con el widget abierto, que corre del
/// mismo archivo.
///
/// Referencia funcional y técnica:
/// README.md, sección "Actualizaciones".
/// </summary>
public static class Actualizador
{
    private const string Repositorio = "MatiasArrillaga/AlgoritmoDevTools";

    /// <summary>Nombre del archivo que publica el workflow. El release tiene que traer este.</summary>
    private const string NombreDelArchivo = "DevTools.exe";

    /// <summary>
    /// Sufijo del ejecutable que se corrió de nombre para dejarle el lugar al nuevo. Queda en disco
    /// hasta el próximo arranque: todavía lo están ejecutando los procesos abiertos.
    ///
    /// Puede llevar un número atrás —<c>.anterior2</c>— cuando el nombre a secas sigue tomado. Lo
    /// explica <see cref="NombreLibreParaElViejo"/>.
    /// </summary>
    private const string SufijoAnterior = ".anterior";

    /// <summary>
    /// GitHub rechaza las consultas sin User-Agent con un 403, así que no es opcional.
    /// </summary>
    private const string Agente = "AlgoritmoDevTools";

    /// <summary>
    /// Verbo con el que la versión nueva se arranca a sí misma al reiniciar, seguido del número de
    /// proceso de la que la puso. Lo escribe <see cref="Relanzar"/> y lo lee el arranque.
    /// </summary>
    public const string VerboReinicio = "--reinicio";

    /// <summary>
    /// Cuánto espera la versión nueva a que termine la que la instaló. Es la espera a que un proceso
    /// cierre su ventana, no a que haga nada: con más que esto, algo se trabó y conviene seguir
    /// —abrir igual— antes que dejar al usuario mirando una aplicación que no vuelve.
    /// </summary>
    private static readonly TimeSpan EsperaDelReinicio = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Variable de entorno que reemplaza la dirección a consultar.
    ///
    /// Existe para poder probar el camino completo —el aviso, la descarga y el reemplazo— contra un
    /// servidor local, sin tener que publicar un release de verdad nada más que para ver si anda.
    /// Sin ella puesta, que es lo normal, se consulta GitHub.
    /// </summary>
    private const string VariableDePrueba = "DEVTOOLS_RELEASES";

    /// <summary>De dónde se pregunta cuál es la última versión.</summary>
    private static string UrlDelUltimoRelease =>
        Environment.GetEnvironmentVariable(VariableDePrueba)
        ?? $"https://api.github.com/repos/{Repositorio}/releases/latest";

    /// <summary>La versión de este ejecutable. Sale del Version del csproj.</summary>
    public static Version VersionActual =>
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

    /// <summary>
    /// Pregunta cuál es la última publicada. Devuelve null si no hay ninguna más nueva que la
    /// propia, o si no se pudo averiguar: quedarse sin internet, estar fuera de la VPN o que todavía
    /// no haya ningún release no son errores que valga la pena mostrarle a nadie.
    /// </summary>
    public static async Task<VersionPublicada?> BuscarAsync(CancellationToken cancelacion = default)
    {
        try
        {
            using var cliente = Cliente();

            var respuesta = await cliente.GetAsync(UrlDelUltimoRelease, cancelacion);

            if (!respuesta.IsSuccessStatusCode) return null;

            await using var contenido = await respuesta.Content.ReadAsStreamAsync(cancelacion);
            using var json = await JsonDocument.ParseAsync(contenido, cancellationToken: cancelacion);

            var raiz = json.RootElement;
            var etiqueta = raiz.GetProperty("tag_name").GetString();
            if (etiqueta is null) return null;

            // Las etiquetas se escriben "v1.2.0"; el número es lo que viene después de la v.
            if (!Version.TryParse(etiqueta.TrimStart('v', 'V'), out var numero)) return null;
            if (numero <= VersionActual) return null;

            var archivo = raiz.GetProperty("assets").EnumerateArray().FirstOrDefault(asset =>
                string.Equals(asset.GetProperty("name").GetString(), NombreDelArchivo, StringComparison.OrdinalIgnoreCase));

            // Un release sin el ejecutable adentro no sirve para actualizar: puede ser uno escrito a
            // mano, o uno cuyo workflow falló a mitad de camino.
            if (archivo.ValueKind is JsonValueKind.Undefined) return null;

            var url = archivo.GetProperty("browser_download_url").GetString();
            if (url is null) return null;

            return new VersionPublicada(numero, etiqueta, url, raiz.GetProperty("body").GetString() ?? string.Empty);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Baja la versión nueva y la deja en el lugar del ejecutable actual.
    ///
    /// El reemplazo es en dos pasos y en ese orden: primero el actual se corre de nombre —lo que
    /// Windows permite aunque lo estén ejecutando— y recién después se escribe el nuevo. Al revés no
    /// se puede: escribir sobre un archivo en uso falla, y borrarlo antes dejaría la carpeta sin
    /// ejecutable si la descarga se corta.
    ///
    /// Se descarga a un temporal primero, por lo mismo: una descarga a medias no tiene que llegar a
    /// pisar nada.
    /// </summary>
    /// <returns>Null si salió bien, o el mensaje de error.</returns>
    public static async Task<string?> ActualizarAsync(VersionPublicada version, CancellationToken cancelacion = default)
    {
        var destino = Environment.ProcessPath;
        if (destino is null) return "no se pudo determinar la ruta del ejecutable.";

        var temporal = destino + ".descarga";
        var carpeta = Path.GetDirectoryName(destino) ?? destino;

        // En cuál de los dos pasos que tocan el disco se estaba, para poder decirlo si Windows lo
        // rechaza. Son permisos distintos —crear un archivo nuevo y renombrar el que está corriendo—
        // y saber cuál falló es la diferencia entre poder arreglarlo y volver a probar a ciegas.
        var paso = "crear el archivo de la descarga";

        try
        {
            // Lo que haya quedado de un intento anterior se saca antes de empezar: crear encima de
            // un archivo que quedó oculto o de sólo lectura falla por permisos, y ese error manda a
            // buscar el problema en la carpeta, donde no está.
            if (File.Exists(temporal)) File.Delete(temporal);

            using (var cliente = Cliente())
            await using (var remoto = await cliente.GetStreamAsync(version.UrlDelArchivo, cancelacion))
            await using (var local = File.Create(temporal))
            {
                await remoto.CopyToAsync(local, cancelacion);
            }

            paso = "reemplazar el ejecutable";

            var anterior = NombreLibreParaElViejo(destino);

            File.Move(destino, anterior);

            try
            {
                File.Move(temporal, destino);
            }
            catch (Exception)
            {
                // Si el segundo paso falla, la carpeta queda sin ejecutable: se devuelve el viejo a
                // su nombre antes de salir.
                File.Move(anterior, destino);
                throw;
            }

            Esconder(anterior);

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return $"Windows no dejó {paso} en {carpeta}. Abrí DevTools como administrador y probá " +
                   "de nuevo, o instalalo en una carpeta tuya.";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            if (File.Exists(temporal))
            {
                try { File.Delete(temporal); } catch { /* lo que quedó a medias no vale un error. */ }
            }
        }
    }

    /// <summary>
    /// Arranca la versión recién puesta. Lo llama la ventana después de actualizar, justo antes de
    /// cerrarse.
    ///
    /// Le pasa su propio número de proceso para que la que arranca sepa a quién esperar. Sin eso las
    /// dos conviven un instante, y la nueva llega a buscar el ejecutable viejo para borrarlo cuando
    /// la vieja todavía lo está ejecutando: no se puede, y queda en la carpeta hasta el arranque
    /// siguiente. Esperar a que termine de cerrar evita toda esa secuela.
    /// </summary>
    public static void Relanzar()
    {
        var exe = Environment.ProcessPath;
        if (exe is null) return;

        try
        {
            var inicio = new ProcessStartInfo(exe) { UseShellExecute = true };
            inicio.ArgumentList.Add(VerboReinicio);
            inicio.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

            Process.Start(inicio);
        }
        catch (Exception)
        {
            // No poder relanzar no es motivo para dejar la actualización por la mitad: ya está
            // puesta, y la próxima vez que alguien lo abra va a correr la nueva.
        }
    }

    /// <summary>
    /// Espera a que termine de cerrarse el proceso que largó el reinicio, para recién después tocar
    /// su ejecutable. Lo llama el arranque cuando viene con <see cref="VerboReinicio"/>.
    /// </summary>
    /// <param name="numeroDeProceso">El que escribió <see cref="Relanzar"/> en la línea de comandos.</param>
    public static void EsperarAlQueActualizo(string? numeroDeProceso)
    {
        if (!int.TryParse(numeroDeProceso, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero)) return;

        try
        {
            using var anterior = Process.GetProcessById(numero);
            anterior.WaitForExit((int)EsperaDelReinicio.TotalMilliseconds);
        }
        catch (ArgumentException)
        {
            // Ya había terminado cuando se lo fue a buscar, que es lo que se estaba esperando.
        }
        catch (Exception)
        {
            // Cualquier otra cosa no justifica demorar el arranque: a lo sumo el ejecutable viejo
            // queda un rato más en la carpeta.
        }
    }

    /// <summary>
    /// Elige con qué nombre se corre el ejecutable que se está reemplazando.
    ///
    /// Lo normal es <see cref="SufijoAnterior"/> a secas, y si quedó uno de la actualización pasada
    /// se borra y se usa ese mismo nombre. Lo que no se puede dar por hecho es que se deje borrar:
    /// **Windows no permite tocar un ejecutable que alguien está ejecutando**, y el candidato
    /// natural es justamente el widget, que corre del archivo que dejó la actualización anterior y
    /// se queda abierto durante semanas.
    ///
    /// Ese intento fallaba con un acceso denegado —no con un error de archivo en uso, que es lo que
    /// uno esperaría—, así que la actualización entera se caía diciendo que no había permiso para
    /// escribir en la carpeta. La carpeta estaba perfecta. Por eso acá se busca un nombre libre en
    /// lugar de insistir con uno solo: el viejo no estorba, y la actualización sigue.
    /// </summary>
    private static string NombreLibreParaElViejo(string destino)
    {
        var fijo = destino + SufijoAnterior;

        if (!File.Exists(fijo)) return fijo;

        try
        {
            File.Delete(fijo);
            return fijo;
        }
        catch (Exception)
        {
            // Lo está ejecutando alguien. Se le busca otro nombre en vez de fallar.
        }

        for (var numero = 2; numero < 100; numero++)
        {
            var otro = fijo + numero.ToString(CultureInfo.InvariantCulture);

            if (!File.Exists(otro)) return otro;

            try
            {
                File.Delete(otro);
                return otro;
            }
            catch (Exception)
            {
                // Otro que sigue en uso. Al siguiente.
            }
        }

        // Cien ejecutables viejos tomados a la vez no es un caso real: si se llegó hasta acá hay
        // algo más raro pasando, y el error de Windows va a explicarlo mejor que uno inventado.
        return fijo;
    }

    /// <summary>
    /// Borra los ejecutables que quedaron de actualizaciones anteriores. Se llama al arrancar, que
    /// es cuando ya nadie los está ejecutando; mientras corrían no se podía.
    ///
    /// Son varios y no uno: cada actualización hecha con el widget abierto deja el suyo, porque el
    /// que estaba en uso no se pudo reutilizar. Se borran todos de una, que es cuando se puede.
    /// </summary>
    public static void LimpiarAnterior()
    {
        var exe = Environment.ProcessPath;
        if (exe is null) return;

        try
        {
            var carpeta = Path.GetDirectoryName(exe);
            if (carpeta is null) return;

            foreach (var viejo in Directory.EnumerateFiles(carpeta, Path.GetFileName(exe) + SufijoAnterior + "*"))
            {
                try
                {
                    File.Delete(viejo);
                }
                catch (Exception)
                {
                    // Sigue tomado por algún proceso que no cerró —el widget, casi siempre, que
                    // corre del mismo archivo—: son 55 MB que se van en el próximo arranque, y no
                    // hay nada que el usuario pueda hacer al respecto. Por eso queda oculto: para
                    // que mientras tanto no parezca que hay dos DevTools instalados.
                }
            }
        }
        catch (Exception)
        {
            // Ni siquiera se pudo mirar la carpeta. No es motivo para no abrir.
        }
    }

    /// <summary>
    /// Le pone el atributo de oculto al ejecutable que se corrió de nombre.
    ///
    /// No se puede borrar todavía —lo están ejecutando los procesos abiertos— y a la vista queda
    /// como un segundo ejecutable de 55 MB al lado del bueno, con el nombre casi igual. Oculto sigue
    /// funcionando igual para quien lo está corriendo, y desaparece de la carpeta.
    /// </summary>
    private static void Esconder(string archivo)
    {
        try
        {
            File.SetAttributes(archivo, File.GetAttributes(archivo) | FileAttributes.Hidden);
        }
        catch (Exception)
        {
            // Que se vea no rompe nada.
        }
    }

    private static HttpClient Cliente()
    {
        var cliente = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        cliente.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Agente, "1.0"));
        return cliente;
    }
}

/// <summary>Una versión publicada en GitHub que es más nueva que la que está corriendo.</summary>
/// <param name="Numero">Versión, sacada de la etiqueta.</param>
/// <param name="Etiqueta">La etiqueta tal cual, para mostrarla.</param>
/// <param name="UrlDelArchivo">De dónde se baja el ejecutable.</param>
/// <param name="Notas">Lo que se escribió en el release.</param>
public sealed record VersionPublicada(Version Numero, string Etiqueta, string UrlDelArchivo, string Notas);
