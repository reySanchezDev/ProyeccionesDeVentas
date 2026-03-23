using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.Dominio.Servicios
{
    /// <summary>
    /// Contrato para leer la matriz y guardar el clic de un checkbox.
    /// </summary>
    public interface ICalendarioPedidosRepositorio
    {
        /// <summary>
        /// Devuelve toda la matriz (todas las sucursales con sus 7 días 0/1).
        /// </summary>
        Task<IReadOnlyList<CalendarioMatrizRow>> ObtenerMatrizAsync(CancellationToken ct = default);

        /// <summary>
        /// Persiste el cambio de un checkbox y devuelve la fila actualizada para esa sucursal.
        /// </summary>
        /// <param name="storeNo">Código de sucursal (SK*** o SR***)</param>
        /// <param name="diaSemanaIso">1=Lunes ... 7=Domingo</param>
        /// <param name="marcado">true=checked → inserta; false=unchecked → elimina</param>
        Task<CalendarioMatrizRow> GuardarDiaAsync(string storeNo, byte diaSemanaIso, bool marcado, CancellationToken ct = default);
    }
}
