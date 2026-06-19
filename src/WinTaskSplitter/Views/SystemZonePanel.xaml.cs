using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;

namespace WinTaskSplitter.Views;

public partial class SystemZonePanel : UserControl
{
    private readonly DispatcherTimer _clockTimer;

    public SystemZonePanel()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        ClockTime.Text = now.ToString("HH:mm:ss");
        ClockDate.Text = now.ToString("dd.MM.yyyy");
    }
}
