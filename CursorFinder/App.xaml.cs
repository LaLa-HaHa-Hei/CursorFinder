using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace CursorFinder;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Exit(object sender, ExitEventArgs e)
    {
        RestoreAllDefaultCursors();
    }

    private static void RestoreAllDefaultCursors()
    {
        foreach (SystemCursorId cursorId in Enum.GetValues(typeof(SystemCursorId)))
        {
            CursorManagement.RestoreDefaultCursorImage(cursorId);
        }
    }
}

