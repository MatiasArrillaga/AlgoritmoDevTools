using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// La poda del recorrido, que es lo que cambió al subirlo al Core.
///
/// Lo que se prueba acá es que la poda sea <b>un parámetro</b> y no una lista cableada: el Disk
/// Cleaner pasa la suya porque va a borrar, y Busca Loco pasa otra porque sólo lee. Heredar la del
/// Disk Cleaner haría que el buscador devuelva cero resultados en carpetas donde el texto está, sin
/// decir por qué.
///
/// El caso concreto de <c>RutasProtegidas</c> se verifica del otro lado, en las pruebas del Disk
/// Cleaner: son ellas las que garantizan que su poda sigue puesta.
/// </summary>
public class RecorridoSeguroTest
{
    [Fact]
    public void Sin_poda_se_recorre_todo()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("raiz.cs", "uno");
        arbol.Texto(@"adentro\hondo.cs", "dos");

        var archivos = RecorridoSeguro.BuscarArchivos(arbol.Raiz).ToList();

        Assert.Equal(2, archivos.Count);
    }

    [Fact]
    public void Con_poda_la_rama_entera_queda_afuera()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("raiz.cs", "uno");
        arbol.Texto(@"obj\generado.cs", "dos");
        arbol.Texto(@"obj\mas\hondo.cs", "tres");

        var archivos = RecorridoSeguro.BuscarArchivos(
            arbol.Raiz,
            noEntrarEn: carpeta => Path.GetFileName(carpeta) == "obj").ToList();

        Assert.Single(archivos);
        Assert.EndsWith("raiz.cs", archivos[0]);
    }

    [Fact]
    public void La_misma_carpeta_con_otra_poda_si_se_recorre()
    {
        // La cara que importa: lo que una herramienta excluye, otra lo necesita.
        using var arbol = new ArbolDeArchivos();
        arbol.Texto(@"obj\generado.cs", "dos");

        var conPoda = RecorridoSeguro.BuscarArchivos(arbol.Raiz, noEntrarEn: c => Path.GetFileName(c) == "obj");
        var sinPoda = RecorridoSeguro.BuscarArchivos(arbol.Raiz);

        Assert.Empty(conPoda);
        Assert.Single(sinPoda);
    }

    [Fact]
    public void El_filtro_de_archivos_se_aplica()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "x");
        arbol.Texto("dos.razor", "x");

        var archivos = RecorridoSeguro.BuscarArchivos(
            arbol.Raiz,
            coincide: a => a.Extension == ".cs").ToList();

        Assert.Single(archivos);
    }

    [Fact]
    public void Las_rutas_salen_sin_el_prefijo_de_ruta_larga()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "x");

        Assert.All(
            RecorridoSeguro.BuscarArchivos(arbol.Raiz),
            ruta => Assert.False(ruta.StartsWith(@"\\?\", StringComparison.Ordinal)));
    }

    [Fact]
    public void Encuentra_un_archivo_en_una_ruta_de_mas_de_260_caracteres()
    {
        using var arbol = new ArbolDeArchivos();

        var profundo = arbol.Raiz;
        while (profundo.Length < 250) profundo = Path.Combine(profundo, "unsegmentobastantelargo");

        Directory.CreateDirectory(@"\\?\" + profundo);
        File.WriteAllText(@"\\?\" + Path.Combine(profundo, "hondo.cs"), "x");

        Assert.Single(RecorridoSeguro.BuscarArchivos(arbol.Raiz));
    }

    [Fact]
    public void Cancelar_corta_el_recorrido()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "x");

        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => RecorridoSeguro.BuscarArchivos(arbol.Raiz, cancelacion: cancelacion.Token).ToList());
    }

    [Fact]
    public void Una_carpeta_que_no_existe_no_tira()
    {
        Assert.Empty(RecorridoSeguro.BuscarArchivos(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid())));
    }
}
