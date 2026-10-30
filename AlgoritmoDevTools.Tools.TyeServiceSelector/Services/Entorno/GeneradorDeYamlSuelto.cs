using System.Text.RegularExpressions;

using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;

/// <summary>
/// Escribe un tye derivado con un servicio solo, para sumarlo a un entorno que ya está corriendo.
///
/// <para>
/// <b>Un segundo tye convive con el primero</b>, aunque la documentación de AlgoritmoCore diga que
/// no se ejecute otro mientras el anterior siga activo. Convive con tres condiciones, y las tres
/// salieron de romperlo primero:
/// </para>
/// <list type="number">
/// <item>El derivado lleva <b>sólo el servicio y su sidecar</b>. Con <c>ui</c> adentro, o con la
/// extensión <c>seq</c>, el segundo tye recrea contenedores proxy de puertos que el primero ya
/// tiene (5190, 5341) y se lleva puesto el entorno que estaba andando.</item>
/// <item>Se corre con <b><c>--no-build</c></b>: compilar regenera las bibliotecas compartidas y eso
/// mata los procesos del entorno principal, que quedan esperando un cambio de archivo.</item>
/// <item>Con <b>su propio puerto de panel</b>, porque el 8000 está ocupado.</item>
/// </list>
///
/// <para>
/// Los dos entornos se ven como uno solo porque los sidecars de las dos instancias comparten
/// <c>.tye/dapr-name-resolution.db</c>: el servicio agregado queda invocable por app-id desde
/// cualquier otro, y conserva los puertos del maestro.
/// </para>
///
/// <para>
/// El archivo vive <b>fuera del repositorio</b>, al lado de los repos, para no dejar AlgoritmoCore
/// sucio. Por eso hay que reescribirle las rutas: tye las resuelve relativas al yaml, igual que en
/// <c>Utils/Tye/tye.staging.yaml</c>.
/// </para>
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.TyeServiceSelector/README.referencia-tecnica-funcional-selector-de-servicios.md.
/// </summary>
public static class GeneradorDeYamlSuelto
{
    /// <summary>Carpeta de los derivados, al lado de los repositorios y no adentro de ninguno.</summary>
    public const string NombreDeLaCarpeta = ".devYamels";

    private static readonly Regex Proyecto = new(@"^(\s*project:\s*)(\S+)\s*$", RegexOptions.Compiled);
    private static readonly Regex Directorio = new(@"^(\s*workingDirectory:\s*)(\S+)\s*$", RegexOptions.Compiled);

    public static string Carpeta => Path.Combine(RutasDeTrabajo.CarpetaDeRepositorios, NombreDeLaCarpeta);

    public static string RutaDelYaml(string servicio) =>
        Path.Combine(Carpeta, $"tye.devtools.{servicio}.yaml");

    /// <summary>
    /// Escribe el derivado y devuelve su ruta, o null si el maestro no tiene ese servicio o su
    /// sidecar: las dos mitades van juntas o no va ninguna, porque un servicio sin sidecar no puede
    /// invocar a nadie y un sidecar sin servicio espera para siempre.
    /// </summary>
    public static string? Generar(string servicio)
    {
        var principal = TyeServiceToggler.BloqueDelMaestro(servicio);
        var sidecar = TyeServiceToggler.BloqueDelMaestro(servicio + TyeServiceToggler.DaprSuffix);
        if (principal is null || sidecar is null) return null;

        var prefijo = PrefijoHaciaElRepositorio();
        var lineas = new List<string>
        {
            $"# Derivado del tye.yaml maestro para levantar SOLO '{servicio}', aparte del entorno principal.",
            "# Generado por DevTools. El maestro no se modifica.",
            "#",
            "# Las rutas van relativas a ESTE archivo, que es como las resuelve tye. Sin 'ui' ni la",
            "# extension seq a proposito: declaran puertos que el entorno principal ya tiene, y tye",
            "# levanta un contenedor proxy por cada servicio con binding.",
            "",
            $"name: softcerealcore-{servicio}",
            "linuxcontainersonwindows: false",
            "services:"
        };

        lineas.AddRange(ReescribirRutas(principal, prefijo));
        lineas.Add(string.Empty);
        lineas.AddRange(ReescribirRutas(sidecar, prefijo));
        lineas.Add(string.Empty);

        Directory.CreateDirectory(Carpeta);
        var destino = RutaDelYaml(servicio);
        File.WriteAllLines(destino, lineas);

        return destino;
    }

    /// <summary>
    /// Cómo se llega al repositorio desde la carpeta de los derivados. Son hermanas, así que es
    /// subir una y entrar: <c>../AlgoritmoCore</c>.
    /// </summary>
    private static string PrefijoHaciaElRepositorio() =>
        $"../{RutasDeTrabajo.NombreDeAlgoritmoCore}";

    /// <summary>
    /// Ajusta las rutas que el maestro escribe desde la raíz del repositorio. Lo demás del bloque
    /// se copia tal cual: los puertos, las variables y el comando del sidecar tienen que ser los
    /// mismos que usaría el entorno entero.
    /// </summary>
    public static IReadOnlyList<string> ReescribirRutas(IEnumerable<string> bloque, string prefijo) =>
        Reescribir(bloque, prefijo).ToList();

    private static IEnumerable<string> Reescribir(IEnumerable<string> bloque, string prefijo)
    {
        foreach (var linea in bloque)
        {
            var proyecto = Proyecto.Match(linea);
            if (proyecto.Success)
            {
                yield return $"{proyecto.Groups[1].Value}{prefijo}/{proyecto.Groups[2].Value}";
                continue;
            }

            var directorio = Directorio.Match(linea);
            if (directorio.Success)
            {
                var destino = directorio.Groups[2].Value == "."
                    ? prefijo
                    : $"{prefijo}/{directorio.Groups[2].Value}";

                yield return $"{directorio.Groups[1].Value}{destino}";
                continue;
            }

            yield return linea;
        }
    }
}
