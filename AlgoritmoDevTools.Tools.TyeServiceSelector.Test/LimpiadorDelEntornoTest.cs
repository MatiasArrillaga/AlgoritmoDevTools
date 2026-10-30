using AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Test;

/// <summary>
/// La lectura de los sidecars que están corriendo.
///
/// El app-id sale de la línea de comandos porque es el único lugar donde está: todos los procesos
/// se llaman <c>daprd.exe</c>. Y de ese app-id depende todo el criterio del limpiador — se compara
/// contra los servicios que reportan los paneles vivos, y lo que no figura en ninguno sobra.
///
/// Las líneas son del formato real de esta máquina, del 29/10/2026.
/// </summary>
public class LimpiadorDelEntornoTest
{
    [Fact]
    public void Lee_el_pid_y_el_app_id_de_cada_sidecar()
    {
        const string salida = """
            59280 "C:\Program Files\dapr\daprd.exe" --app-id ui --app-port 5190 --dapr-http-port 3500
            42692 "C:\Program Files\dapr\daprd.exe" --app-id contabilidad --app-port 2736 --dapr-http-port 45736
            105292 "C:\Program Files\dapr\daprd.exe" --app-id stock --app-port 2743 --dapr-http-port 45743
            """;

        var sidecars = LimpiadorDelEntorno.LeerSidecars(salida);

        Assert.Equal(3, sidecars.Count);
        Assert.Equal(59280, sidecars[0].Pid);
        Assert.Equal("ui", sidecars[0].AppId);
        Assert.Equal("stock", sidecars[2].AppId);
    }

    [Fact]
    public void Una_linea_sin_app_id_no_cuenta()
    {
        // Si no se sabe a qué servicio acompaña, no hay forma de decidir si sobra: queda afuera.
        const string salida = """
            1234 "C:\Program Files\dapr\daprd.exe" --metrics-port 47000
            5678 "C:\Program Files\dapr\daprd.exe" --app-id directorio --app-port 2738
            """;

        var sidecar = Assert.Single(LimpiadorDelEntorno.LeerSidecars(salida));

        Assert.Equal("directorio", sidecar.AppId);
    }

    [Fact]
    public void Una_salida_vacia_o_rota_no_tira()
    {
        Assert.Empty(LimpiadorDelEntorno.LeerSidecars(""));
        Assert.Empty(LimpiadorDelEntorno.LeerSidecars("no hay procesos"));
        Assert.Empty(LimpiadorDelEntorno.LeerSidecars("sinpid --app-id cereales"));
    }
}
