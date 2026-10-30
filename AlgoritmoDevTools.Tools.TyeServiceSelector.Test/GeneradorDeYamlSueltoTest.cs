using AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Test;

/// <summary>
/// La reescritura de rutas del yaml derivado.
///
/// Es lo único del generador que decide algo: el resto del bloque se copia tal cual del maestro, a
/// propósito, para que el servicio agregado arranque con los mismos puertos y variables que si lo
/// hubiera levantado el entorno entero.
///
/// Importa porque el derivado vive fuera del repositorio —para no dejarlo sucio— y tye resuelve las
/// rutas relativas al yaml: si quedaran como están en el maestro, no encontraría ni el proyecto ni
/// el script del sidecar.
/// </summary>
public class GeneradorDeYamlSueltoTest
{
    private const string Prefijo = "../AlgoritmoCore";

    [Fact]
    public void La_ruta_del_proyecto_queda_relativa_al_yaml()
    {
        string[] bloque =
        [
            "- name: stock",
            "  project: Microservicios/Stock/Algoritmo.Stock.API/Algoritmo.Stock.API.csproj"
        ];

        var reescrito = GeneradorDeYamlSuelto.ReescribirRutas(bloque, Prefijo);

        Assert.Equal("  project: ../AlgoritmoCore/Microservicios/Stock/Algoritmo.Stock.API/Algoritmo.Stock.API.csproj", reescrito[1]);
    }

    [Fact]
    public void El_directorio_del_sidecar_pasa_de_la_raiz_al_repositorio()
    {
        // En el maestro es ".", que es la raíz del repositorio porque el maestro vive ahí.
        string[] bloque = ["- name: stock-dapr", "  workingDirectory: ."];

        var reescrito = GeneradorDeYamlSuelto.ReescribirRutas(bloque, Prefijo);

        Assert.Equal("  workingDirectory: ../AlgoritmoCore", reescrito[1]);
    }

    [Fact]
    public void Los_puertos_y_el_comando_del_sidecar_se_copian_sin_tocar()
    {
        // Es la condición de que el servicio agregado se comporte igual que si lo hubiera levantado
        // el entorno entero: mismos puertos, mismo script, mismas variables.
        string[] bloque =
        [
            "- name: stock-dapr",
            "  executable: powershell",
            "  args: -NoProfile -File Utils/Tye/Start-DevelopmentDapr.ps1 -AppId stock -AppPort 2743 -HttpPort 45743 -GrpcPort 42743 -MetricsPort 47015"
        ];

        var reescrito = GeneradorDeYamlSuelto.ReescribirRutas(bloque, Prefijo);

        Assert.Equal(bloque, reescrito);
    }

    [Fact]
    public void Las_variables_de_entorno_no_se_tocan()
    {
        string[] bloque =
        [
            "  env:",
            "  - name: DAPR_HTTP_PORT",
            "    value: \"45743\"",
            "  bindings:",
            "  - port: 2743"
        ];

        Assert.Equal(bloque, GeneradorDeYamlSuelto.ReescribirRutas(bloque, Prefijo));
    }
}
