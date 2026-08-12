using InventarioApp.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace InventarioApp.Repositories
{
    public interface IRolRepository
    {
        List<Rol> Listar();
        Rol ObtenerPorId(int id);
        int Insertar(Rol rol);
        void Actualizar(Rol rol);
        void Eliminar(int id);

        List<PermisoDeRol> ObtenerPermisos(int idRol);
        void GuardarPermisos(int idRol, IEnumerable<int> idsPermisos);
    }

    public class RolRepository : IRolRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        public List<Rol> Listar()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Rol>("BEGIN pkg_roles.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public Rol ObtenerPorId(int id)
        {
            var pId = new OracleParameter("p_id_rol", OracleDbType.Int32) { Value = id };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Rol>(
                    "BEGIN pkg_roles.sp_obtener_por_id(:p_id_rol, :p_cursor); END;",
                    pId, pCursor).FirstOrDefault();
            }
        }

        public int Insertar(Rol rol)
        {
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = rol.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)rol.Descripcion ?? DBNull.Value };
            var pIdOut = new OracleParameter("p_id_rol_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_roles.sp_registrar(:p_nombre, :p_descripcion, :p_id_rol_out); END;",
                    pNombre, pDescripcion, pIdOut);

                // .ToString() antes de convertir: el OUT llega como OracleDecimal,
                // que NO implementa IConvertible y revienta en Convert.ToInt32.
                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Rol rol)
        {
            var pId = new OracleParameter("p_id_rol", OracleDbType.Int32) { Value = rol.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = rol.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)rol.Descripcion ?? DBNull.Value };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_roles.sp_modificar(:p_id_rol, :p_nombre, :p_descripcion); END;",
                    pId, pNombre, pDescripcion);
            }
        }

        public void Eliminar(int id)
        {
            var pId = new OracleParameter("p_id_rol", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_roles.sp_eliminar(:p_id_rol); END;", pId);
            }
        }

        public List<PermisoDeRol> ObtenerPermisos(int idRol)
        {
            var pId = new OracleParameter("p_id_rol", OracleDbType.Int32) { Value = idRol };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<PermisoDeRol>(
                    "BEGIN pkg_roles.sp_obtener_permisos(:p_id_rol, :p_cursor); END;",
                    pId, pCursor).ToList();
            }
        }

        //  La lista de ids viaja como texto ("3,7,11") y el procedure la parte
        //  en filas. Es UNA llamada en vez de N, y sobre todo es atómica: el
        //  DELETE y los INSERT viven o mueren juntos.
        public void GuardarPermisos(int idRol, IEnumerable<int> idsPermisos)
        {
            string csv = idsPermisos == null ? null : string.Join(",", idsPermisos);

            var pId = new OracleParameter("p_id_rol", OracleDbType.Int32) { Value = idRol };
            var pIds = new OracleParameter("p_ids_permisos", OracleDbType.Varchar2)
            {
                // Cadena vacía y NULL son lo mismo en Oracle, pero mandar
                // DBNull explícito deja clara la intención: "sin permisos".
                Value = string.IsNullOrEmpty(csv) ? (object)DBNull.Value : csv
            };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_roles.sp_guardar_permisos(:p_id_rol, :p_ids_permisos); END;",
                    pId, pIds);
            }
        }
    }
}
