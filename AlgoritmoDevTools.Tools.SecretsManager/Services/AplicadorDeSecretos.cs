using AlgoritmoDevTools.Integrations.SoftCerealCore;

namespace AlgoritmoDevTools.Tools.SecretsManager.Services;

/// <summary>
/// Aplica una conexión guardada a los user-secrets y deja todo lo demás consistente.
///
/// Está acá y no en la vista porque hay tres puntos de entrada que tienen que hacer exactamente lo
/// mismo: el botón "Modificar Secreto", el ítem de conexión del menú contextual y el diálogo
/// "Elegir base...". Si cada uno lo resolviera por su lado, el que se olvide de persistir la base o
/// de resincronizar el menú deja el submenú mostrando una base que ya no es la vigente.
/// </summary>
public static class AplicadorDeSecretos
{
    /// <summary>
    /// Reescribe los secretos Development y DAPR apuntando a <paramref name="baseDatos"/>, recuerda
    /// esa base como la última de la conexión y resincroniza el menú contextual si está instalado.
    /// </summary>
    public static void Aplicar(SavedConnection conexion, string baseDatos, SavedConnectionsRepository repositorio)
    {
        var datos = new SQLService.ConnectionData(
            server: conexion.Server,
            user: conexion.User,
            password: conexion.Password,
            dataBase: baseDatos,
            useIntegratedSecurity: conexion.UseIntegratedSecurity);

        SecretService.Shared.SetSecrets(datos);

        // Se persiste después de aplicar: si el set de secretos falla, la conexión no queda
        // recordando una base que en realidad nunca se usó.
        repositorio.UpdateLastDataBase(conexion.Id, baseDatos);
        conexion.LastDataBase = baseDatos;

        MenuContextualSecretos.Sincronizar(repositorio.GetAll());
    }
}
