namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketSucursalDownloadItem
    {
        public string CodSucursal { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public int? TicketPromedio { get; set; }
    }
}
