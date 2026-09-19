namespace PharmaCare.Services;

public static class CurrencyFormatter
{
    public const string CurrencyCode = "JOD";

    public static string Format(decimal amount)
        => $"{amount:0.00} JOD";
}
