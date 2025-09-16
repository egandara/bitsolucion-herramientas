/*
-----------------------------------------
AÑADIR LÓGICA PARA EL SIDEBAR COLAPSABLE
-----------------------------------------
*/
document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById('sidebar-toggle');

    if (toggleButton) {
        toggleButton.addEventListener('click', function () {
            // Alterna la clase en el body
            document.body.classList.toggle('sidebar-collapsed');

            // Guarda la preferencia en localStorage
            if (document.body.classList.contains('sidebar-collapsed')) {
                localStorage.setItem('sidebar_state', 'collapsed');
            } else {
                localStorage.setItem('sidebar_state', 'expanded');
            }
        });
    }

    // Comprueba la preferencia al cargar la página
    const sidebarState = localStorage.getItem('sidebar_state');
    if (sidebarState === 'collapsed') {
        document.body.classList.add('sidebar-collapsed');
    }
});