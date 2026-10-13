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
    /// Junta lo que hay guardado en esta máquina.
    /// </summary>
    /// <param name="conContrasenas">
    /// Si va la contraseña de cada conexión. Van en texto plano —así están guardadas—, así que el
    /// archivo pasa a ser una credencial y quien lo exporta tiene que decidirlo cada vez.
    /// </param>
    public PaqueteDeConfiguracion Armar(bool conContrasenas) => new()
    {
        Exportado = DateTimeOffset.Now,
        Origen = Environment.UserName,
        IncluyeContrasenas = conContrasenas,

        Conexiones = _conexiones.GetAll()
            .Select(conexion => new ConexionExportada
            {
                Servidor = conexion.Server,
                Base = conexion.DataBase,
                Usuario = conexion.User,
                // Con autenticación de Windows no hay nada que mandar: la contraseña guardada está
                // de más y entra cada uno con su propia cuenta.
                Contrasena = conContrasenas && !conexion.UseIntegratedSecurity ? conexion.Password : null,
                SeguridadIntegrada = conexion.UseIntegratedSecurity
            })
            .ToList(),

        Dominios = _dominios.GetAll(),

        PerfilesDeTye = _perfiles.GetProfileNames()
            .Select(nombre => new PerfilExportado
            {
                Nombre = nombre,
                Servicios = (_perfiles.GetProfileServices(nombre) ?? []).ToList()
            })
            .ToList()
    };

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
