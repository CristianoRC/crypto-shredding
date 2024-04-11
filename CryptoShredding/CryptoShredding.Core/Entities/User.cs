namespace CryptoShredding.Core.Entities;

//Poderia usar Value Objects, mas para esse exemplo só deixaria mais complexo...
public record User(Guid Id, string Name, string Document, string Address);