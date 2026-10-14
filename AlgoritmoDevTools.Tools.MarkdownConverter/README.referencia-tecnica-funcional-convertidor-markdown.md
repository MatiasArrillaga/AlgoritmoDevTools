# Referencia técnica y funcional — Convertidor a Markdown

Convierte ERS, relevamientos y planes a Markdown, para pasárselos a un asistente sin gastar contexto de más.

Referencia general: [README.md](../README.md) · Interfaz: [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md)

---

## 1. Por qué importa

Un PDF no se lee como texto: cada página se procesa como imagen, unos 1500-2000 tokens cada una, así que una ERS de 30 páginas se lleva más de 50k. El mismo documento en HTML cuesta unas cuatro veces más que en Markdown.

Medido: la ERS de A Remito, **1.370 KB de `.docx`**, queda en **5,6 KB de `.md`** (~1,4k tokens). Y además se puede leer por secciones en vez de entera.

## 2. Qué hace

- Convierte uno o varios documentos; el `.md` queda **al lado del original**.
- Las imágenes van a `media/<nombre del documento>/` con ruta relativa.
- Opcionalmente quita el texto tachado y genera un `.html` para leer.
- Puede colgarse del clic derecho del explorador.

Se arrastran los documentos sobre la ventana o se eligen con el botón.

## 3. Reglas en vigor

### Las imágenes van a una subcarpeta por documento

No es prolijidad: **pandoc numera las imágenes `image1`, `image2`… arrancando de uno en cada archivo**. Sin la subcarpeta, convertir dos ERS en la misma carpeta hace que la segunda pise las imágenes de la primera.

### Quitar el texto tachado

Tildado por defecto. En las ERS el tachado marca requisitos descartados: sacarlo no sólo ahorra contexto, **evita que el asistente implemente algo que ya no va**.

Funciona con el tachado de párrafo entero —la línea desaparece— y con el que está en medio de una oración —se saca sólo ese fragmento—.

### La limpieza del relleno de Word

En una ERS típica es cerca de un tercio del archivo:

| Se descarta | Por qué |
|---|---|
| El índice de contenido | Los encabezados ya están; el índice es ~22% del archivo |
| El `style="width:5.9in;height:1.08in"` de cada imagen | Queda `![](media/image1.png)`, que dice lo mismo |
| Las líneas en blanco que deja el índice al salir | Evita un hueco de veinte líneas |

El patrón del índice **exige el número de página anidado a propósito**: así nunca borra un enlace escrito a mano.

### Si lo que entra ya es un `.md`

Lo único que se puede hacer es la vista HTML: se genera el `.html` con el tema elegido y **el `.md` original no se toca**. Sin tema elegido, avisa que no hay nada que convertir, porque el destino sería el mismo archivo.

### Formatos que no entran

Los **formatos viejos de Office** (`.doc`, `.ppt`, `.xls`) no los lee pandoc: avisa que hay que reguardarlos. El **PDF** tampoco: avisa que hay que conseguir el `.docx` original.

## 4. El arrastrar y soltar

**El motor de Edge no entrega la ruta en disco de lo que se suelta** —por seguridad sólo da su contenido— y pandoc necesita la ruta real para dejar el `.md` al lado del original. Tampoco cede el arrastre al formulario: en Windows Forms lo recibe el control que está bajo el cursor, y el navegador tapa la ventana entera.

La salida es una franja dibujada con Windows Forms por encima del navegador, apoyada sobre la barra de estado, que sí recibe las rutas. Aparece sola: el navegador no da la ruta pero **sí ve que hay un arrastre en curso**, así que avisa por `postMessage`.

El detalle completo, incluido por qué la franja parpadeaba, está en el documento de la [interfaz HTML](../AlgoritmoDevTools.UI/README.referencia-tecnica-funcional-interfaz-html.md).

