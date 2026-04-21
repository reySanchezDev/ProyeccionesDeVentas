using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.Dominio.Interfaces
{
    public interface ITransaccionSucursalService
    {
        Task<List<TransaccionSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales);

        Task<List<TransaccionSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TransaccionSucursalBulkUploadItem> items);

        Task<TransaccionSucursalBulkUploadResult> InsertarMasivoAsync(TransaccionSucursalBulkUploadRequest request);

        Task<List<TransaccionSucursalItem>> ConsultarAsync(TransaccionSucursalConsultaFilter filter);

        Task<TransaccionSucursalSaveResponse> ActualizarAsync(TransaccionSucursalSaveRequest request);

        Task<TransaccionSucursalDeleteMonthResult> EliminarMesAsync(TransaccionSucursalDeleteMonthRequest request);
    }
}
