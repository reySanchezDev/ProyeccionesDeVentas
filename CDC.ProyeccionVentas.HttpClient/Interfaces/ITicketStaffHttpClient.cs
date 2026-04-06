using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface ITicketStaffHttpClient
    {
        Task<List<string>> ObtenerCatalogoPuestosAsync();

        Task<List<TicketStaffDownloadItem>> DescargarPlantillaAsync(List<string> puestos);

        Task<List<TicketStaffExistingItem>> FiltrarExistentesCargaAsync(List<TicketStaffBulkUploadItem> items);

        Task<TicketStaffBulkUploadResult> InsertarMasivoAsync(TicketStaffBulkUploadRequest request);

        Task<List<TicketStaffItem>> ConsultarAsync(TicketStaffConsultaFilter filter);

        Task<TicketStaffSaveResponse> GuardarAsync(TicketStaffSaveRequest request);

        Task<TicketStaffDeleteMonthResult> EliminarMesAsync(TicketStaffDeleteMonthRequest request);
    }
}
