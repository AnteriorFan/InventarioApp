using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IModeloService
    {
        List<Modelo> ObtenerTodos();
        int Crear(Modelo modelo, int idUsuario);
        void Actualizar(Modelo modelo, int idUsuario);
        void Eliminar(int id);
    }

    public class ModeloService : IModeloService
    {
        private readonly IModeloRepository _modeloRepository;
        public ModeloService() : this(new ModeloRepository()) { }
        public ModeloService(IModeloRepository modeloRepository) { _modeloRepository = modeloRepository; }

        public List<Modelo> ObtenerTodos() => _modeloRepository.Listar();
        public int Crear(Modelo modelo, int idUsuario) => _modeloRepository.Insertar(modelo, idUsuario);
        public void Actualizar(Modelo modelo, int idUsuario) => _modeloRepository.Actualizar(modelo, idUsuario);
        public void Eliminar(int id) => _modeloRepository.Eliminar(id);
    }
}
