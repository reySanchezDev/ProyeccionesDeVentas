namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TransaccionSucursalSaveResponse
    {
        public string Accion { get; set; } = string.Empty;
        public int Id { get; set; }
        public decimal TransaccionProyectada { get; set; }
        public decimal? UltimoValorTransaccionProyectada { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
