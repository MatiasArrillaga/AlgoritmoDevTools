using AlgoritmoDevTools.UI.Servicios;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Puente entre el arrastre de Windows y los componentes de la interfaz HTML.
///
/// Lo alimenta <see cref="MainFormBlazor"/>, que es quien recibe los eventos de arrastre con las
/// rutas reales. Los componentes sólo escuchan.
/// </summary>
public sealed class ArchivosArrastrados : IArchivosArrastrados
{
    public event EventHandler<IReadOnlyList<string>>? Soltados;

    public event EventHandler<bool>? ArrastreEnCurso;

    public void NotificarSoltados(IReadOnlyList<string> rutas) => Soltados?.Invoke(this, rutas);

    public void NotificarArrastre(bool enCurso) => ArrastreEnCurso?.Invoke(this, enCurso);
}
