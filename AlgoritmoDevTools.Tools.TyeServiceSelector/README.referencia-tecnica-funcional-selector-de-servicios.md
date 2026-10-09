# Referencia técnica y funcional — Selector de Servicios (Tye)

Elige con casillas qué microservicios levanta el entorno local, sin editar a mano el `tye.yaml`.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

---

## 1. Objetivo funcional

Levantar sólo los microservicios que hacen falta para lo que se está tocando, en vez de los diecisiete, y poder volver a una combinación usada antes sin rearmarla.

## 2. Qué hace

- Lee la lista `services:` del `tye.yaml` maestro y la muestra en casillas.
- Genera `tye.devtools.yaml` en la raíz de AlgoritmoCore con la selección.
- Guarda y aplica **perfiles** con nombre.
- Copia el comando de arranque al portapapeles.

**El `tye.yaml` maestro no se modifica nunca**: queda limpio en git. El archivo generado tiene que estar en la raíz del repositorio para que las rutas relativas `project:` resuelvan.

## 3. Reglas en vigor

### Sólo se listan microservicios

La lista sale de los ítems en la **columna 0** de `services:`. Las entradas de `env:` tienen la misma forma (`- name: X`) pero indentadas, y no son servicios.

Hasta que se acotó el parseo a la columna 0, `SOFTCEREALCORE_PROCESS_CULTURE`, `ASPNETCORE_URLS` y demás variables aparecían como opciones tildables, y al destildar un servicio sus líneas hijas quedaban a medio comentar.

### Cada servicio arrastra su sidecar de Dapr

Los sidecars (`cereales-dapr`, `produccion-dapr`, …) **no se ofrecen en la lista**: al tildar `produccion` quedan activos `produccion` y `produccion-dapr`, y al destildarlo se comentan los dos.

Hasta el commit `0dd570fa5` de AlgoritmoCore (02/10/2026) los sidecars los derivaba Tye solo desde la extensión `dapr`, así que comentar el servicio alcanzaba. Esa extensión se eliminó y ahora son servicios de primer nivel que ejecutan `Utils/Tye/Start-DevelopmentDapr.ps1`.

**Las dos mitades sueltas fallan**: un servicio sin sidecar no puede invocar a nadie, y un sidecar sin servicio queda esperando para siempre una aplicación que no va a arrancar.

### El servicio de seguridad no se puede destildar

Sin él no levanta ninguno de los demás. La casilla está deshabilitada a propósito.

### El toggle es por comentarios de línea

Los servicios destildados se **comentan** (`# `) en vez de borrarse, así re-tildarlos los descomenta sin perder su definición. Es reversible y preserva el formato, cosa que un parser de YAML no haría.

Sólo se toca lo que cambia de estado: un bloque que el maestro ya trae como corresponde se copia tal cual, así sobreviven los comentarios escritos a mano que caen dentro de él —descomentarlos los volvería YAML inválido—.

## 4. El perfil vigente

Se guarda **al generar**, no al elegirlo del combo: lo que cuenta es con qué perfil quedó escrito el archivo, porque el archivo es lo que de verdad va a levantar. Elegir un perfil sin generar no deja registro.

Queda en `ConfiguracionDevTools.UltimoPerfilDeTye` y lo usan dos pantallas: el inicio lo muestra, y esta vuelve a él al abrirse.

**Al abrir no se reaplica**: los servicios tildados salen del archivo generado, y pisarlos borraría cualquier ajuste hecho a mano después. El combo dice de dónde se viene; las casillas dicen cómo está ahora.

> Como es un valor guardado, si se cambian servicios a mano después de aplicar un perfil, el nombre sigue diciendo ese perfil aunque la selección ya no coincida. Hubo una versión que lo deducía comparando los servicios activos contra cada perfil, justamente para evitar esto, pero no acertaba en la práctica y se reemplazó por el valor guardado.

## 5. Almacenamiento

`%LOCALAPPDATA%/AlgoritmoDevTools/TyeServiceSelector/data.db` — tabla `Profiles` (`Name` como clave, `Services` en CSV).

La raíz del repositorio sale de `RutasDeTrabajo.AlgoritmoCore`, o sea de la carpeta elegida en Configuraciones.

## 6. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/TyeServiceSelectorPagina.razor(.cs)` | Pantalla (interfaz HTML) |
| `Views/TyeServiceSelectorView.cs` | Pantalla de la ventana clásica, bajo `--clasico` |
| `Services/TyeServiceToggler.cs` | Lectura del maestro y generación del archivo |
| `Services/ProfileRepository.cs` | CRUD de perfiles |

## 7. Qué tendrían que cubrir las pruebas

- Una variable de `env:` nunca aparece como servicio.
- Tildar un servicio activa también su sidecar; destildarlo comenta los dos.
- El servicio de seguridad queda activo aunque se pida desmarcar todos.
- Un bloque que ya estaba como corresponde se copia sin tocar, con sus comentarios a mano.
- Volver a tildar un servicio comentado restituye su definición completa.
- Generar deja registrado el perfil; elegirlo del combo no.

## 8. Deuda conocida

| Qué | Por qué |
|---|---|
| El perfil guardado puede quedar desfasado de la selección | Se eligió el valor guardado sobre la deducción, que no acertaba |
| El `tye.devtools.yaml` aparece sin trackear en AlgoritmoCore | Conviene agregarlo al `.gitignore` de ese repositorio |
| Las rutas del maestro y del generado ya no se muestran | Se quitaron de la pantalla por pedido; quedan en el documento |

---

**Este documento debe actualizarse** cuando cambie el formato del `tye.yaml` maestro, la convención de los sidecars, el esquema de perfiles o la regla del servicio obligatorio.
