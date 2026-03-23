using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.AuthService.Servicios
{
    public class ProyeccionVentasService : IProyeccionVentasService
    {
        private readonly IProyeccionVentasRepository _repository;

        public ProyeccionVentasService(IProyeccionVentasRepository repository)
        {
            _repository = repository;
        }

        public async Task InsertarProyeccionesAsync(IEnumerable<ProyeccionVentaDto> proyecciones)
        {
            await _repository.InsertarProyeccionesAsync(proyecciones);
        }
    }
}