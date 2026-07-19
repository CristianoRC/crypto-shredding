using Customers.Core.Entities;
using Customers.Core.Services.CryptoService;
using Microsoft.AspNetCore.Mvc;

namespace Customers.Api.Controllers;

public record CreateCustomerRequest(string Name, string Document, string Address);

[ApiController]
[Route("api/customers")]
public class CustomersController(CryptoService cryptoService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request)
    {
        var customer = await cryptoService.CreateAsync(new Customer(Guid.NewGuid(), request.Name, request.Document, request.Address));
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new { id = customer.Id });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var customers = await cryptoService.ListAsync();
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var encrypted = await cryptoService.GetEncryptedAsync(id);
        if (encrypted is null)
            return NotFound();

        var decrypted = await cryptoService.TryDecryptAsync(id);

        return Ok(new
        {
            encrypted,
            status = decrypted.Status.ToString().ToLowerInvariant(),
            decrypted = decrypted.Customer,
            source = decrypted.Source
        });
    }

    [HttpDelete("{id:guid}/key")]
    public async Task<IActionResult> Shred(Guid id)
    {
        var shredded = await cryptoService.ShredAsync(id);
        return shredded ? NoContent() : NotFound();
    }
}
