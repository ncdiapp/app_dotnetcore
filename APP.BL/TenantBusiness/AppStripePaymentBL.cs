using Stripe.Checkout;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using App.BL;
using APP.Components.Dto;

namespace APP.BL.AppMgr
{
    public class AppStripePaymentBL
    {
        private readonly StripeClient _stripeClient;

        //public AppStripePaymentBL()
        //{
        //    // Initialize the Stripe client with your Stripe secret key
        //    StripeConfiguration.ApiKey = "YOUR_STRIPE_SECRET_KEY";
        //    _stripeClient = new StripeClient();
        //}

        public static async Task<AppStripeCheckOutDto> CreateStripeCheckoutSession(AppStripeCheckOutDto checkOutDto)
        {           
            try
            {
                StripeConfiguration.ApiKey = AppTenantSettingBL.GetStringValue(EmTenantSettings.StripeGateWaySecretkey);
               
                var priceService = new PriceService();
                var priceOptions = new PriceCreateOptions
                {
                    ProductData = new PriceProductDataOptions
                    {
                        Name = checkOutDto.ProductName,
                    },
                    UnitAmount = (long)(checkOutDto.Amount * 100), // Amount in cents
                    Currency = checkOutDto.CurrencyCode, // "usd",
                };
                var price = await priceService.CreateAsync(priceOptions);

                // Create a Checkout Session with the Price
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>                        
                    {
                        new SessionLineItemOptions
                        {
                            Price = price.Id,
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = checkOutDto.SuccessUrl,
                    CancelUrl = checkOutDto.CancelUrl,
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                //session.Id;

                checkOutDto.SessionId = session.Id;
                checkOutDto.PaymentUrl = session.Url;

                return checkOutDto;
            }
            catch (StripeException stripeException)
            {
                string errorMsg = $"Stripe Error: {stripeException.StripeError?.Message}";
                checkOutDto.ErrorMessage = errorMsg;
                return checkOutDto;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error: {ex.Message}";
                checkOutDto.ErrorMessage = errorMsg;
                return checkOutDto;
            }




        }


        public static string RetrieveStripeCheckoutSession(string sessionId)
        {
            try
            {
                var sessionService = new SessionService();
                Session sessionDto = sessionService.Get(sessionId);
                return Newtonsoft.Json.JsonConvert.SerializeObject(sessionDto);                
            }
            catch (StripeException e)
            {
                // Handle Stripe API error
                Console.WriteLine($"Error retrieving Checkout session: {e.StripeError.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Handle other errors
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}
