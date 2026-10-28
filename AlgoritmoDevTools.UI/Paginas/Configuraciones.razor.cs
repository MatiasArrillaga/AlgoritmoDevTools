using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

/// <summary>Cada cosa que se puede configurar. El orden es el del menú.</summary>
public enum Seccion
{
    Apariencia,
    Repositorios,
    Inicio,
    MenuContextual,
    VisualStudio,
    ExportarEImportar,
    Actualizaciones
}

/// <summary>Una entrada del menú de Configuraciones.</summary>
/// <param name="Id">Qué sección muestra.</param>
/// <param name="Grupo">Encabezado bajo el que se agrupa. Las de un mismo grupo van juntas.</param>
/// <param name="Nombre">Cómo se la nombra en el menú.</param>
public sealed record EntradaDeAjustes(Seccion Id, string Grupo, string Nombre);

public partial class Configuraciones : IDisposable
{
    /// <summary>
    /// El menú de la izquierda. Agregar una sección es una línea más acá y un bloque en el markup;
    /// el agrupado sale solo de <see cref="EntradaDeAjustes.Grupo"/>, así que las de un mismo grupo
    /// tienen que ir seguidas.
    /// </summary>
    private static readonly EntradaDeAjustes[] Secciones =
    [
        new(Seccion.Apariencia, "General", "Apariencia"),
        new(Seccion.Repositorios, "General", "Repositorios"),
        new(Seccion.Inicio, "Integración con Windows", "Inicio con Windows"),
        new(Seccion.MenuContextual, "Integración con Windows", "Menú contextual"),
        new(Seccion.VisualStudio, "Integración con Visual Studio", "Extensiones"),
        new(Seccion.ExportarEImportar, "Configuración", "Exportar e importar"),
        new(Seccion.Actualizaciones, "Configuración", "Actualizaciones")
    ];

    /// <summary>
    /// Con qué sección abrir. La manda el armazón cuando viene de un aviso que se resuelve en una en
    /// particular; null es la primera, que es lo habitual.
    /// </summary>
    [Parameter]
    public Seccion? Inicial { get; set; }

    private Seccion _seccion = Seccion.Apariencia;

    [Inject]
    private IDialogosDelSistema Dialogos { get; set; } = default!;

