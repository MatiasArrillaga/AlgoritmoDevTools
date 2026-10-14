using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services;

/// <summary>
/// Un ítem tal como se va a ejecutar: con las rutas que siguen existiendo y lo que pesan hoy.
/// </summary>
public sealed record ItemDelPlan(
    ItemLimpieza Item,
    IReadOnlyList<string> Rutas,
    long Bytes,
    IReadOnlyList<ProcesoBloqueante> Bloqueantes)
{
    public bool RequiereAdmin => Item.RequiereAdmin;
}

/// <summary>
/// Lo que pasaría si se ejecutara. Nada de esto borró nada.
/// </summary>
/// <param name="Items">Lo que entra, ya filtrado.</param>
/// <param name="Descartados">Lo que el usuario había elegido y no entra, con el motivo.</param>
public sealed record PlanDeLimpieza(
    IReadOnlyList<ItemDelPlan> Items,
    IReadOnlyList<string> Descartados)
{
    public long Bytes => Items.Sum(i => i.Bytes);
    public int Rutas => Items.Sum(i => i.Rutas.Count);
    public bool NecesitaElevar => Items.Any(i => i.RequiereAdmin);
    public bool EstaVacio => Items.Count == 0;
}

/// <summary>
/// La previsualización de una limpieza.
///
/// <b>Esta clase no sabe borrar.</b> No tiene ningún método que escriba en el disco, y eso no es
/// una omisión: el dry-run es lo que el usuario mira antes de decidir, así que la garantía de que
/// no toca nada tiene que ser estructural y no una promesa. El borrado llega después, en su propio
/// servicio.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class LimpiezaService
{
    /// <summary>
    /// Arma el plan con lo que el usuario tildó.
    ///
    /// Vuelve a filtrar todo lo que ya se filtró antes —lo que no se borra, lo trabado, lo
    /// protegido—. Es a propósito: es el último punto antes de que alguien apriete el botón, y si
    /// algo de eso llegó hasta acá es que hay un error en otro lado, no una razón para dejarlo
    /// pasar.
    /// </summary>
    public Task<PlanDeLimpieza> PrevisualizarAsync(
        IEnumerable<ItemOfrecido> elegidos,
        CancellationToken cancelacion = default) =>
        Task.Run(() =>
        {
            var items = new List<ItemDelPlan>();
            var descartados = new List<string>();

            foreach (var ofrecido in elegidos)
            {
                cancelacion.ThrowIfCancellationRequested();

                var item = ofrecido.Item;
                if (!ofrecido.Tildado) continue;

                if (!item.SeBorra)
                {
                    descartados.Add($"{item.Ubicacion}: no se borra, se resuelve desde su propia herramienta.");
                    continue;
                }

                if (item.MotivoParaNoTocarlo is not null)
                {
                    descartados.Add($"{item.Ubicacion}: {item.MotivoParaNoTocarlo}");
                    continue;
                }

                var rutas = RutasQueSiguenEnPie(item, out var protegidas);

                if (rutas.Count == 0)
                {
                    // Cuál de los dos motivos es cambia qué entiende quien lo lee: "no hay nada"
                    // suena a que la carpeta desapareció, y acá lo que pasó es que no se mira.
                    descartados.Add(protegidas > 0
                        ? $"{item.Ubicacion}: está entre las rutas que no se tocan."
                        : $"{item.Ubicacion}: ya no hay nada ahí.");
                    continue;
                }

                // Una ruta protegida que haya llegado hasta acá es un error de otro lado. Se deja
                // afuera y se dice, en lugar de borrarla o de callarlo.
                if (protegidas > 0)
                    descartados.Add($"{item.Ubicacion}: {protegidas} de sus rutas no se tocan y quedan afuera; el resto sí entra.");

                items.Add(new ItemDelPlan(item, rutas, MedirLoQueEntra(rutas, cancelacion), item.Bloqueantes));
            }

            return new PlanDeLimpieza(items, descartados);
        }, cancelacion);

    /// <summary>
    /// Las rutas que todavía existen y que se pueden tocar. Entre el análisis y la confirmación
    /// pueden haber pasado minutos: listar algo que ya no está haría que el reporte final mostrara
    /// errores que no son errores.
    /// </summary>
    private static List<string> RutasQueSiguenEnPie(ItemLimpieza item, out int protegidas)
    {
        protegidas = 0;
        var vivas = new List<string>();

        foreach (var ruta in item.Rutas)
        {
            if (RutasProtegidas.EstaProtegida(ruta))
            {
                protegidas++;
                continue;
            }

            if (Directory.Exists(ruta) || File.Exists(ruta)) vivas.Add(ruta);
        }

        return vivas;
    }

    /// <summary>
    /// Mide ahora y no reusa lo del análisis: es el número que el usuario va a ver antes de
    /// decidir, y entre una cosa y la otra una compilación pudo haber llenado las carpetas de
    /// nuevo.
    /// </summary>
    private static long MedirLoQueEntra(IEnumerable<string> rutas, CancellationToken cancelacion)
    {
        long bytes = 0;

        foreach (var ruta in rutas)
        {
            cancelacion.ThrowIfCancellationRequested();

            if (Directory.Exists(ruta))
            {
                bytes += EnumeracionSegura.Medir(ruta, cancelacion).Bytes;
                continue;
            }

            try
            {
                bytes += new FileInfo(ruta).Length;
            }
            catch
            {
                // Un archivo que no se puede consultar no suma, pero tampoco impide seguir.
            }
        }

        return bytes;
    }
}
