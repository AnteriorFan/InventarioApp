using System.Collections.Generic;

namespace InventarioApp.Models
{
    //  Modelos de las pantallas de seguridad (Roles y Usuarios).
    //
    //  Mismo criterio que DashboardViewModel: van juntos en un archivo porque
    //  ninguno es una tabla, son la forma del RESULTADO de las consultas de
    //  pkg_roles / pkg_permisos y de lo que la vista necesita mostrar.
    //
    //  Recordatorio: todo tiene que ser PROPIEDAD ({ get; set; }).
    //  Database.SqlQuery<T> ignora los campos públicos en silencio.

    /// <summary>
    /// Un permiso del catálogo, más si el ROL lo tiene asignado o no.
    /// Es lo que devuelve pkg_roles.sp_obtener_permisos.
    /// </summary>
    public class PermisoDeRol : Permiso
    {
        //  Oracle no tiene BOOLEAN en SQL, así que el procedure manda 'S'/'N'
        //  (la misma convención de las columnas 'activo' del esquema). El mapeo
        //  a bool se hace aquí, no en la vista.
        public string AsignadoFlag { get; set; }

        public bool Asignado
        {
            get { return AsignadoFlag == "S"; }
        }
    }

    /// <summary>
    /// Un permiso del catálogo visto desde un USUARIO concreto: qué le da su
    /// rol, y qué excepción individual tiene encima.
    /// Es lo que devuelve pkg_permisos.sp_obtener_matriz_usuario.
    /// </summary>
    public class PermisoDeUsuario : Permiso
    {
        /// <summary>'S' si el rol del usuario ya trae este permiso.</summary>
        public string DelRolFlag { get; set; }

        /// <summary>'S' concedido a mano, 'N' negado a mano, null si no hay excepción.</summary>
        public string OverrideFlag { get; set; }

        public bool VieneDelRol
        {
            get { return DelRolFlag == "S"; }
        }

        //  Los tres estados que pinta la pantalla. Son excluyentes y cubren
        //  todos los casos, por eso son radios y no un checkbox:
        //
        //    HEREDA   -> sin excepción; hace lo que diga el rol (hoy y mañana)
        //    CONCEDER -> se le da, aunque el rol no lo traiga
        //    NEGAR    -> se le quita, aunque el rol sí lo traiga
        //
        //  La diferencia entre HEREDA y CONCEDER no es cosmética: si mañana le
        //  quitas el permiso al rol, el que estaba en HEREDA lo pierde y el que
        //  estaba en CONCEDER lo conserva.
        public const string EstadoHereda = "HEREDA";
        public const string EstadoConceder = "CONCEDER";
        public const string EstadoNegar = "NEGAR";

        public string Estado
        {
            get
            {
                if (OverrideFlag == "S") return EstadoConceder;
                if (OverrideFlag == "N") return EstadoNegar;
                return EstadoHereda;
            }
        }

        /// <summary>
        /// Lo que realmente puede hacer el usuario hoy, ya combinando rol +
        /// excepción. Es el mismo cálculo que hace
        /// pkg_permisos.sp_obtener_por_usuario, replicado aquí solo para
        /// mostrarlo en pantalla.
        /// </summary>
        public bool Efectivo
        {
            get
            {
                if (OverrideFlag == "N") return false;   // negar le gana al rol
                if (OverrideFlag == "S") return true;
                return VieneDelRol;
            }
        }
    }

    /// <summary>Pantalla "permisos de un rol".</summary>
    public class RolPermisosViewModel
    {
        public Rol Rol { get; set; }
        public List<PermisoDeRol> Permisos { get; set; }

        public RolPermisosViewModel()
        {
            Rol = new Rol();
            Permisos = new List<PermisoDeRol>();
        }
    }

    /// <summary>Pantalla "excepciones de un usuario".</summary>
    public class UsuarioPermisosViewModel
    {
        public Usuario Usuario { get; set; }
        public List<PermisoDeUsuario> Permisos { get; set; }

        public UsuarioPermisosViewModel()
        {
            Usuario = new Usuario();
            Permisos = new List<PermisoDeUsuario>();
        }
    }
}
