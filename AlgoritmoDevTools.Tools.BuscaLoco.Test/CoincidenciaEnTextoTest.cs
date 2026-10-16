using System.Diagnostics;
using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// Lo literal, lo regular, y sobre todo que una expresión no pueda colgar la búsqueda.
/// </summary>
public class CoincidenciaEnTextoTest
{
    private static CoincidenciaEnTexto Crear(string texto, bool esExpresion = false, bool mayusculas = false, bool palabra = false)
    {
        var buscador = CoincidenciaEnTexto.Crear(texto, esExpresion, mayusculas, palabra, out var error);
        Assert.Null(error);
        return buscador!;
    }

    [Fact]
    public void Encuentra_el_texto_literal()
    {
        var coincidencias = Crear("Cotizacion").Buscar("var TipoCotizacion = 1;").ToList();

        Assert.Single(coincidencias);
        Assert.Equal((8, 10), coincidencias[0]);
    }

    [Fact]
    public void Sin_distinguir_mayusculas_encuentra_igual()
    {
        Assert.Single(Crear("tipocotizacion").Buscar("var TipoCotizacion = 1;"));
    }

    [Fact]
    public void Distinguiendo_mayusculas_no_encuentra()
    {
        Assert.Empty(Crear("tipocotizacion", mayusculas: true).Buscar("var TipoCotizacion = 1;"));
    }

    [Fact]
    public void Palabra_entera_no_cuenta_adentro_de_otra_palabra()
    {
        var coincidencias = Crear("Cotizacion", palabra: true).Buscar("TipoCotizacionDTO y Cotizacion suelta").ToList();

        // La de TipoCotizacionDTO esta pegada a letras de los dos lados; la otra esta sola.
        Assert.Single(coincidencias);
        Assert.Equal(20, coincidencias[0].Desde);
    }

    [Fact]
    public void Encuentra_todas_las_veces_que_aparece_en_la_linea()
    {
        Assert.Equal(2, Crear("Monto").Buscar("Monto = otroMonto;").Count());
    }

    [Fact]
    public void Una_expresion_encuentra_las_dos_formas()
    {
        var coincidencias = Crear("TipoCotizacion(DTO)?", esExpresion: true)
            .Buscar("CreateMap<TipoCotizacion, TipoCotizacionDTO>()")
            .ToList();

        Assert.Equal(2, coincidencias.Count);
        Assert.Equal(14, coincidencias[0].Largo);     // TipoCotizacion
        Assert.Equal(17, coincidencias[1].Largo);     // TipoCotizacionDTO
    }

    [Fact]
    public void Una_expresion_que_matchea_vacio_no_devuelve_nada()
    {
        // `a*` matchea la cadena vacia en cada posicion: resaltar eso seria resaltar la nada. La
        // linea no tiene ninguna `a`, asi que todo lo que encuentre seria de largo cero.
        Assert.Empty(Crear("a*", esExpresion: true).Buscar("zzz zzz"));
    }

    [Fact]
    public void Una_expresion_invalida_avisa_en_lugar_de_tirar()
    {
        var buscador = CoincidenciaEnTexto.Crear("TipoCotizacion(DTO", esExpresion: true, false, false, out var error);

        Assert.Null(buscador);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Una_expresion_con_lookaround_igual_funciona()
    {
        // NonBacktracking no acepta lookarounds: tiene que caer al motor clasico sin que se note.
        var buscador = CoincidenciaEnTexto.Crear(@"Monto(?=\s*=)", esExpresion: true, false, false, out var error);

        Assert.Null(error);
        Assert.Single(buscador!.Buscar("Monto = 1; otroMonto;"));
    }

    [Fact]
    public void Un_patron_con_backtracking_catastrofico_termina()
    {
        // Es LA prueba de la decision: sin NonBacktracking esto tarda tiempo exponencial, y como
        // Regex no consulta el CancellationToken, cancelar no lo salva. El sintoma es "se colgo".
        var buscador = Crear("(a+)+$", esExpresion: true);
        var linea = new string('a', 5000) + "X";

        var reloj = Stopwatch.StartNew();
        var coincidencias = buscador.Buscar(linea).ToList();
        reloj.Stop();

        Assert.Empty(coincidencias);
        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(10), $"tardó {reloj.Elapsed.TotalSeconds:N1} s");
    }

    [Fact]
    public void Una_linea_vacia_no_devuelve_nada()
    {
        Assert.Empty(Crear("algo").Buscar(string.Empty));
    }
}
