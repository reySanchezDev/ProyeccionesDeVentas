using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class FiltroProyeccionVentas
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string? CodSucursal { get; set; }
        public List<string> CodSucursales { get; set; } = new();
    }
}
