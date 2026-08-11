using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IMovimientoService
    {
        void Registrar(int idItem, string tipoMovimiento, int cantidad, string observaciones);
        List<MovimIentosinventario> ObtenerPorItem(int idItem);
    }

    public class MovimientoService : IMovimientoService
    {
        private readonly IMovimientosRepository _movimientoRepository;

        public MovimientoService() : this(new MovimientoRepository()) { }
        public MovimientoService(IMovimientosRepository movimientoRepository)
        {
            _movimientoRepository = movimientoRepository;
        }

        public void Registrar(int idItem, string tipoMovimiento, int cantidad, string observaciones)
        {
            _movimientoRepository.RegistrarMovimiento(idItem, tipoMovimiento, cantidad, observaciones);
        }

        public List<MovimIentosinventario> ObtenerPorItem(int idItem)
        {
            return _movimientoRepository.ObtenerMovimientosPorItem(idItem);
        }
    }
}
