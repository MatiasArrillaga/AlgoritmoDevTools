using AlgoritmoDevTools.Tools.BuscaLoco.Model;
using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// Lo que sale al exportar, y sobre todo que una fila no se parta por el contenido.
/// </summary>
public class ExportadorDeResultadosTest
{
    private static ArchivoConHallazgos Archivo(string ruta, params Hallazgo[] hallazgos) => new(ruta, hallazgos);

    private static Hallazgo Hallazgo(string ruta, int linea, string texto) => new(ruta, linea, texto, 0, 1);

    /// <summary>Cuenta los separadores que están fuera de comillas, que son los que parten columnas.</summary>
    private static int Columnas(string renglon)
    {
        var columnas = 1;
        var adentro = false;

        foreach (var caracter in renglon)
        {
            if (caracter == '"') adentro = !adentro;
            else if (caracter == ';' && !adentro) columnas++;
        }

        return columnas;
    }

    [Fact]
    public void El_csv_arranca_con_el_encabezado()
    {
        var csv = ExportadorDeResultados.ACsv([Archivo(@"C:\x\uno.cs", Hallazgo(@"C:\x\uno.cs", 1, "algo"))]);

        Assert.StartsWith("Archivo;Carpeta;Linea;Texto", csv);
    }

    [Fact]
    public void Hay_una_fila_por_coincidencia()
    {
        var csv = ExportadorDeResultados.ACsv(
        [
            Archivo(@"C:\x\uno.cs", Hallazgo(@"C:\x\uno.cs", 1, "a"), Hallazgo(@"C:\x\uno.cs", 7, "b")),
            Archivo(@"C:\x\dos.cs", Hallazgo(@"C:\x\dos.cs", 3, "c"))
        ]);

        // Tres coincidencias mas el encabezado.
        Assert.Equal(4, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Una_ruta_con_coma_no_parte_el_renglon()
    {
        // Con separador punto y coma, la coma no necesita escape: lo que importa es que el renglon
        // siga teniendo sus cuatro columnas.
        var csv = ExportadorDeResultados.ACsv(
            [Archivo(@"C:\carpeta, con coma\uno.cs", Hallazgo(@"C:\carpeta, con coma\uno.cs", 1, "algo"))]);

        var renglon = csv.Split('\n')[1];

        Assert.Equal(4, Columnas(renglon));
    }

    [Fact]
    public void Un_punto_y_coma_en_el_texto_queda_entre_comillas()
    {
        var csv = ExportadorDeResultados.ACsv(
            [Archivo(@"C:\x\uno.cs", Hallazgo(@"C:\x\uno.cs", 1, "var a = 1; var b = 2;"))]);

        var renglon = csv.Split('\n')[1];

        Assert.Contains('"', renglon);
        Assert.Equal(4, Columnas(renglon));
    }

    [Fact]
    public void Una_comilla_en_el_texto_se_duplica()
    {
        var csv = ExportadorDeResultados.ACsv(
            [Archivo(@"C:\x\uno.cs", Hallazgo(@"C:\x\uno.cs", 1, "dice \"hola\"; y sigue"))]);

        Assert.Contains("\"\"hola\"\"", csv);
    }

    [Fact]
    public void Un_archivo_encontrado_solo_por_nombre_figura_igual()
    {
        var csv = ExportadorDeResultados.ACsv([Archivo(@"C:\x\uno.razor")]);

        Assert.Equal(2, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("uno.razor", csv);
    }

    [Fact]
    public void El_texto_plano_sale_como_ruta_linea_texto()
    {
        var txt = ExportadorDeResultados.ATexto(
            [Archivo(@"C:\x\uno.cs", Hallazgo(@"C:\x\uno.cs", 42, "  var a = 1;  "))]);

        Assert.Equal(@"C:\x\uno.cs:42:var a = 1;", txt.Trim());
    }

    [Theory]
    [InlineData(@"C:\salida.csv", "Archivo;")]
    [InlineData(@"C:\salida.txt", @"C:\x\uno.cs:1:")]
    public void El_formato_sale_de_la_extension_elegida(string destino, string comoArranca)
    {
        var contenido = ExportadorDeResultados.Armar(
            [Archivo(@"C:\x\uno.cs", Hallazgo(@"C:\x\uno.cs", 1, "algo"))], destino);

        Assert.StartsWith(comoArranca, contenido);
    }
}
