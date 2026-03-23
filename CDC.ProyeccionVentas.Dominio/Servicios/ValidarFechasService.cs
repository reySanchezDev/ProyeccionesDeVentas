using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Servicios
{
    public class ValidarFechasService : IValidarFechasService
    {
        private readonly IValidarFechasRepository _repository;

        public ValidarFechasService(IValidarFechasRepository repository)

        {
            _repository = repository;
        }

        public async Task<List<ValidarFechaRequest>> FiltrarFechasYaExistentesAsync(List<ValidarFechaRequest> datosArchivo)
        {
            return await _repository.FiltrarExistentesAsync(datosArchivo);
        }

        public async Task<List<ValidarFechaRequest>> ObtenerFechasYSucursalesExistentesAsync()
        {
            return await _repository.ObtenerFechasYSucursalesExistentesAsync();
        }


    }
}