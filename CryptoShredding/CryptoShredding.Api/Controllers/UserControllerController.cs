using CryptoShredding.Core.Entities;
using CryptoShredding.Core.Services.CryptoService;
using Microsoft.AspNetCore.Mvc;

namespace CryptoShredding.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserControllerController : ControllerBase
{
    private readonly CryptoService _cryptoService;

    public UserControllerController(CryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    [HttpPost]
    public async Task<IActionResult> EncryptUser([FromBody] User user)
    {
        var encryptedUser = await _cryptoService.EncryptUser(user);
        var decryptedUser = await _cryptoService.DecryptUser(encryptedUser);
        return Ok(new {decryptedUser, encryptedUser});
    }

    [HttpPost("userId")]
    public async Task<IActionResult> DeleteEncryptKey(Guid userId)
    {
        await _cryptoService.DeleteEncryptKey(userId);
        return Ok();
    }
}