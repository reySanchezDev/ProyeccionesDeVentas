namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TransaccionSucursalBulkUploadItem
    {
        public string CodSucursal { get; set; } = string.Empty;
        public decimal TransaccionProyectada { get; set; }
    }
}
