using System.Text;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>Por qué un archivo no se miró.</summary>
public enum MotivoDeDescarte
{
    /// <summary>Se leyó bien.</summary>
    Ninguno,

    /// <summary>Tiene bytes nulos: es un compilado, una imagen o un comprimido.</summary>
    EsBinario,

    /// <summary>Pasa el límite de tamaño. No se abre para no comerse la memoria.</summary>
    DemasiadoGrande,

    /// <summary>Está tomado por otro proceso, o no hay permiso.</summary>
    NoSePudoAbrir
}

/// <summary>Lo que se pudo leer de un archivo, o por qué no.</summary>
/// <param name="Texto">El contenido ya decodificado. Nulo si no se pudo leer.</param>
/// <param name="Descarte">El motivo, cuando no se pudo.</param>
public sealed record LecturaDeArchivo(string? Texto, MotivoDeDescarte Descarte)
{
    public bool SePudoLeer => Texto is not null;

    public static LecturaDeArchivo Descartada(MotivoDeDescarte motivo) => new(null, motivo);
}

/// <summary>
/// Abre un archivo y devuelve su texto, con el encoding que de verdad tiene.
///
/// <b>Es la pieza que decide si este buscador sirve o no.</b>
///
/// En este repositorio hay archivos de código guardados sin BOM que escriben sus acentos en
/// Windows-1252: un <c>ó</c> es el byte <c>F3</c>, que en UTF-8 no significa nada. Un lector que
/// asuma UTF-8 los reemplaza por el carácter de reemplazo, y entonces <b>buscar «Días» en esos
/// archivos devuelve cero</b>. No falla ni avisa: parece que la palabra no está.
///
/// Por eso se lee en dos intentos: primero UTF-8 estricto, que falla de verdad cuando los bytes no
/// son UTF-8, y recién ahí Windows-1252.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public static class LectorDeArchivos
{
    /// <summary>
    /// Hasta dónde se mira buscando un byte nulo. Alcanza y sobra: un compilado tiene nulos en la
    /// cabecera, y un archivo de texto que llegue limpio hasta los 8 KB no los va a tener después.
    /// </summary>
    private const int BytesQueSeMiranParaSaberSiEsBinario = 8 * 1024;

    /// <summary>
    /// Más grande que esto no se abre. Un archivo de código nunca llega; lo que llega son volcados,
    /// bases de datos y logs enormes, que no son lo que uno viene a buscar y cuestan la memoria.
    /// </summary>
    public const long LimiteDeTamanoPorDefecto = 32L * 1024 * 1024;

    /// <summary>UTF-8 que tira cuando los bytes no son válidos, que es justamente lo que se quiere saber.</summary>
    private static readonly UTF8Encoding Utf8Estricto = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Lee el archivo entero y lo devuelve como texto, o dice por qué no se pudo.
    ///
    /// Nunca tira: un archivo tomado por otro proceso es un dato más del recorrido, y en un árbol
    /// grande van a aparecer varios.
    /// </summary>
    public static LecturaDeArchivo Leer(string ruta, long limiteDeTamano = LimiteDeTamanoPorDefecto)
    {
        byte[] bytes;

        try
        {
            var info = new FileInfo(ruta);
            if (info.Length > limiteDeTamano) return LecturaDeArchivo.Descartada(MotivoDeDescarte.DemasiadoGrande);

            bytes = File.ReadAllBytes(ruta);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return LecturaDeArchivo.Descartada(MotivoDeDescarte.NoSePudoAbrir);
        }

        if (EsBinario(bytes)) return LecturaDeArchivo.Descartada(MotivoDeDescarte.EsBinario);

        return new LecturaDeArchivo(Decodificar(bytes), MotivoDeDescarte.Ninguno);
    }

    /// <summary>
    /// Un byte nulo en el arranque delata a un compilado, una imagen o un comprimido. Sin este
    /// filtro, buscar en un árbol con <c>bin</c> adentro devuelve coincidencias dentro de los
    /// <c>.dll</c>, en líneas de miles de caracteres que rompen el listado.
    /// </summary>
    private static bool EsBinario(ReadOnlySpan<byte> bytes)
    {
        var hasta = Math.Min(bytes.Length, BytesQueSeMiranParaSaberSiEsBinario);
        return bytes[..hasta].IndexOf((byte)0) >= 0;
    }

    /// <summary>
    /// Convierte los bytes en texto con el encoding que el archivo tenga de verdad.
    ///
    /// Si trae marca, se le cree. Si no, se prueba UTF-8 de verdad —el estricto, que tira— y recién
    /// cuando falla se lee como Windows-1252, que es lo que guardaron los editores viejos.
    /// </summary>
    private static string Decodificar(byte[] bytes)
    {
        if (TieneMarca(bytes, 0xEF, 0xBB, 0xBF)) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (TieneMarca(bytes, 0xFF, 0xFE)) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (TieneMarca(bytes, 0xFE, 0xFF)) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            return Utf8Estricto.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // Los bytes no son UTF-8: el archivo es de los viejos. Latin1 cubre todas las vocales
            // acentuadas y la eñe, que es lo que se busca en castellano; lo que queda afuera son
            // las comillas tipográficas del tramo 80-9F de Windows-1252, que nadie busca.
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static bool TieneMarca(byte[] bytes, params byte[] marca)
    {
        if (bytes.Length < marca.Length) return false;

        for (var i = 0; i < marca.Length; i++)
        {
            if (bytes[i] != marca[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// Las líneas del texto, numeradas desde 1 como las cuenta cualquier editor.
    ///
    /// Se recorre con un lector y no con un <c>Split</c> para no quedarse con el texto entero
    /// duplicado en memoria mientras se busca.
    /// </summary>
    public static IEnumerable<(int Numero, string Texto)> Lineas(string texto)
    {
        using var lector = new StringReader(texto);

        var numero = 0;
        string? linea;

        while ((linea = lector.ReadLine()) is not null)
        {
            numero++;
            yield return (numero, linea);
        }
    }
}
