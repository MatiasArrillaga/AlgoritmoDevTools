using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using AlgoritmoDevTools.Tools.MarkdownConverter.Services;
using AlgoritmoDevTools.Tools.SecretsManager.Services;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Los menús contextuales de todas las herramientas, en un solo lugar.
///
/// Antes cada pantalla tenía sus botones de agregar y quitar, y para saber qué había puesto en el
/// explorador había que recorrerlas una por una. El menú es uno solo —todas cuelgan de la misma
/// raíz y comparten su presupuesto de ítems—, así que decidirlo repartido no tenía mucho sentido.
///
/// Prender y apagar es todo lo que hay: qué muestra cada grupo lo arma la herramienta con lo que
/// haya guardado en ese momento, y lo reescribe sola cuando eso cambia. Por eso acá no se guarda
/// nada: el registro de Windows ya dice qué está instalado, y preguntárselo es más barato que
/// mantener una copia que se puede desincronizar.
///
/// **Para sumar una herramienta** alcanza con agregar una entrada más a <see cref="Todos"/>.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md.
/// </summary>
public sealed class MenusContextuales
{
    private readonly DomainRepository _dominios;
    private readonly SavedConnectionsRepository _conexiones;
    private readonly DocumentConverter _convertidor;

    public MenusContextuales(
        DomainRepository dominios,
        SavedConnectionsRepository conexiones,
        DocumentConverter convertidor)
    {
        _dominios = dominios;
        _conexiones = conexiones;
        _convertidor = convertidor;

        Todos =
        [
            new MenuDeHerramienta
            {
                Herramienta = "Commands Maker",
                Que = "Elegir el dominio y copiar sus tres comandos de migraciones.",
                EstaInstalado = MenuContextualComandos.EstaInstalado,
                Instalar = () => MenuContextualComandos.TryInstalar(_dominios.GetAll(), _dominios.GetRecientes()),
                Quitar = MenuContextualComandos.TryDesinstalar
            },

            new MenuDeHerramienta
            {
                Herramienta = "Secrets Manager",
                Que = "Restaurar los secretos y apuntar el proyecto a una conexión guardada.",
                EstaInstalado = MenuContextualSecretos.EstaInstalado,
                Instalar = () => MenuContextualSecretos.TryInstalar(_conexiones.GetAll()),
                Quitar = MenuContextualSecretos.TryDesinstalar
            },

            new MenuDeHerramienta
            {
                Herramienta = "Markdown Converter",
                // El único que no cuelga de la raíz DevTools: aparece al hacer clic derecho sobre el
                // documento, no sobre el fondo de la carpeta, así que no gasta del presupuesto.
                Que = "\"Convertir a Markdown\" al hacer clic derecho sobre un documento.",
                CuentaEnElPresupuesto = false,
                EstaInstalado = () => MenuContextual.EstaInstalado(_convertidor.ExtensionesSoportadas),
                Instalar = () => MenuContextual.TryInstalar(_convertidor.ExtensionesSoportadas),
                Quitar = () => MenuContextual.TryDesinstalar(_convertidor.ExtensionesSoportadas)
            }
        ];
    }

    public IReadOnlyList<MenuDeHerramienta> Todos { get; }

    /// <summary>Cuántas entradas dibuja hoy el explorador debajo de la raíz DevTools.</summary>
    public static int ItemsUsados => MenuContextualDevTools.ContarItems();

    public static int LimiteDeItems => MenuContextualDevTools.LimiteDeItems;

    /// <summary>
    /// Pasarse del límite no da ningún error: el explorador dibuja los primeros y descarta el resto
    /// en silencio, así que el síntoma es "me faltan opciones" sin nada que lo explique. Es lo único
    /// que hay que poder avisar antes de que pase.
    /// </summary>
    public static bool SeLlenoElMenu => ItemsUsados >= LimiteDeItems;
}

/// <summary>El menú de una herramienta: qué pone en el explorador y cómo se prende y se apaga.</summary>
public sealed class MenuDeHerramienta
{
    /// <summary>Nombre de la herramienta, el mismo con el que figura en el menú lateral.</summary>
    public required string Herramienta { get; init; }

    /// <summary>Qué queda a mano con este menú puesto.</summary>
    public required string Que { get; init; }

    /// <summary>
    /// Si sus ítems salen del presupuesto compartido. Lo que no cuelga de la raíz DevTools —un verbo
    /// sobre una extensión, por ejemplo— no compite con el resto.
    /// </summary>
    public bool CuentaEnElPresupuesto { get; init; } = true;

    public required Func<bool> EstaInstalado { get; init; }

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public required Func<string?> Instalar { get; init; }

    /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
    public required Func<string?> Quitar { get; init; }
}
