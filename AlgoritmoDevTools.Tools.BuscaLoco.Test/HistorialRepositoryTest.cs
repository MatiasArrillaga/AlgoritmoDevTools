using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// El historial de búsquedas y carpetas.
///
/// La poda es lo que más conviene tener cubierto: identifica la fila por <c>rowid</c> porque armar
/// la clave compuesta pegando los dos textos dentro del SQL rompía la sentencia entera.
/// </summary>
public class HistorialRepositoryTest
{
    [Fact]
    public void Guarda_y_devuelve_una_busqueda()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarBusqueda("Días", "*.cs", false);

        var guardadas = historial.UltimasBusquedas();

        Assert.Single(guardadas);
        Assert.Equal("Días", guardadas[0].Texto);
        Assert.Equal("*.cs", guardadas[0].Nombre);
        Assert.False(guardadas[0].EsExpresion);
    }

    [Fact]
    public void La_misma_busqueda_dos_veces_no_se_duplica()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarBusqueda("Días", "*.cs", false);
        historial.GuardarBusqueda("Días", "*.cs", false);

        Assert.Single(historial.UltimasBusquedas());
    }

    [Fact]
    public void El_mismo_texto_con_otro_nombre_es_otra_busqueda()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarBusqueda("Días", "*.cs", false);
        historial.GuardarBusqueda("Días", "*.razor", false);

        Assert.Equal(2, historial.UltimasBusquedas().Count);
    }

    [Fact]
    public void La_mas_reciente_va_primero()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarBusqueda("vieja", "", false);
        Thread.Sleep(5);
        historial.GuardarBusqueda("nueva", "", false);

        Assert.Equal("nueva", historial.UltimasBusquedas()[0].Texto);
    }

    [Fact]
    public void Las_dos_vacias_no_se_guardan()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarBusqueda("", "", false);
        historial.GuardarBusqueda("   ", "  ", false);

        Assert.Empty(historial.UltimasBusquedas());
    }

    [Fact]
    public void Se_guardan_hasta_veinte_y_quedan_las_mas_nuevas()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        for (var i = 0; i < 25; i++)
        {
            historial.GuardarBusqueda($"busqueda{i:D2}", "", false);
            Thread.Sleep(2);
        }

        var guardadas = historial.UltimasBusquedas();

        Assert.Equal(20, guardadas.Count);
        Assert.Equal("busqueda24", guardadas[0].Texto);
        Assert.DoesNotContain(guardadas, b => b.Texto == "busqueda00");
    }

    [Fact]
    public void Guarda_y_devuelve_una_carpeta()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarRaiz(@"C:\repos\AlgoritmoCore");

        Assert.Contains(@"C:\repos\AlgoritmoCore", historial.UltimasRaices());
    }

    [Fact]
    public void La_misma_carpeta_dos_veces_no_se_duplica()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarRaiz(@"C:\repos");
        historial.GuardarRaiz(@"C:\repos");

        Assert.Single(historial.UltimasRaices());
    }

    [Fact]
    public void Un_texto_con_caracteres_raros_no_rompe_la_poda()
    {
        // La poda armaba la clave pegando texto y nombre con un separador adentro del SQL, y eso
        // reventaba la sentencia. Ahora va por rowid, asi que nada de lo que escriba el usuario
        // puede romperla.
        using var almacenamiento = new AlmacenamientoDePrueba();
        var historial = new HistorialRepository(almacenamiento);

        historial.GuardarBusqueda("con 'comillas' y \"dobles\"", "*.cs", false);
        historial.GuardarBusqueda("con ; punto y coma", "*.cs", false);
        historial.GuardarBusqueda("con -- guiones de comentario", "*.cs", false);

        Assert.Equal(3, historial.UltimasBusquedas().Count);
    }

    [Fact]
    public void Abrir_dos_veces_el_mismo_almacenamiento_no_pierde_lo_guardado()
    {
        using var almacenamiento = new AlmacenamientoDePrueba();

        new HistorialRepository(almacenamiento).GuardarBusqueda("Días", "*.cs", false);

        // El esquema se crea si no existe: volver a abrirlo no puede borrar nada.
        Assert.Single(new HistorialRepository(almacenamiento).UltimasBusquedas());
    }
}
