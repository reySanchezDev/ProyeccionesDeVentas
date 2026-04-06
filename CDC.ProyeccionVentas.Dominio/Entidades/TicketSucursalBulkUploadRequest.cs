namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketSucursalBulkUploadRequest
    {
        public string CodigoEmpleadoAccion { get; set; } = string.Empty;
        public List<TicketSucursalBulkUploadItem> Items { get; set; } = new();
    }
}
