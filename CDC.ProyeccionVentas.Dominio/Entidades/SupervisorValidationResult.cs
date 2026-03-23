namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class SupervisorValidationResult
    {
        public bool ExisteEmpleado { get; set; }
        public bool EsSupervisor { get; set; }
        public int CantidadStaff { get; set; }
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
