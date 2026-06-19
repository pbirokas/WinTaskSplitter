namespace WinTaskSplitter.Models;

public class Zone
{
    public Guid   Id            { get; set; } = Guid.NewGuid();
    public string Name          { get; set; } = "Neue Zone";
    public bool   IsSystem      { get; set; } = false;
    public bool   IsGeneral     { get; set; } = false;
    public int    Order         { get; set; } = 0;
    public bool   ShowLabel     { get; set; } = true;
    public double Width         { get; set; } = 200;
    public string BackgroundColor { get; set; } = "#FF202020";
    public string BorderColor   { get; set; } = "#FF3A3A3A";
    public double BorderThickness { get; set; } = 1.0;
}
