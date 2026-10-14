using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// Las reglas del modelo: qué se puede elegir y qué no.
/// </summary>
public class ItemLimpiezaTest
{
    private static ItemLimpieza Item(GrupoLimpieza grupo = GrupoLimpieza.Seguro) => new()
    {
        Id = "x",
        Ubicacion = "x",
        Descripcion = "qué es",
        Consecuencia = "qué se pierde",
        Grupo = grupo
    };

    [Fact]
    public void Lo_seguro_viene_tildado_y_lo_demas_no()
    {
        // Sale del grupo y no se asigna a mano: así el invariante deja de ser algo que cada
        // detector tenga que recordar.
        Assert.True(Item(GrupoLimpieza.Seguro).TildadoPorDefecto);
        Assert.False(Item(GrupoLimpieza.ConCriterio).TildadoPorDefecto);
    }

    [Fact]
    public void Un_proceso_en_ejecucion_advierte_pero_no_impide_elegir()
    {
        // En una máquina de desarrollo casi siempre hay un compilador vivo. Tomarlo como
        // impedimento dejaría el ítem más grande del grupo seguro apagado prácticamente siempre, y
        // de 446 carpetas fallaron 3 —dos de las cuales se borraron en el reintento—.
        var conProcesos = Item() with
        {
            Bloqueantes = [new ProcesoBloqueante(123, "MSBuild", null)]
        };

        Assert.True(conProcesos.SePuedeElegir);
    }

    [Fact]
    public void Un_motivo_en_contra_si_impide_elegir()
    {
        var trabado = Item() with { MotivoParaNoTocarlo = "hay una transacción abierta" };
        Assert.False(trabado.SePuedeElegir);
    }

    [Fact]
    public void Lo_que_deriva_no_se_borra_ni_se_elige()
    {
        var deriva = Item() with { Derivacion = new AccionDerivada("Abrir", "x") };

        Assert.False(deriva.SeBorra);
        Assert.False(deriva.SePuedeElegir);
    }

    [Fact]
    public void Lo_normal_se_puede_elegir()
    {
        var normal = Item();

        Assert.True(normal.SeBorra);
        Assert.True(normal.SePuedeElegir);
    }
}
