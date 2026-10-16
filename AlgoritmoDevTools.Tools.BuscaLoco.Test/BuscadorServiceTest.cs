using AlgoritmoDevTools.Tools.BuscaLoco.Model;
using AlgoritmoDevTools.Tools.BuscaLoco.Services;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Test;

/// <summary>
/// El buscador entero, sobre archivos de verdad.
/// </summary>
public class BuscadorServiceTest
{
    private readonly BuscadorService _buscador = new();

    private static CriterioDeBusqueda Criterio(
        string raiz,
        string texto = "",
        string nombre = "",
        bool expresion = false,
        bool subcarpetas = true,
        bool ruido = true,
        bool palabra = false,
        bool mayusculas = false) =>
        new(raiz, texto, nombre, expresion, false, subcarpetas, mayusculas, palabra, ruido);

    [Fact]
    public async Task Encuentra_el_texto_en_los_dos_encodings()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("nuevo.cs", "// Cantidad de Días\n");
        arbol.TextoAnsi("viejo.cs", "// Cálculo de Días\n");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días", "*.cs"));

        Assert.Equal(2, resultado.Archivos.Count);
    }

    [Fact]
    public async Task No_mira_adentro_de_los_binarios()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Binario("compilado.dll", "Días");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días"));

        Assert.Empty(resultado.Archivos);
    }

    [Fact]
    public async Task Saltea_las_carpetas_de_ruido()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("bueno.cs", "Días");
        arbol.Texto(@"obj\generado.cs", "Días");
        arbol.Texto(@"bin\compilado.cs", "Días");
        arbol.Texto(@"node_modules\paquete.cs", "Días");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días", "*.cs"));

        Assert.Single(resultado.Archivos);
    }

    [Fact]
    public async Task Con_el_ruido_apagado_mira_tambien_esas_carpetas()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("bueno.cs", "Días");
        arbol.Texto(@"obj\generado.cs", "Días");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días", "*.cs", ruido: false));

        Assert.Equal(2, resultado.Archivos.Count);
    }

    [Fact]
    public async Task Sin_subcarpetas_solo_mira_la_raiz()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("raiz.cs", "Días");
        arbol.Texto(@"adentro\hondo.cs", "Días");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días", subcarpetas: false));

        Assert.Single(resultado.Archivos);
        Assert.Equal("raiz.cs", resultado.Archivos[0].Nombre);
    }

    [Fact]
    public async Task Los_dos_campos_se_combinan_con_y()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "TipoCotizacion");
        arbol.Texto("dos.razor", "TipoCotizacion");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "TipoCotizacion", "*.razor"));

        Assert.Single(resultado.Archivos);
        Assert.Equal("dos.razor", resultado.Archivos[0].Nombre);
    }

    [Fact]
    public async Task Solo_por_nombre_devuelve_el_archivo_sin_abrirlo()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.razor", "lo que sea");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, nombre: "*.razor"));

        Assert.Single(resultado.Archivos);
        Assert.Empty(resultado.Archivos[0].Hallazgos);
    }

    [Fact]
    public async Task Cuenta_una_coincidencia_por_aparicion_y_no_por_linea()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "Monto = otroMonto;\n");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Monto"));

        Assert.Equal(2, resultado.Coincidencias);
    }

    [Fact]
    public async Task El_hallazgo_trae_el_numero_de_linea_correcto()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "primera\nsegunda\nacá está Días\n");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días"));

        Assert.Equal(3, resultado.Archivos[0].Hallazgos[0].Linea);
    }

    [Fact]
    public async Task Avisa_por_lotes_mientras_recorre()
    {
        using var arbol = new ArbolDeArchivos();
        for (var i = 0; i < 30; i++) arbol.Texto($"archivo{i}.cs", "Días");

        var avisados = 0;
        var avance = new AvanceDePrueba(lote => avisados += lote.Count);

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días"), avance);

        // Lo que importa no es cuantos lotes, sino que por los lotes pase todo lo encontrado: si se
        // perdiera alguno, la pantalla mostraria menos de lo que hubo.
        Assert.Equal(resultado.Archivos.Count, avisados);
    }

    [Fact]
    public async Task Cancelar_corta_la_busqueda()
    {
        using var arbol = new ArbolDeArchivos();
        for (var i = 0; i < 50; i++) arbol.Texto($"archivo{i}.cs", "Días");

        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días"), cancelacion: cancelacion.Token));
    }

    [Fact]
    public async Task Las_rutas_devueltas_nunca_llevan_el_prefijo_de_ruta_larga()
    {
        using var arbol = new ArbolDeArchivos();
        arbol.Texto("uno.cs", "Días");

        var resultado = await _buscador.BuscarAsync(Criterio(arbol.Raiz, "Días"));

        Assert.All(resultado.Archivos, a => Assert.False(a.Ruta.StartsWith(@"\\?\", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "  ")]
    public void No_se_puede_buscar_sin_parametros(string texto, string nombre)
    {
        var sePuede = BuscadorService.SePuedeBuscar(Criterio(@"C:\algo", texto, nombre), out var error);

        Assert.False(sePuede);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("algo", "")]
    [InlineData("", "*.cs")]
    [InlineData("algo", "*.cs")]
    public void Con_al_menos_un_campo_si_se_puede(string texto, string nombre)
    {
        Assert.True(BuscadorService.SePuedeBuscar(Criterio(@"C:\algo", texto, nombre), out _));
    }

    [Fact]
    public void Sin_carpeta_no_se_puede_buscar()
    {
        Assert.False(BuscadorService.SePuedeBuscar(Criterio(string.Empty, "algo"), out _));
    }

    [Fact]
    public void Una_expresion_invalida_deja_el_boton_bloqueado()
    {
        // Bloqueado y no habilitado-y-fallando: si la validacion viviera en el manejador del clic,
        // el boton se veria disponible y no haria nada.
        Assert.False(BuscadorService.SePuedeBuscar(Criterio(@"C:\algo", "TipoCotizacion(DTO", expresion: true), out _));
    }

    private sealed class AvanceDePrueba(Action<IReadOnlyList<ArchivoConHallazgos>> alAvanzar)
        : IProgress<IReadOnlyList<ArchivoConHallazgos>>
    {
        public void Report(IReadOnlyList<ArchivoConHallazgos> value) => alAvanzar(value);
    }
}
