using CDC.ProyeccionVentas.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Interfaces
{
    public interface IValidarFechasService
    {
        Task<List<ValidarFechaRequest>> ObtenerFechasYSucursalesExistentesAsync();
        Task<List<ValidarFechaRequest>> FiltrarFechasYaExistentesAsync(List<ValidarFechaRequest> datosArchivo);
    }
}