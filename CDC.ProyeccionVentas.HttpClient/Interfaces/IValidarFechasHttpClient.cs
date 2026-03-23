using CDC.ProyeccionVentas.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface IValidarFechasHttpClient
    {
        Task<List<ValidarFechaRequest>> FiltrarExistentesAsync(List<ValidarFechaRequest> datos);
 
    }
}