namespace InventarioApp.Models
{
    public class Permiso
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        /// <summary>
        /// Prefijo del nombre: de "ITEMS_CREAR" saca "ITEMS".
        /// Sirve para agrupar la matriz por módulo en la vista, en vez de
        /// presentar 14 checkboxes sueltos en una lista plana.
        /// </summary>
        public string Modulo
        {
            get
            {
                if (string.IsNullOrEmpty(Nombre)) return "OTROS";

                int guion = Nombre.IndexOf('_');
                return guion > 0 ? Nombre.Substring(0, guion) : Nombre;
            }
        }

        /// <summary>
        /// La parte de la acción: de "ITEMS_CREAR" saca "CREAR".
        /// </summary>
        public string Accion
        {
            get
            {
                if (string.IsNullOrEmpty(Nombre)) return "";

                int guion = Nombre.IndexOf('_');
                return guion > 0 ? Nombre.Substring(guion + 1).Replace('_', ' ') : Nombre;
            }
        }
    }
}
