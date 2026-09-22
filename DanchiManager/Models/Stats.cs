namespace DanchiManager.Models;

public readonly record struct BuildingStats(
    int Units,
    int Vacant,
    int Hospital,
    int People,
    int Bikes,
    int Bicycles);

public static class AppConstants
{
    public const string UnlockPassword = "1502";
    public const int DefaultFee = 100;

    public static int FiscalWarekiYear(DateTime? now = null)
    {
        var d = now ?? DateTime.Now;
        var y = d.Year - 2018;
        if (d.Month < 4) y--;
        return y;
    }
}
