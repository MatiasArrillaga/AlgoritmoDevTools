using AlgoritmoDevTools.Core.Abstractions;
using AlgoritmoDevTools.Tools.DiskCleaner.Model;
using AlgoritmoDevTools.Tools.DiskCleaner.Services;
using Microsoft.Data.Sqlite;

namespace AlgoritmoDevTools.Tools.DiskCleaner.Test;

/// <summary>
/// Lo que el usuario eligió en esta máquina.
/// </summary>
public class PerfilRepositoryTest
{
    private static ItemLimpieza Item(string id, GrupoLimpieza grupo, AccionDerivada? deriva = null) => new()
    {
        Id = id,
        Ubicacion = id,
        Descripcion = "qué es",
        Consecuencia = "qué se pierde",
        Grupo = grupo,
        Derivacion = deriva
    };

    [Fact]
    public void Sin_preferencias_manda_el_grupo()
    {
        using var arbol = new ArbolDePrueba();
        var repo = new PerfilRepository(new StorageDePrueba(arbol.Raiz));

        var ofrecidos = repo.Aplicar([
            Item("seguro", GrupoLimpieza.Seguro),
            Item("criterio", GrupoLimpieza.ConCriterio)
        ]);

        Assert.True(ofrecidos.Single(o => o.Item.Id == "seguro").Tildado);
        Assert.False(ofrecidos.Single(o => o.Item.Id == "criterio").Tildado);
    }

    [Fact]
    public void El_tilde_sobrevive_a_reabrir()
    {
        using var arbol = new ArbolDePrueba();
        var items = new[] { Item("seguro", GrupoLimpieza.Seguro), Item("criterio", GrupoLimpieza.ConCriterio) };

        var primero = new PerfilRepository(new StorageDePrueba(arbol.Raiz));
        primero.Guardar([new ItemOfrecido(items[0], false), new ItemOfrecido(items[1], true)]);

        // Otra instancia, como después de cerrar y volver a abrir la herramienta.
        var segundo = new PerfilRepository(new StorageDePrueba(arbol.Raiz));
        var ofrecidos = segundo.Aplicar(items);

        Assert.False(ofrecidos.Single(o => o.Item.Id == "seguro").Tildado);
        Assert.True(ofrecidos.Single(o => o.Item.Id == "criterio").Tildado);
    }

    [Fact]
    public void Ignorar_saca_del_resultado_en_lugar_de_destildar()
    {
        using var arbol = new ArbolDePrueba();
        var repo = new PerfilRepository(new StorageDePrueba(arbol.Raiz));
        var items = new[] { Item("uno", GrupoLimpieza.Seguro), Item("dos", GrupoLimpieza.Seguro) };

        repo.Ignorar("dos");
        var ofrecidos = repo.Aplicar(items);

        // Destildarlo no alcanzaría: quien lo marcó pidió no volver a verlo.
        Assert.Single(ofrecidos);
        Assert.DoesNotContain(ofrecidos, o => o.Item.Id == "dos");
    }

    [Fact]
    public void Guardar_los_visibles_no_revive_a_un_ignorado()
    {
        using var arbol = new ArbolDePrueba();
        var repo = new PerfilRepository(new StorageDePrueba(arbol.Raiz));
        var items = new[] { Item("uno", GrupoLimpieza.Seguro), Item("dos", GrupoLimpieza.Seguro) };

        repo.Ignorar("dos");
        repo.Guardar(repo.Aplicar(items));

        Assert.DoesNotContain(repo.Aplicar(items), o => o.Item.Id == "dos");
    }

    [Fact]
    public void Se_puede_volver_a_ofrecer_lo_ignorado()
    {
        using var arbol = new ArbolDePrueba();
        var repo = new PerfilRepository(new StorageDePrueba(arbol.Raiz));
        var items = new[] { Item("uno", GrupoLimpieza.Seguro) };

        repo.Ignorar("uno");
        repo.DejarDeIgnorar("uno");

        // Sin esto, marcar algo por error no tendría vuelta atrás salvo borrando la base a mano.
        Assert.Single(repo.Aplicar(items));
    }

    [Fact]
    public void Un_item_que_deriva_nunca_se_ofrece_tildado()
    {
        using var arbol = new ArbolDePrueba();
        var repo = new PerfilRepository(new StorageDePrueba(arbol.Raiz));
        var deriva = Item("deriva", GrupoLimpieza.Seguro, new AccionDerivada("Abrir", "x"));

        // Aunque haya quedado tildado en la base de una versión donde sí se borraba: si no, el ítem
        // entraría a la limpieza por la puerta de atrás.
        repo.Guardar([new ItemOfrecido(deriva, true)]);

        Assert.False(repo.Aplicar([deriva]).Single().Tildado);
    }

    /// <summary>
    /// Apunta a una carpeta temporal en lugar de a la base real del usuario.
    /// </summary>
    private sealed class StorageDePrueba(string carpeta) : IToolStorage
    {
        public string ToolId => "DiskCleanerTest";
        public string DataFolder => carpeta;
        public string DatabasePath => Path.Combine(carpeta, "data.db");

        public SqliteConnection OpenConnection()
        {
            var conexion = new SqliteConnection($"Data Source={DatabasePath}");
            conexion.Open();
            return conexion;
        }
    }
}
