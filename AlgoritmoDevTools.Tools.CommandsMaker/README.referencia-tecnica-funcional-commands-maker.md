# Referencia técnica y funcional — Commands Maker

Arma los comandos de migraciones de EF Core para el dominio elegido y los deja en el portapapeles.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

---

## 1. Objetivo funcional

Que armar un `Add-Migration` no implique acordarse de la cadena de conexión, del formato del nombre ni de qué parámetros de cifrado hacen falta.

## 2. Qué hace

Genera los tres comandos —`add-migration`, `remove-migration` y `update-database`— listos para pegar en la consola del administrador de paquetes, y los copia al portapapeles.

- Toma `Server` y `Database` del secreto `SoftCerealCore.Development.ConnectionString`.
- Mantiene una lista local de dominios, **compartida con el Schema Change Detector**.
- El nombre de la migración se puede editar, con o sin el dominio como prefijo.

## 3. Reglas en vigor

### La cadena que se arma no es la del secreto

Se reemplazan las credenciales por:

```text
Integrated Security = true; MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;
```

Es lo mismo que usan los scripts de PowerShell del backend: corre como el usuario de Windows, que tiene permisos de administrador en el SQL de desarrollo.

**`Encrypt` y `TrustServerCertificate` no son opcionales**: `Microsoft.Data.SqlClient` cifra por defecto y el SQL de desarrollo usa un certificado autofirmado. Sin esos dos parámetros el comando falla al conectarse.

La cola vive en una sola constante (`CONNECTION_SUFFIX`), así que la toman los tres comandos. Cambiarla en un solo lugar es lo que evita que se desincronicen.

### El dominio usado queda como el último

Al copiar un comando se registra el uso del dominio. Ese dato define cuál destaca el menú contextual del explorador, que se reescribe en el momento.

### El prefijo del nombre

Con la casilla tildada, el nombre queda `[Dominio].LoQueEscribasVos`. Al cambiar de dominio o destildar, se rearma respetando lo que se haya escrito después del punto.

## 4. El menú contextual y su presupuesto

Cuelga del grupo `DevTools` del clic derecho sobre el fondo de una carpeta, con los tres comandos del dominio destacado y el resto bajo *Otro dominio*.

**El explorador admite 16 ítems y el presupuesto es compartido entre herramientas.** Pasarse no da error: el explorador descarta el resto en silencio. Por eso la pantalla muestra cuántos se están usando —es la única pista de por qué faltan opciones— y el cartel se recalcula cada vez que la pantalla vuelve a mostrarse, porque lo que haga otra herramienta cambia la cuenta.

El menú es estático: se reescribe solo al agregar, borrar o usar un dominio.

## 5. Almacenamiento

`%LOCALAPPDATA%/AlgoritmoDevTools/CommandsMaker/data.db` — tabla `DOMINIOS`.

Lo comparte el Schema Change Detector: los dominios son los mismos.

## 6. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/CommandsMakerPagina.razor(.cs)` | Pantalla (interfaz HTML) |
| `Views/CommandsMakerView.cs` | Pantalla de la ventana clásica, bajo `--clasico` |
| `Services/GeneradorDeComandos.cs` | Arma los comandos y resuelve la cadena |
| `Services/MenuContextualComandos.cs` | Alta y baja del grupo en el registro |
| `Integrations.SoftCerealCore/DomainRepository.cs` | CRUD de dominios y registro de uso |

## 7. Qué tendrían que cubrir las pruebas

- Los tres comandos terminan con la misma cola de conexión.
- La cadena generada nunca lleva usuario ni contraseña.
- El prefijo de dominio se pone y se saca sin perder lo escrito a mano.
- Copiar un comando registra el uso del dominio y reescribe el menú.
- Borrar el dominio destacado deja el menú apuntando a otro existente.

## 8. Deuda conocida

| Qué | Por qué |
|---|---|
| El presupuesto de 16 ítems se reparte a ojo | Es un límite del explorador; sólo se puede avisar |
| Los textos están en el código | Convención vigente del repositorio |

---

**Este documento debe actualizarse** cuando cambien los parámetros de conexión que exige el backend, la forma de los comandos o el contrato del menú contextual.
