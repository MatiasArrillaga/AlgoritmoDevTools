namespace AlgoritmoDevTools.Tools.BuscaLoco.Model;

/// <summary>
/// Qué se busca y con qué opciones.
///
/// Los dos campos de texto <b>conviven</b>: no hay un interruptor que elija entre buscar por nombre
/// o por contenido. Vacío significa «no filtres por eso», y llenar los dos es la búsqueda que más
/// se usa — ese texto, pero sólo en los <c>.razor</c>—. Un interruptor obligaría a elegir uno y
/// perdería justamente esa combinación, que es lo que hace que valga la pena tener el buscador acá
/// y no usar el de Windows.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.BuscaLoco/README.referencia-tecnica-funcional-busca-loco.md
/// </summary>
/// <param name="Raiz">Desde dónde se busca. Arranca en la carpeta de repositorios y se puede cambiar.</param>
/// <param name="Texto">Qué buscar adentro de los archivos. Vacío: no se mira el contenido.</param>
/// <param name="Nombre">
/// Qué nombres interesan. Admite varios separados por coma o punto y coma (<c>*.cs, *.razor</c>) y
/// también parte del nombre (<c>*Factory.cs</c>). Vacío: todos.
/// </param>
/// <param name="TextoEsExpresion">El texto es una expresión regular y no algo literal.</param>
/// <param name="NombreEsExpresion">
/// El nombre es una expresión regular. Ahí <b>no se separa por coma</b>: en una expresión la coma es
/// sintaxis —<c>\w{2,3}</c>— y partir por ella rompería el patrón en silencio.
/// </param>
/// <param name="Subcarpetas">Si se baja por todo el árbol o se mira sólo la carpeta elegida.</param>
/// <param name="DistinguirMayusculas">Vale para el texto y para el nombre: una opción por concepto.</param>
/// <param name="PalabraEntera">
/// Se ignora cuando <paramref name="TextoEsExpresion"/> está encendido. Se implementa envolviendo el
/// patrón en <c>\b…\b</c>, y hacer eso sobre una expresión arbitraria le cambia el significado sin
/// avisar: con expresión, el <c>\b</c> lo pone quien escribe.
/// </param>
/// <param name="SaltearRuido">
/// Saltear <c>bin</c>, <c>obj</c>, <c>.git</c> y <c>node_modules</c>. Es por velocidad y no por
/// seguridad —en AlgoritmoCore son 446 carpetas de compilados—, y por eso se puede apagar.
/// </param>
public sealed record CriterioDeBusqueda(
    string Raiz,
    string Texto,
    string Nombre,
    bool TextoEsExpresion,
    bool NombreEsExpresion,
    bool Subcarpetas,
    bool DistinguirMayusculas,
    bool PalabraEntera,
    bool SaltearRuido)
{
    /// <summary>
    /// Si hay algo para buscar. Con los dos campos vacíos la búsqueda recorrería todo para no
    /// filtrar nada: sobre AlgoritmoCore son minutos de disco para devolver todos los archivos, que
    /// no es una respuesta que nadie haya pedido. El que llega ahí llegó por error.
    ///
    /// Se compara con <c>Trim</c> porque un campo con espacios está tan vacío como uno vacío.
    /// </summary>
    public bool TieneQueBuscar =>
        !string.IsNullOrWhiteSpace(Texto) || !string.IsNullOrWhiteSpace(Nombre);

    /// <summary>Si hay que abrir los archivos, o alcanza con mirar sus nombres.</summary>
    public bool MiraElContenido => !string.IsNullOrWhiteSpace(Texto);
}
