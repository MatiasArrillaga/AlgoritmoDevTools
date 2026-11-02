// Lo poco que no se puede resolver desde Blazor.
window.devtools = window.devtools || {};

/*
  Deja las casillas que mandan sobre otras en el estado intermedio —ni tildada ni vacía—, que es el
  que avisa que ahí adentro hay algunos elegidos y otros no.

  Va por acá y no desde el componente porque "intermedio" no es un atributo del HTML: es una
  propiedad que sólo existe en el elemento ya creado, así que no hay forma de pedirla desde el
  marcado. El componente deja escrito data-parcial y esto lo traduce después de cada dibujado.
*/
window.devtools.sincronizarParciales = function () {
    document.querySelectorAll('input[type=checkbox][data-parcial]').forEach(function (casilla) {
        casilla.indeterminate = casilla.getAttribute('data-parcial') === 'true';
    });
};


// La barra del entorno no tiene marco, asi que arrastrarla no lo hace Windows solo. Tampoco sirve
// el hit-test del formulario: el visor ocupa toda la ventana y se queda con el mouse. Entonces la
// pagina avisa que se empezo a arrastrar desde la zona de agarre y el host se encarga del resto,
// que es el mismo camino que usa la franja de archivos arrastrados.
//
// Colapsada, la pestana entera es zona de agarre: se la puede llevar a donde no moleste. Que un
// clic sin movimiento la despliegue lo resuelve el host comparando la posicion antes y despues,
// porque el arrastre lo maneja Windows con su bucle modal y el clic no vuelve aca.
document.addEventListener('mousedown', function (evento) {
    if (evento.button !== 0) return;
    if (!evento.target.closest('.barra-agarre, .barra-solapa')) return;

    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage('barra:mover');
    }
});
