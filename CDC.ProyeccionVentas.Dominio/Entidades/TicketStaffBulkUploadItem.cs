namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketStaffBulkUploadItem
    {
        public DateTime Fecha { get; set; }
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreStaff { get; set; } = string.Empty;
        public int TicketPromedio { get; set; }
    }
}
