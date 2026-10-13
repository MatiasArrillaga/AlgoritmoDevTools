using System.Text.Json;
using System.Text.Json.Serialization;
using AlgoritmoDevTools.Integrations.SoftCerealCore;
using AlgoritmoDevTools.Tools.TyeServiceSelector.Services;

namespace AlgoritmoDevTools.UI.Servicios;

/// <summary>
/// Pasa la configuración de una máquina a otra: arma el archivo que se exporta y mete en los
/// repositorios lo que trae el que se importa.
///
/// Existe porque alguien que recién entra al equipo arranca con las herramientas vacías: tiene que
/// cargar a mano los trece dominios, las conexiones a cada servidor y los perfiles de servicios que
/// el resto ya tiene armados. Con un archivo lo resuelve en un paso.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md.
/// </summary>
public sealed class TransferenciaDeConfiguracion
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SavedConnectionsRepository _conexiones;
    private readonly DomainRepository _dominios;
    private readonly ProfileRepository _perfiles;

    public TransferenciaDeConfiguracion(
        SavedConnectionsRepository conexiones,
        DomainRepository dominios,
        ProfileRepository perfiles)
    {
        _conexiones = conexiones;
        _dominios = dominios;
        _perfiles = perfiles;
    }

    /// <summary>
    /// Lee todo lo que hay guardado en esta máquina y lo deja listo para que la pantalla lo muestre
    /// como un árbol. Viene todo marcado menos las contraseñas: lo normal es querer pasar todo, y
    /// desmarcar lo que sobra es menos trabajo que marcar lo que falta.
    ///
    /// La contraseña no se trae acá. Se lee al armar el paquete, sólo si quedó marcada, para que no
    /// ande dando vueltas en memoria por una pantalla que a lo mejor se cancela.
    /// </summary>
    public SeleccionDeExportacion PrepararSeleccion() => new()
    {
        Conexiones = _conexiones.GetAll()
            .Select(conexion => new Seleccionable<ConexionExportada>
            {
                Valor = new ConexionExportada
                {
                    Servidor = conexion.Server,
                    Base = conexion.DataBase,
                    Usuario = conexion.User,
                    SeguridadIntegrada = conexion.UseIntegratedSecurity
                },
                Etiqueta = conexion.ToString()
            })
            .ToList(),

        Dominios = _dominios.GetAll()
            .Select(dominio => new Seleccionable<string> { Valor = dominio, Etiqueta = dominio })
            .ToList(),

        PerfilesDeTye = _perfiles.GetProfileNames()
            .Select(nombre => new Seleccionable<PerfilExportado>
            {
                Valor = new PerfilExportado
                {
                    Nombre = nombre,
                    Servicios = (_perfiles.GetProfileServices(nombre) ?? []).ToList()
                },
                Etiqueta = nombre
            })
            .ToList()
    };

    /// <summary>
    /// Arma el paquete con lo que quedó marcado en el árbol.
    /// </summary>
    public PaqueteDeConfiguracion Armar(SeleccionDeExportacion seleccion)
    {
        // Copias y no las instancias del árbol: abajo se les escribe la contraseña, y escribirla
        // sobre el árbol la dejaría puesta para siempre. Alcanzaría con marcar "incluir las
        // contraseñas", exportar, desmarcarlo y volver a exportar para que el segundo archivo las
        // llevara igual, sin que nada en pantalla lo dijera.
        var conexiones = seleccion.Conexiones
            .Where(elemento => elemento.Marcado)
            .Select(elemento => new ConexionExportada
            {
                Servidor = elemento.Valor.Servidor,
                Base = elemento.Valor.Base,
                Usuario = elemento.Valor.Usuario,
                SeguridadIntegrada = elemento.Valor.SeguridadIntegrada
            })
            .ToList();

        if (seleccion.Contrasenas)
        {
            // La contraseña se busca recién acá, contra lo que hay guardado. Con autenticación de
            // Windows no va ninguna, aunque haya uno guardada: cada uno entra con su propia cuenta.
            var guardadas = _conexiones.GetAll();

            foreach (var conexion in conexiones.Where(c => !c.SeguridadIntegrada))
            {
                conexion.Contrasena = guardadas
                    .FirstOrDefault(guardada =>
                        guardada.Server == conexion.Servidor &&
                        guardada.DataBase == conexion.Base &&
                        guardada.User == conexion.Usuario)
                    ?.Password;
            }
        }

        return new PaqueteDeConfiguracion
        {
            Exportado = DateTimeOffset.Now,
            Origen = Environment.UserName,
            IncluyeContrasenas = seleccion.Contrasenas,
            Conexiones = conexiones,
            Dominios = Marcados(seleccion.Dominios),
            PerfilesDeTye = Marcados(seleccion.PerfilesDeTye)
        };
    }

    private static List<T> Marcados<T>(IEnumerable<Seleccionable<T>> elementos) =>
        elementos.Where(elemento => elemento.Marcado).Select(elemento => elemento.Valor).ToList();

    public void Escribir(string ruta, PaqueteDeConfiguracion paquete) =>
        File.WriteAllText(ruta, JsonSerializer.Serialize(paquete, Opciones));

    /// <summary>
    /// Lee un archivo exportado. Tira si no se puede leer: a diferencia de las preferencias, acá el
    /// usuario eligió el archivo a mano y tiene que enterarse de que ése no era.
    /// </summary>
    public PaqueteDeConfiguracion Leer(string ruta)
    {
        var paquete = JsonSerializer.Deserialize<PaqueteDeConfiguracion>(File.ReadAllText(ruta), Opciones)
            ?? throw new InvalidDataException("El archivo está vacío.");

        if (paquete.Formato > PaqueteDeConfiguracion.FormatoActual)
        {
            throw new InvalidDataException(
                $"El archivo lo escribió una versión más nueva de DevTools (formato {paquete.Formato}). Actualizá la tuya antes de importarlo.");
        }

        return paquete;
    }

    /// <summary>
    /// Agrega lo que falta y no toca nada de lo que ya está.
    ///
    /// Es a propósito: el que importa puede tener conexiones con otra contraseña, dominios propios o
    /// un perfil con el mismo nombre y otros servicios adentro. Pisarlos sería romperle lo que ya
    /// tenía andando por traer algo de otra máquina. Lo repetido se cuenta y se informa.
    /// </summary>
    public ResultadoDeImportacion Importar(PaqueteDeConfiguracion paquete)
    {
        int conexionesNuevas = 0, conexionesRepetidas = 0;
        foreach (var conexion in paquete.Conexiones)
        {
            if (string.IsNullOrWhiteSpace(conexion.Servidor)) continue;

            if (_conexiones.Exists(conexion.Servidor, conexion.Base, conexion.Usuario))
            {
                conexionesRepetidas++;
                continue;
            }

            _conexiones.Add(new SavedConnection
            {
                Server = conexion.Servidor,
                DataBase = conexion.Base,
                User = conexion.Usuario,
                Password = conexion.Contrasena ?? string.Empty,
                UseIntegratedSecurity = conexion.SeguridadIntegrada
            });
            conexionesNuevas++;
        }

        // Se compara contra lo que ya hay sin distinguir mayúsculas, y el conjunto se va llenando:
        // así un archivo que repite un dominio adentro tampoco lo carga dos veces.
        var dominiosPropios = _dominios.GetAll().ToHashSet(StringComparer.OrdinalIgnoreCase);
        int dominiosNuevos = 0, dominiosRepetidos = 0;
        foreach (var dominio in paquete.Dominios)
        {
            if (string.IsNullOrWhiteSpace(dominio)) continue;

            if (!dominiosPropios.Add(dominio.Trim()))
            {
                dominiosRepetidos++;
                continue;
            }

            _dominios.Add(dominio.Trim());
            dominiosNuevos++;
        }

        var perfilesPropios = _perfiles.GetProfileNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        int perfilesNuevos = 0, perfilesRepetidos = 0;
        foreach (var perfil in paquete.PerfilesDeTye)
        {
            if (string.IsNullOrWhiteSpace(perfil.Nombre)) continue;

            if (!perfilesPropios.Add(perfil.Nombre))
            {
                perfilesRepetidos++;
                continue;
            }

            _perfiles.SaveProfile(perfil.Nombre, perfil.Servicios);
            perfilesNuevos++;
        }

        return new ResultadoDeImportacion(
            conexionesNuevas, conexionesRepetidas,
            dominiosNuevos, dominiosRepetidos,
            perfilesNuevos, perfilesRepetidos);
    }
}
