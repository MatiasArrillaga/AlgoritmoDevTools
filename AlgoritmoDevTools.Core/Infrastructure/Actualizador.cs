using System.Diagnostics;
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
    /// </summary>
    private const string SufijoAnterior = ".anterior";

    /// <summary>
    /// GitHub rechaza las consultas sin User-Agent con un 403, así que no es opcional.
    /// </summary>
    private const string Agente = "AlgoritmoDevTools";

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

            var respuesta = await cliente.GetAsync(
                $"https://api.github.com/repos/{Repositorio}/releases/latest", cancelacion);

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
        var anterior = destino + SufijoAnterior;

        try
        {
            using (var cliente = Cliente())
            await using (var remoto = await cliente.GetStreamAsync(version.UrlDelArchivo, cancelacion))
            await using (var local = File.Create(temporal))
            {
                await remoto.CopyToAsync(local, cancelacion);
            }

            // De una actualización anterior, si el arranque no llegó a limpiarlo.
            if (File.Exists(anterior)) File.Delete(anterior);

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

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return "no hay permiso para escribir en la carpeta del ejecutable.";
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
    /// Arranca el ejecutable de nuevo. Lo llama la ventana después de actualizar, antes de cerrarse.
    /// </summary>
    public static void Relanzar()
    {
        var exe = Environment.ProcessPath;
        if (exe is null) return;

        try
        {
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // No poder relanzar no es motivo para dejar la actualización por la mitad: ya está
            // puesta, y la próxima vez que alguien lo abra va a correr la nueva.
        }
    }

    /// <summary>
    /// Borra el ejecutable que quedó de la actualización anterior. Se llama al arrancar, que es
    /// cuando ya nadie lo está ejecutando; mientras corría no se podía.
    /// </summary>
    public static void LimpiarAnterior()
    {
        var exe = Environment.ProcessPath;
        if (exe is null) return;

        try
        {
            var anterior = exe + SufijoAnterior;
            if (File.Exists(anterior)) File.Delete(anterior);
        }
        catch (Exception)
        {
            // Sigue tomado por algún proceso que no cerró: son 55 MB que se van en el próximo
            // arranque, y no hay nada que el usuario pueda hacer al respecto.
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
