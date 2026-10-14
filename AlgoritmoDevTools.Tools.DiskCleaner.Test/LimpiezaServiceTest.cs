using AlgoritmoDevTools.Tools.DiskCleaner.Model;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// La previsualización.
///
/// <b>Que no borre nada es lo que más importa de toda la herramienta</b>: es lo que el usuario mira
/// antes de decidir, y si eso tocara el disco no habría ningún momento en que pueda echarse atrás.
/// </summary>
public class LimpiezaServiceTest
{
    private static ItemLimpieza Item(
        string id,
        string[] rutas,
        AccionDerivada? deriva = null,
        string? motivo = null) => new()
    {
        Id = id,
        Ubicacion = id,
        Descripcion = "qué es",
        Consecuencia = "qué se pierde",
        Grupo = GrupoLimpieza.Seguro,
        Rutas = rutas,
        Derivacion = deriva,
        MotivoParaNoTocarlo = motivo
    };

    [Fact]
    public async Task El_dry_run_no_borra_nada()
    {
        using var arbol = new ArbolDePrueba();
        var carpeta = arbol.Carpeta("paraBorrar");
        var dentro = arbol.Archivo(@"paraBorrar\uno.bin", 1000);
        var hondo = arbol.Archivo(@"paraBorrar\hondo\dos.bin", 500);
        var suelto = arbol.Archivo("suelto.bin", 250);

        var plan = await new LimpiezaService().PrevisualizarAsync(
            [new ItemOfrecido(Item("normal", [carpeta, suelto]), true)]);

        Assert.False(plan.EstaVacio);

        // Todo sigue exactamente donde estaba, con el mismo tamaño.
        Assert.True(Directory.Exists(carpeta));
        Assert.True(File.Exists(dentro));
        Assert.True(File.Exists(hondo));
        Assert.Equal(250, new FileInfo(suelto).Length);
    }

    [Fact]
    public async Task Mide_lo_que_realmente_entra()
    {
        using var arbol = new ArbolDePrueba();
        var carpeta = arbol.Carpeta("paraBorrar");
        arbol.Archivo(@"paraBorrar\uno.bin", 1000);
        arbol.Archivo(@"paraBorrar\hondo\dos.bin", 500);
        var suelto = arbol.Archivo("suelto.bin", 250);

        var plan = await new LimpiezaService().PrevisualizarAsync(
            [new ItemOfrecido(Item("normal", [carpeta, suelto]), true)]);

        Assert.Equal(1750, plan.Bytes);
        Assert.Equal(2, plan.Rutas);
    }

    [Fact]
    public async Task Lo_destildado_no_entra()
    {
        using var arbol = new ArbolDePrueba();
        var carpeta = arbol.Carpeta("x");

        var plan = await new LimpiezaService().PrevisualizarAsync(
            [new ItemOfrecido(Item("destildado", [carpeta]), false)]);

        Assert.True(plan.EstaVacio);
    }

    [Fact]
    public async Task Vuelve_a_filtrar_lo_que_ya_estaba_filtrado()
    {
        // Todo esto ya se filtró antes de llegar acá. Se vuelve a filtrar porque este es el último
        // punto antes del botón: si algo de esto llegó, hay un error en otro lado.
        using var arbol = new ArbolDePrueba();
        var carpeta = arbol.Carpeta("x");
        var protegida = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlgoritmoDevTools");

        var plan = await new LimpiezaService().PrevisualizarAsync(
        [
            new ItemOfrecido(Item("deriva", [carpeta], deriva: new AccionDerivada("Abrir", "x")), true),
            new ItemOfrecido(Item("trabado", [carpeta], motivo: "hay una transacción abierta"), true),
            new ItemOfrecido(Item("protegido", [protegida]), true),
            new ItemOfrecido(Item("fantasma", [Path.Combine(arbol.Raiz, "no-existe")]), true)
        ]);

        Assert.True(plan.EstaVacio);
        Assert.Equal(4, plan.Descartados.Count);
    }

    [Fact]
    public async Task Cada_descarte_dice_su_motivo()
    {
        using var arbol = new ArbolDePrueba();
        var protegida = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlgoritmoDevTools");

        var plan = await new LimpiezaService().PrevisualizarAsync(
            [new ItemOfrecido(Item("protegido", [protegida]), true)]);

        // "Ya no hay nada ahí" sonaría a que la carpeta desapareció, y lo que pasa es que no se
        // mira: cuál de los dos motivos es cambia qué entiende quien lo lee.
        Assert.Contains("no se tocan", Assert.Single(plan.Descartados));
    }

    [Fact]
    public async Task Un_item_con_rutas_mixtas_entra_solo_con_las_que_quedan()
    {
        using var arbol = new ArbolDePrueba();
        var existe = arbol.Carpeta("existe");
        arbol.Archivo(@"existe\x.bin", 60);

        var plan = await new LimpiezaService().PrevisualizarAsync(
            [new ItemOfrecido(Item("mixto", [existe, Path.Combine(arbol.Raiz, "no-existe")]), true)]);

        Assert.Equal(1, plan.Rutas);
        Assert.Equal(60, plan.Bytes);
    }

    [Fact]
    public async Task Avisa_cuando_algo_va_a_necesitar_permisos()
    {
        using var arbol = new ArbolDePrueba();
        var carpeta = arbol.Carpeta("x");
        arbol.Archivo(@"x\a.bin", 10);

        var conAdmin = Item("conAdmin", [carpeta]) with { RequiereAdmin = true };
        var plan = await new LimpiezaService().PrevisualizarAsync([new ItemOfrecido(conAdmin, true)]);

        Assert.True(plan.NecesitaElevar);
    }
}
