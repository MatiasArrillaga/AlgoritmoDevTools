using System.Globalization;
using System.Text;
using AlgoritmoDevTools.Tools.BuscaLoco.Model;

namespace AlgoritmoDevTools.Tools.BuscaLoco.Services;

/// <summary>
/// Pasa lo encontrado a un archivo.
///
/// Exporta <b>exactamente lo que se ve</b>: si la pantalla muestra una lista filtrada, eso es lo que
/// sale. Un botón que exporta algo distinto de lo que hay en pantalla es una sorpresa.
///
/// Dos formatos, cada uno con su destino: el CSV para mirar en Excel, y el texto plano con la forma
/// <c>ruta:línea:texto</c>, que es la que se pega en un ticket y la que entienden las herramientas
/// de la terminal.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
public static class ExportadorDeResultados
{
    /// <summary>
    /// El separador es punto y coma y no coma.
    ///
    /// Excel usa el separador de lista de la configuración regional, y acá es el punto y coma: con
    /// coma, cada renglón entra entero en la primera celda. Además las rutas tienen comas más
    /// seguido de lo que uno espera, así se escapan menos campos.
    /// </summary>
    private const char Separador = ';';

    /// <summary>El filtro para el diálogo de guardar de Windows.</summary>
    public const string FiltroDeArchivos = "Planilla (CSV)|*.csv|Texto plano|*.txt";

    /// <summary>Arma el contenido según la extensión que el usuario haya elegido al guardar.</summary>
    public static string Armar(IReadOnlyList<ArchivoConHallazgos> archivos, string rutaDestino) =>
        Path.GetExtension(rutaDestino).Equals(".txt", StringComparison.OrdinalIgnoreCase)
            ? ATexto(archivos)
            : ACsv(archivos);

    /// <summary>
    /// Una fila por coincidencia, con el archivo y la carpeta en columnas separadas para poder
    /// ordenar y filtrar en la planilla.
    /// </summary>
    public static string ACsv(IReadOnlyList<ArchivoConHallazgos> archivos)
    {
        var texto = new StringBuilder();
        texto.Append("Archivo").Append(Separador)
             .Append("Carpeta").Append(Separador)
             .Append("Linea").Append(Separador)
             .Append("Texto").AppendLine();

        foreach (var archivo in archivos)
        {
            // Un archivo que entró sólo por su nombre no tiene coincidencias, y tiene que figurar
            // igual: es un resultado de la búsqueda.
            if (archivo.Hallazgos.Count == 0)
            {
                texto.Append(Escapar(archivo.Nombre)).Append(Separador)
                     .Append(Escapar(archivo.Carpeta)).Append(Separador)
                     .Append(Separador)
                     .AppendLine();

                continue;
            }

            foreach (var hallazgo in archivo.Hallazgos)
            {
                texto.Append(Escapar(archivo.Nombre)).Append(Separador)
                     .Append(Escapar(archivo.Carpeta)).Append(Separador)
                     .Append(hallazgo.Linea.ToString(CultureInfo.InvariantCulture)).Append(Separador)
                     .Append(Escapar(hallazgo.Texto.Trim()))
                     .AppendLine();
            }
        }

        return texto.ToString();
    }

    /// <summary>La forma <c>ruta:línea:texto</c>, una por coincidencia.</summary>
    public static string ATexto(IReadOnlyList<ArchivoConHallazgos> archivos)
    {
        var texto = new StringBuilder();

        foreach (var archivo in archivos)
        {
            if (archivo.Hallazgos.Count == 0)
            {
                texto.AppendLine(archivo.Ruta);
                continue;
            }

            foreach (var hallazgo in archivo.Hallazgos)
            {
                texto.AppendLine(hallazgo.ComoLinea());
            }
        }

        return texto.ToString();
    }

    /// <summary>
    /// Encierra el campo entre comillas cuando trae algo que rompería la fila, y duplica las
    /// comillas de adentro, que es como se escapan en un CSV.
    /// </summary>
    private static string Escapar(string campo)
    {
        if (!campo.Contains(Separador) && !campo.Contains('"') && !campo.Contains('\n') && !campo.Contains('\r'))
        {
            return campo;
        }

        return '"' + campo.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }
}
