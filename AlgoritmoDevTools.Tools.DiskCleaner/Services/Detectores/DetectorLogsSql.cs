using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// Los archivos de log de SQL Server que crecieron en un pico y nunca se achicaron. En la máquina
/// del incidente, 34,24 GB repartidos en tres bases, dos de ellas con menos del 0,2 % realmente en
/// uso.
///
/// Lo que se creyó al principio —que estaban en FULL sin backups de log— era falso: el problema no
/// es el modelo de recuperación sino que crecer es automático y achicar no.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorLogsSql : IDetector
{
    /// <summary>
    /// Con una transacción abierta el espacio del log no se puede reusar: achicarlo no recupera
    /// nada y puede quedarse esperando.
    /// </summary>
    private const string TRANSACCION_ABIERTA = "ACTIVE_TRANSACTION";

    private readonly SavedConnectionsRepository _conexiones;

    public DetectorLogsSql(SavedConnectionsRepository conexiones) => _conexiones = conexiones;

    public string Nombre => "Logs de SQL Server";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            // Sólo las instancias de esta máquina: el .ldf de un servidor remoto ocupa el disco de
            // ese servidor, no el que estamos limpiando.
            var locales = _conexiones.GetAll().Where(EsDeEstaMaquina).ToArray();
            if (locales.Length == 0) return [];

            var items = new List<ItemLimpieza>();
            var yaVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var conexion in locales)
            {
                cancelacion.ThrowIfCancellationRequested();

                var credenciales = new SQLService.ConnectionData(
                    conexion.Server, conexion.User, conexion.Password, conexion.UseIntegratedSecurity);

                var bases = SQLService.TryGetDatabases(credenciales);
                if (!bases.IsSuccess) continue;

                var logs = SQLService.TryGetLogFiles(credenciales, bases.Databases);
                if (!logs.IsSuccess) continue;

                foreach (var log in logs.Logs)
                {
                    // Dos conexiones guardadas pueden apuntar a la misma instancia con distintas
                    // credenciales, y entonces el mismo archivo llegaría dos veces.
                    if (!yaVistos.Add(log.PhysicalPath)) continue;

                    var item = Construir(log);
                    if (item is not null) items.Add(item);
                }
            }

            return items;
        }, cancelacion);

    private static ItemLimpieza? Construir(SQLService.LogFile log)
    {
        // Lo que se recupera no es lo que ocupa el archivo sino lo que tiene asignado de más.
        // Mostrar el tamaño total prometería espacio que el shrink no va a devolver.
        var recuperable = log.AllocatedBytes - log.UsedBytes;
        if (recuperable <= 0) return null;

        var transaccionAbierta = string.Equals(log.ReuseWait, TRANSACCION_ABIERTA, StringComparison.OrdinalIgnoreCase);
        var porcentajeUsado = log.AllocatedBytes == 0 ? 0 : log.UsedBytes * 100.0 / log.AllocatedBytes;

        return new ItemLimpieza
        {
            // El nombre lógico se repite entre bases hermanas; el archivo físico no.
            Id = "log-sql:" + log.PhysicalPath.ToLowerInvariant(),
            Ubicacion = $"Log de {log.Database} — {Path.GetFileName(log.PhysicalPath)}",
            Bytes = recuperable,
            Grupo = GrupoLimpieza.Seguro,
            Riesgo = NivelRiesgo.Bajo,
            Descripcion =
                $"El archivo tiene {Gb(log.AllocatedBytes)} asignados y usa {porcentajeUsado:0.##} %. " +
                $"Creció en algún pico y nunca se achicó. Modelo de recuperación: {log.RecoveryModel}.",
            Consecuencia =
                "Se devuelve al disco el espacio asignado que no está en uso. Los datos no se tocan y la base sigue funcionando igual.",
            ComoSeRevierte = "Volviendo a crecer solo cuando haga falta.",
            Rutas = [log.PhysicalPath],
            MotivoParaNoTocarlo = transaccionAbierta
                ? "Hay una transacción abierta sosteniendo el log, así que achicarlo no recuperaría nada. Para ver cuál es: DBCC OPENTRAN."
                : null
        };
    }

    private static string Gb(long bytes) => $"{bytes / 1024d / 1024 / 1024:0.##} GB";

    /// <summary>
    /// Si la instancia corre en esta máquina. Se compara contra el nombre del equipo además de las
    /// formas de escribir "acá", porque mucha gente guarda la conexión con el nombre real.
    /// </summary>
    private static bool EsDeEstaMaquina(SavedConnection conexion)
    {
        var servidor = conexion.Server;
        if (string.IsNullOrWhiteSpace(servidor)) return false;

        // El servidor puede venir como "host,1433" o "host\INSTANCIA".
        var host = servidor.Split(',', '\\')[0].Trim();

        return host is "." or "(local)" or "localhost" or "127.0.0.1" or "::1"
            || string.Equals(host, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }
}
