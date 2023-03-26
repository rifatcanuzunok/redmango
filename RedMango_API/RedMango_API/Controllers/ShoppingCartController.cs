using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using System.Net;
using System.Xml;

namespace RedMango_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        protected ApiResponse _response;
        private readonly ApplicationDbContext _db;

        public ShoppingCartController(ApplicationDbContext db)
        {
            _response = new();
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
        {
            try
            {
                ShoppingCart shoppingCart;
                if (string.IsNullOrEmpty(userId))
                {
                    shoppingCart = new();
                }
                else
                {
                    shoppingCart = _db.ShoppingCarts
                    .Include(x => x.CartItems)
                    .ThenInclude(x => x.MenuItem)
                    .FirstOrDefault(x => x.UserId == userId);
                }
                 
                if (shoppingCart.CartItems != null && shoppingCart.CartItems.Count() > 0)
                {
                    shoppingCart.CartTotal = shoppingCart.CartItems.Sum(x=> x.Quantity * x.MenuItem.Price   );
                }
                _response.Result = shoppingCart;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);


            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString()};
                _response.StatusCode = HttpStatusCode.BadRequest;
            }
            return _response;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int menuItemId, int updateQuantityBy)
        {
            // Shopping cart will have one entry per user id even if a user has many items in cart.
            // Cart items will have all the items in shopping cart for a user.
            // updatequantityby will have count by with and items quantity needs to be updated.
            // if it is -1 that means we have lower a count if it is 5 it means we have to add 5 count to existing count.
            // if updatequantityby is 0, item will be removed.


            // user adds a new item to a new shopping cart for the first time
            // user adds a new item to an existing shopping cart
            // user updates an existing item count
            // user removes an existing item

            ShoppingCart shoppingCart = _db.ShoppingCarts.Include(x=>x.CartItems).FirstOrDefault(x => x.UserId == userId);
            MenuItem menuItem = _db.MenuItems.FirstOrDefault(x => x.Id == menuItemId);
            if (menuItem == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess= false;
                return BadRequest(_response);
            }
            if (shoppingCart == null && updateQuantityBy > 0)
            {
                // create a shopping cart and add cart item
                ShoppingCart newCart = new ShoppingCart
                {
                    UserId = userId,
                };
                _db.ShoppingCarts.Add(newCart);
                _db.SaveChanges();

                CartItem newCartItem = new CartItem
                {
                    MenuItemId = menuItemId,
                    Quantity = updateQuantityBy,
                    ShoppingCartId = newCart.Id,
                    MenuItem=null
                };
                _db.CartItems.Add(newCartItem);
                _db.SaveChanges();

            }
            else
            {
                // shopping cart exists
                CartItem cartItemInCart = shoppingCart.CartItems.FirstOrDefault(x => x.MenuItemId == menuItemId);
                if (cartItemInCart == null)
                {
                    // item does not exist in current cart
                    CartItem newCartItem = new CartItem
                    {
                        MenuItemId = menuItemId,
                        Quantity = updateQuantityBy,
                        ShoppingCartId = shoppingCart.Id,
                        MenuItem = null
                    };
                    _db.CartItems.Add(newCartItem);
                    _db.SaveChanges();
                }
                else
                {
                    // item already exist in the cart, we have to update quantity
                    int newQuantity = cartItemInCart.Quantity +updateQuantityBy;
                    if (updateQuantityBy==0 || newQuantity<=0)
                    {
                        // remove cart item from cart and if it is the only item then remove cart
                        _db.CartItems.Remove(cartItemInCart);
                        if (shoppingCart.CartItems.Count() == 1)
                        {
                            _db.ShoppingCarts.Remove(shoppingCart);
                        }
                        _db.SaveChanges();
                    }
                    else
                    {
                        cartItemInCart.Quantity = newQuantity;
                        _db.SaveChanges();
                    }
                }
            }
            return _response;
        }
    }
}
