namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketStaffBulkUploadItem
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreStaff { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public int TicketPromedio { get; set; }
    }
}
