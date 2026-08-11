using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IActivoService
    {
        List<Activo> ObtenerTodos();
        Activo ObtenerPorId(int id);
        Activo ObtenerPorCodigo(string codigo);
        int Crear(Activo activo, int idUsuario);
        void Actualizar(Activo activo, int idUsuario);
        void Eliminar(int id);
    }

    public class ActivoService : IActivoService
    {
        private readonly IActivoRepository _activoRepository;
        public ActivoService() : this(new ActivoRepository()) { }
        public ActivoService(IActivoRepository activoRepository) { _activoRepository = activoRepository; }

        public List<Activo> ObtenerTodos() => _activoRepository.Listar();
        public Activo ObtenerPorId(int id) => _activoRepository.ObtenerPorId(id);
        public Activo ObtenerPorCodigo(string codigo) => _activoRepository.ObtenerPorCodigo(codigo);
        public int Crear(Activo activo, int idUsuario) => _activoRepository.Insertar(activo, idUsuario);
        public void Actualizar(Activo activo, int idUsuario) => _activoRepository.Actualizar(activo, idUsuario);
        public void Eliminar(int id) => _activoRepository.Eliminar(id);
    }
}
