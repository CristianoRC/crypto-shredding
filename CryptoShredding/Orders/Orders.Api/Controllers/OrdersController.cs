using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Entities;
using Orders.Api.Services;

namespace Orders.Api.Controllers;

public record CreateOrderRequest(Guid CustomerId, string Product, decimal Amount);

[ApiController]
[Route("api/orders")]
public class OrdersController(OrdersDbContext db, CustomersClient customersClient) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Product = request.Product,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), null, new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var orders = await db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();

        var result = new List<object>(orders.Count);
        foreach (var order in orders)
        {
            var lookup = await customersClient.GetCustomerAsync(order.CustomerId);
            result.Add(new
            {
                order.Id,
                order.CustomerId,
                order.Product,
                order.Amount,
                order.CreatedAt,
                customer = lookup.Customer,
                customerStatus = lookup.Found ? lookup.Status : "notfound"
            });
        }

        return Ok(result);
    }
}
