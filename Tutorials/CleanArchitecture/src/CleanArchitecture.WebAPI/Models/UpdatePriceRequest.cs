namespace CleanArchitecture.WebAPI.Models;

public record UpdatePriceRequest(
    decimal NewPrice,
    string Currency);
