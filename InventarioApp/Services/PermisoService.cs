using InventarioApp.Repositories;
using System.Collections.Generic;

namespace InventarioApp.Services
{
    public interface IPermisoService
    {
        bool UsuarioTienePermiso(string usuarioLogin, string permiso);

        /// <summary>
        /// Todos los permisos efectivos del usuario, de un jalón.
        /// Para cuando hay que preguntar por varios (el navbar), en vez de
        /// llamar UsuarioTienePermiso() una vez por cada uno.
        /// </summary>
        List<string> ObtenerDeUsuario(string usuarioLogin);
    }

    public class PermisoService : IPermisoService
    {
        private readonly IPermisoRepository _permisoRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public PermisoService() : this(new PermisoRepository(), new UsuarioRepository()) { }
        public PermisoService(IPermisoRepository permisoRepository, IUsuarioRepository usuarioRepository)
        {
            _permisoRepository = permisoRepository;
            _usuarioRepository = usuarioRepository;
        }

        public bool UsuarioTienePermiso(string usuarioLogin, string permiso)
        {
            return ObtenerDeUsuario(usuarioLogin).Contains(permiso);
        }

        public List<string> ObtenerDeUsuario(string usuarioLogin)
        {
            var usuario = _usuarioRepository.ObtenerPorLogin(usuarioLogin);

            // Lista vacía, no null: el que llama puede hacer .Contains() sin
            // comprobar nada.
            if (usuario == null) return new List<string>();

            return _permisoRepository.ObtenerPorUsuario(usuario.Id);
        }
    }
}
