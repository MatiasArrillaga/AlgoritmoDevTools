namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// Una carpeta temporal que se borra sola al terminar el test.
///
/// Los tests de esta herramienta trabajan sobre archivos de verdad y no sobre un disco simulado:
/// lo que hay que probar —que un junction no se siga, que una carpeta sin permiso no corte el
/// recorrido, que una ruta de más de 260 caracteres se mida— no existe en una abstracción.
/// </summary>
public sealed class ArbolDePrueba : IDisposable
{
    public ArbolDePrueba()
    {
        Raiz = Path.Combine(Path.GetTempPath(), "dc-test-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(Raiz);
    }

    public string Raiz { get; }

    public string Archivo(string relativa, int bytes)
    {
        var ruta = Path.Combine(Raiz, relativa);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllBytes(ruta, new byte[bytes]);
        return ruta;
    }

    public string Carpeta(string relativa)
    {
        var ruta = Path.Combine(Raiz, relativa);
        Directory.CreateDirectory(ruta);
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
