using System.Diagnostics;

using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>
/// El entorno local corriendo sobre tye.
///
/// <para>
/// <b>Lo que el panel de tye deja hacer, y lo que no.</b> Su API sirve para mirar el entorno y para
/// bajarlo entero, pero no tiene forma de arrancar ni parar un servicio suelto. Las dos acciones que
/// faltan se consiguen por fuera, y así es como están resueltas acá:
/// </para>
/// <list type="bullet">
/// <item><b>Reiniciar</b> un servicio es tocarle la fecha a un archivo de su proyecto. Tye corre los
/// servicios con <c>dotnet watch</c>, que al ver el cambio reinicia ese proceso y ninguno más. Es un
/// reinicio de verdad —proceso nuevo, no recarga en caliente—, así que el servicio vuelve a leer los
/// user-secrets: por eso es también la forma de que tome una base de datos distinta.</item>
/// <item><b>Bajar</b> un servicio es matarle el proceso. Tye cuenta la caída pero no lo relanza,
/// que para esto es lo que queremos. El sidecar se deja vivo a propósito: sin él, el servicio que
/// vuelve se queda esperando los secretos de Dapr para siempre.</item>
/// </list>
///
/// <para>
/// <b>Por qué se leen varios paneles.</b> Sumar un dominio al entorno vivo exige levantarlo con su
/// propio <c>tye run</c>, y cada uno abre su panel en un puerto distinto. Acá se los busca a todos y
/// se los junta, para que arriba se vea un entorno y no una pila de instancias.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public sealed class ProveedorTye : IProveedorDeEntorno
{
    /// <summary>Donde abre el panel el entorno principal. Los agregados toman los que siguen.</summary>
    public const int PuertoInicial = EntornoCompleto.PuertoPrincipal;

    /// <summary>Cuántos puertos se miran. Diez instancias de tye a la vez no es un escenario real.</summary>
    public const int PuertosAExplorar = 10;

    /// <summary>
    /// El archivo que se toca para reiniciar todos de una: lo referencian las dieciséis APIs, así
    /// que <c>dotnet watch</c> reinicia a todas las que estén corriendo, y además recupera a las que
    /// estuvieran caídas esperando un cambio.
    /// </summary>
    public const string ArchivoCompartido = @"Algoritmo.Microservices.Shared.API\BaseClasses\BaseProgram.cs";

    private static readonly TimeSpan EsperaCorta = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan EsperaDeFrenado = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Uno solo para todo el proceso, y a propósito.
    ///
    /// <para>
    /// La barra sondea cada tres segundos y cada sondeo mira diez puertos: con un cliente nuevo por
    /// consulta serían doscientos sockets por minuto, cada uno quedando un rato en TIME_WAIT. En
    /// minutos se agotan los puertos efímeros de Windows y las conexiones empiezan a fallar todas
    /// —la barra deja de encontrar el panel y muestra que no hay entorno aunque esté andando—.
    /// </para>
    ///
    /// <para>
    /// La espera va por llamada, con un token de cancelación, porque el cliente es uno y acá hacen
    /// falta dos tiempos: el sondeo tiene que ser corto y el frenado largo.
    /// </para>
    /// </summary>
    private static readonly HttpClient Cliente = new() { Timeout = Timeout.InfiniteTimeSpan };

    public async Task<EntornoCompleto> ObtenerEstadoAsync(CancellationToken cancelacion = default)
    {
        var consultas = Enumerable
            .Range(PuertoInicial, PuertosAExplorar)
            .Select(puerto => LeerPanelAsync(puerto, cancelacion));

        var paneles = await Task.WhenAll(consultas);

        return new EntornoCompleto(paneles.OfType<EstadoDelEntorno>().ToList());
    }

    public async Task FrenarAsync(CancellationToken cancelacion = default)
    {
        var entorno = await ObtenerEstadoAsync(cancelacion);

        foreach (var panel in entorno.Paneles)
        {
            try
            {
                using var espera = Espera(EsperaDeFrenado, cancelacion);
                await Cliente.DeleteAsync($"http://localhost:{panel.PuertoDelPanel}/api/v1/control", espera.Token);
            }
            catch (Exception)
            {
                // Un panel que no contesta al pedido de frenado es uno que ya se estaba cerrando.
            }
        }
    }

    public async Task<bool> ReiniciarServicioAsync(string nombre, CancellationToken cancelacion = default)
    {
        var entorno = await ObtenerEstadoAsync(cancelacion);
        var servicio = entorno.Buscar(nombre);
        if (servicio?.ProyectoCsproj is null) return false;

        return TocarElProyecto(servicio.ProyectoCsproj);
    }

    public Task ReiniciarTodoAsync(CancellationToken cancelacion = default)
    {
        Tocar(Path.Combine(RutasDeTrabajo.AlgoritmoCore, ArchivoCompartido));
        return Task.CompletedTask;
    }

    public async Task<bool> BajarServicioAsync(string nombre, CancellationToken cancelacion = default)
    {
        var entorno = await ObtenerEstadoAsync(cancelacion);
        var servicio = entorno.Buscar(nombre);
        if (servicio?.Pid is not int pid) return false;

        try
        {
            using var proceso = Process.GetProcessById(pid);
            proceso.Kill(entireProcessTree: false);
            return true;
        }
        catch (Exception)
        {
            // El proceso ya no estaba: el servicio igual queda abajo, que es lo que se pedía.
            return false;
        }
    }

    public async Task<bool> SumarServicioAsync(string nombre, CancellationToken cancelacion = default)
    {
        var yaml = GeneradorDeYamlSuelto.Generar(nombre);
        if (yaml is null) return false;

        var puerto = await PrimerPuertoLibreAsync(cancelacion);
        if (puerto is null) return false;

        // Siempre sin compilar: con otro tye arriba, compilar regenera las bibliotecas compartidas
        // y mata los procesos del entorno que ya estaba corriendo.
        return new LanzadorDeTye().Levantar(yaml, puerto.Value, sinCompilar: true) is not null;
    }

    public async Task<bool> SacarServicioAsync(string nombre, CancellationToken cancelacion = default)
    {
        var entorno = await ObtenerEstadoAsync(cancelacion);
        var servicio = entorno.Buscar(nombre);

        // Los del entorno principal no se pueden sacar: frenar ese panel bajaría a todos.
        if (servicio is null || servicio.PuertoDelPanel == EntornoCompleto.PuertoPrincipal) return false;

        try
        {
            using var espera = Espera(EsperaDeFrenado, cancelacion);
            await Cliente.DeleteAsync($"http://localhost:{servicio.PuertoDelPanel}/api/v1/control", espera.Token);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// El primer puerto de panel sin usar. Es plomería: el servicio conserva los puertos del
    /// maestro, esto es sólo para que la instancia nueva no choque con las que ya están.
    /// </summary>
    private async Task<int?> PrimerPuertoLibreAsync(CancellationToken cancelacion)
    {
        var entorno = await ObtenerEstadoAsync(cancelacion);
        var ocupados = entorno.Paneles.Select(p => p.PuertoDelPanel).ToHashSet();

        for (var puerto = PuertoInicial + 1; puerto < PuertoInicial + PuertosAExplorar; puerto++)
        {
            if (!ocupados.Contains(puerto)) return puerto;
        }

        return null;
    }

    // --- Lectura de los paneles ----------------------------------------------

    private static async Task<EstadoDelEntorno?> LeerPanelAsync(int puerto, CancellationToken cancelacion)
    {
        try
        {
            using var espera = Espera(EsperaCorta, cancelacion);
            var json = await Cliente.GetStringAsync($"http://localhost:{puerto}/api/v1/services", espera.Token);
            var estado = LectorDeEstadoDeTye.Parsear(json, puerto);
            return estado.Servicios.Count > 0 ? estado : null;
        }
        catch (Exception)
        {
            // Nadie escuchando, o algo que no es un panel de tye. En los dos casos, no hay entorno ahí.
            return null;
        }
    }

    /// <summary>Corta la llamada si tarda de más, sin tocar el cliente compartido.</summary>
    private static CancellationTokenSource Espera(TimeSpan cuanto, CancellationToken cancelacion)
    {
        var espera = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        espera.CancelAfter(cuanto);
        return espera;
    }

    // --- El toque que dispara el reinicio ------------------------------------

    /// <summary>
    /// Toca un archivo fuente del proyecto. Se prefiere un <c>.cs</c> antes que el <c>.csproj</c>
    /// porque cambiarle la fecha al proyecto hace que se reevalúe la restauración de paquetes, que
    /// tarda bastante más y no hace falta para reiniciar.
    /// </summary>
    private static bool TocarElProyecto(string rutaDelCsproj)
    {
        var carpeta = Path.GetDirectoryName(rutaDelCsproj);
        if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta)) return false;

        var programa = Path.Combine(carpeta, "Program.cs");
        var archivo = File.Exists(programa)
            ? programa
            : Directory.EnumerateFiles(carpeta, "*.cs", SearchOption.TopDirectoryOnly).FirstOrDefault();

        return archivo is not null && Tocar(archivo);
    }

    /// <summary>
    /// Le cambia la fecha de modificación sin tocar el contenido. Git compara contenido, así que
    /// el repositorio no queda sucio; <c>dotnet watch</c> mira la fecha, así que se da por enterado.
    /// </summary>
    private static bool Tocar(string archivo)
    {
        try
        {
            if (!File.Exists(archivo)) return false;
            File.SetLastWriteTimeUtc(archivo, DateTime.UtcNow);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
