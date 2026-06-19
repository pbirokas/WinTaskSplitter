namespace WinTaskSplitter.Models;

public class AppSettings
{
    public string        Edge            { get; set; } = "Bottom"; // Bottom | Top | Left | Right
    public double        BarThickness    { get; set; } = 48;
    public bool          StartWithWindows { get; set; } = false;
    public double        LabelFontSize      { get; set; } = 8;
    public Guid          StartButtonZoneId  { get; set; } = Guid.Empty;
    public List<Zone>    Zones              { get; set; } = [];
    public List<AppAssignment> Assignments { get; set; } = [];

    public static AppSettings CreateDefault()
    {
        var privateZone = new Zone { Name = "Privat",  Order = 0, BackgroundColor = "#FF1A2433", BorderColor = "#FF2A4A7F" };
        var workZone    = new Zone { Name = "Arbeit",  Order = 1, BackgroundColor = "#FF1A2A1A", BorderColor = "#FF2A6A2A" };
        var devZone     = new Zone { Name = "Dev",     Order = 2, BackgroundColor = "#FF2A1A2A", BorderColor = "#FF6A2A6A" };
        var sysZone     = new Zone { Name = "System",  Order = 3, BackgroundColor = "#FF202020", BorderColor = "#FF3A3A3A", IsSystem = true };
        var genZone     = new Zone { Name = "Allgemein", Order = 4, BackgroundColor = "#FF282828", BorderColor = "#FF484848", IsGeneral = true };

        return new AppSettings
        {
            StartButtonZoneId = genZone.Id,
            Zones =
            [
                privateZone,
                workZone,
                devZone,
                sysZone,
                genZone
            ]
        };
    }
}
