using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface ITransaccionSucursalHttpClient
    {
        Task<List<TransaccionSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales);

        Task<List<TransaccionSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TransaccionSucursalBulkUploadItem> items);

        Task<TransaccionSucursalBulkUploadResult> InsertarMasivoAsync(TransaccionSucursalBulkUploadRequest request);

        Task<List<TransaccionSucursalItem>> ConsultarAsync(TransaccionSucursalConsultaFilter filter);

        Task<TransaccionSucursalSaveResponse> GuardarAsync(TransaccionSucursalSaveRequest request);

        Task<TransaccionSucursalDeleteMonthResult> EliminarMesAsync(TransaccionSucursalDeleteMonthRequest request);
    }
}
