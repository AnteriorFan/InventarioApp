using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IEdificioService
    {
        List<Edificio> ObtenerTodos();
        int Crear(Edificio edificio, int idUsuario);
        void Actualizar(Edificio edificio, int idUsuario);
        void Eliminar(int id);
    }

    public class EdificioService : IEdificioService
    {
        private readonly IEdificioRepository _edificioRepository;

        public EdificioService() : this(new EdificioRepository()) { }
        public EdificioService(IEdificioRepository edificioRepository)
        {
            _edificioRepository = edificioRepository;
        }

        public List<Edificio> ObtenerTodos() => _edificioRepository.Listar();
        public int Crear(Edificio edificio, int idUsuario) => _edificioRepository.Insertar(edificio, idUsuario);
        public void Actualizar(Edificio edificio, int idUsuario) => _edificioRepository.Actualizar(edificio, idUsuario);
        public void Eliminar(int id) => _edificioRepository.Eliminar(id);
    }
}
