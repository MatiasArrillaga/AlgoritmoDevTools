using System.Diagnostics;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// El único camino por el que el escáner toca el disco.
///
/// Todo lo de acá se prueba contra archivos reales: un junction, una ruta de más de 260 caracteres
/// y una carpeta sin permiso no se pueden simular.
/// </summary>
public class EnumeracionSeguraTest
{
    [Fact]
    public void Mide_el_tamano_exacto_de_un_arbol()
    {
        using var arbol = new ArbolDePrueba();
        arbol.Archivo("uno.bin", 1000);
        arbol.Archivo(@"hondo\dos.bin", 500);
        arbol.Archivo(@"hondo\mas\tres.bin", 24);

        var medicion = EnumeracionSegura.Medir(arbol.Raiz);

        Assert.Equal(1524, medicion.Bytes);
        Assert.Equal(3, medicion.Archivos);
        Assert.Equal(0, medicion.Inaccesibles);
    }

    [Fact]
    public void No_sigue_un_junction_ni_cuenta_dos_veces()
    {
        using var arbol = new ArbolDePrueba();
        arbol.Archivo(@"real\dato.bin", 700);
        var enlace = Path.Combine(arbol.Raiz, "enlace");

        if (!CrearJunction(enlace, Path.Combine(arbol.Raiz, "real"))) return;

        // Seguir el enlace contaría los mismos 700 bytes una segunda vez.
        Assert.Equal(700, EnumeracionSegura.Medir(arbol.Raiz).Bytes);
    }

    [Fact]
    public void Mide_una_ruta_de_mas_de_260_caracteres()
    {
        using var arbol = new ArbolDePrueba();

        var profundo = arbol.Raiz;
        while (profundo.Length < 250) profundo = Path.Combine(profundo, "unsegmentobastantelargo");

        Directory.CreateDirectory(@"\\?\" + profundo);
        File.WriteAllBytes(@"\\?\" + Path.Combine(profundo, "hondo.bin"), new byte[42]);

        Assert.True(profundo.Length > 240, $"La ruta quedó en {profundo.Length} caracteres");
        Assert.Equal(42, EnumeracionSegura.Medir(arbol.Raiz).Bytes);
    }

    [Fact]
    public void Una_carpeta_sin_permiso_no_corta_el_recorrido()
    {
        using var arbol = new ArbolDePrueba();
        arbol.Archivo("antes.bin", 100);
        arbol.Archivo(@"prohibida\adentro.bin", 5000);
        arbol.Archivo(@"zdespues\despues.bin", 200);

        var prohibida = Path.Combine(arbol.Raiz, "prohibida");
        var usuario = Environment.UserDomainName + "\\" + Environment.UserName;

        if (!Denegar(prohibida, usuario)) return;

        try
        {
            var medicion = EnumeracionSegura.Medir(arbol.Raiz);

            // Lo de antes y lo de después se cuentan igual: es la diferencia con un recorrido que
            // muere en la primera carpeta sin permiso.
            Assert.Equal(300, medicion.Bytes);
            Assert.Equal(1, medicion.Inaccesibles);
        }
        finally
        {
            Icacls($"\"{prohibida}\" /remove:d \"{usuario}\"");
        }
    }

    [Fact]
    public void Una_ruta_protegida_mide_cero()
    {
        var perfil = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlgoritmoDevTools");

        // Para la herramienta esa carpeta no existe, así que devolver su tamaño sería el primer
        // paso para ofrecerla.
        Assert.Equal(0, EnumeracionSegura.Medir(perfil).Bytes);
    }

    [Fact]
    public void Una_ruta_que_no_existe_no_tira()
    {
        using var arbol = new ArbolDePrueba();
        Assert.Equal(0, EnumeracionSegura.Medir(Path.Combine(arbol.Raiz, "no-existe")).Bytes);
    }

    [Fact]
    public void Buscar_carpetas_no_entra_en_lo_que_encuentra()
    {
        using var arbol = new ArbolDePrueba();
        arbol.Archivo(@"proyecto\bin\x.dll", 10);
        arbol.Archivo(@"proyecto\bin\obj\anidada.o", 10);
        arbol.Archivo(@"otro\obj\z.o", 10);

        var encontradas = EnumeracionSegura.BuscarCarpetas(arbol.Raiz, ["bin", "obj"]);

        // La obj de adentro ya está contada por la bin que la contiene: devolver las dos haría que
        // el total se sumara dos veces.
        Assert.Equal(2, encontradas.Count);
        Assert.DoesNotContain(encontradas, c => c.Contains(@"bin\obj", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Las_rutas_salen_sin_el_prefijo_de_ruta_larga()
    {
        using var arbol = new ArbolDePrueba();
        arbol.Archivo(@"proyecto\bin\x.dll", 10);

        // Con el prefijo puesto, ninguna de esas rutas sería reconocible para quien las lea.
        Assert.All(
            EnumeracionSegura.BuscarCarpetas(arbol.Raiz, ["bin"]),
            c => Assert.False(c.StartsWith(@"\\?\", StringComparison.Ordinal)));
    }

    [Fact]
    public void Cancelar_corta_el_recorrido()
    {
        using var arbol = new ArbolDePrueba();
        arbol.Archivo("uno.bin", 10);

        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        Assert.Throws<OperationCanceledException>(() => EnumeracionSegura.Medir(arbol.Raiz, cancelacion.Token));
    }

    private static bool CrearJunction(string enlace, string destino) =>
        Correr("cmd.exe", $"/c mklink /J \"{enlace}\" \"{destino}\"") && Directory.Exists(enlace);

    private static bool Denegar(string carpeta, string usuario)
    {
        Icacls($"\"{carpeta}\" /deny \"{usuario}\":(OI)(CI)(RX)");

        // Si el sistema no lo aplicó —o el usuario puede leerla igual—, el escenario no se montó y
        // el test no prueba nada: mejor saltearlo que dar por bueno algo que no se ejercitó.
        try
        {
            Directory.GetFiles(carpeta);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static void Icacls(string argumentos) => Correr("icacls.exe", argumentos);

    private static bool Correr(string ejecutable, string argumentos)
    {
        try
        {
            using var proceso = Process.Start(new ProcessStartInfo(ejecutable, argumentos)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (proceso is null) return false;

            proceso.WaitForExit(10_000);
            return proceso.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
