using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// El glob, los varios patrones separados por coma, y la trampa de que en modo expresión la coma
/// deja de separar porque ahí es sintaxis.
/// </summary>
public class FiltroDeNombreTest
{
    private static FiltroDeNombre Crear(string patron, bool esExpresion = false, bool mayusculas = false)
    {
        var filtro = FiltroDeNombre.Crear(patron, esExpresion, mayusculas, out var error);
        Assert.Null(error);
        return filtro!;
    }

    [Fact]
    public void Un_patron_vacio_deja_pasar_todo()
    {
        var filtro = Crear(string.Empty);

        Assert.True(filtro.DejaPasarTodo);
        Assert.True(filtro.Coincide("cualquier.cosa"));
    }

    [Fact]
    public void El_glob_de_extension_filtra_por_extension()
    {
        var filtro = Crear("*.cs");

        Assert.True(filtro.Coincide("Programa.cs"));
        Assert.False(filtro.Coincide("Pagina.razor"));
    }

    [Fact]
    public void El_punto_del_glob_es_un_punto_y_no_cualquier_caracter()
    {
        // Sin escapar, "*.cs" encontraria tambien "archivocs".
        Assert.False(Crear("*.cs").Coincide("archivocs"));
    }

    [Theory]
    [InlineData("*.cs, *.razor")]
    [InlineData("*.cs;*.razor")]
    [InlineData("*.cs ; *.razor")]
    public void Varios_patrones_se_separan_por_coma_o_punto_y_coma(string patron)
    {
        var filtro = Crear(patron);

        Assert.True(filtro.Coincide("Programa.cs"));
        Assert.True(filtro.Coincide("Pagina.razor"));
        Assert.False(filtro.Coincide("datos.json"));
    }

    [Fact]
    public void Un_patron_sin_comodines_busca_por_parte_del_nombre()
    {
        var filtro = Crear("Factory");

        Assert.True(filtro.Coincide("OperacionFactory.cs"));
        Assert.True(filtro.Coincide("Factory.cs"));
        Assert.False(filtro.Coincide("Operacion.cs"));
    }

    [Fact]
    public void En_modo_expresion_la_coma_NO_separa()
    {
        // Es la trampa: `\w{2,3}` lleva una coma que es sintaxis. Partir por ella dejaria dos
        // patrones rotos, y en silencio.
        var filtro = Crear(@"^\w{2,3}\.cs$", esExpresion: true);

        Assert.True(filtro.Coincide("abc.cs"));
        Assert.False(filtro.Coincide("abcde.cs"));
    }

    [Fact]
    public void Una_expresion_invalida_avisa_en_lugar_de_tirar()
    {
        var filtro = FiltroDeNombre.Crear("(sin cerrar", esExpresion: true, false, out var error);

        Assert.Null(filtro);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Sin_distinguir_mayusculas_encuentra_igual()
    {
        Assert.True(Crear("*.CS").Coincide("programa.cs"));
    }

    [Fact]
    public void Distinguiendo_mayusculas_no_encuentra()
    {
        Assert.False(Crear("*.CS", mayusculas: true).Coincide("programa.cs"));
    }
}
