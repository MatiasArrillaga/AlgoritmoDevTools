using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Core.Abstractions;

/// <summary>
/// Expone almacenamiento aislado por herramienta. Cada tool recibe su propia carpeta
/// bajo %LOCALAPPDATA%/AlgoritmoDevTools/{toolId}/ y su propio archivo SQLite.
/// </summary>
public interface IToolStorage
{
    string ToolId { get; }
    string DataFolder { get; }
    string DatabasePath { get; }
    SqliteConnection OpenConnection();
}