using InventarioApp.Models;
using InventarioApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Services
{
    public interface IAreaService
    {
        List<Area> ObtenerTodos();
        int Crear(Area edificio, int idUsuario);
        void Actualizar(Area edificio, int idUsuario);
        void Eliminar(int id);
    } 
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _AreaRepository;

        public AreaService() : this(new AreaRepository()) { }
        public AreaService(IAreaRepository edificioRepository)
        {
            _AreaRepository = edificioRepository;
        }

        public List<Area> ObtenerTodos() => _AreaRepository.Listar();
        public int Crear(Area edificio, int idUsuario) => _AreaRepository.Insertar(edificio, idUsuario);
        public void Actualizar(Area edificio, int idUsuario) => _AreaRepository.Actualizar(edificio, idUsuario);
        public void Eliminar(int id) => _AreaRepository.Eliminar(id);
    }
}