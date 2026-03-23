using CDC.ProyeccionVentas.Dominio.Entidades;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface ICalendarioHttpClient
    {
        Task<List<CalendarioMatrizRow>> ObtenerMatrizAsync();
        Task<CalendarioMatrizRow> GuardarDiaAsync(string storeNo, int diaSemanaIso, bool marcado);
    }
}
