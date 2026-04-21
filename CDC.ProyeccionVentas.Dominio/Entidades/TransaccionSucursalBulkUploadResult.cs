namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class TransaccionSucursalBulkUploadResult
    {
        public int RegistrosInsertados { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
