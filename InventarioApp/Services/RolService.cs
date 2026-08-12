using InventarioApp.Models;
using InventarioApp.Repositories;
using System.Collections.Generic;

namespace InventarioApp.Services
{
    public interface IRolService
    {
        List<Rol> ObtenerTodos();
        Rol ObtenerPorId(int id);
        int Crear(Rol rol);
        void Actualizar(Rol rol);
        void Eliminar(int id);

        RolPermisosViewModel ObtenerConPermisos(int idRol);
        void GuardarPermisos(int idRol, IEnumerable<int> idsPermisos);
    }

    public class RolService : IRolService
    {
        private readonly IRolRepository _rolRepository;

        public RolService() : this(new RolRepository()) { }

        public RolService(IRolRepository rolRepository)
        {
            _rolRepository = rolRepository;
        }

        public List<Rol> ObtenerTodos() => _rolRepository.Listar();
        public Rol ObtenerPorId(int id) => _rolRepository.ObtenerPorId(id);
        public int Crear(Rol rol) => _rolRepository.Insertar(rol);
        public void Actualizar(Rol rol) => _rolRepository.Actualizar(rol);
        public void Eliminar(int id) => _rolRepository.Eliminar(id);

        public RolPermisosViewModel ObtenerConPermisos(int idRol)
        {
            var rol = _rolRepository.ObtenerPorId(idRol);
            if (rol == null) return null;

            return new RolPermisosViewModel
            {
                Rol = rol,
                Permisos = _rolRepository.ObtenerPermisos(idRol)
            };
        }

        public void GuardarPermisos(int idRol, IEnumerable<int> idsPermisos)
        {
            _rolRepository.GuardarPermisos(idRol, idsPermisos);
        }
    }
}
