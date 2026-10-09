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

    private readonly ServicioDeTemas _temas = ServicioDeTemas.Instancia;

    private bool _carpetaExiste = true;
    private bool _hayAlgoritmoCore = true;

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

    private void ElegirCarpetaDeRepositorios()
    {
        var elegida = Dialogos.ElegirCarpeta(
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

    /// <summary>
    /// Pasa un color del tema a hexadecimal para escribirlo en un style en línea. Las miniaturas no
    /// pueden salir de las variables CSS porque muestran un tema distinto del que está aplicado.
    /// </summary>
    private static string Hex(System.Drawing.Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private void OnTemaCambiado(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose() => _temas.TemaCambiado -= OnTemaCambiado;
}
