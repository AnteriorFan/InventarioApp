using System;
using System.Collections.Generic;
using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IItemService // Interfaz que define el contrato del servicio
    {
        List<Item> ObtenerTodos(); // Método que obtiene todos los items
        int Crear(Item item); // Método que crea un nuevo item

        Item ObtenerPorId(int id); // Método que obtiene un item por su ID

        void Actualizar(Item item); // Método que actualiza un item existente

        void Eliminar(int id); // Método que elimina un item por su ID
    }

    public class ItemService : IItemService // Clase que implementa la interfaz
    {
        private readonly IItemRepository _itemRepository; // Referencia al repositorio (solo lectura)
        public ItemService() : this(new ItemRepository()) { }

        // Constructor: recibe la inyección de dependencia del repositorio
        public ItemService(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository; // Asigna el repositorio
        }

        // Implementa el método de la interfaz
        public List<Item> ObtenerTodos()
        {
            return _itemRepository.Listar(); // Llama al método Listar() del repositorio
        }

        public int Crear(Item item)
        {
            return _itemRepository.Insertar(item); // Llama al método Insertar() del repositorio
        }

        public Item ObtenerPorId(int id)
        {
            return _itemRepository.ObtenerPorId(id); // Llama al método ObtenerPorId() del repositorio
        }

        public void Actualizar(Item item)
        {
            _itemRepository.Actualizar(item); // Llama al método Actualizar() del repositorio
        }

        public void Eliminar(int id)
        {
           _itemRepository.Eliminar(id); // Llama al método Eliminar() del repositorio
        }

    }
}