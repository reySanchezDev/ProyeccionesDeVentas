using CDC.ProyeccionVentas.AuthService.Servicios;
using CDC.ProyeccionVentas.Dominio.Entidades;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly StoreService _storeService;

        public StoresController(StoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Store>>> ObtenerStores()
        {
            var stores = await _storeService.ObtenerStoresAsync();
            return Ok(stores);
        }
    }
}