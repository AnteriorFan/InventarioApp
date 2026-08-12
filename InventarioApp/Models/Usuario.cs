namespace InventarioApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string UsuarioLogin { get; set; }
        public string PasswordHash { get; set; }

        //  Campos que solo llena pkg_usuarios.sp_listar_admin / sp_obtener_por_id,
        //  para la pantalla de administración de usuarios.
        //
        //  sp_obtener_por_login (el del login) NO los devuelve, y está bien:
        //  Database.SqlQuery deja en su default las propiedades que no vengan en
        //  el cursor, y nadie los lee durante la autenticación. La alternativa
        //  sería un modelo aparte, pero es la misma denormalización que ya hacen
        //  Item.NombreCategoria o Activo.NombreResponsable.
        //
        //  IdRol es nullable porque la columna se agregó con ALTER TABLE después
        //  de que ya había usuarios: puede haber gente sin rol asignado.
        public int? IdRol { get; set; }
        public string NombreRol { get; set; }
        public int NumExcepciones { get; set; }

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }
    }
}
