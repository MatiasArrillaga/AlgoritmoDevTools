using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// La pieza que decide si este buscador sirve en este repositorio.
///
/// El caso que importa es el primero: hay archivos de código guardados sin BOM que escriben sus
/// acentos en Windows-1252, y un lector que asuma UTF-8 los rompe. Ahí «Días» deja de encontrarse
/// sin fallar ni avisar.
/// </summary>
public class LectorDeArchivosTest
{
    [Fact]
    public void Lee_un_archivo_en_windows_1252_sin_bom()
    {
        using var arbol = new ArbolDeArchivos();
        var ruta = arbol.TextoAnsi("viejo.cs", "// Cantidad de Días del mes\n");

        var lectura = LectorDeArchivos.Leer(ruta);

        Assert.True(lectura.SePudoLeer);
        Assert.Contains("Días", lectura.Texto);
    }

    [Fact]
    public void Lee_un_archivo_en_utf8_con_bom()
    {
        using var arbol = new ArbolDeArchivos();
        var ruta = arbol.Texto("nuevo.cs", "// Cálculo de Días\n");

        var lectura = LectorDeArchivos.Leer(ruta);

        Assert.Contains("Cálculo", lectura.Texto);

        // La marca no tiene que llegar al texto: si llega, el primer caracter deja de ser el que es
        // y una busqueda anclada al principio de archivo no encuentra nada.
        Assert.StartsWith("//", lectura.Texto);
    }

    [Fact]
    public void Un_archivo_con_byte_nulo_se_descarta_por_binario()
    {
        using var arbol = new ArbolDeArchivos();
        var ruta = arbol.Binario("compilado.dll", "Días");

        var lectura = LectorDeArchivos.Leer(ruta);

        Assert.False(lectura.SePudoLeer);
        Assert.Equal(MotivoDeDescarte.EsBinario, lectura.Descarte);
    }

    [Fact]
    public void Un_archivo_mas_grande_que_el_limite_no_se_abre()
    {
        using var arbol = new ArbolDeArchivos();
        var ruta = arbol.Texto("grande.txt", new string('x', 5000));

        var lectura = LectorDeArchivos.Leer(ruta, limiteDeTamano: 1000);

        Assert.Equal(MotivoDeDescarte.DemasiadoGrande, lectura.Descarte);
    }

    [Fact]
    public void Un_archivo_que_no_existe_no_tira()
    {
        using var arbol = new ArbolDeArchivos();

        var lectura = LectorDeArchivos.Leer(Path.Combine(arbol.Raiz, "no-existe.cs"));

        Assert.Equal(MotivoDeDescarte.NoSePudoAbrir, lectura.Descarte);
    }

    [Fact]
    public void Las_lineas_se_numeran_desde_uno()
    {
        var lineas = LectorDeArchivos.Lineas("primera\nsegunda\ntercera").ToList();

        Assert.Equal(3, lineas.Count);
        Assert.Equal((1, "primera"), lineas[0]);
        Assert.Equal((3, "tercera"), lineas[2]);
    }

    [Fact]
    public void El_retorno_de_carro_no_queda_pegado_al_final_de_la_linea()
    {
        // Los archivos del repositorio se guardan con CRLF: si el \r sobrevive, toda busqueda
        // anclada al final de linea deja de encontrar.
        var lineas = LectorDeArchivos.Lineas("uno\r\ndos\r\n").ToList();

        Assert.Equal("uno", lineas[0].Texto);
        Assert.Equal("dos", lineas[1].Texto);
    }
}