> Con `"%1"` Windows lanza **un proceso por archivo**: diez documentos seleccionados arrancan diez instancias. Para varios conviene arrastrarlos juntos, que los procesa en lote.

## 5. El menú contextual

Pone *Convertir a Markdown* en el clic derecho, para todas las extensiones soportadas. **La conversión corre sin abrir la ventana**: aparece el `.md` al lado del original y el proceso termina. El éxito no se avisa —ya se ve en la carpeta—; **sólo hay cartel cuando algo falla**.

- Escribe en `HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations`: **no necesita permisos de administrador**.
- Usa `SystemFileAssociations` y no el ProgID de cada extensión, para no pisar la asociación de Word o Excel.
- Registra la ruta de `Environment.ProcessPath`, o sea el ejecutable que está corriendo. Con `PublishSingleFile`, `Assembly.Location` viene vacío y no sirve. **Si se mueve el `.exe`, hay que volver a agregarlo.**
- Las dos operaciones son idempotentes.
- Al terminar avisa al explorador con `SHChangeNotify(SHCNE_ASSOCCHANGED)`.

> En **Windows 11** los verbos clásicos del registro quedan escondidos en *Mostrar más opciones*. Para eso está el botón **Menú clásico de Windows 11**, que devuelve el menú completo de Windows 10. **Cambia el menú de todo el sistema**, no sólo esta opción, y reinicia el explorador. Es reversible con el mismo botón.

## 6. Requisito: pandoc

`winget install --id JohnMacFarlane.Pandoc -e`

Se busca en este orden: al lado del `.exe` —así se puede repartir el Shell con `pandoc.exe` en la misma carpeta—, después en `%LOCALAPPDATA%\Pandoc\` y por último en el PATH. Si no está, la pantalla lo dice y muestra el comando en vez de fallar.

## 7. Piezas

| Archivo | Rol |
|---|---|
| `AlgoritmoDevTools.UI/Paginas/MarkdownConverterPagina.razor(.cs)` | Pantalla (interfaz HTML) |
| `Views/MarkdownConverterView.cs` | Pantalla de la ventana clásica, bajo `--clasico` |
| `Services/DocumentConverter.cs` | Conversión y limpieza |
| `Services/PandocRunner.cs` | Ubicación e invocación de pandoc |
| `Services/MenuContextual.cs` | Alta y baja en el registro |
| `AlgoritmoDevTools.Shell/ZonaDeArrastre.cs` | La franja donde se sueltan los documentos |

## 8. Trampas verificadas

### `--extract-media` va con un punto

Va `--extract-media=.`, **no** `--extract-media=media`. Pandoc le pega adelante la ruta interna del documento (`word/media/`), así que pasarle `media` termina generando `media/media/image1.png`. Con el punto queda `media/image1.png`.

### No usa el `ProcessRunner` del Core

Ese envoltorio expone `dotnet` y `powershell`, y llamar a pandoc a través de PowerShell obligaría a escapar rutas con espacios y comillas. `PandocRunner` replica el mismo criterio —sin ventana, UTF-8, `Kill(entireProcessTree)` al cancelar— invocando el ejecutable directo con `ArgumentList`.

## 9. Almacenamiento

Ninguno. El `.md` se escribe al lado del original, así que no hay nada que recordar entre sesiones.

## 10. Qué tendrían que cubrir las pruebas

- Dos documentos distintos en la misma carpeta no se pisan las imágenes.
- El patrón del índice no borra un enlace escrito a mano.
- El tachado se saca tanto de párrafo entero como de un fragmento.
- Un `.md` de entrada sin tema elegido no se sobrescribe.
- Un `.doc` o un `.pdf` avisan en lugar de fallar.
- Cancelar a mitad de una tanda corta sin dejar procesos de pandoc colgados.

---

**Este documento debe actualizarse** cuando cambien los formatos soportados, las reglas de limpieza, la forma de invocar a pandoc o el mecanismo de arrastre.
