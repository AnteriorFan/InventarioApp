using InventarioApp.Models;
using InventarioApp.Repositories;
using System.Collections.Generic;

namespace InventarioApp.Services
{
    public interface IMarcaService
    {
        List<Marca> ObtenerTodas();
        int Crear(Marca marca, int idUsuario);
        void Actualizar(Marca marca, int idUsuario);
        void Eliminar(int id);
    }

    public class MarcaService : IMarcaService
    {
        private readonly IMarcaRepository _marcaRepository;
        public MarcaService() : this(new MarcaRepository()) { }
        public MarcaService(IMarcaRepository marcaRepository) { _marcaRepository = marcaRepository; }

        public List<Marca> ObtenerTodas() => _marcaRepository.Listar();
        public int Crear(Marca marca, int idUsuario) => _marcaRepository.Insertar(marca, idUsuario);
        public void Actualizar(Marca marca, int idUsuario) => _marcaRepository.Actualizar(marca, idUsuario);
        public void Eliminar(int id) => _marcaRepository.Eliminar(id);
    }
}
