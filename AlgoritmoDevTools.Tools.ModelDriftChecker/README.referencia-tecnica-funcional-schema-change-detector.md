# Referencia técnica y funcional — Schema Change Detector

Contesta una sola pregunta: *¿los commits que acabo de traer de master exigen correr migraciones?*

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

---

## 1. Objetivo funcional

Evitar los dos errores caros después de un `git pull`: arrancar el backend contra una base desactualizada, o correr migraciones a ciegas por las dudas.

## 2. Cómo funciona

Mantiene un **baseline** por repositorio: el SHA del último commit en el que el usuario confirmó que su base local estaba migrada.

Al verificar, corre `git diff --name-only <baseline> HEAD` y clasifica los archivos tocados:

| Severidad | Regla | Qué significa |
|---|---|---|
| Definitiva | `*/Algoritmo.*.Infrastructure/**/*DbContext*.cs` | Seguro hay que migrar |
| Probable | `*/Algoritmo.*.Infrastructure/**/*Configuration*.cs` | Muy probable que haya migración |
| Posible | `*/Algoritmo.*.Domain/**/*.cs` | Archivo de dominio tocado: puede ser propiedad, lógica o comentario |

### Filtros del nivel Posible

Se excluyen por falsos positivos conocidos:

- Nombres que terminan en `Event`, `Rule`, `Repository`, `RepositoryAsync`, `Error`, `Errors`, `Request`, `Requests`, `Command`, `Commands`, `Handler`, `Handlers`, `Response`, `Responses`, `Query`, `Queries`.
- Interfaces de repositorio (`I*Repository*`).
- Archivos con `public enum`.
- Archivos con `[NotMapped]`.

### Deduplicación por nombre de clase

Si `Usuario.cs` aparece en ocho dominios, se muestra una sola vez con la lista de rutas debajo.

## 3. Flujo de trabajo

1. La primera vez: **Usar HEAD como baseline**, que asume que la base está en ese estado.
2. Después de un `git pull`, volver a la herramienta: verifica sola al entrar.
3. Si marca migración: correr las migraciones y tocar **Ya migré**.
4. Si no hay cambios relevantes: arrancar el backend tranquilo.

## 4. Por qué no se usa `dotnet ef`

Porque el `AlgoritmoDbContextFactory` de AlgoritmoCore corre `CheckTablesInventoryResources` al construir el DbContext: requiere la función SQL `GetTableColumnsInfo` y permisos que no todos los usuarios tienen.

El enfoque por `git diff` evita el problema por completo: no toca la base, no compila y no carga ensamblados.

**La limitación es real y hay que tenerla presente**: compara *código contra archivos*, no contra el esquema de la base. En la práctica cubre la enorme mayoría de los casos, pero no detecta que alguien haya tocado la base a mano.

## 5. Almacenamiento

`%LOCALAPPDATA%/AlgoritmoDevTools/ModelDriftChecker/data.db` — tabla `SchemaBaseline`, indexada por ruta del repositorio.

Que la clave sea la ruta importa ahora que la carpeta de repositorios es configurable: **cambiarla implica otro baseline**, porque es otro repositorio desde el punto de vista de la tabla.

## 6. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/ModelDriftCheckerPagina.razor(.cs)` | Pantalla (interfaz HTML) |
| `Views/ModelDriftCheckerView.cs` | Pantalla de la ventana clásica, bajo `--clasico` |
| `Services/SchemaChangeDetector.cs` | Diff, clasificación y lectura de HEAD |
| `Services/ChangeClassification.cs` | Severidades y resultado |
| `Services/BaselineRepository.cs` | Persistencia del baseline |

## 7. Decisiones que conviene no revisitar

### La confirmación vive en la página

Mover el baseline es irreversible en la práctica —se pierde desde dónde comparar—, así que pide confirmación. En la interfaz HTML esa confirmación es un bloque dentro de la pantalla y no un cuadro de diálogo: el visor no muestra `confirm()` del navegador.

### Verifica solo al entrar, pero sólo si hay baseline

Con baseline cargado es lo que se viene a mirar. Sin baseline no tiene sentido correr git para nada.

## 8. Qué tendrían que cubrir las pruebas

- Un `DbContext` tocado sale como Definitiva.
- Un `*Handler.cs` de Domain **no** aparece en Posible.
- Un archivo con `public enum` tampoco.
- La misma clase en varios dominios se lista una vez con sus rutas.
- Sin baseline, la pantalla no corre git.
- `GetBaseline` con una ruta que nunca se usó devuelve nulo en lugar de fallar.

## 9. Deuda conocida

| Qué | Por qué |
|---|---|
| Compara código, no el esquema real | Evitar `dotnet ef` fue deliberado: ver sección 4 |
| El baseline es por ruta | Cambiar la carpeta de repositorios implica empezar de cero |

---

**Este documento debe actualizarse** cuando cambien las reglas de clasificación, los filtros de falsos positivos o la estructura de carpetas de AlgoritmoCore que esas reglas asumen.
