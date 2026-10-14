using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Lo que el usuario eligió en esta máquina: qué deja tildado y qué no quiere que se le vuelva a
/// ofrecer.
///
/// Es por máquina a propósito y no viaja en el archivo de transferencia de configuración: acá
/// Docker puede existir o no y la papelera pesa lo que pesa en este disco, así que llevárselo a otra
/// le cambiaría a alguien algo que no es suyo.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class PerfilRepository
{
    private readonly IToolStorage _storage;

    public PerfilRepository(IToolStorage storage)
    {
        _storage = storage;
        CrearEsquemaSiNoExiste();
    }

    private void CrearEsquemaSiNoExiste()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS PREFERENCIAS (
                ITEM_ID      TEXT PRIMARY KEY,
                TILDADO      INTEGER NOT NULL,
                IGNORAR      INTEGER NOT NULL DEFAULT 0,
                ACTUALIZADO  TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Resuelve qué se le ofrece al usuario y con qué tilde.
    ///
    /// Un ítem marcado como ignorado <b>desaparece del resultado</b>: destildarlo no alcanzaría,
    /// porque la idea es no volver a verlo. El que nunca se tocó sale con el tilde de su grupo.
    /// </summary>
    public IReadOnlyList<ItemOfrecido> Aplicar(IEnumerable<ItemLimpieza> detectados)
    {
        var guardadas = Cargar();
        var ofrecidos = new List<ItemOfrecido>();

        foreach (var item in detectados)
        {
            var tienePreferencia = guardadas.TryGetValue(item.Id, out var preferencia);
            if (tienePreferencia && preferencia.Ignorar) continue;

            // Un ítem que deriva no se borra, así que no se tilda por más que haya quedado tildado
            // en una versión anterior donde sí se borraba.
            var tildado = item.SeBorra
                && (tienePreferencia ? preferencia.Tildado : item.TildadoPorDefecto);

            ofrecidos.Add(new ItemOfrecido(item, tildado));
        }

        return ofrecidos;
    }

    /// <summary>Guarda el tilde de cada ítem, respetando lo que ya estuviera ignorado.</summary>
    public void Guardar(IEnumerable<ItemOfrecido> ofrecidos)
    {
        using var db = _storage.OpenConnection();
        using var transaccion = db.BeginTransaction();

        foreach (var ofrecido in ofrecidos)
        {
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaccion;
            cmd.CommandText = @"
                INSERT INTO PREFERENCIAS (ITEM_ID, TILDADO, IGNORAR, ACTUALIZADO)
                VALUES (@id, @tildado, 0, @actualizado)
                ON CONFLICT(ITEM_ID) DO UPDATE SET TILDADO = @tildado, ACTUALIZADO = @actualizado;";
            cmd.Parameters.AddWithValue("@id", ofrecido.Item.Id);
            cmd.Parameters.AddWithValue("@tildado", ofrecido.Tildado ? 1 : 0);
            cmd.Parameters.AddWithValue("@actualizado", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        transaccion.Commit();
    }

    /// <summary>
    /// "No me lo ofrezcas nunca más". El ítem deja de aparecer, incluso en el resumen.
    /// </summary>
    public void Ignorar(string itemId) => EscribirIgnorar(itemId, true);

    /// <summary>
    /// Vuelve a ofrecerlo. Sin esto, marcar un ítem por error no tendría vuelta atrás salvo
    /// borrando la base a mano.
    /// </summary>
    public void DejarDeIgnorar(string itemId) => EscribirIgnorar(itemId, false);

    private void EscribirIgnorar(string itemId, bool ignorar)
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO PREFERENCIAS (ITEM_ID, TILDADO, IGNORAR, ACTUALIZADO)
            VALUES (@id, 0, @ignorar, @actualizado)
            ON CONFLICT(ITEM_ID) DO UPDATE SET IGNORAR = @ignorar, ACTUALIZADO = @actualizado;";
        cmd.Parameters.AddWithValue("@id", itemId);
        cmd.Parameters.AddWithValue("@ignorar", ignorar ? 1 : 0);
        cmd.Parameters.AddWithValue("@actualizado", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private Dictionary<string, Preferencia> Cargar()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT ITEM_ID, TILDADO, IGNORAR FROM PREFERENCIAS";
        using var reader = cmd.ExecuteReader();

        var preferencias = new Dictionary<string, Preferencia>(StringComparer.Ordinal);
        while (reader.Read())
            preferencias[reader.GetString(0)] = new Preferencia(reader.GetInt64(1) != 0, reader.GetInt64(2) != 0);

        return preferencias;
    }

    private readonly record struct Preferencia(bool Tildado, bool Ignorar);
}
