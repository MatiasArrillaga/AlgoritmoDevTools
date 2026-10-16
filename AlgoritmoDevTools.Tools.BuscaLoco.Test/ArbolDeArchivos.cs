using System.Text;
using AlgoritmoDevTools.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// Una carpeta temporal con archivos de verdad, que se borra sola al terminar el test.
///
/// Es pariente de <c>ArbolDePrueba</c> del Disk Cleaner pero no el mismo: allá los archivos se
/// crean vacíos porque lo único que importa es cuánto ocupan, y acá lo que importa es <b>qué dicen
/// y con qué codificación están guardados</b>. Compartir uno solo obligaría a que el de allá
/// supiera de encodings que no usa.
///
/// Los tests de esta herramienta trabajan sobre archivos reales y no sobre un disco simulado: un
/// archivo en Windows-1252, uno binario y una ruta de más de 260 caracteres no existen en una
/// abstracción.
/// </summary>
public sealed class ArbolDeArchivos : IDisposable
{
    public ArbolDeArchivos()
    {
        Raiz = Path.Combine(Path.GetTempPath(), "bl-test-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(Raiz);
    }

    public string Raiz { get; }

    /// <summary>Un archivo de texto en UTF-8 con BOM, que es como están casi todos en el repositorio.</summary>
    public string Texto(string relativa, string contenido) =>
        Escribir(relativa, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(contenido));

    /// <summary>
    /// Un archivo en Windows-1252 <b>sin BOM</b>: el caso que decide si el buscador sirve en este
    /// repositorio. Sus acentos son bytes sueltos que en UTF-8 no significan nada.
    /// </summary>
    public string TextoAnsi(string relativa, string contenido) =>
        Escribir(relativa, Encoding.Latin1.GetBytes(contenido));

    /// <summary>Un archivo con un byte nulo adelante: lo que delata a un compilado o una imagen.</summary>
    public string Binario(string relativa, string contenido)
    {
        var bytes = new List<byte> { 0x4D, 0x5A, 0x00, 0x00 };
        bytes.AddRange(Encoding.UTF8.GetBytes(contenido));
        return Escribir(relativa, [.. bytes]);
    }

    public string Carpeta(string relativa)
    {
        var ruta = Path.Combine(Raiz, relativa);
        Directory.CreateDirectory(ruta);
        return ruta;
    }

    private string Escribir(string relativa, byte[] bytes)
    {
        var ruta = Path.Combine(Raiz, relativa);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllBytes(ruta, bytes);
        return ruta;
    }

    public void Dispose()
    {
        try
        {
            // Con el prefijo, porque algún test deja rutas que superan el límite de 260.
            Directory.Delete(@"\\?\" + Raiz, recursive: true);
        }
        catch
        {
            // Un temporal que no se pudo borrar no puede hacer fallar al test que ya pasó.
        }
    }
}

/// <summary>
/// Almacenamiento aislado para los tests del historial, en una carpeta temporal propia.
///
/// No se usa el <c>ToolStorage</c> de verdad porque escribe en la carpeta del usuario, y un test no
/// puede dejar basura ahí ni pisar el historial de quien lo corre.
/// </summary>
public sealed class AlmacenamientoDePrueba : IToolStorage, IDisposable
{
    public AlmacenamientoDePrueba()
    {
        ToolId = "BuscaLocoTest";
        DataFolder = Path.Combine(Path.GetTempPath(), "bl-db-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(DataFolder);
        DatabasePath = Path.Combine(DataFolder, "data.db");
    }

    public string ToolId { get; }

    public string DataFolder { get; }

    public string DatabasePath { get; }

    public SqliteConnection OpenConnection()
    {
        var conexion = new SqliteConnection($"Data Source={DatabasePath}");
        conexion.Open();
        return conexion;
    }

    public void Dispose()
    {
        // SQLite deja el archivo tomado un instante despues de cerrar la ultima conexion.
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(DataFolder, recursive: true);
        }
        catch
        {
        }
    }
}
