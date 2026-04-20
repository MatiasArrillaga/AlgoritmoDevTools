using AlgoritmoDevTools.Tools.ModelDriftChecker.Services;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AlgoritmoDevTools.Tools.ModelDriftChecker.Views;

public partial class ModelDriftCheckerView : UserControl
{
    private readonly BaselineRepository _baselines;
    private readonly string _repoPath = SchemaChangeDetector.AlgoritmoCoreRoot;

    public ModelDriftCheckerView(BaselineRepository baselines)
    {
        _baselines = baselines;
        InitializeComponent();
        SetupTooltips();
    }

    private void SetupTooltips()
    {
        var tips = new ToolTip { AutoPopDelay = 12_000, InitialDelay = 400, ReshowDelay = 200 };
        tips.SetToolTip(VerificarBtn, "Compara el baseline con HEAD y lista archivos de Domain/Infrastructure que podrían requerir migración.");
        tips.SetToolTip(SetBaselineBtn, "Marca HEAD como el punto de referencia. Usalo la primera vez o cuando querés reinicializar sin haber migrado.");
        tips.SetToolTip(MarcarMigradoBtn, "Después de correr las migraciones en tu BD local, movés el baseline a HEAD para 'cerrar' los cambios.");
        tips.SetToolTip(RepoLbl, "Ruta del repo de AlgoritmoCore sobre el que se analiza el diff.");
        tips.SetToolTip(BaselineLbl, "Último commit en el que confirmaste que tu BD estaba migrada.");
        tips.SetToolTip(HeadLbl, "Commit actual del repo (lo último que trajiste del pull).");
    }

    private async void ModelDriftCheckerView_Load(object? sender, EventArgs e)
    {
        RepoLbl.Text = $"Repo: {_repoPath}";
        RefreshBaselineLabel();
        await UpdateHeadLabel();

        // Si hay baseline, verificar automáticamente al abrir
        if (_baselines.GetBaseline(_repoPath) is not null)
            await RunCheck();
        else
            SetStatus("Sin baseline. Usá 'Usar HEAD como baseline' para arrancar.", Color.Gray);
    }

    private void RefreshBaselineLabel()
    {
        var baseline = _baselines.GetBaseline(_repoPath);
        if (baseline is null)
        {
            BaselineLbl.Text = "Baseline: — (no seteado)";
        }
        else
        {
            BaselineLbl.Text = $"Baseline: {SchemaChangeDetector.DescribeCommit(_repoPath, baseline)}";
        }
    }

    private async Task UpdateHeadLabel()
    {
        var head = await Task.Run(() => SchemaChangeDetector.GetCurrentHead(_repoPath));
        if (head is null)
        {
            HeadLbl.Text = "HEAD: — (no se pudo leer)";
            return;
        }
        var desc = await Task.Run(() => SchemaChangeDetector.DescribeCommit(_repoPath, head));
        HeadLbl.Text = $"HEAD: {desc}";
    }

    private async void VerificarBtn_Click(object? sender, EventArgs e)
    {
        await UpdateHeadLabel();
        await RunCheck();
    }

    private async Task RunCheck()
    {
        var baseline = _baselines.GetBaseline(_repoPath);
        if (baseline is null)
        {
            SetStatus("Sin baseline configurado.", Color.Gray);
            OutputTxt.Clear();
            return;
        }

        SetBusy(true);
        SetStatus("Analizando diff...", Color.Gray);

        var result = await Task.Run(() => SchemaChangeDetector.Check(_repoPath, baseline));
        RenderResult(result);
        SetBusy(false);
    }

    private void RenderResult(ChangeCheckResult result)
    {
        if (!result.Success)
        {
            SetStatus($"Error: {result.Error}", Color.Firebrick);
            OutputTxt.Text = result.Error ?? string.Empty;
            return;
        }

        if (result.RelevantFiles.Count == 0)
        {
            SetStatus($"✅ Sin cambios relevantes ({result.CommitsBetween} commit(s) desde el baseline).", Color.ForestGreen);
            OutputTxt.Text = "No hay archivos de Domain/Infrastructure modificados.";
            return;
        }

        var (color, icon, text) = result.HighestSeverity switch
        {
            ChangeSeverity.Definite => (Color.Firebrick, "🔴", "Requiere migración"),
            ChangeSeverity.Likely => (Color.Firebrick, "🔴", "Probable migración"),
            ChangeSeverity.Possible => (Color.DarkOrange, "⚠️", "Posible migración"),
            _ => (Color.Gray, "—", "Sin cambios")
        };

        SetStatus($"{icon} {text} — {result.RelevantFiles.Count} archivo(s) relevante(s) en {result.CommitsBetween} commit(s).", color);

        var sb = new StringBuilder();
        sb.AppendLine($"Baseline → HEAD: {Short(result.BaselineSha)}..{Short(result.HeadSha)}");
        sb.AppendLine($"Commits entre baseline y HEAD: {result.CommitsBetween}");
        sb.AppendLine();

        foreach (var group in result.RelevantFiles.GroupBy(f => f.Severity).OrderByDescending(g => g.Key))
        {
            var header = group.Key switch
            {
                ChangeSeverity.Definite => "🔴 DEFINITIVA — casi seguro requieren migración:",
                ChangeSeverity.Likely => "🔴 PROBABLE — nombre sugiere EntityConfiguration:",
                ChangeSeverity.Possible => "⚠️ POSIBLE — archivo de Domain (puede ser propiedad, lógica o comentario):",
                _ => ""
            };
            sb.AppendLine(header);
            foreach (var f in group.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  • {f.DisplayName}");
            }
            sb.AppendLine();
        }

        OutputTxt.Text = sb.ToString();
    }

    private async void SetBaselineBtn_Click(object? sender, EventArgs e)
    {
        var head = await Task.Run(() => SchemaChangeDetector.GetCurrentHead(_repoPath));
        if (head is null)
        {
            MessageBox.Show("No se pudo leer HEAD.", "Schema Change Detector",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _baselines.SetBaseline(_repoPath, head);
        RefreshBaselineLabel();
        SetStatus($"Baseline seteado a {Short(head)}.", Color.ForestGreen);
        OutputTxt.Text = "Baseline guardado. Próxima vez que traigas commits de master, tocá 'Verificar'.";
    }

    private async void MarcarMigradoBtn_Click(object? sender, EventArgs e)
    {
        var head = await Task.Run(() => SchemaChangeDetector.GetCurrentHead(_repoPath));
        if (head is null)
        {
            MessageBox.Show("No se pudo leer HEAD.", "Schema Change Detector",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var confirm = MessageBox.Show(
            $"Mover el baseline a {Short(head)}. ¿Ya corriste las migraciones en tu BD local?",
            "Schema Change Detector", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _baselines.SetBaseline(_repoPath, head);
        RefreshBaselineLabel();
        await RunCheck();
    }

    private static string Short(string sha) => sha.Length >= 7 ? sha[..7] : sha;

    private void SetStatus(string text, Color color)
    {
        StatusLbl.Text = text;
        StatusLbl.ForeColor = color;
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        VerificarBtn.Enabled = !busy;
        SetBaselineBtn.Enabled = !busy;
        MarcarMigradoBtn.Enabled = !busy;
    }
}
