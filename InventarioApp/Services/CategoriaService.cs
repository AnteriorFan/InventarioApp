using System.Collections.Generic;
using System.Linq;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface ICategoriaService
    {
        List<Categoria> ObtenerTodas();
        Categoria ObtenerPorId(int id);
        int Crear(Categoria categoria);
        void Actualizar(Categoria categoria);
        void Eliminar(int id);
    }

    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriaService() : this(new CategoriaRepository()) { }
        public CategoriaService(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public List<Categoria> ObtenerTodas()
        {
            return _categoriaRepository.Listar();
        }

        //  Sin procedure dedicado: el catálogo son unas cuantas filas y ya vienen
        //  todas en sp_listar. Mismo criterio que MarcasController.Edit.
        public Categoria ObtenerPorId(int id)
        {
            return _categoriaRepository.Listar().FirstOrDefault(c => c.Id == id);
        }

        public int Crear(Categoria categoria) => _categoriaRepository.Insertar(categoria);
        public void Actualizar(Categoria categoria) => _categoriaRepository.Actualizar(categoria);
        public void Eliminar(int id) => _categoriaRepository.Eliminar(id);
    }
}
