using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class TyeServiceSelectorPagina : IDisposable
{
    [Inject]
    private ProfileRepository Perfiles { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private const string SinPerfil = "(sin perfil)";

    /// <summary>
    /// Servicios que siempre quedan tildados, con el motivo que se muestra al pasar el mouse. La
    /// casilla va deshabilitada: destildarlos deja un entorno que no sirve para nada. Sumar uno
    /// nuevo es agregarlo acá y en <c>Views/TyeServiceSelectorView.cs</c>, que tiene su propia copia.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> ServiciosObligatorios =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["seguridad"] = "Sin este servicio no levanta ninguno de los demás.",
            ["ui"] = "Es el front: sin él no hay pantalla desde donde usar lo que se levante.",
        };

    /// <summary>
    /// Servicio que la pantalla vuelve a tildar cada vez que arma la selección entera —"Desmarcar
    /// todos", aplicar un perfil—, porque casi todo circuito termina pasando por él. A diferencia
    /// de los obligatorios se puede destildar a mano, y al refrescar manda el archivo generado.
    /// </summary>
    private const string ServicioSugerido = "orquestador";

    private sealed class ServicioElegible
    {
        public required string Nombre { get; init; }
        public bool Activo { get; set; }
    }

    private List<ServicioElegible> _servicios = new();
    private IReadOnlyList<string> _nombresDePerfil = Array.Empty<string>();
    private string _perfilElegido = SinPerfil;
    private string _nombreNuevoPerfil = string.Empty;
    private bool _ocupado;
    private bool _guardandoPerfil;
    private bool _eliminandoPerfil;

    protected override void OnInitialized()
    {
        Releer();
        RecargarPerfiles();

        // Se vuelve al perfil con el que se generó el archivo vigente, sin reaplicarlo: los
        // servicios tildados ya salen de ese archivo, y pisarlos acá borraría cualquier ajuste
        // hecho a mano después.
        var ultimo = ConfiguracionDevTools.Compartida.UltimoPerfilDeTye;
        if (!string.IsNullOrWhiteSpace(ultimo) && _nombresDePerfil.Contains(ultimo))
        {
            _perfilElegido = ultimo;
        }
    }

    /// <summary>
    /// Deja registrado con qué perfil quedó generado el archivo, para que el inicio lo muestre y
    /// para volver a él la próxima vez que se abra esta pantalla.
    /// </summary>
    private static void RecordarPerfil(string? nombre)
    {
        var configuracion = ConfiguracionDevTools.Compartida;
        if (configuracion.UltimoPerfilDeTye == nombre) return;

        configuracion.UltimoPerfilDeTye = nombre;
        configuracion.Guardar();
    }

    private void Releer()
    {
        if (!File.Exists(TyeServiceToggler.MasterYamlPath))
        {
            _servicios = new List<ServicioElegible>();
            Informar($"No se encontró el maestro en {TyeServiceToggler.MasterYamlPath}.", "estado-error");
            return;
        }

        _servicios = TyeServiceToggler.ReadServices()
            .Select(s => new ServicioElegible { Nombre = s.Name, Activo = s.Enabled || EsObligatorio(s.Name) })
            .ToList();

        if (_servicios.Count == 0)
        {
            Informar("No se encontraron servicios en la lista 'services:' del tye.yaml.", "estado-advertencia");
            return;
        }

        var activos = _servicios.Count(s => s.Activo);
        var hayGenerado = File.Exists(TyeServiceToggler.GeneratedYamlPath);

        Informar(
            hayGenerado
                ? $"{_servicios.Count} servicios — {activos} activos (según {TyeServiceToggler.GeneratedFileName})."
                : $"{_servicios.Count} servicios — todos activos (todavía no generaste {TyeServiceToggler.GeneratedFileName}).",
            "estado-neutro");
    }

    private static bool EsObligatorio(string nombre) => ServiciosObligatorios.ContainsKey(nombre);

    /// <summary>Por qué el servicio no se puede destildar, o <c>null</c> si sí se puede.</summary>
    private static string? MotivoObligatorio(string nombre) =>
        ServiciosObligatorios.TryGetValue(nombre, out var motivo) ? motivo : null;

    private static bool EsSugerido(string nombre) =>
        string.Equals(nombre, ServicioSugerido, StringComparison.OrdinalIgnoreCase);

    /// <summary>Texto que se muestra al pasar el mouse por la casilla del servicio.</summary>
    private static string TituloDe(string nombre) =>
        MotivoObligatorio(nombre)
        ?? (EsSugerido(nombre)
            ? $"Sugerido: casi todo circuito termina pasando por '{nombre}'. Se puede destildar."
            : nombre);

    private void Alternar(ServicioElegible servicio, ChangeEventArgs e)
    {
        if (EsObligatorio(servicio.Nombre)) return;
        servicio.Activo = e.Value is true;
    }

    /// <summary>
    /// Al desmarcar sobreviven los obligatorios y el sugerido: la lista entera en blanco no es un
    /// punto de partida útil para ninguna combinación.
    /// </summary>
    private void MarcarTodos(bool valor)
    {
        foreach (var servicio in _servicios)
        {
            servicio.Activo = valor || EsObligatorio(servicio.Nombre) || EsSugerido(servicio.Nombre);
        }
    }

    // --- Perfiles -----------------------------------------------------------

    private void RecargarPerfiles() => _nombresDePerfil = Perfiles.GetProfileNames();

    private string? PerfilActual() => _perfilElegido == SinPerfil ? null : _perfilElegido;

    private void CambiarPerfil(ChangeEventArgs e)
    {
        _perfilElegido = e.Value?.ToString() ?? SinPerfil;

        var nombre = PerfilActual();
        if (nombre is null) return;

        var delPerfil = Perfiles.GetProfileServices(nombre);
        if (delPerfil is null)
        {
            Informar($"El perfil '{nombre}' ya no existe.", "estado-advertencia");
            RecargarPerfiles();
            _perfilElegido = SinPerfil;
            return;
        }

        foreach (var servicio in _servicios)
        {
            servicio.Activo = delPerfil.Contains(servicio.Nombre)
                || EsObligatorio(servicio.Nombre)
                || EsSugerido(servicio.Nombre);
        }

        // Un perfil guardado sin el sugerido igual lo recibe. Se avisa para que la diferencia
        // entre lo guardado y lo tildado no se lea como un perfil mal guardado.
        var porSugerencia = _servicios.Any(s => EsSugerido(s.Nombre)) && !delPerfil.Contains(ServicioSugerido);
        var activos = _servicios.Count(s => s.Activo);
        Informar(
            $"Perfil '{nombre}' aplicado — {activos} servicio(s) tildado(s)."
            + (porSugerencia ? $" '{ServicioSugerido}' quedó tildado por sugerencia." : string.Empty)
            + " Tocá 'Generar y guardar' para escribir el archivo.",
            "estado-neutro");
    }

    /// <summary>
    /// Los bloques se abren y se cierran con el mismo botón, y abrir uno cierra el otro: dos
    /// bloques desplegados a la vez compiten por la atención sin que haga falta.
    /// </summary>
    private void AlternarGuardarPerfil()
    {
        _guardandoPerfil = !_guardandoPerfil;
        _eliminandoPerfil = false;
        if (_guardandoPerfil) _nombreNuevoPerfil = PerfilActual() ?? string.Empty;
    }

    private void AlternarEliminarPerfil()
    {
        _eliminandoPerfil = !_eliminandoPerfil;
        _guardandoPerfil = false;
    }

    private void TeclaEnNombreDePerfil(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") GuardarPerfil();
        else if (e.Key == "Escape") _guardandoPerfil = false;
    }

    private void GuardarPerfil()
    {
        var nombre = _nombreNuevoPerfil.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) return;

        if (string.Equals(nombre, SinPerfil, StringComparison.OrdinalIgnoreCase))
        {
            Informar($"'{SinPerfil}' es un nombre reservado.", "estado-advertencia");
            return;
        }

        var activos = _servicios.Where(s => s.Activo).Select(s => s.Nombre).ToList();
        if (activos.Count == 0)
        {
            Informar("No hay servicios tildados. Tildá al menos uno antes de guardar el perfil.", "estado-advertencia");
            return;
        }

        Perfiles.SaveProfile(nombre, activos);
        RecargarPerfiles();
        _perfilElegido = nombre;
        _guardandoPerfil = false;
        _nombreNuevoPerfil = string.Empty;
        Informar($"Perfil '{nombre}' guardado con {activos.Count} servicio(s).", "estado-exito");
    }

    private void EliminarPerfil()
    {
        var nombre = PerfilActual();
        _eliminandoPerfil = false;
        if (nombre is null) return;

        Perfiles.DeleteProfile(nombre);
        RecargarPerfiles();
        _perfilElegido = SinPerfil;
        RecordarPerfil(null);
        Informar($"Perfil '{nombre}' eliminado.", "estado-neutro");
    }

    // --- Generación ---------------------------------------------------------

    private async Task Generar()
    {
        var seleccion = _servicios.ToDictionary(s => s.Nombre, s => s.Activo, StringComparer.Ordinal);

        if (seleccion.Count == 0 || seleccion.Values.All(v => !v))
        {
            Informar("Tildá al menos un servicio antes de generar.", "estado-advertencia");
            return;
        }

        _ocupado = true;
        Informar("Generando archivo...", "estado-neutro");
        StateHasChanged();

        try
        {
            await Task.Run(() => TyeServiceToggler.GenerateSelection(seleccion));

            // Recién acá queda registrado el perfil: es el momento en que se escribe el archivo, y
            // el archivo es lo que de verdad va a levantar. Elegir uno sin generar no cuenta.
            RecordarPerfil(PerfilActual());

            var activos = seleccion.Values.Count(v => v);
            Informar($"{TyeServiceToggler.GeneratedFileName} generado — {activos} servicio(s) activo(s). Corré: {TyeServiceToggler.RunCommand}", "estado-exito");
        }
        catch (Exception ex)
        {
            Informar($"Error al generar: {ex.Message}", "estado-error");
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task CopiarComando()
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", TyeServiceToggler.RunCommand);
            Informar($"Comando copiado: {TyeServiceToggler.RunCommand}", "estado-exito");
        }
        catch (Exception)
        {
            // Si el portapapeles no está disponible, al menos que el comando quede a la vista.
            Informar($"No se pudo copiar. El comando es: {TyeServiceToggler.RunCommand}", "estado-advertencia");
        }
    }

    private void Informar(string texto, string clase) => Barra.Informar(texto, clase);

    public void Dispose() => Barra.Limpiar();
}
