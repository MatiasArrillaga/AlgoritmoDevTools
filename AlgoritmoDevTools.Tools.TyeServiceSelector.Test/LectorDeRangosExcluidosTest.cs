using AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Test;

/// <summary>
/// Los tramos de puertos que Windows se reserva, y el cruce contra los que piden los contenedores.
///
/// La salida de ejemplo es la real de esta máquina el 29/10/2026, el día que el tramo 58021-58120
/// dejó a <c>dapr_placement</c> y <c>dapr_scheduler</c> sin poder arrancar.
/// </summary>
public class LectorDeRangosExcluidosTest
{
    private const string SalidaDeNetsh = """

        Protocolo tcp Intervalos de exclusión de puertos

        Puerto de inicio    Puerto final
        ----------          --------
              2869        2869
              5357        5357
             50000       50059     *
             57821       57920
             58021       58120
             59829       59928

        * - Exclusiones de puertos administrados.
        """;

    [Fact]
    public void Lee_todos_los_tramos_incluido_el_administrado()
    {
        var rangos = LectorDeRangosExcluidos.Parsear(SalidaDeNetsh);

        Assert.Equal(6, rangos.Count);
        Assert.Contains(rangos, r => r.Desde == 58021 && r.Hasta == 58120);
        // El que viene con asterisco es una exclusión administrada, pero reserva igual.
        Assert.Contains(rangos, r => r.Desde == 50000 && r.Hasta == 50059);
    }

    [Fact]
    public void No_confunde_los_titulos_ni_los_separadores_con_tramos()
    {
        // El encabezado está traducido y cambia con el idioma de Windows: por eso se leen los
        // números y no los títulos.
        var rangos = LectorDeRangosExcluidos.Parsear(SalidaDeNetsh);

        Assert.DoesNotContain(rangos, r => r.Desde == 0);
        Assert.All(rangos, r => Assert.True(r.Desde <= r.Hasta));
    }

    [Fact]
    public void Una_salida_vacia_o_rota_no_tira()
    {
        Assert.Empty(LectorDeRangosExcluidos.Parsear(""));
        Assert.Empty(LectorDeRangosExcluidos.Parsear("netsh no esta disponible"));
    }

    [Fact]
    public void El_cruce_encuentra_el_puerto_tapado_y_dice_por_que_tramo()
    {
        var rangos = LectorDeRangosExcluidos.Parsear(SalidaDeNetsh);

        // Los dos que pide dapr_placement: el 6050 está libre, el 58080 cae en el tramo reservado.
        var choques = LectorDeRangosExcluidos.Cruzar([6050, 58080, 59090], rangos);

        var (puerto, rango) = Assert.Single(choques);
        Assert.Equal(58080, puerto);
        Assert.Equal(58021, rango.Desde);
        Assert.Equal(58120, rango.Hasta);
    }

    [Fact]
    public void Sin_choques_el_cruce_vuelve_vacio()
    {
        var rangos = LectorDeRangosExcluidos.Parsear(SalidaDeNetsh);

        Assert.Empty(LectorDeRangosExcluidos.Cruzar([2735, 6050, 6060], rangos));
    }
}
