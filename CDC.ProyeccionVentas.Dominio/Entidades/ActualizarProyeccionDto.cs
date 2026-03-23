using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class ActualizarProyeccionDto
    {
        public int Id { get; set; }
        public decimal Monto { get; set; }
        public int? TicketPromedio { get; set; }
    }
}
