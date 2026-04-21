namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TransaccionSucursalBulkUploadRequest
    {
        public string CodigoEmpleadoAccion { get; set; } = string.Empty;
        public List<TransaccionSucursalBulkUploadItem> Items { get; set; } = new();
    }
}
