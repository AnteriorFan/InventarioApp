using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface ITipoMovimientoService
    {
        List<TipoMovimiento> ObtenerTodos();
        int Crear(TipoMovimiento tipo);
        void Actualizar(TipoMovimiento tipo);
        void Eliminar(int id);
    }

    public class TipoMovimientoService : ITipoMovimientoService
    {
        private readonly ITipoMovimientoRepository _tipoMovimientoRepository;
        public TipoMovimientoService() : this(new TipoMovimientoRepository()) { }
        public TipoMovimientoService(ITipoMovimientoRepository tipoMovimientoRepository) { _tipoMovimientoRepository = tipoMovimientoRepository; }

        public List<TipoMovimiento> ObtenerTodos() => _tipoMovimientoRepository.Listar();
        public int Crear(TipoMovimiento tipo) => _tipoMovimientoRepository.Insertar(tipo);
        public void Actualizar(TipoMovimiento tipo) => _tipoMovimientoRepository.Actualizar(tipo);
        public void Eliminar(int id) => _tipoMovimientoRepository.Eliminar(id);
    }
}
