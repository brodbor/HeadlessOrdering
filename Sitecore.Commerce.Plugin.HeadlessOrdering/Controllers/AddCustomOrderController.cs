
namespace Sitecore.Commerce.Plugin.Reporting.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Orders;


    using Sitecore.Commerce.Plugin.Carts;
    using Sitecore.Commerce.Plugin.Catalog;
    using System.Web.Http.OData;
    using Sitecore.Commerce.Plugin.Fulfillment;
    using Sitecore.Commerce.Plugin.Payments;
    using Sitecore.Commerce.Plugin.Customers;

    public class AddCustomOrderController : CommerceController
    {
        public AddCustomOrderController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment)
            : base(serviceProvider, globalEnvironment)
        {
        }
        [HttpPut]
        [Route("AddCustomOrder")]
        public async Task<IActionResult> Put([FromBody] ODataActionParameters value)
        {
            if (!this.ModelState.IsValid || value == null)
                return (IActionResult)new BadRequestObjectResult((object)value);

            string id = value["customerId"].ToString();

            if (!this.ModelState.IsValid || string.IsNullOrEmpty(id))
                return (IActionResult)this.NotFound();


            GetCustomerCommand command0 = this.Command<GetCustomerCommand>();

            Customer customer = await command0.Process(this.CurrentContext, id);
            var y = customer.GetComponent<AddressComponent>();


            //refactor
            string randoms = Guid.NewGuid().ToString().Replace("-", string.Empty).Replace("+", string.Empty);

            string cartId = string.Format("{0}{1}", "CUSTOM", randoms);
            string str = value["itemId"].ToString();
            Decimal result;
            string q = "1";



            if (!Decimal.TryParse(q, out result))
                return (IActionResult)new BadRequestObjectResult((object)q);


            AddCartLineCommand command = this.Command<AddCartLineCommand>();
            CartLineComponent line = new CartLineComponent()
            {
                ItemId = str,
                Quantity = result
            };
            Cart cart = await command.Process(this.CurrentContext, cartId, line);

            //FulfillmentComponent

            FulfillmentComponent fulfillment = (PhysicalFulfillmentComponent)new PhysicalFulfillmentComponent()
            {
                ShippingParty = new Party()
                {
                    Address1 = y.Party.Address1,
                    City = y.Party.City,
                    ZipPostalCode = y.Party.ZipPostalCode,
                    State = y.Party.State,
                    StateCode = y.Party.StateCode,
                    CountryCode = y.Party.CountryCode,
                    AddressName = y.Party.AddressName,
                    Name = y.Party.Name

                },
                FulfillmentMethod = new EntityReference()
                {
                    Name = "Ground",
                    EntityTarget = "B146622D-DC86-48A3-B72A-05EE8FFD187A"
                }
            };

            SetCartFulfillmentCommand _CartFulfillmentCommand = this.Command<SetCartFulfillmentCommand>();
            Cart cart1 = await _CartFulfillmentCommand.Process(CurrentContext, cartId, fulfillment);


            //FederatedPaymentComponent


            decimal gt;
            Decimal.TryParse(cart1.Totals.GrandTotal.Amount.ToString(), out gt);



            FederatedPaymentComponent paymentComponent = new FederatedPaymentComponent(new Money() { Amount = gt });

            paymentComponent.PaymentMethod = new EntityReference()
            {
                EntityTarget = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                Name = "Federated"
            };
        
            paymentComponent.PaymentMethodNonce = "fake-valid-nonce";

            paymentComponent.BillingParty = new Party()
            {

                Address1 = y.Party.Address1,
                City = y.Party.City,
                ZipPostalCode = y.Party.ZipPostalCode,
                State = y.Party.State,
                StateCode = y.Party.StateCode,
                CountryCode = y.Party.CountryCode,
                AddressName = y.Party.AddressName,

            };

            AddPaymentsCommand _PaymentsCommand = this.Command<AddPaymentsCommand>();
            Cart cart2 = await _PaymentsCommand.Process(this.CurrentContext, cartId, (IEnumerable<PaymentComponent>)new List<PaymentComponent>()
              {
                (PaymentComponent) paymentComponent
              });

            //CreateOrderCommand


            string email = customer.Email;


            CreateOrderCommand _CreateOrderCommand = this.Command<CreateOrderCommand>();


            Order order = await _CreateOrderCommand.Process(this.CurrentContext, cartId, email);



            return (IActionResult)new ObjectResult((object)_CreateOrderCommand);


        }
    }
}
