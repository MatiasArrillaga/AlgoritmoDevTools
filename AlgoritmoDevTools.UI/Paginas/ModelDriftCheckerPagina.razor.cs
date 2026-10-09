using AlgoritmoDevTools.Core.Theming;
using AlgoritmoDevTools.Tools.ModelDriftChecker.Services;
using AlgoritmoDevTools.UI;
using AlgoritmoDevTools.UI.Componentes;
using AlgoritmoDevTools.UI.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text;

namespace AlgoritmoDevTools.UI.Paginas;

public partial class ModelDriftCheckerPagina : IDisposable
{
    [Inject]
    private BaselineRepository Baselines { get; set; } = default!;

    [Inject]
    private BarraDeEstado Barra { get; set; } = default!;

    private readonly string _repoPath = SchemaChangeDetector.AlgoritmoCoreRoot;

    private string _baselineTexto = "—";
    private string _headTexto = "—";
    private string _headSha = string.Empty;
    private string _salida = string.Empty;
    private bool _ocupado;
    private bool _confirmandoMigrado;

    protected override async Task OnInitializedAsync()
    {
        RefrescarBaseline();
        await RefrescarHead();

        // Con baseline cargado se verifica solo al entrar: es lo que el usuario viene a mirar.
        if (Baselines.GetBaseline(_repoPath) is not null)
        {
            await Verificar();
            return;
        }

        Informar("Sin baseline. Usá 'Usar HEAD como baseline' para arrancar.", "estado-neutro");
    }

    private void RefrescarBaseline()
    {
        var baseline = Baselines.GetBaseline(_repoPath);
        _baselineTexto = baseline is null
            ? "— (no seteado)"
            : SchemaChangeDetector.DescribeCommit(_repoPath, baseline);
    }

    private async Task RefrescarHead()
    {
        var head = await Task.Run(() => SchemaChangeDetector.GetCurrentHead(_repoPath));
        if (head is null)
        {
            _headSha = string.Empty;
            _headTexto = "— (no se pudo leer)";
            return;
        }

        _headSha = head;
        _headTexto = await Task.Run(() => SchemaChangeDetector.DescribeCommit(_repoPath, head));
    }

    private async Task Verificar()
    {
        await RefrescarHead();

        var baseline = Baselines.GetBaseline(_repoPath);
        if (baseline is null)
        {
            Informar("Sin baseline configurado.", "estado-neutro");
            _salida = string.Empty;
            return;
        }

        _ocupado = true;
        Informar("Analizando diff...", "estado-neutro");
        StateHasChanged();

        var resultado = await Task.Run(() => SchemaChangeDetector.Check(_repoPath, baseline));

        _ocupado = false;
        Mostrar(resultado);
    }

    private void Mostrar(ChangeCheckResult resultado)
    {
        if (!resultado.Success)
        {
            Informar($"Error: {resultado.Error}", "estado-error");
            _salida = resultado.Error ?? string.Empty;
            return;
        }

        if (resultado.RelevantFiles.Count == 0)
        {
            Informar($"Sin cambios relevantes ({resultado.CommitsBetween} commit(s) desde el baseline).", "estado-exito");
            _salida = "No hay archivos de Domain/Infrastructure modificados.";
            return;
        }

        var (clase, texto) = resultado.HighestSeverity switch
        {
            ChangeSeverity.Definite => ("estado-error", "Requiere migración"),
            ChangeSeverity.Likely => ("estado-error", "Probable migración"),
            ChangeSeverity.Possible => ("estado-advertencia", "Posible migración"),
            _ => ("estado-neutro", "Sin cambios")
        };

        Informar($"{texto} — {resultado.RelevantFiles.Count} archivo(s) relevante(s) en {resultado.CommitsBetween} commit(s).", clase);

        var sb = new StringBuilder();
        sb.AppendLine($"Baseline → HEAD: {Corto(resultado.BaselineSha)}..{Corto(resultado.HeadSha)}");
        sb.AppendLine($"Commits entre baseline y HEAD: {resultado.CommitsBetween}");
        sb.AppendLine();

        foreach (var grupo in resultado.RelevantFiles.GroupBy(f => f.Severity).OrderByDescending(g => g.Key))
        {
            sb.AppendLine(grupo.Key switch
            {
                ChangeSeverity.Definite => "DEFINITIVA — casi seguro requieren migración:",
                ChangeSeverity.Likely => "PROBABLE — el nombre sugiere un EntityConfiguration:",
                ChangeSeverity.Possible => "POSIBLE — archivo de Domain (puede ser propiedad, lógica o comentario):",
                _ => string.Empty
            });

            foreach (var archivo in grupo.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  - {archivo.DisplayName}");
            }

            sb.AppendLine();
        }

        _salida = sb.ToString();
    }

    private async Task UsarHeadComoBaseline()
    {
        await RefrescarHead();
        if (string.IsNullOrEmpty(_headSha))
        {
            Informar("No se pudo leer HEAD.", "estado-error");
            return;
        }

        Baselines.SetBaseline(_repoPath, _headSha);
        RefrescarBaseline();
        Informar($"Baseline seteado a {Corto(_headSha)}.", "estado-exito");
        _salida = "Baseline guardado. La próxima vez que traigas commits de master, tocá Verificar.";
    }

    /// <summary>El mismo botón abre y cierra la confirmación.</summary>
    private async Task AlternarConfirmacionMigrado()
    {
        if (_confirmandoMigrado)
        {
            _confirmandoMigrado = false;
            return;
        }

        await RefrescarHead();
        if (string.IsNullOrEmpty(_headSha))
        {
            Informar("No se pudo leer HEAD.", "estado-error");
            return;
        }

        _confirmandoMigrado = true;
    }

    private async Task ConfirmarMigrado()
    {
        _confirmandoMigrado = false;
        Baselines.SetBaseline(_repoPath, _headSha);
        RefrescarBaseline();
        await Verificar();
    }

    private void Informar(string texto, string clase) => Barra.Informar(texto, clase);

    private static string Corto(string sha) => sha.Length >= 7 ? sha[..7] : sha;

    public void Dispose() => Barra.Limpiar();
}
