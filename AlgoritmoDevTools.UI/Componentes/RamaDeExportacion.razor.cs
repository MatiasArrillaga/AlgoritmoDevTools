using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;

namespace AlgoritmoDevTools.UI.Componentes;

/// <summary>En qué estado quedó una casilla que manda sobre otras.</summary>
public enum EstadoDeRama
{
    Nada,
    Algo,
    Todo
}

public partial class RamaDeExportacion<T>
{
    /// <summary>Nombre de la herramienta, el mismo con el que figura en el menú.</summary>
    [Parameter, EditorRequired]
    public required string Herramienta { get; set; }

    /// <summary>Cómo se llama el conjunto: "Todas las conexiones", "Todos los dominios".</summary>
    [Parameter, EditorRequired]
    public required string Grupo { get; set; }

    [Parameter, EditorRequired]
    public required List<Seleccionable<T>> Elementos { get; set; }

    /// <summary>Qué decir cuando la herramienta no tiene nada guardado todavía.</summary>
    [Parameter]
    public string Vacio { get; set; } = "No hay nada guardado.";

    /// <summary>
    /// Opciones propias de la herramienta, que no son elementos de la lista. Las usa Secrets Manager
    /// para el tilde de las contraseñas.
    /// </summary>
    [Parameter]
    public RenderFragment? Opciones { get; set; }

    /// <summary>Avisa que algo cambió, para que la pantalla recalcule si queda algo para exportar.</summary>
    [Parameter]
    public EventCallback Cambio { get; set; }

    private EstadoDeRama EstadoDelGrupo => Resolver(Elementos.Count(e => e.Marcado), Elementos.Count);

    /// <summary>
    /// El estado de la herramienta es el del grupo. Las opciones no cuentan: marcar "incluir las
    /// contraseñas" no es exportar nada por sí solo, y dejaría la casilla de arriba diciendo que hay
    /// algo elegido cuando no se eligió ninguna conexión.
    /// </summary>
    private EstadoDeRama Estado => EstadoDelGrupo;

    private static EstadoDeRama Resolver(int marcados, int total) => marcados switch
    {
        0 => EstadoDeRama.Nada,
        var cantidad when cantidad == total => EstadoDeRama.Todo,
        _ => EstadoDeRama.Algo
    };

    private Task MarcarTodo(bool marcado) => MarcarElementos(marcado);

    private async Task MarcarElementos(bool marcado)
    {
        foreach (var elemento in Elementos)
        {
            elemento.Marcado = marcado;
        }

        await Cambio.InvokeAsync();
    }

    private async Task Marcar(Seleccionable<T> elemento, bool marcado)
    {
        elemento.Marcado = marcado;
        await Cambio.InvokeAsync();
    }
}
