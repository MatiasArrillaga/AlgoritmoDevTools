using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services.Entorno;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class TyeServiceSelectorPagina : IDisposable
{
    [Inject]
    private ProfileRepository Perfiles { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

    [Inject]
    private IVentanaPrincipal Ventana { get; set; } = default!;

    [Inject]
    private ArranqueDeLaBarra BarraDelEntorno { get; set; } = default!;

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
    private RevisionDelEntorno? _revision;
    private CancellationTokenSource? _vigia;
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

    protected override async Task OnInitializedAsync() => await Revisar();

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

    // --- Levantar el entorno ------------------------------------------------

    /// <summary>
    /// Cómo está el entorno ahora: contra qué base se va a levantar y qué le falta. Se muestra en
    /// la pantalla, sin pedir confirmación: el dato sirve para enterarse, no para frenar a nadie.
    /// </summary>
    private sealed record RevisionDelEntorno(
        string? Servidor,
        string? BaseDeDatos,
        string? ErrorDeConexion,
        ResultadoDePrerrequisitos Prerrequisitos);

    /// <summary>
    /// Mira contra qué base se va a levantar y qué le falta al entorno. Corre sola al abrir la
    /// pantalla y después de destrabar algo: el dato está a la vista antes de apretar nada, que es
    /// lo que evita el ciclo completo cuando la conexión apuntaba a otro lado.
    /// </summary>
    private async Task Revisar()
    {
        try
        {
            // Las dos consultas levantan procesos (docker, netsh, dotnet user-secrets), así que
            // van fuera del hilo de la interfaz.
            var conexion = await Task.Run(ChequeoDePrerrequisitos.ConexionConfigurada);
            var prerrequisitos = await Task.Run(() => new ChequeoDePrerrequisitos().Revisar());

            _revision = new RevisionDelEntorno(conexion.Servidor, conexion.Base, conexion.Error, prerrequisitos);
        }
        catch (Exception ex)
        {
            Informar($"No se pudo revisar el entorno: {ex.Message}", "estado-error");
        }

        StateHasChanged();
        Vigilar();
    }

    /// <summary>
    /// Mientras falte algo —o mientras haya un entorno corriendo—, vuelve a mirar sola cada cinco
    /// segundos.
    ///
    /// <para>
    /// El remedio de casi todos los faltantes pasa por afuera de esta pantalla —abrir Docker
    /// Desktop y esperar a que arranque es el caso de todos los días—, así que una revisión que
    /// corre una sola vez al abrir deja el cartel viejo colgado: el usuario arregla lo que había
    /// que arreglar y la pantalla le sigue diciendo que está roto.
    /// </para>
    ///
    /// <para>
    /// Sólo mira mientras hay algo que mirar: cuando no falta nada se apaga, porque cada vuelta
    /// levanta un <c>docker ps</c>, un <c>netsh</c> y los user-secrets.
    /// </para>
    /// </summary>
    private void Vigilar()
    {
        // Mientras falte algo, porque el remedio pasa por afuera de esta pantalla; y mientras haya
        // entorno corriendo, porque si no se frena desde la barra el botón se queda diciendo
        // «Controlar» para siempre.
        var hayQueSeguirMirando = _revision is not null
            && (!_revision.Prerrequisitos.Listo || _revision.Prerrequisitos.HayEntornoCorriendo);

        if (!hayQueSeguirMirando)
        {
            Parar();
            return;
        }

        // Ya hay una mirando: Revisar() vuelve a pasar por acá en cada vuelta.
        if (_vigia is not null) return;

        _vigia = new CancellationTokenSource();
        var corte = _vigia.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(5));
                while (await reloj.WaitForNextTickAsync(corte))
                {
                    await InvokeAsync(Revisar);
                }
            }
            catch (OperationCanceledException)
            {
                // Se apagó porque ya no falta nada, o porque se cerró la pantalla.
            }
        }, corte);
    }

    private void Parar()
    {
        _vigia?.Cancel();
        _vigia?.Dispose();
        _vigia = null;
    }

    /// <summary>
    /// Corre el remedio del faltante con permisos de administrador y vuelve a revisar. El caso real
    /// es el de los puertos que Windows le reserva a Hyper-V: sin elevar, el comando contesta
    /// «Error de sistema 5» y el contenedor sigue sin arrancar.
    /// </summary>
    private async Task Destrabar(FaltanteDelEntorno faltante)
    {
        _ocupado = true;
        Informar("Pidiendo permisos de administrador...", "estado-neutro");
        StateHasChanged();

        try
        {
            var resultado = await Task.Run(() => ArregloElevado.Ejecutar(faltante.Remedio));

            Informar(resultado switch
            {
                ResultadoDelArreglo.Hecho => "Listo. Revisando de nuevo...",
                ResultadoDelArreglo.Cancelado => "Se canceló el permiso, no se tocó nada.",
                _ => "El arreglo no funcionó. Probalo a mano en una consola de administrador."
            }, resultado == ResultadoDelArreglo.Hecho ? "estado-exito" : "estado-advertencia");

            if (resultado != ResultadoDelArreglo.Hecho) return;
        }
        finally
        {
            _ocupado = false;
        }

        await Revisar();
    }

    /// <summary>
    /// Escribe el archivo y nada más, para quien prefiera arrancar tye por su cuenta desde una
    /// consola. No es el camino principal —para eso está Levantar— pero el comando no desapareció y
    /// hay quien lo usa así.
    /// </summary>
    private async Task Generar()
    {
        var seleccion = _servicios.ToDictionary(s => s.Nombre, s => s.Activo, StringComparer.Ordinal);
        if (seleccion.Values.All(activo => !activo))
        {
            Informar("Tildá al menos un servicio antes de generar.", "estado-advertencia");
            return;
        }

        _ocupado = true;
        StateHasChanged();

        try
        {
            await Task.Run(() => TyeServiceToggler.GenerateSelection(seleccion));
            RecordarPerfil(PerfilActual());

            var activos = seleccion.Values.Count(activo => activo);
            Informar(
                $"{TyeServiceToggler.GeneratedFileName} generado — {activos} servicio(s) activo(s). Corré: {TyeServiceToggler.RunCommand}",
                "estado-exito");
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

    /// <summary>
    /// Abre la barra, escribe el archivo, deja registrado con qué base se levanta y arranca tye en
    /// su propia consola. La ventana se minimiza recién cuando el panel contesta: antes de eso no se sabe si
    /// arrancó, y minimizar una ventana para que el usuario descubra solo que falló sería peor que
    /// no minimizarla.
    /// </summary>
    private async Task Levantar()
    {
        // Un segundo tye con los mismos servicios se pelea por los puertos y se pisa los
        // contenedores. Si ya hay uno, no se levanta nada: la barra se engancha al que está. Se
        // vuelve a preguntar acá en vez de confiar en el rótulo, porque entre el render y el clic
        // el entorno pudo haberse frenado.
        if (LanzadorDeTye.HayTyeCorriendo())
        {
            Controlar();
            return;
        }

        var seleccion = _servicios.ToDictionary(s => s.Nombre, s => s.Activo, StringComparer.Ordinal);
        if (seleccion.Values.All(activo => !activo))
        {
            Informar("Tildá al menos un servicio antes de levantar.", "estado-advertencia");
            return;
        }

        _ocupado = true;
        Informar("Generando el archivo y arrancando tye...", "estado-neutro");
        StateHasChanged();

        // La barra sale con el clic, antes de generar nada: es lo único que el usuario tiene para
        // mirar mientras tye compila, y apareciendo al final el botón parece no haber hecho nada.
        // Si después algo falla, la barra dice «Sin entorno», que es la verdad.
        var errorDeLaBarra = BarraDelEntorno.Lanzar();

        try
        {
            await Task.Run(() => TyeServiceToggler.GenerateSelection(seleccion));

            // Recién acá queda registrado el perfil: es el momento en que se escribe el archivo, y
            // el archivo es lo que de verdad va a levantar. Elegir uno sin levantar no cuenta.
            RecordarPerfil(PerfilActual());
            SelloDeConexion.Escribir(_revision?.Servidor, _revision?.BaseDeDatos);

            var proceso = new LanzadorDeTye().Levantar(
                TyeServiceToggler.GeneratedFileName,
                EntornoCompleto.PuertoPrincipal,
                sinCompilar: false);

            if (proceso is null)
            {
                Informar("No se pudo arrancar tye. ¿Está instalado como herramienta global de dotnet?", "estado-error");
                return;
            }

            if (errorDeLaBarra is not null)
            {
                // Que la barra no abra no impide que el entorno levante, pero hay que decirlo: si
                // se calla, parece que no pasó nada.
                Informar($"Tye arrancando, pero no se pudo abrir la barra: {errorDeLaBarra}", "estado-advertencia");
                return;
            }

            Informar("Tye arrancando en su propia consola. Seguilo en la barra.", "estado-exito");
            Ventana.Minimizar();
        }
        catch (Exception ex)
        {
            Informar($"Error al levantar: {ex.Message}", "estado-error");
        }
        finally
        {
            _ocupado = false;
        }
    }

    /// <summary>
    /// Hay un entorno corriendo, así que el botón controla en vez de levantar. Sale de la última
    /// revisión, que es lo que alcanza para el rótulo; la decisión de verdad la toma
    /// <see cref="Levantar"/> en el momento del clic.
    /// </summary>
    private bool HayQueControlar => _revision?.Prerrequisitos.HayEntornoCorriendo == true;

    /// <summary>
    /// Engancha la barra al entorno que ya está corriendo, sin tocar nada más.
    ///
    /// <para>
    /// No hace falta nada del otro lado: la barra lee el estado de la API del panel y no de quien
    /// la lanzó, así que controla cualquier tye que conteste —reiniciar, bajar y subir servicios,
    /// frenar—. Engancharse es, literalmente, no levantar otro y abrirla.
    /// </para>
    ///
    /// <para>
    /// <b>No se genera el yaml, no se registra el perfil y no se escribe el sello de conexión.</b>
    /// Ese entorno lo levantó otro —otra sesión de DevTools, o el usuario desde su consola— y con
    /// qué base arrancó no nos consta. Escribir el sello de la conexión configurada ahora sería
    /// afirmar algo que no sabemos, y la barra quedaría comparando contra un dato inventado: diría
    /// que la base está al día, o que cambió, sin fundamento.
    /// </para>
    ///
    /// <para>
    /// La consola de tye tampoco es nuestra, así que <c>PidDeTye</c> queda como esté. Al frenar la
    /// cierra igual el barrido por nombre, que es el caso que ese código ya contempla.
    /// </para>
    /// </summary>
    private void Controlar()
    {
        var error = BarraDelEntorno.Lanzar();

        if (error is not null)
        {
            Informar($"No se pudo abrir la barra: {error}", "estado-error");
            return;
        }

        Informar("Enganchada al entorno que ya estaba corriendo. Controlalo desde la barra.", "estado-exito");
        Ventana.Minimizar();
    }

    private void Informar(string texto, string clase) => Barra.Informar(texto, clase);

    public void Dispose()
    {
        Parar();
        Barra.Limpiar();
    }
}
