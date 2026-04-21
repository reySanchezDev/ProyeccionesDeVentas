namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TransaccionSucursalSaveRequest
    {
        public int Id { get; set; }
        public decimal TransaccionProyectada { get; set; }
        public string CodigoEmpleadoAccion { get; set; } = string.Empty;
    }
}
