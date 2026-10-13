using Microsoft.Win32;

namespace AlgoritmoDevTools.Core.Infrastructure;

/// <summary>
/// Si Windows está dibujando su barra de tareas en claro o en oscuro.
///
/// No es lo mismo que el tema de DevTools, y acá manda Windows: el ícono del widget vive en SU
/// barra, al lado del reloj, así que lo que tiene que contrastar es ese fondo y no el de la
/// aplicación. Un ícono blanco sobre una barra clara no se ve, aunque la ventana esté en modo
/// oscuro.
///
/// Windows guarda dos preferencias por separado —una para el sistema y otra para las aplicaciones—
/// y la barra sigue la del sistema. Si la clave no está, la respuesta es claro: es el valor con el
/// que Windows arranca.
/// </summary>
public static class TemaDeWindows
{
    private const string RUTA = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string VALOR = "SystemUsesLightTheme";

    public static bool BarraDeTareasOscura()
    {
        try
        {
            using var clave = Registry.CurrentUser.OpenSubKey(RUTA);
            return clave?.GetValue(VALOR) is int enClaro && enClaro == 0;
        }
        catch (Exception)
        {
            // Sin acceso al registro no vale la pena romper nada: el claro es lo que trae Windows.
            return false;
        }
    }
}
