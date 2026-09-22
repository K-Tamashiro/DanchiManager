namespace DanchiManager.Models;

public sealed class AppSettings
{
    public string DatabasePath { get; set; } = "";
    public bool UseCustomPath { get; set; }
    public int UiFontSize { get; set; } = 16;
}
