namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketSucursalItem
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string CodSucursal { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public bool ExisteEnCatalogo { get; set; }
        public int TicketPromedio { get; set; }
    }
}
