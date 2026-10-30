using System.Text.Json;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>
/// Traduce lo que devuelve <c>GET /api/v1/services</c> del panel de tye.
///
/// <para>
/// Está separado del proveedor —que es el que habla por red— para poder probarlo con una respuesta
/// guardada, sin levantar el entorno. Todo lo que puede salir mal de un panel ajeno pasa por acá:
/// un servicio sin réplicas, un campo que no vino, un tipo que no conocíamos.
/// </para>
///
/// <para>
/// <b>El formato no es nuestro.</b> Lo define el panel de tye, así que cada campo se lee a la
/// defensiva: si falta o viene con otra forma, el servicio igual aparece en la lista aunque sea sin
/// ese dato. Perder un servicio de la vista es peor que mostrarlo incompleto.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public static class LectorDeEstadoDeTye
{
    /// <summary>
    /// Arma el estado de un panel a partir de su respuesta. Un json que no se puede leer devuelve
    /// la lista vacía en vez de tirar: el panel puede estar justo arrancando o bajando.
    /// </summary>
    public static EstadoDelEntorno Parsear(string json, int puertoDelPanel)
    {
        var servicios = new List<ServicioDelEntorno>();

        try
        {
            using var documento = JsonDocument.Parse(json);
            if (documento.RootElement.ValueKind != JsonValueKind.Array)
                return new EstadoDelEntorno(puertoDelPanel, servicios);

            foreach (var elemento in documento.RootElement.EnumerateArray())
            {
                var servicio = LeerServicio(elemento, puertoDelPanel);
                if (servicio is not null) servicios.Add(servicio);
            }
        }
        catch (JsonException)
        {
            return new EstadoDelEntorno(puertoDelPanel, Array.Empty<ServicioDelEntorno>());
        }

        return new EstadoDelEntorno(puertoDelPanel, servicios);
    }

    private static ServicioDelEntorno? LeerServicio(JsonElement elemento, int puertoDelPanel)
    {
        var nombre = Texto(Hijo(elemento, "description"), "name");
        if (string.IsNullOrWhiteSpace(nombre)) return null;

        var replica = PrimeraReplica(elemento);

        return new ServicioDelEntorno(
            Nombre: nombre,
            Estado: Texto(replica, "state") ?? string.Empty,
            Pid: Entero(replica, "pid"),
            Puertos: Puertos(replica),
            Reinicios: Entero(elemento, "restarts") ?? 0,
            ProyectoCsproj: Texto(Hijo(elemento, "status"), "projectFilePath"),
            Tipo: Texto(elemento, "serviceType") ?? string.Empty,
            PuertoDelPanel: puertoDelPanel);
    }

    /// <summary>
    /// La réplica del servicio, si tiene. Un servicio caído viene con el diccionario vacío, que es
    /// justamente cómo se distingue de uno que está arriba.
    /// </summary>
    private static JsonElement? PrimeraReplica(JsonElement elemento)
    {
        if (!elemento.TryGetProperty("replicas", out var replicas)) return null;
        if (replicas.ValueKind != JsonValueKind.Object) return null;

        foreach (var propiedad in replicas.EnumerateObject()) return propiedad.Value;
        return null;
    }

    private static IReadOnlyList<int> Puertos(JsonElement? replica)
    {
        if (replica is null) return Array.Empty<int>();
        if (!replica.Value.TryGetProperty("ports", out var puertos)) return Array.Empty<int>();
        if (puertos.ValueKind != JsonValueKind.Array) return Array.Empty<int>();

        var leidos = new List<int>();
        foreach (var puerto in puertos.EnumerateArray())
        {
            if (puerto.TryGetInt32(out var valor)) leidos.Add(valor);
        }
        return leidos;
    }

    private static JsonElement? Hijo(JsonElement elemento, string nombre) =>
        elemento.TryGetProperty(nombre, out var hijo) && hijo.ValueKind == JsonValueKind.Object ? hijo : null;

    private static string? Texto(JsonElement? elemento, string nombre) =>
        elemento is not null
        && elemento.Value.TryGetProperty(nombre, out var valor)
        && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;

    /// <summary>
    /// El chequeo del tipo antes de leer el número no es de más: <c>TryGetInt32</c> **lanza** si el
    /// elemento no es un número, no devuelve false. Y el panel manda nulos de rutina —<c>pid</c> y
    /// <c>exitCode</c> de un servicio que no está corriendo—, así que sin esto una excepción se
    /// lleva puesto el panel entero y la barra muestra que no hay entorno aunque esté andando.
    /// </summary>
    private static int? Entero(JsonElement? elemento, string nombre) =>
        elemento is not null
        && elemento.Value.TryGetProperty(nombre, out var valor)
        && valor.ValueKind == JsonValueKind.Number
        && valor.TryGetInt32(out var numero)
            ? numero
            : null;
}
