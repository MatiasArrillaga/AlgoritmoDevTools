using AlgoritmoDevTools.Tools.DiskCleaner.Services;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// La lista negra: lo que el escáner no mira.
///
/// Es el test que impide reintroducir la pérdida del historial de conexiones de SSMS, y corre
/// sobre la lista completa y no sobre un caso suelto.
/// </summary>
public class RutasProtegidasTest
{
    private static string Roaming => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    [Fact]
    public void Ninguna_regla_vigente_queda_desprotegida()
    {
        Assert.NotEmpty(RutasProtegidas.Todas);

        foreach (var regla in RutasProtegidas.Todas)
        {
            // El comodín representa una carpeta cualquiera: se reemplaza por un nombre concreto
            // para poder preguntar por una ruta real.
            var ruta = regla.Replace("*", "una-instancia");
            Assert.True(RutasProtegidas.EstaProtegida(ruta), $"Quedó desprotegida: {regla}");
        }
    }

    [Theory]
    [InlineData(@"Microsoft\SQL Server Management Studio\22.0\UserSettings.xml")]
    [InlineData(@"Microsoft\SQL Server Management Studio\RegSrvr.xml")]
    [InlineData(@"Microsoft\SQL Server Management Studio")]
    [InlineData(@"Microsoft\UserSecrets\abc\secrets.json")]
    [InlineData(@"Microsoft\VisualStudio\17.0\cualquiera.txt")]
    public void La_configuracion_de_usuario_no_se_toca(string relativa) =>
        Assert.True(RutasProtegidas.EstaProtegida(Path.Combine(Roaming, relativa)));

    [Fact]
    public void El_historial_de_conexiones_de_ssms_no_se_enumera()
    {
        // Es la trampa que dejó a SSMS sin su historial: el archivo pesa kilobytes y reconstruirlo
        // es a mano, servidor por servidor.
        var settings = Path.Combine(Roaming, @"Microsoft\SQL Server Management Studio\22.0\UserSettings.xml");
        Assert.True(RutasProtegidas.EstaProtegida(settings));
    }

    [Fact]
    public void El_perfil_de_la_propia_herramienta_esta_protegido() =>
        Assert.True(RutasProtegidas.EstaProtegida(Path.Combine(Local, @"AlgoritmoDevTools\DiskCleaner\data.db")));

    [Fact]
    public void Los_temporales_si_se_escanean()
    {
        // El caso que más fácil se rompe: Temp y el perfil de la herramienta son carpetas hermanas,
        // así que una regla escrita por prefijo se llevaría puesto a uno de los dos.
        Assert.False(RutasProtegidas.EstaProtegida(Path.Combine(Local, "Temp")));
        Assert.False(RutasProtegidas.EstaProtegida(Path.Combine(Local, @"Temp\algo.tmp")));
    }

    [Theory]
    [InlineData("AlgoritmoDevToolsOtro")]
    [InlineData("AlgoritmoDevTools2")]
    public void Un_nombre_que_empieza_igual_no_queda_protegido(string carpeta) =>
        Assert.False(RutasProtegidas.EstaProtegida(Path.Combine(Local, carpeta, "x.db")));

    [Fact]
    public void El_comodin_distingue_los_ajustes_del_resto_de_la_instancia()
    {
        var instancia = Path.Combine(Local, @"Microsoft\VisualStudio\17.0_9f1c");

        Assert.True(RutasProtegidas.EstaProtegida(Path.Combine(instancia, @"Settings\Current.vssettings")));
        Assert.False(RutasProtegidas.EstaProtegida(Path.Combine(instancia, @"ComponentModelCache\x.dll")));
    }

    [Fact]
    public void Una_ruta_larga_con_prefijo_se_reconoce()
    {
        // Las rutas llegan desde la enumeración con el prefijo puesto; compararlas contra una lista
        // escrita sin él no acertaría nunca.
        var conPrefijo = @"\\?\" + Path.Combine(Roaming, @"Microsoft\UserSecrets\x\secrets.json");
        Assert.True(RutasProtegidas.EstaProtegida(conPrefijo));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ante_una_ruta_que_no_se_entiende_contesta_que_si(string ruta) =>
        Assert.True(RutasProtegidas.EstaProtegida(ruta));

    [Fact]
    public void Un_barrido_de_appdata_por_patron_excluye_la_lista_negra()
    {
        // Simula lo que haría una regla escrita "a lo bruto": barrer todo lo que cuelga de APPDATA.
        // Lo que no se puede tocar tiene que quedar afuera del barrido.
        var microsoft = Path.Combine(Roaming, "Microsoft");
        if (!Directory.Exists(microsoft)) return;

        var barridas = Directory
            .EnumerateDirectories(microsoft)
            .Where(d => !RutasProtegidas.EstaProtegida(d))
            .ToArray();

        Assert.DoesNotContain(barridas, d => d.Contains("SQL Server Management Studio", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(barridas, d => d.EndsWith("UserSecrets", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(barridas, d => d.EndsWith("VisualStudio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Una_carpeta_que_contiene_una_protegida_no_se_borra_entera()
    {
        // Un borrado recursivo no consulta la lista negra: le pide al sistema el árbol completo.
        // Esta es la pregunta inversa, y es la última antes de tocar el disco.
        Assert.True(RutasProtegidas.ContieneAlgunaProtegida(Local));
        Assert.True(RutasProtegidas.ContieneAlgunaProtegida(Path.Combine(Roaming, "Microsoft")));
        Assert.True(RutasProtegidas.ContieneAlgunaProtegida(Path.Combine(Local, @"Microsoft\VisualStudio\17.0_9f1c")));

        Assert.False(RutasProtegidas.ContieneAlgunaProtegida(Path.Combine(Local, "Temp")));
    }
}
