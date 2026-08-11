using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

        namespace InventarioApp.Models
    {
        public class Usuario
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string UsuarioLogin { get; set; }
            public string PasswordHash { get; set; }
        }
}