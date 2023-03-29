using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EPM.Mouser.Interview.Models;
using EPM.Mouser.Interview.Data;

namespace EPM.Mouser.Interview.Web.Controllers
{
    
    public class HomeController : Controller
    {

        private readonly WarehouseApi _warehouseApi;
        public HomeController( WarehouseApi warehouseApi)
        {
            _warehouseApi = warehouseApi;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Products"] = await _warehouseApi.GetProductList();
            return View();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(long id)
        { 
            ViewData["Details"] = await _warehouseApi.GetProduct(id);
            return View();
        }

        [HttpGet("Stock")]
        public async Task<IActionResult> Stock(long id)
        {
            ViewData["instock"] = await _warehouseApi.GetPublicInStockProducts();
            return View();
        }

    }
}
