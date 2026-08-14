using InventarioApp.Models;
using InventarioApp.Repositories;
using System.Collections.Generic;

namespace InventarioApp.Services
{
    public interface ICodigoService
    {
        string Regenerar(int idActivo, string motivo, int idUsuario);
        void MarcarEtiquetaImpresa(int idActivo);
        List<Activo> ObtenerEtiquetasPendientes();
    }

    public class CodigoService : ICodigoService
    {
        private readonly ICodigoRepository _codigoRepository;

        public CodigoService() : this(new CodigoRepository()) { }

        public CodigoService(ICodigoRepository codigoRepository)
        {
            _codigoRepository = codigoRepository;
        }

        public string Regenerar(int idActivo, string motivo, int idUsuario)
            => _codigoRepository.Regenerar(idActivo, motivo, idUsuario);

        public void MarcarEtiquetaImpresa(int idActivo)
            => _codigoRepository.MarcarEtiquetaImpresa(idActivo);

        public List<Activo> ObtenerEtiquetasPendientes()
            => _codigoRepository.ListarEtiquetasPendientes();
    }
}
