using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>Lo que se le manda al helper elevado: un ítem y sus rutas, nada más.</summary>
public sealed record TareaElevada(string ItemId, string Ubicacion, IReadOnlyList<string> Rutas);

/// <summary>Lo que el helper elevado contesta.</summary>
public sealed record FalloElevado(string Ruta, string Motivo, IReadOnlyList<string> Quien);

public sealed record ResultadoElevado(
    string ItemId,
    long BytesLiberados,
    int Borradas,
    IReadOnlyList<FalloElevado> Fallos);

/// <summary>
/// El borrado de lo que necesita permiso de administrador.
///
/// <b>La aplicación no corre elevada.</b> De todo lo que hubo que hacer el día del incidente, sólo
/// cuatro operaciones necesitaron permisos; el escaneo y casi toda la limpieza funcionan sin ellos.
/// Elevar el Shell entero costaría un pedido de permiso en cada arranque para las seis herramientas
/// y rompería el arrastrar y soltar del Convertidor a Markdown.
///
/// Así que se relanza el propio ejecutable con un verbo propio y <c>Verb = "runas"</c>. Eso obliga
/// a <c>UseShellExecute = true</c>, que a su vez hace imposible redirigir la salida: por eso lo que
/// va y lo que vuelve viajan en archivos y no por los pipes.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class ElevacionService
{
    /// <summary>Lo que Windows devuelve cuando el usuario cierra el cartel de permisos.</summary>
    private const int EL_USUARIO_CANCELO = 1223;

    public const string VerboLimpiarElevado = "--limpiar-elevado";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>
    /// Pide permiso <b>una sola vez</b> y borra todo lo que lo necesitaba.
    ///
    /// Devuelve <c>null</c> si el usuario no dio permiso. No es un error: es una respuesta, y la
    /// diferencia importa porque un error se muestra en rojo y pide que alguien haga algo.
    /// </summary>
    public async Task<IReadOnlyList<ResultadoElevado>?> EjecutarAsync(
        IReadOnlyList<ItemDelPlan> items,
        CancellationToken cancelacion = default)
    {
        if (items.Count == 0) return [];

        var tareas = items
            .Select(i => new TareaElevada(i.Item.Id, i.Item.Ubicacion, i.Rutas))
            .ToArray();

        var carpeta = Path.Combine(Path.GetTempPath(), "DiskCleaner-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);

        var entrada = Path.Combine(carpeta, "tareas.json");
        var salida = Path.Combine(carpeta, "resultado.json");

        try
        {
            await File.WriteAllTextAsync(entrada, JsonSerializer.Serialize(tareas, Json), cancelacion);

            if (!await CorrerElevadoAsync(entrada, salida, cancelacion)) return null;

            if (!File.Exists(salida)) return [];

            var contenido = await File.ReadAllTextAsync(salida, cancelacion);
            return JsonSerializer.Deserialize<ResultadoElevado[]>(contenido, Json) ?? [];
        }
        finally
        {
            // El archivo lleva la lista de lo que se va a borrar: no tiene por qué quedar dando
            // vueltas en el temporal después de usarse.
            try
            {
                Directory.Delete(carpeta, recursive: true);
            }
            catch
            {
                // Si no se puede, no es motivo para perder el resultado.
            }
        }
    }

    /// <summary>
    /// Devuelve <c>false</c> si el usuario no dio el permiso.
    /// </summary>
    private static Task<bool> CorrerElevadoAsync(string entrada, string salida, CancellationToken cancelacion) =>
        Task.Run(() =>
        {
            // ProcessPath y no Assembly.Location: con el publish de un solo archivo, Location viene
            // vacío y no sirve para relanzarse.
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe)) return false;

            var arranque = new ProcessStartInfo(exe)
            {
                // Las dos van juntas: pedir permisos exige que lo lance el shell, y eso es lo que
                // impide redirigir la salida.
                UseShellExecute = true,
                Verb = "runas"
            };

            arranque.ArgumentList.Add(VerboLimpiarElevado);
            arranque.ArgumentList.Add(entrada);
            arranque.ArgumentList.Add(salida);

            try
            {
                using var proceso = Process.Start(arranque);
                if (proceso is null) return false;

                proceso.WaitForExit();
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == EL_USUARIO_CANCELO)
            {
                // Cerrar el cartel de permisos es una decisión, no una falla. Confundirlas haría
                // que decir "no" se vea como si algo se hubiera roto.
                return false;
            }
        }, cancelacion);

    /// <summary>
    /// Lo que corre del otro lado del permiso. Lee las tareas, borra y deja el resultado escrito.
    ///
    /// <b>Vuelve a validar cada ruta contra la lista negra</b>, porque eso lo hace
    /// <see cref="EjecutorDeLimpieza.BorrarRutas"/>. Es lo que sostiene que un archivo de entrada
    /// manipulado no se convierta en un borrado arbitrario con permisos de administrador.
    /// </summary>
    public static void EjecutarComoHelper(string entrada, string salida)
    {
        var tareas = JsonSerializer.Deserialize<TareaElevada[]>(File.ReadAllText(entrada), Json) ?? [];
        var resultados = new List<ResultadoElevado>();

        foreach (var tarea in tareas)
        {
            var (liberados, borradas, fallos) = EjecutorDeLimpieza.BorrarRutas(tarea.Rutas);

            resultados.Add(new ResultadoElevado(
                tarea.ItemId,
                liberados,
                borradas,
                [.. fallos.Select(f => new FalloElevado(
                    f.Ruta,
                    f.Motivo,
                    [.. f.Quien.Select(q => $"{q.Nombre} ({q.Pid})")]))]));
        }

        File.WriteAllText(salida, JsonSerializer.Serialize(resultados, Json));
    }
}
