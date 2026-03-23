namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketStaffSaveRequest
    {
        public int Id { get; set; }
        public int TicketPromedio { get; set; }
        public string CodigoEmpleadoAccion { get; set; } = string.Empty;
    }
}
