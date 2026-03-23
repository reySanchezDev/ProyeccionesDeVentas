using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.AuthService.Servicios
{
    public class StoreService
    {
        private readonly IStoreRepository _storeRepository;

        public StoreService(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public Task<IEnumerable<Store>> ObtenerStoresAsync()
        {
            return _storeRepository.ObtenerStoresAsync();
        }
    }
}