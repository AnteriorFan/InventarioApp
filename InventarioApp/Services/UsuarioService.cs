using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using System.Collections.Generic;

namespace InventarioApp.Services
{
    public interface IUsuarioService
    {
        List<Usuario> ObtenerParaAdmin();
        Usuario ObtenerPorId(int id);
        void CambiarRol(int idUsuario, int? idRol);
        int Crear(CrearUsuarioViewModel datos);
        UsuarioPermisosViewModel ObtenerConPermisos(int idUsuario);
        void GuardarOverrides(int idUsuario, IEnumerable<int> idsConcedidos, IEnumerable<int> idsNegados);
    }

    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPermisoRepository _permisoRepository;

        public UsuarioService() : this(new UsuarioRepository(), new PermisoRepository()) { }

        public UsuarioService(IUsuarioRepository usuarioRepository, IPermisoRepository permisoRepository)
        {
            _usuarioRepository = usuarioRepository;
            _permisoRepository = permisoRepository;
        }

        public List<Usuario> ObtenerParaAdmin() => _usuarioRepository.ListarParaAdmin();
        public Usuario ObtenerPorId(int id) => _usuarioRepository.ObtenerPorId(id);
        public void CambiarRol(int idUsuario, int? idRol) => _usuarioRepository.CambiarRol(idUsuario, idRol);

        //  El hasheo se hace AQUÍ, en el Service, no en el Controller ni en el
        //  Repository:
        //
        //    - En el Controller sería lógica de negocio metida en la capa que
        //      solo debería traducir HTTP.
        //    - En el Repository, la contraseña en claro tendría que cruzar toda
        //      la capa de datos para nada.
        //
        //  Así el Repository recibe un hash y ya; nadie más abajo llega a ver
        //  la contraseña real, y el algoritmo está en un solo lugar.
        public int Crear(CrearUsuarioViewModel datos)
        {
            string hash = PasswordHasher.Hash(datos.Password);

            return _usuarioRepository.Insertar(
                datos.Nombre.Trim(),
                datos.Apellido.Trim(),
                datos.UsuarioLogin.Trim(),
                hash,
                datos.IdRol);
        }

        //  Este método es la razón de que exista UsuarioService: combina dos
        //  repositories (usuarios + permisos) en un solo objeto para la vista.
        //  Un Controller no debería andar juntando piezas de dos fuentes.
        public UsuarioPermisosViewModel ObtenerConPermisos(int idUsuario)
        {
            var usuario = _usuarioRepository.ObtenerPorId(idUsuario);
            if (usuario == null) return null;

            return new UsuarioPermisosViewModel
            {
                Usuario = usuario,
                Permisos = _permisoRepository.ObtenerMatrizUsuario(idUsuario)
            };
        }

        public void GuardarOverrides(int idUsuario, IEnumerable<int> idsConcedidos, IEnumerable<int> idsNegados)
        {
            _permisoRepository.GuardarOverrides(idUsuario, idsConcedidos, idsNegados);
        }
    }
}
