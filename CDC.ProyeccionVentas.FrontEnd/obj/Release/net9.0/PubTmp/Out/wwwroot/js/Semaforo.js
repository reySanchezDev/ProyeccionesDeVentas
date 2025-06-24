// Verifica conexión a Internet
function verificarConexionInternet() {
    return fetch("https://www.gstatic.com/generate_204", { method: "GET", mode: "no-cors" })
        .then(() => true)
        .catch(() => false);
}

// Verifica conexión al servidor interno (endpoint Razor Pages)
function verificarConexionServidor() {
    return fetch("/Ping", { method: "GET", cache: "no-store" })
        .then(response => response.ok)
        .catch(() => false);
}

// Cambia el color de los iconos según el estado
function actualizarSemaforo() {
    verificarConexionInternet().then(internet => {
        const iconInternet = document.getElementById('iconInternet');
        if (iconInternet) {
            iconInternet.classList.remove('text-success', 'text-danger', 'text-secondary');
            iconInternet.classList.add(internet ? 'text-success' : 'text-danger');
        }
    });

    verificarConexionServidor().then(servidor => {
        const iconServidor = document.getElementById('iconServidor');
        if (iconServidor) {
            iconServidor.classList.remove('text-success', 'text-danger', 'text-secondary');
            iconServidor.classList.add(servidor ? 'text-success' : 'text-danger');
        }
    });
}

// Escanea cada 10 segundos
setInterval(actualizarSemaforo, 10000);
// Ejecuta al cargar
document.addEventListener('DOMContentLoaded', actualizarSemaforo);
