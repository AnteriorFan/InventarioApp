namespace InventarioApp.Models
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        //  Contadores que arma pkg_roles.sp_listar con dos subconsultas.
        //
        //  Se calculan en Oracle, no en C#, porque hacerlo aca significaria una
        //  consulta extra POR CADA rol de la lista (el clasico problema N+1):
        //  6 roles = 13 viajes a la base en vez de 1.
        public int NumPermisos { get; set; }
        public int NumUsuarios { get; set; }
    }
}
