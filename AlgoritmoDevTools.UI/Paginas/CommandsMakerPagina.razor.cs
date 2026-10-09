using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.CommandsMaker.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class CommandsMakerPagina : IDisposable
{
    [Inject]
    private DomainRepository Dominios { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private List<string> _todosLosDominios = new();
    private string _dominio = string.Empty;
    private string _dominioNuevo = string.Empty;
    private string _nombreDeMigracion = string.Empty;
    private string _ultimoComando = string.Empty;
    private string _estadoMenu = string.Empty;
    private string _claseEstadoMenu = "estado-neutro";
    private bool _conPrefijoDeDominio = true;
    private bool _menuInstalado;
    private bool _agregandoDominio;
    private bool _eliminandoDominio;

    protected override void OnInitialized()
    {
        RecargarDominios();

        // Arranca en el último dominio usado, que es el mismo que el menú contextual destaca.
        var ultimo = Dominios.GetUltimoDominio();
        if (!string.IsNullOrWhiteSpace(ultimo) && _todosLosDominios.Contains(ultimo)) _dominio = ultimo;
        else _dominio = _todosLosDominios.FirstOrDefault() ?? string.Empty;

        _nombreDeMigracion = GeneradorDeComandos.NombreDeMigracionSugerido(_dominio);
        ActualizarEstadoDelMenu();
    }

    private void RecargarDominios() => _todosLosDominios = Dominios.GetAll();

    private void CambiarDominio(ChangeEventArgs e)
    {
        _dominio = e.Value?.ToString() ?? string.Empty;
        _nombreDeMigracion = RearmarNombre(_nombreDeMigracion);
    }

    private void CambiarPrefijo(ChangeEventArgs e)
    {
        _conPrefijoDeDominio = e.Value is true;
        _nombreDeMigracion = RearmarNombre(_nombreDeMigracion);
    }

    /// <summary>
    /// Pone o saca el prefijo "[Dominio]." del nombre, respetando lo que el usuario haya escrito
    /// después del punto.
    /// </summary>
    private string RearmarNombre(string nombre)
    {
        var sinPrefijo = nombre;
        var abre = nombre.IndexOf('[');
        var punto = nombre.IndexOf('.');
        if (abre >= 0 && punto > abre) sinPrefijo = nombre[(punto + 1)..];

        return _conPrefijoDeDominio ? $"[{_dominio}].{sinPrefijo}" : sinPrefijo;
    }

    private Task CopiarAdd() => Copiar(GeneradorDeComandos.AddMigration(_dominio, _nombreDeMigracion));

    private Task CopiarRemove() => Copiar(GeneradorDeComandos.RemoveMigration(_dominio));

    private Task CopiarUpdate() => Copiar(GeneradorDeComandos.UpdateDatabase(_dominio));

    private async Task Copiar(string comando)
    {
        _ultimoComando = comando;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", comando);
            Informar("Comando copiado al portapapeles.", "estado-exito");
        }
        catch (Exception)
        {
            Informar("No se pudo copiar: el comando quedó abajo para copiarlo a mano.", "estado-advertencia");
        }

        // Registrar el uso cambia cuál es el dominio destacado, así que el menú se reescribe.
        Dominios.RegistrarUso(_dominio);
        SincronizarMenu();
    }

    /// <summary>
    /// Los bloques se abren y se cierran con el mismo botón, y abrir uno cierra el otro.
    /// </summary>
    private void AlternarAgregarDominio()
    {
        _agregandoDominio = !_agregandoDominio;
        _eliminandoDominio = false;
        if (_agregandoDominio) _dominioNuevo = string.Empty;
    }

    private void AlternarEliminarDominio()
    {
        _eliminandoDominio = !_eliminandoDominio;
        _agregandoDominio = false;
    }

    private void TeclaEnDominioNuevo(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") AgregarDominio();
        else if (e.Key == "Escape") _agregandoDominio = false;
    }

    private void AgregarDominio()
    {
        var nombre = _dominioNuevo.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) return;

        Dominios.Add(nombre);
        RecargarDominios();
        _dominio = nombre;
        _nombreDeMigracion = GeneradorDeComandos.NombreDeMigracionSugerido(_dominio, _conPrefijoDeDominio);
        _dominioNuevo = string.Empty;
        _agregandoDominio = false;
        SincronizarMenu();
        Informar($"Dominio '{nombre}' agregado.", "estado-exito");
    }

    private void EliminarDominio()
    {
        var nombre = _dominio;
        _eliminandoDominio = false;
        if (string.IsNullOrWhiteSpace(nombre)) return;

        Dominios.Remove(nombre);
        RecargarDominios();
        _dominio = _todosLosDominios.FirstOrDefault() ?? string.Empty;
        SincronizarMenu();
        Informar($"Dominio '{nombre}' eliminado.", "estado-neutro");
    }

    // --- Menú contextual ----------------------------------------------------

    private void InstalarMenu()
    {
        var error = MenuContextualComandos.TryInstalar(Dominios.GetAll(), Dominios.GetRecientes());
        if (error is not null) Informar($"No se pudo agregar al menú: {error}", "estado-error");
        ActualizarEstadoDelMenu();
    }

    private void QuitarMenu()
    {
        var error = MenuContextualComandos.TryDesinstalar();
        if (error is not null) Informar($"No se pudo quitar del menú: {error}", "estado-error");
        ActualizarEstadoDelMenu();
    }

    private void SincronizarMenu()
    {
        MenuContextualComandos.Sincronizar(Dominios.GetAll(), Dominios.GetRecientes());
        ActualizarEstadoDelMenu();
    }

    private void ActualizarEstadoDelMenu()
    {
        _menuInstalado = MenuContextualComandos.EstaInstalado();

        if (!_menuInstalado)
        {
            _estadoMenu = "Menú contextual: no instalado.";
            _claseEstadoMenu = "estado-neutro";
            return;
        }

        // Se muestra el presupuesto porque es la única pista de por qué faltan opciones: pasarse de
        // 16 no da error, el explorador descarta el resto en silencio.
        var usados = MenuContextualDevTools.ContarItems();
        var destacado = Dominios.GetUltimoDominio();
        var cual = string.IsNullOrWhiteSpace(destacado) ? string.Empty : $" — destaca {destacado}";

        _estadoMenu = $"Menú contextual: {usados}/{MenuContextualDevTools.LimiteDeItems} ítems{cual}.";
        _claseEstadoMenu = usados >= MenuContextualDevTools.LimiteDeItems ? "estado-error" : "estado-exito";
    }

    private void Informar(string texto, string clase) => Barra.Informar(texto, clase);

    public void Dispose() => Barra.Limpiar();
}
