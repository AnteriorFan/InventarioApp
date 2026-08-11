using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Services
{
    public interface IAuthService
    {
        Usuario ValidarCredenciales(string login, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthService() : this(new UsuarioRepository()) { }
        public AuthService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public Usuario ValidarCredenciales(string login, string password)
        {
            var usuario = _usuarioRepository.ObtenerPorLogin(login);
            if (usuario == null) return null;

            return PasswordHasher.Verify(password, usuario.PasswordHash) ? usuario : null;
        }
    }
}