using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IEstadoService
    {
        List<Estado> ObtenerTodos();
        int Crear(Estado estado);
        void Actualizar(Estado estado);
        void Eliminar(int id);
    }

    public class EstadoService : IEstadoService
    {
        private readonly IEstadoRepository _estadoRepository;
        public EstadoService() : this(new EstadoRepository()) { }
        public EstadoService(IEstadoRepository estadoRepository) { _estadoRepository = estadoRepository; }

        public List<Estado> ObtenerTodos() => _estadoRepository.Listar();
        public int Crear(Estado estado) => _estadoRepository.Insertar(estado);
        public void Actualizar(Estado estado) => _estadoRepository.Actualizar(estado);
        public void Eliminar(int id) => _estadoRepository.Eliminar(id);
    }
}
