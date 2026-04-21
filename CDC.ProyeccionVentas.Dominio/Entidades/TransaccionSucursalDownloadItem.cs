namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TransaccionSucursalDownloadItem
    {
        public string CodSucursal { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public decimal? TransaccionProyectada { get; set; }
    }
}
