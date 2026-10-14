using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;
using AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// Corre el catalogo una sola vez y reparte el resultado.
///
/// Sin conexiones guardadas el detector de logs no tiene a quien preguntarle y devuelve vacio, que
/// es lo que se quiere aca: estos tests no prueban SQL.
/// </summary>
public sealed class EscaneoCompartido : IDisposable
{
    private readonly ArbolDePrueba _arbol = new();

    public EscaneoCompartido()
    {
        var conexiones = new SavedConnectionsRepository(new StorageVacio(_arbol.Raiz));
        Resultado = new EscaneoService(CatalogoDeDetectores.Todos(conexiones)).EscanearAsync();
    }

    public Task<Escaneo> Resultado { get; }

    public void Dispose() => _arbol.Dispose();

    private sealed class StorageVacio(string carpeta) : IToolStorage
    {
        public string ToolId => "DiskCleanerTest";
        public string DataFolder => carpeta;
        public string DatabasePath => Path.Combine(carpeta, "conexiones.db");

        public SqliteConnection OpenConnection()
        {
            var conexion = new SqliteConnection($"Data Source={DatabasePath}");
            conexion.Open();
            return conexion;
        }
    }
}