    [Inject]
    private TransferenciaDeConfiguracion Transferencia { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

    [Inject]
    private MenusContextuales Menus { get; set; } = default!;

    [Inject]
    private ArranqueDelWidget Arranque { get; set; } = default!;

    [Inject]
    private ServicioDeActualizaciones Actualizaciones { get; set; } = default!;

    [Inject]
    private IReinicioDeLaAplicacion Reinicio { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;

    private bool _carpetaExiste = true;
    private bool _hayAlgoritmoCore = true;

    /// <summary>Qué se va a llevar el archivo, mientras el árbol está a la vista.</summary>
    private SeleccionDeExportacion? _seleccion;

    /// <summary>Lo que trajo el archivo elegido, mientras espera que lo confirmen.</summary>
    private PaqueteDeConfiguracion? _aImportar;

    private string? _nombreDelArchivo;

    /// <summary>
    /// Qué herramientas tienen su menú puesto. Se lee del registro una vez al entrar y después de
    /// cada cambio: preguntárselo en cada dibujado sería ir al registro por cada herramienta cada
    /// vez que alguien toca una casilla.
    /// </summary>
    private readonly HashSet<string> _instalados = [];

    /// <summary>
    /// Lo que dice la clave Run, con los tres estados: apagado, puesto apuntando a este ejecutable,
    /// o puesto apuntando a otro. El tercero es el que no se puede confundir con "apagado": pasa
    /// todo el tiempo al abrir esta ventana desde Visual Studio mientras el widget que arranca con
    /// la sesión es el publicado.
    /// </summary>
    private ArranqueConWindows.ArranqueRegistrado _arranque = new(ArranqueConWindows.EstadoDelArranque.Apagado, null);

    /// <summary>
    /// Mientras está prendido, el aviso de que este ejecutable sale de la carpeta de compilación
    /// está a la vista esperando que se confirme o se cancele.
    /// </summary>
    private bool _confirmarCompilacion;

    /// <summary>
    /// Estado de la extensión de Visual Studio, releído en <see cref="RevisarLaExtension"/>. Se
    /// guarda en campos y no se consulta desde el markup porque averiguarlo cuesta —una de las
    /// consultas corre <c>vswhere</c>— y el markup se evalúa en cada render.
    /// </summary>
    private bool _hayVisualStudio;

    private string? _versionDeLaExtension;

    /// <summary>
    /// Qué versión hay publicada, para poder avisar que la instalada quedó vieja. Se averigua
    /// leyendo el nombre del adjunto del release, sin bajarlo. Null mientras no se pudo consultar.
    /// </summary>
    private string? _versionPublicada;

    private bool _trabajandoEnLaExtension;

    /// <summary>Si lo publicado es más nuevo que lo instalado.</summary>
    private bool HayExtensionNueva =>
        ExtensionDeVisualStudio.HayVersionNueva(_versionDeLaExtension, _versionPublicada);

    private bool _widgetCorriendo;
    private bool _instalando;

    /// <summary>El tilde: está puesto en el arranque, sea este ejecutable o cualquier otro.</summary>
    private bool ArrancaConWindows => _arranque.EstaPuesto;

    /// <summary>Está puesto, pero el que va a arrancar no es el ejecutable que corre esta ventana.</summary>
    private bool ApuntaAOtroBinario => _arranque.Estado is ArranqueConWindows.EstadoDelArranque.ApuntaAOtro;

    protected override void OnInitialized()
    {
        _temas.TemaCambiado += OnTemaCambiado;
        if (Inicial is { } pedida) _seccion = pedida;
        RevisarCarpeta();
        RevisarMenus();
        _arranque = Arranque.Leer();
        RevisarLaExtension();
        _widgetCorriendo = ArranqueDelWidget.EstaCorriendo();
    }

    /// <summary>
    /// Levanta el widget sin esperar a reiniciar la sesión. El estado se vuelve a mirar después de
    /// un momento: el proceso tarda en tomar el mutex, y preguntar en el acto diría que no arrancó.
    /// </summary>
    private async Task LanzarWidget()
    {
        var error = Arranque.Lanzar();

        if (error is not null)
        {
            Barra.Informar($"No se pudo abrir el widget: {error}", "estado-error");
            return;
        }

        await Task.Delay(1200);
        _widgetCorriendo = ArranqueDelWidget.EstaCorriendo();

        Barra.Informar(
            _widgetCorriendo
                ? "Widget abierto: está al lado del reloj."
                : "El widget se lanzó, pero todavía no respondió. Fijate en la bandeja.",
            _widgetCorriendo ? "estado-exito" : "estado-advertencia");
    }

    /// <summary>
    /// Pone o saca el widget de los programas que arrancan con la sesión.
    ///
    /// Prenderlo con el Shell corriendo desde la carpeta de compilación no se hace de una: primero
    /// se avisa. Un widget residente levantado desde <c>bin\Debug</c> mantiene tomados los DLL del
    /// proyecto y la próxima compilación muere con un error que no lo nombra; y si ya había una
    /// entrada apuntando al publicado, prender el tilde la pisaba sin decir nada.
    /// </summary>
    private void CambiarArranque(bool activar)
    {
        _confirmarCompilacion = false;

        if (activar && ArranqueDelWidget.CorreDesdeCompilacion())
        {
            _confirmarCompilacion = true;
            return;
        }

        AplicarArranque(activar);
    }

    /// <summary>Lo que se eligió en el aviso: registrar igual el ejecutable de la compilación.</summary>
    private void ActivarDesdeCompilacion()
    {
        _confirmarCompilacion = false;
        AplicarArranque(activar: true, aunqueCorraDesdeCompilacion: true);
    }

    /// <summary>
    /// Escribe el cambio y vuelve a leer del registro: si la escritura falló, la pantalla tiene que
    /// quedar como está el sistema y no como se pidió.
    /// </summary>
    private void AplicarArranque(bool activar, bool aunqueCorraDesdeCompilacion = false)
    {
        var error = activar ? Arranque.Activar(aunqueCorraDesdeCompilacion) : Arranque.Desactivar();

        if (error is not null)
        {
            Barra.Informar($"No se pudo cambiar el inicio con Windows: {error}", "estado-error");
        }
        else
        {
            Barra.Informar(activar
                ? "El widget va a arrancar con Windows."
                : "El widget ya no arranca con Windows.", "estado-exito");
        }

        _arranque = Arranque.Leer();
    }

    /// <summary>
    /// Relee si hay dónde instalar extensiones y qué versión hay puesta.
    ///
    /// No mira si Visual Studio está abierto, a propósito: avisarlo de entrada obliga a leer una
    /// advertencia que la mayoría de las veces no viene al caso. Que el IDE esté abierto lo
    /// descubre el servicio al intentar, y recién ahí se dice, que es cuando importa.
    /// </summary>
    private void RevisarLaExtension()
    {
        _hayVisualStudio = ExtensionDeVisualStudio.HayVisualStudio();
        _versionDeLaExtension = ExtensionDeVisualStudio.VersionInstalada();
    }

    private Task InstalarLaExtension() => CambiarLaExtension(instalar: true);

    private Task QuitarLaExtension() => CambiarLaExtension(instalar: false);

    /// <summary>
    /// Pone o saca la extensión. Al revés que los menús del explorador, esto no es instantáneo: hay
    /// una descarga y después una ventana del instalador esperando a que el usuario acepte el UAC.
    /// Por eso los botones quedan deshabilitados mientras tanto, y el estado se relee al terminar en
    /// vez de darlo por hecho: el usuario puede haber cancelado el instalador.
    /// </summary>
    private async Task CambiarLaExtension(bool instalar)
    {
        _trabajandoEnLaExtension = true;

        // Pisa lo que hubiera de un intento anterior. Sin esto, mientras corre el nuevo se sigue
        // leyendo el error del viejo —"hay que cerrar Visual Studio"— aunque ya se haya cerrado, y
        // parece que falló otra vez. Además hay un rato largo sin novedades: puede haber una
        // descarga y después una ventana del instalador esperando a que el usuario conteste.
        Barra.Informar(instalar
            ? "Instalando la extensión de Visual Studio..."
            : "Quitando la extensión de Visual Studio...");

        var error = instalar
            ? await ExtensionDeVisualStudio.TryInstalarAsync()
            : await ExtensionDeVisualStudio.TryDesinstalarAsync();

        _trabajandoEnLaExtension = false;
        RevisarLaExtension();

        if (error is not null)
        {
            var que = instalar ? "instalar" : "quitar";
            Barra.Informar($"No se pudo {que} la extensión de Visual Studio: {error}", "estado-error");
            return;
        }

        var hecho = instalar ? "instalada" : "quitada";
        Barra.Informar($"Extensión de Visual Studio {hecho}.", "estado-exito");
    }

    private void RevisarMenus()
    {
        _instalados.Clear();

        foreach (var menu in Menus.Todos.Where(menu => menu.EstaInstalado()))
        {
            _instalados.Add(menu.Herramienta);
        }
    }

    /// <summary>
    /// Prende o apaga el menú de una herramienta. Qué ítems escribe los decide ella con lo que haya
    /// guardado; acá sólo se dice si va o no va.
    /// </summary>
    private void CambiarMenu(MenuDeHerramienta menu, bool instalar)
    {
        var error = instalar ? menu.Instalar() : menu.Quitar();

        if (error is not null)
        {
            var que = instalar ? "agregar al" : "quitar del";
            Barra.Informar($"No se pudo {que} menú contextual: {error}", "estado-error");
        }
        else
        {
            var que = instalar ? "agregado al" : "quitado del";
            Barra.Informar($"{menu.Herramienta}: {que} menú contextual.", "estado-exito");
        }

        RevisarMenus();
    }

    /// <summary>
    /// Avisa si la carpeta elegida no sirve. Sin esto, el error aparece mucho después y en otra
    /// pantalla: el detector de migraciones diciendo que no puede leer HEAD, o el selector de
    /// servicios sin encontrar el tye.yaml.
    /// </summary>
    private void RevisarCarpeta()
    {
        var carpeta = RutasDeTrabajo.CarpetaDeRepositorios;
        _carpetaExiste = Directory.Exists(carpeta);
        _hayAlgoritmoCore = _carpetaExiste && Directory.Exists(RutasDeTrabajo.AlgoritmoCore);
    }

    private async Task ElegirCarpetaDeRepositorios()
    {
        var elegida = await Dialogos.ElegirCarpeta(
            "Elegí la carpeta que contiene los repositorios",
            RutasDeTrabajo.CarpetaDeRepositorios);

        if (string.IsNullOrWhiteSpace(elegida)) return;

        // La predeterminada se guarda como vacía: así, si alguna vez cambia, el valor acompaña.
        var esLaPorDefecto = string.Equals(
            elegida.TrimEnd(Path.DirectorySeparatorChar),
            RutasDeTrabajo.CarpetaDeRepositoriosPorDefecto.TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

        _temas.Configuracion.CarpetaDeRepositorios = esLaPorDefecto ? null : elegida;
        _temas.Configuracion.Guardar();
        RevisarCarpeta();
    }

    private void RestaurarCarpetaPorDefecto()
    {
        _temas.Configuracion.CarpetaDeRepositorios = null;
        _temas.Configuracion.Guardar();
        RevisarCarpeta();
    }

    private void Elegir(Tema tema) => _temas.Cambiar(tema);

    // --- Exportar e importar ----------------------------------------------------

    /// <summary>
    /// Abre el árbol con todo lo que hay para exportar. Se arma recién acá, al pedirlo, y no al
    /// entrar a la pantalla: son tres consultas a SQLite que no tienen por qué correr cada vez que
    /// alguien viene a cambiar el tema.
    /// </summary>
    private void PrepararExportacion()
    {
        _aImportar = null;
        _seleccion = Transferencia.PrepararSeleccion();
        Barra.Limpiar();
    }

    private async Task Exportar()
    {
        if (_seleccion is not { } seleccion) return;

        var ruta = await Dialogos.ElegirDondeGuardar(
            "Elegí dónde guardar la configuración",
            "Configuración de DevTools|*.json",
            $"devtools-{Environment.UserName}-{DateTime.Now:yyyy-MM-dd}.json");

        if (string.IsNullOrWhiteSpace(ruta)) return;

        try
        {
            var paquete = Transferencia.Armar(seleccion);
            Transferencia.Escribir(ruta, paquete);
            Barra.Informar($"Exportado a {Path.GetFileName(ruta)}: {Contenido(paquete)}.", "estado-exito");
            _seleccion = null;
        }
        catch (Exception ex)
        {
            Barra.Informar($"No se pudo exportar: {ex.Message}", "estado-error");
        }
    }

    /// <summary>
    /// Pone en su estado intermedio las casillas que mandan sobre otras cuando hay algunas marcadas
    /// y otras no. Es lo único del árbol que no se puede dejar resuelto en el marcado.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool primeraVez)
    {
        if (_seleccion is not null)
        {
            await JS.InvokeVoidAsync("devtools.sincronizarParciales");
        }

        // Después del primer dibujo y no en OnInitialized: esto sale a internet, y la pantalla no
        // tiene por qué esperarlo para mostrarse. Si no hay red, se queda sin saber qué hay
        // publicado y no pasa nada más.
        if (primeraVez && _hayVisualStudio)
        {
            _versionPublicada = await ExtensionDeVisualStudio.VersionPublicadaAsync();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Lee el archivo y lo deja a la vista, pero no toca nada todavía: primero hay que poder ver qué
    /// trae y de quién es. Recién ahí se confirma.
    /// </summary>
    private async Task ElegirQueImportar()
    {
        _seleccion = null;

        var elegidos = await Dialogos.ElegirArchivos(
            "Elegí el archivo de configuración",
            "Configuración de DevTools|*.json");

        if (elegidos.Count == 0) return;

        try
        {
            _aImportar = Transferencia.Leer(elegidos[0]);
            _nombreDelArchivo = Path.GetFileName(elegidos[0]);
            Barra.Limpiar();
        }
        catch (Exception ex)
        {
            _aImportar = null;
            Barra.Informar($"No se pudo leer el archivo: {ex.Message}", "estado-error");
        }
    }

    private void Importar()
    {
        if (_aImportar is null) return;

        try
        {
            Barra.Informar(Resumen(Transferencia.Importar(_aImportar)), "estado-exito");
        }
        catch (Exception ex)
        {
            Barra.Informar($"No se pudo importar: {ex.Message}", "estado-error");
        }
        finally
        {
            _aImportar = null;
        }
    }

    // --- Actualizaciones --------------------------------------------------------

    private Task Buscar() => Actualizaciones.BuscarAsync();

    /// <summary>
    /// Baja la versión nueva y la deja puesta. No reinicia sola: puede haber algo a medio hacer en
    /// otra pantalla, y perderlo por una actualización que podía esperar sería peor que la demora.
    /// </summary>
    private async Task Instalar()
    {
        _instalando = true;
        Barra.Informar("Bajando la versión nueva...", "estado-neutro");

        try
        {
            var error = await Actualizaciones.InstalarAsync();

            Barra.Informar(
                error is null
                    ? "Listo: la versión nueva quedó puesta. Falta reiniciar para empezar a usarla."
                    : $"No se pudo actualizar: {error}",
                error is null ? "estado-exito" : "estado-error");
        }
        finally
        {
            _instalando = false;
        }
    }

    /// <summary>
    /// Cierra y vuelve a abrir, que es todo lo que falta para que empiece a correr la versión que ya
    /// quedó instalada. No pregunta: el botón está abajo del aviso que lo explica, y quien no quiera
    /// reiniciar ahora simplemente no lo toca.
    /// </summary>
    private void Reiniciar()
    {
        Barra.Informar("Reiniciando...", "estado-neutro");
        Reinicio.Reiniciar();
    }

    /// <summary>Qué trae el paquete, en palabras: "5 conexiones, 13 dominios y 3 perfiles de Tye".</summary>
    private static string Contenido(PaqueteDeConfiguracion paquete) => Enumerar(
        (paquete.Conexiones.Count, "conexión", "conexiones"),
        (paquete.Dominios.Count, "dominio", "dominios"),
        (paquete.PerfilesDeTye.Count, "perfil de Tye", "perfiles de Tye")) ?? "nada";

    private static string Resumen(ResultadoDeImportacion resultado)
    {
        var agregado = Enumerar(
            (resultado.ConexionesNuevas, "conexión", "conexiones"),
            (resultado.DominiosNuevos, "dominio", "dominios"),
            (resultado.PerfilesNuevos, "perfil de Tye", "perfiles de Tye"));

        if (agregado is null)
        {
            return "No había nada nuevo: todo lo del archivo ya lo tenías.";
        }

        return resultado.TotalRepetido == 0
            ? $"Se agregó: {agregado}."
            : $"Se agregó: {agregado}. Otras {resultado.TotalRepetido} ya las tenías y quedaron como estaban.";
    }

    /// <summary>
    /// Arma "5 conexiones, 13 dominios y 3 perfiles" salteando lo que está en cero, o null si no
    /// quedó nada que enumerar.
    /// </summary>
    private static string? Enumerar(params (int Cantidad, string Singular, string Plural)[] partes)
    {
        var textos = partes
            .Where(parte => parte.Cantidad > 0)
            .Select(parte => $"{parte.Cantidad} {(parte.Cantidad == 1 ? parte.Singular : parte.Plural)}")
            .ToList();

        return textos.Count switch
        {
            0 => null,
            1 => textos[0],
            _ => $"{string.Join(", ", textos.Take(textos.Count - 1))} y {textos[^1]}"
        };
    }

    /// <summary>
    /// Pasa un color del tema a hexadecimal para escribirlo en un style en línea. Las miniaturas no
    /// pueden salir de las variables CSS porque muestran un tema distinto del que está aplicado.
    /// </summary>
    private static string Hex(System.Drawing.Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private void OnTemaCambiado(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        _temas.TemaCambiado -= OnTemaCambiado;
        Barra.Limpiar();
    }
}
