using AlgoritmoDevTools.Core.Infrastructure;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Services.Detectores;

/// <summary>
/// El archivo de hibernación. En la máquina del incidente, 15,89 GB.
///
/// <b>Se le pregunta a powercfg, no se asume.</b> Si la hibernación ya está deshabilitada el ítem
/// no aparece; si está habilitada aparece <b>destildado</b>, porque en el equipo hay gente que
/// hiberna y apagarla le cambia cómo usa la máquina.
///
/// Usa <c>RunProcess</c> y no <c>RunPowerShell</c>: powercfg devuelve códigos de salida distintos
/// de cero que son respuestas, no errores, y los otros dos envoltorios los convierten en excepción.
///
/// Referencia funcional y técnica:
/// AlgoritmoDevTools.Tools.DiskCleaner/README.referencia-tecnica-funcional-limpieza-disco.md
/// </summary>
public sealed class DetectorHibernacion : IDetector
{
    public string Nombre => "Hibernación";

    public Task<IReadOnlyList<ItemLimpieza>> DetectarAsync(CancellationToken cancelacion) =>
        Task.Run<IReadOnlyList<ItemLimpieza>>(() =>
        {
            if (!EstaHabilitada(cancelacion)) return [];

            var hiberfil = Path.Combine(
                Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? @"C:\",
                "hiberfil.sys");

            // El archivo es del sistema y no se puede leer sin elevar, pero su tamaño sí se puede
            // consultar. Si ni eso se puede, se informa sin cifra en lugar de no informar.
            long bytes = 0;
            try
            {
                var info = new FileInfo(hiberfil);
                if (info.Exists) bytes = info.Length;
            }
            catch
            {
                // Queda en cero: el ítem igual vale, porque lo que se ofrece es la opción.
            }

            return
            [
                new ItemLimpieza
                {
                    Id = "hibernacion",
                    Ubicacion = "Archivo de hibernación",
                    Bytes = bytes,
                    Grupo = GrupoLimpieza.ConCriterio,
                    Riesgo = NivelRiesgo.Medio,
                    Descripcion = "El espacio reservado para guardar la memoria cuando la máquina hiberna. Es también lo que usa el inicio rápido de Windows.",
                    Consecuencia = "Se deja de poder hibernar y el arranque pasa a ser el completo. Si no hibernás, no vas a notar nada salvo que tarda un poco más en encender.",
                    ComoSeRevierte = "Volviendo a habilitarla con powercfg /h on.",
                    RequiereAdmin = true,
                    Rutas = [hiberfil]
                }
            ];
        }, cancelacion);

    private static bool EstaHabilitada(CancellationToken cancelacion)
    {
        try
        {
            var resultado = ProcessRunner.RunProcess("powercfg.exe", ["/a"], cancellationToken: cancelacion);
            var salida = resultado.StdOut;

            // powercfg lista primero los estados disponibles y después los que no lo están, con el
            // motivo. Preguntar por "está en la lista de disponibles" es lo único que no depende
            // del idioma de la máquina... salvo por el nombre del estado, que sí se traduce.
            return salida.Contains("Hibernat", StringComparison.OrdinalIgnoreCase)
                && !NoEstaDisponible(salida);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Sin respuesta de powercfg no se ofrece: es preferible no mostrar el ítem antes que
            // ofrecer apagar algo que quizá ya está apagado.
            return false;
        }
    }

    private static bool NoEstaDisponible(string salida)
    {
        var desde = salida.IndexOf("no están disponibles", StringComparison.OrdinalIgnoreCase);
        if (desde < 0) desde = salida.IndexOf("are not available", StringComparison.OrdinalIgnoreCase);
        if (desde < 0) return false;

        return salida[desde..].Contains("Hibernat", StringComparison.OrdinalIgnoreCase);
    }
}
