namespace AlgoritmoDevTools.Integrations.VisualStudio;

/// <summary>
/// Con qué argumentos se relanza DevTools para cada acción del Commands Maker.
///
/// <b>Están duplicados a propósito y hay que mantenerlos a mano.</b> El original vive en
/// <c>MenuContextualComandos</c> (AlgoritmoDevTools.Tools.CommandsMaker), que es net10.0-windows;
/// este proyecto es net8.0-windows porque se lo impone el SDK de extensibilidad, así que no puede
/// referenciarlo. No hay forma de que el compilador avise si uno cambia y el otro no: el síntoma
/// sería que el ítem del menú no hace nada, porque el Shell cae en el <c>switch</c> sin coincidir y
/// termina abriendo la ventana principal.
///
/// Si alguna vez se tocan, hay que tocar los dos.
/// </summary>
internal static class VerbosDelShell
{
    public const string Add = "--comando-add";
    public const string Remove = "--comando-remove";
    public const string Update = "--comando-update";
    public const string Elegir = "--comando-elegir";
}
