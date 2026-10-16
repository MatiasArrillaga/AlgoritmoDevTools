using AlgoritmoDevTools.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>Una búsqueda que ya se hizo, para volver a ella sin reescribirla.</summary>
public sealed record BusquedaRecordada(string Texto, string Nombre, bool EsExpresion);

/// <summary>
/// Lo que se buscó y dónde, en esta máquina.
///
/// Existe para que abrir la herramienta no empiece siempre en blanco: alternar entre el repositorio
/// y una carpeta suelta no puede costar navegar el árbol cada vez.
///
/// Es por máquina a propósito y no viaja en el archivo de transferencia de configuración: las
/// carpetas de uno no son las de otro, y qué buscó cada uno es su avance personal.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public sealed class HistorialRepository
{
    /// <summary>
    /// Cuántas se recuerdan de cada clase. Más que esto no sirve: nadie baja veinte entradas
    /// buscando la que hizo hace una semana, y la lista se vuelve ruido.
    /// </summary>
    private const int CuantasSeGuardan = 20;

    private readonly IToolStorage _storage;

    public HistorialRepository(IToolStorage storage)
    {
        _storage = storage;
        CrearEsquemaSiNoExiste();
    }

    private void CrearEsquemaSiNoExiste()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            -- La clave es el par texto + nombre: repetir la misma busqueda la actualiza en lugar de
            -- acumular una entrada nueva cada vez.
            CREATE TABLE IF NOT EXISTS BUSQUEDAS (
                TEXTO        TEXT NOT NULL,
                NOMBRE       TEXT NOT NULL,
                ES_EXPRESION INTEGER NOT NULL,
                USADA        TEXT NOT NULL,
                PRIMARY KEY (TEXTO, NOMBRE)
            );

            CREATE TABLE IF NOT EXISTS RAICES (
                RUTA   TEXT PRIMARY KEY,
                USADA  TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Anota una búsqueda que se ejecutó. Las dos vacías no se guardan: no se pueden repetir.
    /// </summary>
    public void GuardarBusqueda(string texto, string nombre, bool esExpresion)
    {
        if (string.IsNullOrWhiteSpace(texto) && string.IsNullOrWhiteSpace(nombre)) return;

        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO BUSQUEDAS (TEXTO, NOMBRE, ES_EXPRESION, USADA)
            VALUES ($texto, $nombre, $expresion, $usada)
            ON CONFLICT (TEXTO, NOMBRE) DO UPDATE SET ES_EXPRESION = $expresion, USADA = $usada;";

        cmd.Parameters.AddWithValue("$texto", texto);
        cmd.Parameters.AddWithValue("$nombre", nombre);
        cmd.Parameters.AddWithValue("$expresion", esExpresion ? 1 : 0);
        cmd.Parameters.AddWithValue("$usada", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        Podar(db, "BUSQUEDAS");
    }

    /// <summary>Anota una carpeta desde la que se buscó.</summary>
    public void GuardarRaiz(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return;

        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO RAICES (RUTA, USADA) VALUES ($ruta, $usada)
            ON CONFLICT (RUTA) DO UPDATE SET USADA = $usada;";

        cmd.Parameters.AddWithValue("$ruta", ruta);
        cmd.Parameters.AddWithValue("$usada", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        Podar(db, "RAICES");
    }

    /// <summary>Las últimas búsquedas, de la más reciente a la más vieja.</summary>
    public IReadOnlyList<BusquedaRecordada> UltimasBusquedas()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT TEXTO, NOMBRE, ES_EXPRESION FROM BUSQUEDAS ORDER BY USADA DESC LIMIT {CuantasSeGuardan};";

        var busquedas = new List<BusquedaRecordada>();
        using var lector = cmd.ExecuteReader();

        while (lector.Read())
        {
            busquedas.Add(new BusquedaRecordada(lector.GetString(0), lector.GetString(1), lector.GetInt32(2) == 1));
        }

        return busquedas;
    }

    /// <summary>Las últimas carpetas desde las que se buscó, de la más reciente a la más vieja.</summary>
    public IReadOnlyList<string> UltimasRaices()
    {
        using var db = _storage.OpenConnection();
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT RUTA FROM RAICES ORDER BY USADA DESC LIMIT {CuantasSeGuardan};";

        var raices = new List<string>();
        using var lector = cmd.ExecuteReader();

        while (lector.Read())
        {
            raices.Add(lector.GetString(0));
        }

        return raices;
    }

    /// <summary>
    /// Borra lo que sobra de las más viejas.
    ///
    /// Se poda al escribir y no al leer: la lista se mantiene chica sola, y una consulta nunca tiene
    /// que pagar la limpieza de otro.
    ///
    /// Se identifica la fila por <c>rowid</c>, que SQLite le da a toda tabla, y no por su clave: la
    /// de BUSQUEDAS son dos columnas, y armar una clave compuesta pegando los dos textos obliga a
    /// meter un separador adentro del SQL. Con un carácter raro —probado con el nulo— el motor ni
    /// siquiera llega a ejecutar la sentencia, y con uno común cualquier texto que lo contenga haría
    /// colisionar dos entradas distintas.
    /// </summary>
    private static void Podar(SqliteConnection db, string tabla)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = $@"
            DELETE FROM {tabla}
            WHERE rowid NOT IN (
                SELECT rowid FROM {tabla} ORDER BY USADA DESC LIMIT {CuantasSeGuardan}
            );";
        cmd.ExecuteNonQuery();
    }
}
