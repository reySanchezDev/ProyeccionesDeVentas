using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface ITicketSucursalHttpClient
    {
        Task<List<TicketSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales);

        Task<List<TicketSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TicketSucursalBulkUploadItem> items);

        Task<TicketSucursalBulkUploadResult> InsertarMasivoAsync(TicketSucursalBulkUploadRequest request);

        Task<List<TicketSucursalItem>> ConsultarAsync(TicketSucursalConsultaFilter filter);

        Task<TicketSucursalSaveResponse> GuardarAsync(TicketSucursalSaveRequest request);

        Task<TicketSucursalDeleteMonthResult> EliminarMesAsync(TicketSucursalDeleteMonthRequest request);
    }
}
