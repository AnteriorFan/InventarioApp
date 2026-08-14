using InventarioApp.Models;
using InventarioApp.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace InventarioApp.Services
{
    public interface IMovimientoActivoService
    {
        int Registrar(RegistrarMovimientoViewModel datos, string imagenKey, int idUsuario);
        List<MovimientoActivo> ObtenerPorActivo(int idActivo);
        List<MovimientoActivo> ObtenerRecientes(int limite);

        /// <summary>
        /// Devuelve el tipo elegido para poder consultar sus reglas
        /// (requiere motivo / requiere imagen) antes de intentar guardar.
        /// </summary>
        TipoMovimiento ObtenerTipo(int idTipoMovimiento);
    }

    public class MovimientoActivoService : IMovimientoActivoService
    {
        private readonly IMovimientoActivoRepository _movimientoRepository;
        private readonly ITipoMovimientoRepository _tipoRepository;

        public MovimientoActivoService() : this(new MovimientoActivoRepository(), new TipoMovimientoRepository()) { }

        public MovimientoActivoService(IMovimientoActivoRepository movimientoRepository, ITipoMovimientoRepository tipoRepository)
        {
            _movimientoRepository = movimientoRepository;
            _tipoRepository = tipoRepository;
        }

        public int Registrar(RegistrarMovimientoViewModel datos, string imagenKey, int idUsuario)
        {
            return _movimientoRepository.Registrar(datos, imagenKey, idUsuario);
        }

        public List<MovimientoActivo> ObtenerPorActivo(int idActivo) => _movimientoRepository.ListarPorActivo(idActivo);
        public List<MovimientoActivo> ObtenerRecientes(int limite) => _movimientoRepository.ListarRecientes(limite);

        //  No hay un sp_obtener_por_id para tipos: el catálogo es de seis o siete
        //  filas y ya viene entero en sp_listar. Filtrarlo en memoria evita un
        //  procedure más para algo que cabe en una pantalla.
        public TipoMovimiento ObtenerTipo(int idTipoMovimiento)
        {
            return _tipoRepository.Listar().FirstOrDefault(t => t.Id == idTipoMovimiento);
        }
    }
}
