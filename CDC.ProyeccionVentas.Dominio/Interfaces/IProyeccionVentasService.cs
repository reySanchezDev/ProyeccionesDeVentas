using CDC.ProyeccionVentas.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Interfaces
{
    public interface IProyeccionVentasService
    {
        Task InsertarProyeccionesAsync(IEnumerable<ProyeccionVentaDto> proyecciones);
    }
}