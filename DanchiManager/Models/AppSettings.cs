namespace DanchiManager.Models;

public sealed class AppSettings
{
    public string WindowTitle { get; set; } = "団地管理";
    public string EnvelopeTitle { get; set; } = AppConstants.AssociationTitle;
    public int EnvelopeStartMonth { get; set; } = 4;
    public int MonthlyFee { get; set; } = 100;
    public string DatabasePath { get; set; } = "";
    public bool UseCustomPath { get; set; }
    public int UiFontSize { get; set; } = 16;
    public string LastBuildingName { get; set; } = "";
    public string PhonePrefix1 { get; set; } = "075-";
    public string PhonePrefix2 { get; set; } = "090-";
    public string PhonePrefix3 { get; set; } = "070-";
    public string PhonePrefix4 { get; set; } = "050-";
    public string PhonePrefix5 { get; set; } = "080-";
    public string NoteButton1 { get; set; } = "非会員";
    public string NoteButton2 { get; set; } = "空家賃";
    public string NoteButton3 { get; set; } = "の息子";
    public string NoteButton4 { get; set; } = "の娘";
    public string NoteButton5 { get; set; } = "";
    public string NoteButton6 { get; set; } = "";
    public string NoteButton7 { get; set; } = "";
    public string NoteButton8 { get; set; } = "";
    public string NoteButton9 { get; set; } = "";
    public string NoteButton10 { get; set; } = "";
}
