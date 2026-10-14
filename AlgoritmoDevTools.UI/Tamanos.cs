using System.Globalization;

namespace AlgoritmoDevTools.UI;

/// <summary>
/// Cómo se escribe un tamaño en pantalla.
///
/// Vive en un solo lugar porque lo usan la limpieza de disco y el inicio, y dos formatos distintos
/// para el mismo número —"79,5 GB" acá y "79.5 GB" allá— se leen como dos datos distintos.
/// </summary>
public static class Tamanos
{
    private static readonly string[] Unidades = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// Con una decimal y en la cultura de la máquina. Los bytes no llevan decimales: "1,5 B" no
    /// significa nada.
    /// </summary>
    public static string Legible(long bytes)
    {
        double valor = bytes;
        var unidad = 0;

        while (valor >= 1024 && unidad < Unidades.Length - 1)
        {
            valor /= 1024;
            unidad++;
        }

        return $"{valor.ToString(unidad == 0 ? "0" : "0.#", CultureInfo.CurrentCulture)} {Unidades[unidad]}";
    }
}
