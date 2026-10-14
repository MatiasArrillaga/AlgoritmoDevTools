using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;
using AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// Qué ofrece el catálogo, corrido contra la máquina real.
///
/// Es un test de integración: tarda, porque mide de verdad. Lo que verifica no se puede verificar
/// de otra forma — que algo no aparezca en el resultado sólo se sabe mirando el resultado.
/// </summary>
[Trait("Velocidad", "Lento")]
public class CatalogoTest(EscaneoCompartido compartido) : IClassFixture<EscaneoCompartido>
{
    /// <summary>
    /// Un solo escaneo para todos los tests de esta clase. Medir el disco tarda cerca de un minuto
    /// y acá se mira el mismo resultado desde varios ángulos: repetirlo por test convertiría la
    /// suite en algo que nadie corre.
    /// </summary>
    private Task<Escaneo> Escanear() => compartido.Resultado;

    [Fact]
    public async Task Ningun_item_expone_lo_que_no_se_debe_borrar()
    {
        var escaneo = await Escanear();
        var todo = escaneo.Items
            .SelectMany(i => i.Rutas.Append(i.Ubicacion).Append(i.Descripcion).Append(i.Consecuencia))
            .ToArray();

        // Un grupo "nunca borrar" es una tentación: alguien lee la ruta y la borra por fuera de la
        // herramienta. El total puede ser honesto sin nombrarlas.
        Assert.DoesNotContain(todo, t => t.Contains(@"Windows\Installer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(todo, t => t.Contains("pagefile.sys", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Instances_nunca_entra_en_lo_que_se_borra()
    {
        var escaneo = await Escanear();
        var aBorrar = escaneo.Items.SelectMany(i => i.Rutas).ToArray();

        // Es la trampa más cara del documento: borrar eso se llevó el inventario de productos
        // instalados y no se pudo recuperar. Hoy no puede pasar porque el ítem no borra; el test
        // existe para que siga siendo cierto si alguien le agrega borrado más adelante.
        Assert.DoesNotContain(aBorrar, r => r.Contains("_Instances", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(aBorrar, r => Path.GetFileName(r).StartsWith('_'));
        Assert.DoesNotContain(aBorrar, r =>
            r.Contains("VisualStudio", StringComparison.OrdinalIgnoreCase) &&
            r.Contains("Packages", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Ninguna_ruta_protegida_llega_al_resultado()
    {
        var escaneo = await Escanear();

        Assert.DoesNotContain(
            escaneo.Items.SelectMany(i => i.Rutas),
            RutasProtegidas.EstaProtegida);
    }

    [Fact]
    public async Task Lo_que_deriva_no_lleva_rutas_para_borrar()
    {
        var escaneo = await Escanear();

        // Dejarlas cargadas sería dejar a mano una lista de cosas para borrar que justamente se
        // decidió no borrar.
        Assert.All(
            escaneo.Items.Where(i => !i.SeBorra),
            i => Assert.Empty(i.Rutas));
    }

    [Fact]
    public async Task Lo_seguro_viene_tildado_y_lo_demas_no()
    {
        var escaneo = await Escanear();

        Assert.All(escaneo.Items, i =>
            Assert.Equal(i.Grupo == GrupoLimpieza.Seguro, i.TildadoPorDefecto));
    }

    [Fact]
    public async Task Un_detector_que_falla_no_voltea_el_escaneo()
    {
        var servicio = new EscaneoService([new DetectorQueFalla(), new DetectorQueAnda()]);
        var escaneo = await servicio.EscanearAsync();

        Assert.Single(escaneo.Items);
        Assert.Equal("el que falla", Assert.Single(escaneo.Fallaron));
    }

    private sealed class DetectorQueFalla : IDetector
    {
        public string Nombre => "el que falla";
        public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
            throw new InvalidOperationException("se rompió");
    }

    private sealed class DetectorQueAnda : IDetector
    {
        public string Nombre => "el que anda";

        public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
            Task.FromResult<IReadOnlyList<ItemLimpieza>>(
            [
                new ItemLimpieza
                {
                    Id = "anda",
                    Ubicacion = "anda",
                    Descripcion = "qué es",
                    Consecuencia = "qué se pierde"
                }
            ]);
    }

}
