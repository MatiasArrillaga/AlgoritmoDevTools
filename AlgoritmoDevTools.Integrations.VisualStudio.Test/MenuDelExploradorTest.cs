namespace AlgoritmoDevTools.Integrations.VisualStudio.Test;

/// <summary>
/// El parser de la línea del registro es la pieza que puede romperse en silencio: si el Commands
/// Maker cambia cómo escribe el ítem, acá no falla nada en compilación y el síntoma sería un menú
/// que aparece con los nombres pelados, sin explicación.
/// </summary>
public class MenuDelExploradorTest
{
    /// <summary>
    /// La línea exacta que escribe <c>MenuContextualDevTools.EscribirItem</c>, leída del registro
    /// de un equipo real. Si esta prueba falla, el formato cambió.
    /// </summary>
    [Fact]
    public void Interpretar_la_linea_real_del_registro()
    {
        var entrada = MenuDelExplorador.Interpretar(
            @"""C:\Users\alguien\Desktop\DevTools.exe"" --comando-add Cereales");

        Assert.NotNull(entrada);
        Assert.Equal(@"C:\Users\alguien\Desktop\DevTools.exe", entrada!.Ejecutable);
        Assert.Equal("Cereales", entrada.Dominio);
    }

    /// <summary>Las rutas del equipo tienen espacios casi siempre: por eso el exe va entre comillas.</summary>
    [Fact]
    public void Interpretar_una_ruta_con_espacios()
    {
        var entrada = MenuDelExplorador.Interpretar(
            @"""C:\Program Files\Algoritmo DevTools\DevTools.exe"" --comando-update OperacionesComerciales");

        Assert.NotNull(entrada);
        Assert.Equal(@"C:\Program Files\Algoritmo DevTools\DevTools.exe", entrada!.Ejecutable);
        Assert.Equal("OperacionesComerciales", entrada.Dominio);
    }

    /// <summary>
    /// Hoy ningún dominio tiene espacios, pero el dominio se toma como todo lo que sigue al verbo y
    /// no como el último token: así un dominio nuevo con espacio falla donde corresponde y no acá,
    /// en silencio y recortado.
    /// </summary>
    [Fact]
    public void Interpretar_un_dominio_con_espacios()
    {
        var entrada = MenuDelExplorador.Interpretar(@"""C:\x\DevTools.exe"" --comando-remove Cuenta Corriente");

        Assert.NotNull(entrada);
        Assert.Equal("Cuenta Corriente", entrada!.Dominio);
    }

    /// <summary>
    /// Todo lo que no tenga la forma esperada da null, que es el mismo camino que "no está
    /// instalado": los ítems quedan con el nombre pelado y explican qué falta. Lo que no puede pasar
    /// nunca es que tire una excepción, porque corre durante la carga de la extensión.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("\"")]
    [InlineData("C:\\x\\DevTools.exe --comando-add Cereales")]      // sin comillas
    [InlineData("\"C:\\x\\DevTools.exe")]                            // comilla sin cerrar
    [InlineData("\"C:\\x\\DevTools.exe\"")]                          // sin argumentos
    [InlineData("\"C:\\x\\DevTools.exe\" --comando-add")]            // verbo sin dominio
    [InlineData("\"C:\\x\\DevTools.exe\" --comando-add   ")]         // dominio en blanco
    [InlineData("\"\" --comando-add Cereales")]                      // sin ejecutable
    public void Lo_que_no_tiene_la_forma_esperada_da_null(string linea)
        => Assert.Null(MenuDelExplorador.Interpretar(linea));
}
