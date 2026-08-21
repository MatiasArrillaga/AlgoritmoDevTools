using AlgoritmoDevTools.Tools.MarkdownConverter.Services;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Convierte los archivos que llegan por linea de comandos (menu contextual del explorador) sin
/// abrir la ventana del Shell.
///
/// El exito no se avisa: el .md aparece al lado del original y eso ya se ve. Solo se muestra un
/// cartel cuando algo fallo, que es lo que de otro modo pasaria desapercibido.
/// </summary>
internal static class ConversionSinVentana
{
    private const string TITULO = "Convertir a Markdown";

    public static void Ejecutar(IReadOnlyList<string> archivos)
    {
        var converter = new DocumentConverter();

        if (!converter.PandocDisponible)
        {
            MessageBox.Show(
                "No se encontró pandoc, que es lo que hace la conversión.\n\n" +
                "Instalalo con:\n    winget install --id JohnMacFarlane.Pandoc -e",
                TITULO, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var problemas = new List<string>();
        var convertidos = 0;

        foreach (var archivo in archivos)
        {
            var resultado = converter.Convert(archivo, ConversionOptions.Default, CancellationToken.None);

            if (resultado.Error is not null)
            {
                problemas.Add($"{resultado.SourceName}: {resultado.Error}");
                continue;
            }

            convertidos++;
        }

        if (problemas.Count == 0) return;

        var encabezado = convertidos > 0
            ? $"Se convirtieron {convertidos} de {archivos.Count} archivo(s). Con estos no se pudo:"
            : "No se pudo convertir:";

        MessageBox.Show(
            encabezado + "\n\n" + string.Join("\n", problemas),
            TITULO, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
