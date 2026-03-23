using System.Collections.Generic;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketStaffBulkUploadRequest
    {
        public string CodigoEmpleadoAccion { get; set; } = string.Empty;
        public List<TicketStaffBulkUploadItem> Items { get; set; } = new();
    }
}
