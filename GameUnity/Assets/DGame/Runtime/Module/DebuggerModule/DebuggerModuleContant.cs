namespace DGame
{
    public partial class DebuggerDriver
    {
        public class Constant
        {
            //DebuggerDriver
            public const string DEFAULT_DEBUGGER_WINDOW_ICON_X = "DebuggerWindow.Icon.X";
            public const string DEFAULT_DEBUGGER_WINDOW_ICON_Y = "DebuggerWindow.Icon.Y";
            public const string DEFAULT_DEBUGGER_WINDOW_WINDOW_X = "DebuggerWindow.Window.X";
            public const string DEFAULT_DEBUGGER_WINDOW_WINDOW_Y = "DebuggerWindow.Window.Y";
            public const string DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH = "DebuggerWindow.Window.Width";
            public const string DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT = "DebuggerWindow.Window.Height";
            public const string DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE = "DebuggerWindow.Window.Scale";
            public const string DEFAULT_DEBUGGER_WINDOW_FPS_STRING = "<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>FPS: {4:F2}</b></color>";
            public const float DEFAULT_DEBUGGER_WINDOW_FPS_UPDATE_INTERVAL = 0.5f;

            // ConsoleWindow
            public const string CONSOLE_WINDOW_LOCK_SCROLL = "DebuggerWindow.Console.LockScroll";
            public const string CONSOLE_WINDOW_INFO_FILTER = "DebuggerWindow.Console.InfoFilter";
            public const string CONSOLE_WINDOW_WARNING_FILTER = "DebuggerWindow.Console.WarningFilter";
            public const string CONSOLE_WINDOW_ERROR_FILTER = "DebuggerWindow.Console.ErrorFilter";
            public const string CONSOLE_WINDOW_FATAL_FILTER = "DebuggerWindow.Console.FatalFilter";
            public const string CONSOLE_WINDOW_LOG_DETAILS_MESSAGE_STRING = "<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>{4}</b></color>{6}{6}{5}";
            public const string CONSOLE_WINDOW_LOG_SINGLE_MESSAGE_STRING = "<color=#{0:x2}{1:x2}{2:x2}{3:x2}>[{4:HH:mm:ss.fff}][{5}] {6}</color>";

            // ScrollableDebuggerWindowBase
            public const float TITLE_WIDTH = 240f;

            // RuntimeMemoryInformationWindow
            public const int SHOW_SAMPLE_COUNT = 300;
        }
    }
}