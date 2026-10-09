namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Dónde están los repositorios sobre los que trabajan las herramientas.
///
/// Antes cada herramienta armaba la ruta por su cuenta y todas asumían
/// <c>%USERPROFILE%\source\repos</c>. Eso andaba mientras todos clonaran en el mismo lugar; quien
/// tuviera los repositorios en otro disco no tenía forma de decirlo. Ahora sale de Configuraciones
/// y el valor de siempre quedó como el predeterminado.
///
/// Se resuelve en cada consulta y no se guarda en un campo: así, al cambiar la carpeta, las
/// herramientas ya abiertas toman la nueva sin reiniciar.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md.
/// </summary>
public static class RutasDeTrabajo
{
    /// <summary>Nombre del repositorio del backend dentro de la carpeta de repositorios.</summary>
    public const string NombreDeAlgoritmoCore = "AlgoritmoCore";

    /// <summary>Lo que se usa mientras el usuario no elija otra cosa.</summary>
    public static string CarpetaDeRepositoriosPorDefecto => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "source",
        "repos");

    /// <summary>Carpeta que contiene los repositorios. Cae en la predeterminada si no hay preferencia.</summary>
    public static string CarpetaDeRepositorios
    {
        get
        {
            var elegida = ConfiguracionDevTools.Compartida.CarpetaDeRepositorios;
            return string.IsNullOrWhiteSpace(elegida) ? CarpetaDeRepositoriosPorDefecto : elegida;
        }
    }

    /// <summary>Raíz del repositorio de AlgoritmoCore.</summary>
    public static string AlgoritmoCore => Path.Combine(CarpetaDeRepositorios, NombreDeAlgoritmoCore);
}
