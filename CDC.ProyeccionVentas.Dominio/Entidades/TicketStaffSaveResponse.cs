namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketStaffSaveResponse
    {
        public string Accion { get; set; } = string.Empty;
        public int Id { get; set; }
        public int TicketPromedio { get; set; }
        public int? UltimoValorTicketPromedio { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
