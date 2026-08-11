using InventarioApp.Models;
using InventarioApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Services
{
    public interface IHistorialService
    {
        void Registrar(int idItem, int idUsuario, string accion, string detalle);
        List<HistorialItem> ObtenerPorItem(int idItem);
    }

    public class HistorialService : IHistorialService
    {
        private readonly IHistorialRepository _historialRepository;

        public HistorialService() : this(new HistorialRepository()) { }
        public HistorialService(IHistorialRepository historialRepository)
        {
            _historialRepository = historialRepository;
        }

        public void Registrar(int idItem, int idUsuario, string accion, string detalle)
        {
            _historialRepository.Registrar(idItem, idUsuario, accion, detalle);
        }

        public List<HistorialItem> ObtenerPorItem(int idItem)
        {
            return _historialRepository.ListarPorItem(idItem);
        }
    }

}