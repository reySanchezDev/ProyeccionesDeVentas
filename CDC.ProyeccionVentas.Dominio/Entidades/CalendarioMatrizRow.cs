using System;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    /// <summary>
    /// Fila de la matriz pivoteada para una sucursal.
    /// Los días se representan 0/1 (unchecked/checked).
    /// </summary>
    public sealed class CalendarioMatrizRow
    {
        public string StoreNo { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;

        public int Lunes { get; set; }
        public int Martes { get; set; }
        public int Miercoles { get; set; }
        public int Jueves { get; set; }
        public int Viernes { get; set; }
        public int Sabado { get; set; }
        public int Domingo { get; set; }
    }
}
