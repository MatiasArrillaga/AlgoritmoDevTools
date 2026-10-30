using AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Test;

/// <summary>
/// Lectura de lo que devuelve el panel de tye.
///
/// Los json de acá son recortes de una respuesta real del entorno, tomada el 29/10/2026: interesa
/// que la forma sea la que manda tye y no la que nos resultaría cómoda.
/// </summary>
public class LectorDeEstadoDeTyeTest
{
    private const string UnServicioArriba = """
        [{
          "description": { "name": "contabilidad" },
          "serviceType": "project",
          "restarts": 0,
          "status": { "projectFilePath": "C:\\repos\\AlgoritmoCore\\Microservicios\\Contabilidad\\Algoritmo.Contabilidad.API\\Algoritmo.Contabilidad.API.csproj" },
          "replicas": { "contabilidad_b0fde22f-c": { "state": "ready", "pid": 12588, "ports": [2736] } }
        }]
        """;

    [Fact]
    public void Un_servicio_con_replica_queda_arriba_con_su_pid()
    {
        var estado = LectorDeEstadoDeTye.Parsear(UnServicioArriba, 8000);

        var servicio = Assert.Single(estado.Servicios);
        Assert.Equal("contabilidad", servicio.Nombre);
        Assert.True(servicio.Arriba);
        Assert.Equal(12588, servicio.Pid);
        Assert.Equal([2736], servicio.Puertos);
        Assert.Equal(8000, servicio.PuertoDelPanel);
        Assert.True(servicio.SePuedeReiniciar);
    }

    [Fact]
    public void Un_servicio_sin_replicas_esta_abajo_pero_sigue_en_la_lista()
    {
        // Es como queda el que se bajó a mano: tye le cuenta la caída y lo deja sin réplica.
        const string caido = """
            [{
              "description": { "name": "contabilidad" },
              "serviceType": "project",
              "restarts": 1,
              "replicas": {}
            }]
            """;

        var servicio = Assert.Single(LectorDeEstadoDeTye.Parsear(caido, 8000).Servicios);

        Assert.False(servicio.Arriba);
        Assert.Null(servicio.Pid);
        Assert.Equal(1, servicio.Reinicios);
    }

    [Fact]
    public void El_sidecar_se_reconoce_por_el_nombre()
    {
        const string sidecar = """
            [{
              "description": { "name": "contabilidad-dapr" },
              "serviceType": "executable",
              "replicas": { "x": { "state": "ready", "pid": 17688 } }
            }]
            """;

        var servicio = Assert.Single(LectorDeEstadoDeTye.Parsear(sidecar, 8000).Servicios);

        Assert.True(servicio.EsSidecar);
        Assert.False(servicio.SePuedeReiniciar);
    }

    [Fact]
    public void El_servicio_externo_no_lo_corre_tye()
    {
        // 'ui' está declarado external: tye le reserva el puerto y nada más, así que nunca tiene réplica.
        const string externo = """
            [{ "description": { "name": "ui" }, "serviceType": "external", "replicas": {} }]
            """;

        var servicio = Assert.Single(LectorDeEstadoDeTye.Parsear(externo, 8000).Servicios);

        Assert.False(servicio.LoCorreTye);
    }

    [Fact]
    public void Un_json_que_no_se_puede_leer_no_tira()
    {
        // El panel puede estar arrancando o cerrándose; perder la lectura no puede romper la pantalla.
        Assert.Empty(LectorDeEstadoDeTye.Parsear("{ esto no es json", 8000).Servicios);
        Assert.Empty(LectorDeEstadoDeTye.Parsear("", 8000).Servicios);
        Assert.Empty(LectorDeEstadoDeTye.Parsear("{\"no\":\"es un arreglo\"}", 8000).Servicios);
    }

    [Fact]
    public void Los_paneles_se_juntan_en_un_solo_entorno_sin_sidecars_ni_externos()
    {
        var principal = LectorDeEstadoDeTye.Parsear(UnServicioArriba, 8000);
        var agregado = LectorDeEstadoDeTye.Parsear("""
            [
              { "description": { "name": "stock" }, "serviceType": "project", "replicas": { "x": { "state": "ready", "pid": 99 } } },
              { "description": { "name": "stock-dapr" }, "serviceType": "executable", "replicas": { "y": { "state": "ready", "pid": 98 } } }
            ]
            """, 8001);

        var entorno = new EntornoCompleto([principal, agregado]);

        Assert.True(entorno.Vivo);
        Assert.Equal(["contabilidad", "stock"], entorno.Servicios.Select(s => s.Nombre));
        Assert.Equal(2, entorno.Listos);
        Assert.Equal("stock", Assert.Single(entorno.Agregados).Nombre);
    }
}
