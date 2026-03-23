using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class ProyeccionVentasToConsulta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string CodSucursal { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int? TicketPromedio { get; set; }
    }
}
