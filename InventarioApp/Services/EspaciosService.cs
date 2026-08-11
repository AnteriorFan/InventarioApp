using InventarioApp.Models;
using InventarioApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Services
{
    public interface IEspacioService
    {
        List<Espacio> ObtenerTodos();
        int Crear(Espacio area, int idUsuario);
        void Actualizar(Espacio area, int idUsuario);
        void Eliminar(int id);
    }

    public class EspaciosService : IEspacioService
    {
        private readonly IEspacioRepository _EspacioRepository;

        public EspaciosService() : this(new EspacioRepository()) { }

        public EspaciosService(IEspacioRepository espacioRepository)
        {
            _EspacioRepository = espacioRepository;
        }

        public List<Espacio> ObtenerTodos() => _EspacioRepository.Listar();
        public int Crear(Espacio area, int idUsuario) => _EspacioRepository.Insertar(area, idUsuario);
        public void Actualizar(Espacio area, int idUsuario) => _EspacioRepository.Actualizar(area, idUsuario);
        public void Eliminar(int id) => _EspacioRepository.Eliminar(id);
        }
}