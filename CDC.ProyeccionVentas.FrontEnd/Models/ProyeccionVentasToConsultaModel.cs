namespace CDC.ProyeccionVentas.FrontEnd.Models
{
    public class ProyeccionVentasToConsultaModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string CodSucursal { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int? TicketPromedio { get; set; }
    }
}
