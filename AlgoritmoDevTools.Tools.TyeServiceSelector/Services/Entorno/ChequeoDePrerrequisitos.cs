using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Integrations.SoftCerealCore;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>Algo que falta para que el entorno pueda levantar, con qué hacer al respecto.</summary>
/// <param name="Que">Qué falta, en una línea.</param>
/// <param name="Remedio">Qué hacer. Vacío si no hay una receta conocida.</param>
/// <param name="NecesitaElevacion">Si el remedio sólo corre en una consola de administrador.</param>
public sealed record FaltanteDelEntorno(string Que, string Remedio, bool NecesitaElevacion = false);

/// <summary>Lo que se encontró antes de levantar.</summary>
/// <param name="HayEntornoCorriendo">
/// Ya hay una instancia de tye. No es un faltante: es el modo «controlar», en el que la barra se
/// engancha a esa instancia en vez de arrancar una segunda que se pelearía por los puertos.
/// </param>
public sealed record ResultadoDePrerrequisitos(
    IReadOnlyList<FaltanteDelEntorno> Faltantes,
    IReadOnlyList<string> Avisos,
    bool HayEntornoCorriendo = false)
{
    public bool Listo => Faltantes.Count == 0;
}

/// <summary>
/// Mira si están las cosas de las que depende el entorno, antes de arrancarlo.
///
/// <para>
/// <b>Por qué vale la pena mirar antes.</b> Si falta el scheduler de Dapr, tye arranca igual: los
/// sidecars no completan el arranque, las APIs se quedan esperando los secretos y el entorno queda
/// a medias sin un solo cartel que explique por qué. Lo mismo si Docker está apagado. Son tres
/// consultas que tardan menos de lo que tarda en fallar.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public sealed class ChequeoDePrerrequisitos
{
    /// <summary>Sin estos tres no hay entorno: los crea <c>dapr init</c>.</summary>
    public static readonly string[] ContenedoresNecesarios = ["dapr_placement", "dapr_scheduler", "dapr_redis"];

    /// <summary>Sólo sirve para el rastreo distribuido: que falte no impide trabajar.</summary>
    public const string ContenedorDeTrazas = "dapr_zipkin";

    public ResultadoDePrerrequisitos Revisar()
    {
        var faltantes = new List<FaltanteDelEntorno>();
        var avisos = new List<string>();

        // Que ya haya un tye corriendo dejó de ser un faltante: levantar un segundo sigue estando
        // mal, pero el camino no es frenar el que está, sino engancharse a él. La pantalla lo
        // resuelve cambiando «Levantar» por «Controlar». Se mira antes del corte por Docker porque
        // un entorno ya levantado se controla igual aunque Docker haya dejado de contestar.
        var hayEntornoCorriendo = LanzadorDeTye.HayTyeCorriendo();

        var contenedores = ContenedoresVivos();
        if (contenedores is null)
        {
            faltantes.Add(new FaltanteDelEntorno(
                "Docker no está respondiendo.",
                "Abrí Docker Desktop y esperá a que termine de arrancar."));
            return new ResultadoDePrerrequisitos(faltantes, avisos, hayEntornoCorriendo);
        }

        foreach (var necesario in ContenedoresNecesarios)
        {
            if (contenedores.Contains(necesario, StringComparer.OrdinalIgnoreCase)) continue;
            faltantes.Add(Diagnosticar(necesario));
        }

        if (!contenedores.Contains(ContenedorDeTrazas, StringComparer.OrdinalIgnoreCase))
        {
            avisos.Add($"{ContenedorDeTrazas} está abajo: no vas a tener trazas en Zipkin, el resto funciona igual.");
        }

        return new ResultadoDePrerrequisitos(faltantes, avisos, hayEntornoCorriendo);
    }

    /// <summary>
    /// La conexión con la que van a arrancar los servicios. Es lo que se muestra en el pre-vuelo,
    /// porque levantar contra la base equivocada se nota tarde y cuesta el ciclo entero.
    /// </summary>
    public static (string? Servidor, string? Base, string? Error) ConexionConfigurada()
    {
        var cadena = SecretService.Shared.GetConnectionString(Constantes.SecretKeys.Development);
        if (string.IsNullOrWhiteSpace(cadena))
            return (null, null, "El secreto Development no está configurado.");

        var partes = ConnectionStringParser.Parse(cadena);
        partes.TryGetValue("Server", out var servidor);
        partes.TryGetValue("Database", out var baseDeDatos);

        return (servidor, baseDeDatos, null);
    }

    // --- Diagnóstico de un contenedor que no levanta -------------------------

    /// <summary>
    /// Cuando falta un contenedor, el motivo que más cuesta descubrir es que Windows le reservó
    /// alguno de sus puertos. Por eso, antes de decir «está abajo», se cruzan los puertos que pide
    /// contra los tramos reservados: si hay choque, el diagnóstico queda cerrado y con receta.
    /// </summary>
    private static FaltanteDelEntorno Diagnosticar(string contenedor)
    {
        var choques = LectorDeRangosExcluidos.Cruzar(PuertosQuePide(contenedor), RangosExcluidos());

        if (choques.Count == 0)
        {
            return new FaltanteDelEntorno(
                $"{contenedor} está abajo.",
                $"Levantalo con: docker start {contenedor}");
        }

        var (puerto, rango) = choques[0];
        return new FaltanteDelEntorno(
            $"{contenedor} no puede arrancar: Windows tiene reservado el puerto {puerto} (tramo {rango.Desde}-{rango.Hasta}).",
            $"net stop winnat; net start winnat; docker start {contenedor}",
            NecesitaElevacion: true);
    }

    private static IReadOnlyList<string>? ContenedoresVivos()
    {
        var salida = Docker("ps", "--format", "{{.Names}}");
        return salida?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IEnumerable<int> PuertosQuePide(string contenedor)
    {
        var salida = Docker("inspect", contenedor, "--format", "{{range $p, $c := .HostConfig.PortBindings}}{{range $c}}{{.HostPort}} {{end}}{{end}}");
        if (salida is null) yield break;

        foreach (var texto in salida.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(texto, out var puerto)) yield return puerto;
        }
    }

    private static IReadOnlyList<RangoExcluido> RangosExcluidos()
    {
        try
        {
            var resultado = ProcessRunner.RunProcess(
                "netsh",
                ["interface", "ipv4", "show", "excludedportrange", "protocol=tcp"]);

            return LectorDeRangosExcluidos.Parsear(resultado.StdOut);
        }
        catch (Exception)
        {
            return Array.Empty<RangoExcluido>();
        }
    }

    /// <summary>
    /// Corre docker con <see cref="ProcessRunner.RunProcess"/>, que no convierte un código de salida
    /// distinto de cero en excepción: acá un error es una respuesta más —Docker apagado, contenedor
    /// que no existe— y no algo que haya que atajar.
    /// </summary>
    private static string? Docker(params string[] argumentos)
    {
        try
        {
            var resultado = ProcessRunner.RunProcess("docker", argumentos);
            return resultado.ExitCode == 0 ? resultado.StdOut : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
