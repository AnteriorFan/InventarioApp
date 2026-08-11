using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IPermisoService
    {
        bool UsuarioTienePermiso(string usuarioLogin, string permiso);
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
            var usuario = _usuarioRepository.ObtenerPorLogin(usuarioLogin);
            if (usuario == null) return false;

            var permisos = _permisoRepository.ObtenerPorUsuario(usuario.Id);
            return permisos.Contains(permiso);
        }
    }
}
