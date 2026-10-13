using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace AlgoritmoDevTools.Shell;

/// <summary>
/// Entrega el contenido de la interfaz —el HTML, el CSS, los scripts— desde adentro del ejecutable.
///
/// El visor los pide por su ruta, igual que un navegador: "/index.html",
/// "/_framework/blazor.webview.js". De fábrica los leería de una carpeta wwwroot al lado del .exe;
/// acá están incrustados en el ensamblado con esa misma ruta y el prefijo "web/", así que alcanza
/// con traducir una en otra.
///
/// Es lo que deja al ejecutable andar solo, sin archivos sueltos al lado. Quién los incrusta, y por
/// qué no alcanza con publicarlo como archivo único, está en el destino EmbeberElContenidoWeb del
/// csproj.
/// </summary>
internal sealed class ContenidoWebEmbebido : IFileProvider
{
    private const string Prefijo = "web/";

    private static readonly Assembly Ensamblado = typeof(ContenidoWebEmbebido).Assembly;

    /// <summary>Ruta web a nombre del recurso. Se arma una vez: la lista no cambia en ejecución.</summary>
    private static readonly Dictionary<string, string> PorRuta = Ensamblado
        .GetManifestResourceNames()
        .Where(nombre => nombre.StartsWith(Prefijo, StringComparison.Ordinal))
        .ToDictionary(nombre => nombre[Prefijo.Length..], nombre => nombre, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Los recursos nacen con el ejecutable, así que su fecha es la de él. Tiene que ser siempre la
    /// misma: es con lo que el visor decide si lo que tiene guardado sigue sirviendo.
    /// </summary>
    private static readonly DateTimeOffset FechaDelEjecutable =
        Environment.ProcessPath is { } ejecutable && File.Exists(ejecutable)
            ? File.GetLastWriteTimeUtc(ejecutable)
            : DateTimeOffset.UnixEpoch;

    public IFileInfo GetFileInfo(string subpath)
    {
        var ruta = (subpath ?? string.Empty).Replace('\\', '/').TrimStart('/');

        return PorRuta.TryGetValue(ruta, out var recurso)
            ? new ArchivoEmbebido(recurso, ruta)
            : new NotFoundFileInfo(ruta);
    }

    /// <summary>No se listan carpetas: el visor pide archivo por archivo.</summary>
    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

    /// <summary>Nada cambia mientras el proceso vive: lo que hay es lo que se compiló.</summary>
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private sealed class ArchivoEmbebido : IFileInfo
    {
        private readonly string _recurso;

        public ArchivoEmbebido(string recurso, string ruta)
        {
            _recurso = recurso;
            Name = ruta[(ruta.LastIndexOf('/') + 1)..];

            using var contenido = Ensamblado.GetManifestResourceStream(recurso)!;
            Length = contenido.Length;
        }

        public bool Exists => true;

        public bool IsDirectory => false;

        public long Length { get; }

        public string Name { get; }

        /// <summary>No hay archivo en el disco que mostrar.</summary>
        public string? PhysicalPath => null;

        public DateTimeOffset LastModified => FechaDelEjecutable;

        public Stream CreateReadStream() => Ensamblado.GetManifestResourceStream(_recurso)!;
    }
}
