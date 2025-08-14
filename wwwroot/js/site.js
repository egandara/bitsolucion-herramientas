// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// EN: wwwroot/js/site.js

function showToast(message, title = 'Notificación', type = 'success') {
    const toastEl = document.getElementById('appToast');
    const toastTitleEl = document.getElementById('toast-title');
    const toastBodyEl = document.getElementById('toast-body');

    // Limpiamos clases de color anteriores
    toastEl.classList.remove('bg-success', 'bg-danger', 'text-white');

    // Asignamos el título y el mensaje
    toastTitleEl.textContent = title;
    toastBodyEl.textContent = message;

    // Asignamos el color según el tipo
    if (type === 'success') {
        toastEl.classList.add('bg-success', 'text-white');
    } else if (type === 'error') {
        toastEl.classList.add('bg-danger', 'text-white');
    }

    const toast = new bootstrap.Toast(toastEl);
    toast.show();
}