using System;
using System.Collections.Generic;

namespace InventarioApp.Models
{
    //  Modelos del dashboard.
    //
    //  Van todos en un archivo porque solo existen para esta pantalla: ninguno
    //  corresponde a una tabla, son la forma que tiene el RESULTADO de cada
    //  consulta de pkg_dashboard. Es el mismo criterio de LoginViewModel: un
    //  Model no es "la plantilla de una tabla", es la forma de los datos que la
    //  app maneja en C#.
    //
    //  REGLA QUE NO SE PUEDE ROMPER: todo tiene que ser PROPIEDAD ({ get; set; }),
    //  nunca campo público. Database.SqlQuery<T> ignora los campos en silencio —
    //  no lanza ninguna excepción, simplemente deja todos los valores en su
    //  default y la pantalla sale llena de ceros.
    //
    //  Los nombres tienen que calzar exactamente con los alias del cursor
    //  (AS TotalItems, AS DiasCobertura...). El mapeo es case-insensitive pero
    //  NO convierte snake_case a PascalCase.

    /// <summary>Fila única con los números grandes de la parte de arriba.</summary>
    public class KpiInventario
    {
        public int TotalItems { get; set; }
        public int TotalUnidades { get; set; }
        public int ItemsAgotados { get; set; }
        public int ItemsBajoMinimo { get; set; }
        public int MovimientosHoy { get; set; }
    }

    /// <summary>Item que hay que reponer, con la proyección de cuándo se acaba.</summary>
    public class ItemReposicion
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public int StockMinimo { get; set; }
        public string UnidadMedida { get; set; }

        /// <summary>Promedio de unidades que salen por día en la ventana analizada.</summary>
        public decimal ConsumoDiario { get; set; }

        //  Nullable a propósito: NULL significa "no se puede proyectar porque
        //  este item no tiene salidas recientes", que NO es lo mismo que cero
        //  días de cobertura. Un int normal aplastaría esa diferencia y la
        //  pantalla diría "se agota hoy" de algo que nadie usa.
        public int? DiasCobertura { get; set; }
        public DateTime? FechaAgotamiento { get; set; }

        /// <summary>Etiqueta de urgencia que consume la vista.</summary>
        public string Urgencia
        {
            get
            {
                if (Cantidad == 0) return "agotado";
                if (DiasCobertura.HasValue && DiasCobertura.Value <= 3) return "critico";
                if (Cantidad <= StockMinimo) return "bajo";
                return "atencion";
            }
        }
    }

    /// <summary>Item dentro del top de más movidos, desglosado en entradas y salidas.</summary>
    public class ItemMovido
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int TotalMovido { get; set; }
        public int TotalEntradas { get; set; }
        public int TotalSalidas { get; set; }
        public int NumMovimientos { get; set; }
    }

    /// <summary>Item clasificado por Pareto (A = el 20% que concentra el 80% del movimiento).</summary>
    public class ItemAbc
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Volumen { get; set; }
        public decimal PorcentajeIndividual { get; set; }
        public decimal PorcentajeAcumulado { get; set; }
        public string Clase { get; set; }
    }

    /// <summary>Entrada del feed de auditoría global de la home.</summary>
    public class MovimientoBitacora
    {
        public int Id { get; set; }
        public int IdItem { get; set; }
        public string CodigoItem { get; set; }
        public string NombreItem { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }
        public string Detalle { get; set; }
    }

    /// <summary>
    /// Todo lo que la vista Home/Index necesita, en un solo objeto.
    /// Mismo principio que DetalleItemViewModel: cuando una pantalla combina
    /// datos de varias fuentes, se arma un ViewModel en lugar de forzar a un
    /// Model existente a cargar información que no le corresponde.
    /// </summary>
    public class DashboardViewModel
    {
        public KpiInventario Kpis { get; set; }
        public List<ItemReposicion> Reposicion { get; set; }
        public List<ItemMovido> MasMovidos { get; set; }
        public List<ItemAbc> Abc { get; set; }
        public List<MovimientoBitacora> Bitacora { get; set; }

        /// <summary>Ventana de días usada en los cálculos (para rotularla en la vista).</summary>
        public int DiasVentana { get; set; }

        //  Flags de permisos resueltos en el Controller. La vista NO consulta
        //  permisos por su cuenta: pregunta por estos booleanos. Así la regla de
        //  "quién ve qué" vive en un solo lugar.
        public bool PuedeVerBitacora { get; set; }
        public bool PuedeRegistrarMovimiento { get; set; }

        public DashboardViewModel()
        {
            // Listas vacías, nunca null: así la vista puede hacer .Count sin
            // comprobar null en cada widget.
            Kpis = new KpiInventario();
            Reposicion = new List<ItemReposicion>();
            MasMovidos = new List<ItemMovido>();
            Abc = new List<ItemAbc>();
            Bitacora = new List<MovimientoBitacora>();
            DiasVentana = 30;
        }
    }
}
