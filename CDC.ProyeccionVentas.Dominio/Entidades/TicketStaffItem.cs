namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketStaffItem
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreStaff { get; set; } = string.Empty;
        public bool ExisteEnCDC { get; set; }
        public int TicketPromedio { get; set; }
    }
}
