using AlgoritmoDevTools.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Core.Infrastructure;

public sealed class ToolStorage : IToolStorage
{
    private const string RootFolderName = "AlgoritmoDevTools";
    private const string DatabaseFileName = "data.db";

    public ToolStorage(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
            throw new ArgumentException("toolId no puede ser vacío.", nameof(toolId));

        ToolId = toolId;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DataFolder = Path.Combine(localAppData, RootFolderName, toolId);
        DatabasePath = Path.Combine(DataFolder, DatabaseFileName);
        Directory.CreateDirectory(DataFolder);
    }

    public string ToolId { get; }
    public string DataFolder { get; }
    public string DatabasePath { get; }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        return connection;
    }
}