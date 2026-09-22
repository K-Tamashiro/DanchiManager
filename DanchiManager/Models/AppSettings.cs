namespace DanchiManager.Models;

public sealed class AppSettings
{
    public string WindowTitle { get; set; } = "団地管理";
    public string EnvelopeTitle { get; set; } = AppConstants.AssociationTitle;
    public int EnvelopeStartMonth { get; set; } = 4;
    public string DatabasePath { get; set; } = "";
    public bool UseCustomPath { get; set; }
    public int UiFontSize { get; set; } = 16;
    public string LastBuildingName { get; set; } = "";
}
