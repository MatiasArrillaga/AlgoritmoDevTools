# Referencia técnica y funcional — Secrets Manager

Administra los `dotnet user-secrets` del proyecto `Algoritmo.Microservices.Shared.API` y un CRUD de conexiones guardadas.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

---

## 1. Objetivo funcional

Que apuntar el entorno local a otro servidor o a otra base sea un par de clics, sin editar a mano el archivo de secretos ni recordar la forma exacta de cada cadena.

## 2. Qué hace

- **Conexiones guardadas**: alta, edición y baja. Cada una tiene servidor, usuario y contraseña —o autenticación de Windows— y se puede probar antes de guardar.
- **Bases del servidor**: al elegir una conexión, lista las bases de datos consultando el servidor, para apuntar a una sin tener que crear otra conexión.
- **Aplicar**: reescribe las tres cadenas modificables apuntando a la conexión y base elegidas.
- **Listar**: vuelve a leer los secretos y los muestra agrupados.
- **Restaurar**: carga desde `secrets/SoftCerealCore.Secrets.json` del repositorio, si existe.
- **Menú contextual del explorador**: deja las conexiones a un clic derecho, sin abrir la aplicación.

Al entrar, la pantalla deja **preseleccionada la conexión y la base a la que apuntan los secretos hoy**. Sale del propio secreto `Development` y no de una preferencia guardada: el secreto es lo que está realmente aplicado, así que acierta también cuando la última vez se aplicó desde el menú contextual.

## 3. Reglas en vigor

### Qué cadenas se tocan y cuáles no

| Cadena | Al aplicar |
|---|---|
| `SoftCerealCore.Development.ConnectionString` | Se reescribe |
| `SoftCerealCore.DAPR.ConnectionString` | Se reescribe |
| `SoftCerealCore.Seguridad.ConnectionString` | Se reescribe, con `Integrated Security=true` y sin `Encrypt` |
| `Staging`, `Production` | **Se conservan**: se releen antes de escribir |
| Las diecisiete `SoftCerealCore.Seguridad.<Dominio>` | **Se conservan**: se releen y se vuelven a escribir |

Qué grupo es cuál lo define `Constantes.SecretTypesModificables`.

**Conservar las que no administra no es una cortesía: es obligatorio.** `ApplySecrets` corre `user-secrets clear` antes de reescribir, así que sin esa copia aplicar una conexión borraba todo lo demás y dejaba los microservicios sin sus secretos de seguridad.

De la conexión elegida, la cadena de Seguridad toma **sólo servidor y base**, nunca usuario ni contraseña.

### La forma de la cadena

Las cadenas generadas terminan en `;TrustServerCertificate=True;Encrypt=False` (`Constantes.ParametrosDeCifrado`), **igual que las del archivo de restauración**. Si no coincidieran, la cadena cambiaría de forma según si se tocó Restaurar o Aplicar.

`Encrypt=False` no es decorativo: `Microsoft.Data.SqlClient` cifra por defecto desde la versión 4.

### El menú contextual es estático

El explorador no ejecuta código nuestro para dibujarlo, así que **las bases no se pueden consultar en vivo**: cada entrada lleva clavada la última base usada de esa conexión (columna `LastDataBase`). Por eso el menú se reescribe solo cada vez que se agrega, borra o reapunta una conexión (`MenuContextualSecretos.Sincronizar`).

Las conexiones que todavía no tienen base no se listan: se llega a ellas por *Elegir base...*, que sí consulta el servidor.

```text
DevTools ▸
    Secrets Manager ▸
        Restaurar secretos
        ────────────────
        localhost,1433 - Algoritmo93 (softcereal)
        10.1.85.93,1433 - Algoritmo (softcereal)
        ────────────────
        Elegir base...
```

La raíz es **DevTools** y no el Secrets Manager para que las demás herramientas cuelguen su propio grupo del mismo menú. Quitar borra el grupo `Secrets Manager`, y la raíz `DevTools` sólo si no quedó ninguna otra colgando. Va en `HKEY_CURRENT_USER`, así que no pide permisos de administrador.

