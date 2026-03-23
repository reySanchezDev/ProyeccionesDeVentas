using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface IStoresHttpClient
    {
        Task<List<Store>> ObtenerStoresAsync();
    }
}
