namespace InventarioApp.Models
{
    public class TipoMovimiento
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        //  Estas dos banderas convierten al catálogo en la fuente de la regla,
        //  en vez de tenerla escrita en el código:
        //
        //    "una Baja exige que se explique por qué"
        //    "un Reporte de daño exige foto de evidencia"
        //
        //  Marcarlas desde la pantalla de Catálogos basta para que el formulario
        //  y el procedure empiecen a exigirlo. No hay que recompilar nada.
        //
        //  Llegan como 'S'/'N' porque Oracle no tiene BOOLEAN en SQL; el mapeo a
        //  bool se hace aquí, igual que en PermisoDeRol.
        public string RequiereMotivoFlag { get; set; }
        public string RequiereImagenFlag { get; set; }

        public bool RequiereMotivo
        {
            get { return RequiereMotivoFlag == "S"; }
            set { RequiereMotivoFlag = value ? "S" : "N"; }
        }

        public bool RequiereImagen
        {
            get { return RequiereImagenFlag == "S"; }
            set { RequiereImagenFlag = value ? "S" : "N"; }
        }
    }
}
