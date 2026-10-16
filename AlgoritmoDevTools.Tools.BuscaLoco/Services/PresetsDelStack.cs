using AlgoritmoDevTools.Core.Infrastructure;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>
/// Un atajo de búsqueda que conoce este stack.
/// </summary>
/// <param name="Nombre">Lo que se lee en el botón.</param>
/// <param name="PatronDeNombre">Lo que escribe en el campo de nombre al tocarlo.</param>
/// <param name="CarpetaEnLaRuta">
/// Cuando está, el archivo además tiene que colgar de una carpeta que se llame así. Es lo que
/// distingue «buscar en las unidades funcionales» de «buscar en todo».
/// </param>
public sealed record PresetDelStack(string Nombre, string PatronDeNombre, string? CarpetaEnLaRuta = null);

/// <summary>
/// Los atajos que sólo tienen sentido adentro de los repositorios del equipo.
///
/// <b>Es lo que separa a esta herramienta de un buscador cualquiera.</b> Un buscador genérico ya
/// existe tres veces; lo que no existe es uno que sepa que <c>README.referencia-*</c> son los
/// documentos técnico-funcionales de la casa, o que los <c>*DbContext*</c> y <c>*Configuration*</c>
/// son los archivos que obligan a migrar.
///
/// Los presets <b>escriben su patrón en el campo</b> en lugar de aplicarlo por abajo: así se ve qué
/// hicieron y se puede retocar. Un desplegable que filtra sin mostrar cómo no enseña nada.
///
/// Sólo se ofrecen cuando la raíz de la búsqueda cae adentro de la carpeta de repositorios:
/// buscando en una carpeta de documentos, filtrar por <c>.razor</c> no significa nada.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public static class PresetsDelStack
{
    public static IReadOnlyList<PresetDelStack> Todos { get; } =
    [
        new("Código C#", "*.cs"),
        new("Vistas .razor", "*.razor"),
        new("Documentos de referencia", "README*.md"),
        new("DbContext y configuraciones", "*DbContext*.cs, *Configuration*.cs"),
        new("Unidades Funcionales", "*.cs", "Unidades Funcionales")
    ];

    /// <summary>
    /// Si conviene ofrecerlos para esa raíz, o sea si está adentro de la carpeta de repositorios.
    ///
    /// La carpeta sale de <see cref="RutasDeTrabajo"/> y no de una ruta escrita a mano: es la que el
    /// usuario eligió en Configuraciones, y quien la tenga en otro disco también merece sus atajos.
    /// </summary>
    public static bool AplicanEn(string raiz)
    {
        if (string.IsNullOrWhiteSpace(raiz)) return false;

        try
        {
            var repositorios = Path.GetFullPath(RutasDeTrabajo.CarpetaDeRepositorios);
            var elegida = Path.GetFullPath(raiz);

            return elegida.StartsWith(repositorios, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Una ruta con caracteres que Windows no acepta no es motivo para romper la pantalla:
            // simplemente no se ofrecen los atajos.
            return false;
        }
    }
}
