﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using Stripe;
using System.Net;

namespace RedMango_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        protected ApiResponse _response;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _db;
        public PaymentController(IConfiguration configuration, ApplicationDbContext db)
        {
            _response = new ApiResponse();
            _configuration = configuration;
            _db = db;
        }
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> MakePayment(string userId)
        {
            ShoppingCart shoppingCart = _db.ShoppingCarts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.MenuItem)
                .FirstOrDefault(x => x.UserId == userId);
            if (shoppingCart == null ||shoppingCart.CartItems == null ||shoppingCart.CartItems.Count()==0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            #region Create Payment Intent
            StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
            shoppingCart.CartTotal = shoppingCart.CartItems.Sum(x=> x.Quantity * x.MenuItem.Price);

            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions
            {
                Amount = (int)(shoppingCart.CartTotal * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" },
            };
            PaymentIntentService service = new PaymentIntentService();
            PaymentIntent response = service.Create(options);
            shoppingCart.StripePaymentIntentId = response.Id;
            shoppingCart.ClientSecret = response.ClientSecret;

            #endregion
            _response.Result = shoppingCart;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);

        }
    }
}
