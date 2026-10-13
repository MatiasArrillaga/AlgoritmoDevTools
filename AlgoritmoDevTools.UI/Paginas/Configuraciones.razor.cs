using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class Configuraciones : IDisposable
{
    [Inject]
    private IDialogosDelSistema Dialogos { get; set; } = default!;

    [Inject]
    private TransferenciaDeConfiguracion Transferencia { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

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

    protected override void OnInitialized()
    {
        _temas.TemaCambiado += OnTemaCambiado;
        RevisarCarpeta();
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
