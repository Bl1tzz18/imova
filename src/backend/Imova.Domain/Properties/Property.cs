namespace Imova.Domain.Properties;

public class Property
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;

    private Property()
    {
    }

    public static Property Create(string title, decimal price, string currency, string city, string district)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City is required.", nameof(city));
        }

        if (string.IsNullOrWhiteSpace(district))
        {
            throw new ArgumentException("District is required.", nameof(district));
        }

        return new Property
        {
            Id = Guid.NewGuid(),
            Title = title,
            Price = price,
            Currency = currency,
            City = city,
            District = district
        };
    }
}
