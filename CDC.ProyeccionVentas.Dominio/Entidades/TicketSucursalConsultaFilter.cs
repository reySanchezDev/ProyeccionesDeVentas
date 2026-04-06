namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TicketSucursalConsultaFilter
    {
        public int Mes { get; set; }
        public int Ano { get; set; }
        public List<string> CodSucursales { get; set; } = new();
    }
}
