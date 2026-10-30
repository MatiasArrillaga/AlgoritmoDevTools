using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Integrations.SoftCerealCore;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>Cómo se compara lo que está corriendo contra lo que está configurado.</summary>
public enum EstadoDelSello
{
    /// <summary>No hay entorno levantado desde la aplicación, así que no hay nada que comparar.</summary>
    SinSello,

    /// <summary>Los servicios corren contra la misma base que está configurada.</summary>
    Coincide,

    /// <summary>La conexión cambió después de levantar: lo que corre sigue con la anterior.</summary>
    Difiere
}

/// <summary>
/// Con qué base de datos se levantó el entorno.
///
/// <para>
/// <b>Por qué hay que anotarlo.</b> La conexión no se puede averiguar mirando el entorno: sale de
/// los user-secrets, cada servicio la lee al arrancar y se la queda. Ni el panel de tye ni Docker
/// la conocen. Si alguien la cambia después, los servicios que ya están corriendo siguen con la
/// anterior y no hay forma de darse cuenta —salvo recordar con cuál arrancaron, que es esto.
/// </para>
///
/// <para>
/// El caso feo que esto destapa es el entorno partido: al cambiar la conexión con tye arriba, los
/// servicios que se reinicien por cualquier motivo toman la base nueva y el resto sigue en la
/// vieja. Mitad y mitad, sin un solo cartel.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
/// <param name="Servidor">Servidor con el que se levantó.</param>
/// <param name="Base">Base con la que se levantó.</param>
/// <param name="Cuando">Cuándo, para poder decir «desde las 9:14».</param>
public sealed record SelloDeConexion(string Servidor, string Base, DateTime Cuando)
{
    private const char Separador = '|';

    /// <summary>Lo que quedó anotado la última vez que se levantó, o null si nunca se levantó desde acá.</summary>
    public static SelloDeConexion? Leer()
    {
        var guardado = ConfiguracionDevTools.Compartida.SelloDeConexion;
        if (string.IsNullOrWhiteSpace(guardado)) return null;

        var partes = guardado.Split(Separador);
        if (partes.Length < 3) return null;

        return DateTime.TryParse(partes[2], out var cuando)
            ? new SelloDeConexion(partes[0], partes[1], cuando)
            : new SelloDeConexion(partes[0], partes[1], DateTime.MinValue);
    }

    public static void Escribir(string? servidor, string? baseDeDatos)
    {
        var configuracion = ConfiguracionDevTools.Compartida;
        configuracion.SelloDeConexion = $"{servidor}{Separador}{baseDeDatos}{Separador}{DateTime.Now:O}";
        configuracion.Guardar();
    }

    /// <summary>Se borra al frenar: sin entorno no hay con qué comparar.</summary>
    public static void Borrar()
    {
        var configuracion = ConfiguracionDevTools.Compartida;
        if (configuracion.SelloDeConexion is null) return;

        configuracion.SelloDeConexion = null;
        configuracion.Guardar();
    }

    /// <summary>
    /// Compara contra la conexión configurada ahora. Devuelve además la base configurada, que es lo
    /// que hay que mostrar cuando no coinciden: sin eso el aviso dice que algo cambió pero no a qué.
    /// </summary>
    public static (EstadoDelSello Estado, SelloDeConexion? Sello, string? BaseConfigurada) Comparar()
    {
        var sello = Leer();

        // La base configurada se devuelve siempre, haya sello o no: sin entorno levantado sigue
        // siendo el dato que interesa —es contra la que va a levantar—.
        var cadena = SecretService.Shared.GetConnectionString(Constantes.SecretKeys.Development);
        if (string.IsNullOrWhiteSpace(cadena)) return (EstadoDelSello.SinSello, sello, null);

        var partes = ConnectionStringParser.Parse(cadena);
        var servidor = partes.GetValueOrDefault("Server");
        var baseDeDatos = partes.GetValueOrDefault("Database");

        if (sello is null) return (EstadoDelSello.SinSello, null, baseDeDatos);

        var coincide = string.Equals(sello.Servidor, servidor, StringComparison.OrdinalIgnoreCase)
            && string.Equals(sello.Base, baseDeDatos, StringComparison.OrdinalIgnoreCase);

        return (coincide ? EstadoDelSello.Coincide : EstadoDelSello.Difiere, sello, baseDeDatos);
    }
}
