using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using EPM.Mouser.Interview.Models;
using EPM.Mouser.Interview.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EPM.Mouser.Interview.Web.Controllers
{
    [Route("api/warehouse")]
    public class WarehouseApi : Controller
    {
        private readonly IWarehouseRepository _warehouseRepository;
        public WarehouseApi(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository;
        }


        [HttpGet]
        [Route("")]
        public async Task<List<Product>> GetProductList()
        {
            var products = await _warehouseRepository.List();
            if (products == null)
            {
                throw new Exception("Id is not Found");
            }
            return products;
        }
        /*
         *  Action: GET
         *  Url: api/warehouse/id
         *  This action should return a single product for an IdS
         */
        [HttpGet]
        [Route("{id}")]
        public async Task<Product> GetProduct(long id)
        {
            var product = await _warehouseRepository.Get(id);
            if (product == null)
            {
                throw new Exception("Id is not Found");
            }
            return product;
        }

        /*
         *  Action: GET
         *  Url: api/warehouse
         *  This action should return a collection of products in stock
         *  In stock means In Stock Quantity is greater than zero and In Stock Quantity is greater than the Reserved Quantity
         */
        [HttpGet]
        [Route("InStock")]
        public async Task<List<Product>> GetPublicInStockProducts()
        {
            var products = await _warehouseRepository.List();
            var availableProducts =  await _warehouseRepository.Query(products => products.InStockQuantity > 0 && products.InStockQuantity > products.ReservedQuantity);
            return availableProducts;
        }


        /*
         *  Action: GET
         *  Url: api/warehouse/order
         *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
         *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
         *       {
         *           "id": 1,
         *           "quantity": 1
         *       }
         *
         *  This action should increase the Reserved Quantity for the product requested by the amount requested
         *
         *  This action should return failure (success = false) when:
         *     - ErrorReason.NotEnoughQuantity when: The quantity being requested would increase the Reserved Quantity to be greater than the In Stock Quantity.
         *     - ErrorReason.QuantityInvalid when: A negative number was requested
         *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpPost]
        [Route("orderItem")]
        public async Task<JsonResult> OrderItem([FromBody] UpdateQuantityRequest request)
        {
            var product = await _warehouseRepository.Get(request.Id);
            if (request.Quantity < 0)
            {
                if (product != null)
                {
                    product.ReservedQuantity += request.Quantity;
                    if (product.ReservedQuantity > product.InStockQuantity)
                    {
                        await _warehouseRepository.UpdateQuantities(product);
                        return Json(new UpdateResponse()
                        {
                            Success = true
                        });
                        
                    }
                    return Json(BadRequest(ErrorReason.NotEnoughQuantity));
                }
               
              return Json(BadRequest(ErrorReason.InvalidRequest));
               
            }
            return Json(BadRequest(ErrorReason.QuantityInvalid));
    }

        /*
         *  Url: api/warehouse/ship
         *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
         *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
         *       {
         *           "id": 1,
         *           "quantity": 1
         *       }
         *
         *
         *  This action should:
         *     - decrease the Reserved Quantity for the product requested by the amount requested to a minimum of zero.
         *     - decrease the In Stock Quantity for the product requested by the amount requested
         *
         *  This action should return failure (success = false) when:
         *     - ErrorReason.NotEnoughQuantity when: The quantity being requested would cause the In Stock Quantity to go below zero.
         *     - ErrorReason.QuantityInvalid when: A negative number was requested
         *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpPost]
        [Route("shipItem")]
        public async Task<JsonResult> ShipItem([FromBody] UpdateQuantityRequest request)
        {
            var product = await _warehouseRepository.Get(request.Id);
            if (request.Quantity < 0)
            {
                if (product != null)
                {
                    var maxRerserved = product.ReservedQuantity - request.Quantity;
                    var maxInstock = product.InStockQuantity - request.Quantity;
                    product.ReservedQuantity = Math.Max(maxRerserved, 0);
                    product.InStockQuantity = Math.Max(maxInstock, 0);
                    if (product.InStockQuantity < 0)
                    {
                        await _warehouseRepository.UpdateQuantities(product);
                        return Json(new UpdateResponse()
                        {
                            Success = true
                        });

                    }
                    return Json(BadRequest(ErrorReason.NotEnoughQuantity));
                }
                
                return Json(BadRequest(ErrorReason.InvalidRequest));
            }
            return Json(BadRequest(ErrorReason.QuantityInvalid));

        }

        /*
        *  Url: api/warehouse/restock
        *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
        *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
        *       {
        *           "id": 1,
        *           "quantity": 1
        *       }
        *
        *
        *  This action should:
        *     - increase the In Stock Quantity for the product requested by the amount requested
        *
        *  This action should return failure (success = false) when:
        *     - ErrorReason.QuantityInvalid when: A negative number was requested
        *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpPost()]
        [Route("restock")]
        public async Task<JsonResult> RestockItem([FromBody] UpdateQuantityRequest request)
        {
            var product = await _warehouseRepository.Get(request.Id);
            if (request.Quantity < 0)
            {
                if (product != null)
                {
                    product.InStockQuantity += request.Quantity;
                    await _warehouseRepository.UpdateQuantities(product);
                    return Json(new UpdateResponse()
                    {
                        Success = true
                    });

                }
               
                return Json(BadRequest(ErrorReason.InvalidRequest));

            }
            return Json(BadRequest(ErrorReason.QuantityInvalid));

        }

        /*
        *  Url: api/warehouse/add
        *  This action should return a EPM.Mouser.Interview.Models.CreateResponse<EPM.Mouser.Interview.Models.Product>
        *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.Product in JSON format in the body of the request
        *       {
        *           "id": 1,
        *           "inStockQuantity": 1,
        *           "reservedQuantity": 1,
        *           "name": "product name"
        *       }
        *
        *
        *  This action should:
        *     - create a new product with:
        *          - The requested name - But forced to be unique - see below
        *          - The requested In Stock Quantity
        *          - The Reserved Quantity should be zero
        *
        *       UNIQUE Name requirements
        *          - No two products can have the same name
        *          - Names should have no leading or trailing whitespace before checking for uniqueness
        *          - If a new name is not unique then append "(x)" to the name [like windows file system does, where x is the next avaiable number]
        *
        *
        *  This action should return failure (success = false) and an empty Model property when:
        *     - ErrorReason.QuantityInvalid when: A negative number was requested for the In Stock Quantity
        *     - ErrorReason.InvalidRequest when: A blank or empty name is requested
        */
        [HttpPost()]
        [Route("add")]
        public async Task<JsonResult> AddNewProduct([FromBody] Product requestData)
        {

            if (requestData.InStockQuantity > 0)
            {
                var products = await _warehouseRepository.List();
                var nameTrim = requestData.Name.Trim();
                var duplicateExists = products.Where(y => y.Name == nameTrim).FirstOrDefault();
                if (duplicateExists != null)
                {
                    string duplicateName = duplicateExists.Name;
                    bool result = Regex.Match(duplicateName, @"^.*?\([^\d]*(\d+)[^\d]*\).*$").Success;
                    int i = result ? int.Parse(Regex.Match(duplicateName, @"\(([^()]+)\)*").Groups[1].Value) : 1;
                    requestData.Name = result ? Regex.Replace(duplicateName, @"\((.*?)\)", string.Empty) + $"({(i + 1)})": requestData.Name + $"({(i)})";
                }
                
             if (string.IsNullOrWhiteSpace(nameTrim) || requestData.ReservedQuantity == 0)
                {
                    var result = await _warehouseRepository.Insert(requestData);
                    return Json(new CreateResponse<Product>()
                    {
                        Success = true
                    });

                }
                else
                {
                    return Json(BadRequest(ErrorReason.InvalidRequest));
                }
            }
            return Json(BadRequest(ErrorReason.QuantityInvalid));
        }
    }
}
