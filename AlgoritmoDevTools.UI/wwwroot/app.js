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
