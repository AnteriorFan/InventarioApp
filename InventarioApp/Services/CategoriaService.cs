using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface ICategoriaService
    {
        List<Categoria> ObtenerTodas();
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
    }
}