A diferencia del Convertidor a Markdown, **siempre pide confirmación y avisa el resultado**: un clic por error dejaría el proyecto sin secretos.

## 4. Almacenamiento

`%LOCALAPPDATA%/AlgoritmoDevTools/Shared/data.db` — tabla `SavedConnections`, con restricción única `(Server, DataBase, UserName)` y columnas `UseIntegratedSecurity` y `LastDataBase`.

Usa la clave `"Shared"` y no `"SecretsManager"` porque esas conexiones las consume también el menú contextual y las acciones sin ventana. **Cambiar esa clave deja a la herramienta sin su historial.**

> `LastDataBase` es deliberadamente distinta de `DataBase`: esa última forma parte de la clave única, así que pisarla cambiaría la identidad de la conexión y podría chocar con otra fila. La conexión es el servidor más las credenciales; la base es un dato de uso.

## 5. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/SecretsManagerPagina.razor(.cs)` | Pantalla (interfaz HTML) |
| `Views/SecretsManagerView.cs` | Pantalla de la ventana clásica, bajo `--clasico` |
| `Services/AplicadorDeSecretos.cs` | Reescribe las cadenas y recuerda la base |
| `Services/MenuContextualSecretos.cs` | Alta y baja del grupo en el registro |
| `AlgoritmoDevTools.Integrations.SoftCerealCore/SecretService.cs` | Lectura y escritura de los secretos |
| `AlgoritmoDevTools.Integrations.SoftCerealCore/SavedConnectionsRepository.cs` | CRUD de conexiones |
| `AlgoritmoDevTools.Integrations.SoftCerealCore/SQLService.cs` | Probar conexión y listar bases |

## 6. Decisiones que conviene no revisitar

### La lectura de secretos ocurre una sola vez

La dispara el armazón al arrancar y tarda unos **2,2 segundos**: levanta el CLI de `dotnet`. La pantalla **no vuelve a pedirlos al entrar**; usa lo que el servicio ya tiene en memoria y se entera de los cambios por el evento `SecretsChanged`.

Releer al entrar lanzaba un segundo proceso contra el mismo archivo mientras el primero seguía corriendo, y la pantalla quedaba colgada en "Leyendo secretos...".

### No se lee el `secrets.json` directamente

Tentador —el archivo se lee en 12 ms contra los 2,2 s del comando— pero **descartado por decisión del equipo**. Los secretos se leen con `dotnet user-secrets list`, que es la vía soportada.

Lo que sí se hizo: llamar a `dotnet` directo en vez de envolverlo en un `powershell`. Era el mismo comando levantando dos procesos; se ahorran unos 700 ms.

### El visor perdió el resaltado en negrita

En la ventana clásica las claves (`Server=`, `Database=`…) salían en negrita porque era un `RichTextBox`. En la interfaz HTML es texto plano. Se puede rehacer con marcado; no está hecho.

## 7. Qué tendrían que cubrir las pruebas

- Aplicar una conexión **conserva** `Staging`, `Production` y las diecisiete de Seguridad.
- La cadena generada termina exactamente igual que la del archivo de restauración.
- La de Seguridad sale con `Integrated Security=true` y sin usuario ni contraseña.
- `UpdateLastDataBase` no altera la identidad de la conexión.
- La preselección acierta cuando hay varias conexiones al mismo servidor y una sola recuerda esa base.
- Con el servidor caído, la lista de bases cae en la última conocida en lugar de vaciarse.

## 8. Deuda conocida

| Qué | Por qué |
|---|---|
| El visor no resalta las claves | Era formato del `RichTextBox` |
| La lectura tarda ~2,2 s | Es el arranque del CLI de `dotnet`; leer el json está descartado |
| Los textos están en el código | Convención vigente del repositorio, que no tiene archivo de recursos |

---

**Este documento debe actualizarse** cuando cambie qué cadenas se administran, la forma de las cadenas generadas, el esquema de `SavedConnections` o el contrato del menú contextual.
