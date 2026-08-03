using InventarioApp.Models;
using InventarioApp.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc;

namespace InventarioApp.Controllers 
{
    public class ItemsController : Controller // Controlador que hereda de Controller (ASP.NET MVC)
    {
        private readonly IItemService _itemService; // Referencia al servicio (solo lectura)

        // Constructor 1: Constructor sin parámetros
        // Cuando se crea sin parámetros, crea automáticamente un nuevo ItemService
        public ItemsController() : this(new ItemService()) { }

        public ActionResult Create() // Acción que se ejecuta cuando accedes a /Items/Create
        {
            return View();
        }

        // Constructor 2: Constructor con parámetro(inyección de dependencia)
        public ItemsController(IItemService itemService)
        {
            _itemService = itemService; // Asigna el servicio recibido
        }

        // Acción que se ejecuta cuando accedes a /Items/Index
        public ActionResult Index()
        {
            var items = _itemService.ObtenerTodos();  // Obtiene todos los items del servicio
            return View(items); // Envía los items a la vista para mostrarlos
        }

        [HttpPost] // Indica que esta acción responde a solicitudes POST
        [ValidateAntiForgeryToken] // Protege contra ataques CSRF - CSRF (Cross-Site Request Forgery) que es un tipo de ataque que ocurre cuando un atacante engaña a un usuario autenticado para que realice acciones no deseadas en una aplicación web en la que está autenticado.
        public ActionResult Create(Item nuevoItem)
        {
            _itemService.Crear(nuevoItem); // Llama al servicio para crear un nuevo item
            return RedirectToAction("Index"); // Redirige a la acción Index después de crear el item
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var item = _itemService.ObtenerPorId(id); // Llama al servicio para obtener un item por su ID
            return View(item); // Redirige a la vista "Item" después de obtener el item
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Item item)
        {
            _itemService.Actualizar(item); // Llama al servicio para actualizar un item existente
            return RedirectToAction("Index"); // Redirige a la acción Index después de actualizar el item
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete()
        {
            int id = Convert.ToInt32(Request.Form["id"]); // Obtiene el ID del item a eliminar desde el formulario
            _itemService.Eliminar(id); // Llama al servicio para eliminar el item por su ID
            return RedirectToAction("Index"); // Redirige a la acción Index después de eliminar el item
        }
    }
}