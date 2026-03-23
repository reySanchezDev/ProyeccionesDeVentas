namespace CDC.ProyeccionVentas.API.Models
{
    /// <summary>Payload para guardar el clic del checkbox.</summary>
    public sealed class GuardarDiaRequest
    {
        public string StoreNo { get; set; } = string.Empty; // SK*** o SR***
        public byte DiaSemanaIso { get; set; }              // 1=Lun ... 7=Dom
        public bool Marcado { get; set; }                   // true=checked, false=unchecked
    }
}
