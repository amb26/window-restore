#r "System.Drawing.dll"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class WindowInfo {
    public string title;
    public int left;
    public int top;
    public int right;
    public int bottom;
    public int hwnd;
}

public struct WINDOWPLACEMENT
{
    public int length;
    public int flags;
    public int showCmd;
    public System.Drawing.Point ptMinPosition;
    public System.Drawing.Point ptMaxPosition;
    public System.Drawing.Rectangle rcNormalPosition;
}

public class WindowRestore
{
    // From: http://www.pinvoke.net/default.aspx/user32.getwindowplacement
    [DllImport("user32.dll")] 
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
    [DllImport("user32.dll")] 
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    const int SW_HIDE =         0;
    const int SW_SHOWNORMAL =       1;
    const int SW_NORMAL =       1;
    const int SW_SHOWMINIMIZED =    2;
    const int SW_SHOWMAXIMIZED =    3;
    const int SW_MAXIMIZE =     3;
    const int SW_SHOWNOACTIVATE =   4;
    const int SW_SHOW =         5;
    const int SW_MINIMIZE =     6;
    const int SW_SHOWMINNOACTIVE =  7;
    const int SW_SHOWNA =       8;
    const int SW_RESTORE =      9;
    
    public static WINDOWPLACEMENT MakePlacement() {
        WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
        placement.length = Marshal.SizeOf(placement);
        return placement;
    }    

    public static WINDOWPLACEMENT GetPlacement(IntPtr hWnd) {
        WINDOWPLACEMENT placement = MakePlacement();
        GetWindowPlacement(hWnd, ref placement);
        return placement;
    }
    
    public static String IntPtrToHex(IntPtr ptr) {
        return ptr.ToInt32().ToString("X");
    }

    // From https://code.msdn.microsoft.com/windowsapps/Enumerate-top-level-9aa9d7c1/view/SourceCode#content
    
    protected delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    protected static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    protected static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")]
    protected static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")]
    protected static extern bool IsWindowVisible(IntPtr hWnd);

    public static ArrayList queryList = null;
        
    public static bool EnumTheWindows(IntPtr hWnd, IntPtr lParam) {
        int size = GetWindowTextLength(hWnd);
        if (size++ > 0 /* && IsWindowVisible(hWnd) */) {
            WINDOWPLACEMENT placement = GetPlacement(hWnd);
            Rectangle rect = placement.rcNormalPosition;
            StringBuilder sb = new StringBuilder(size);
            GetWindowText(hWnd, sb, size);
            Console.WriteLine(sb.ToString() + " " + rect.Left + "," + rect.Top + " " + rect.Width + "," + rect.Height + " hwnd " + IntPtrToHex(hWnd) + " flags " + placement.flags.ToString("X") + " showCmd " + placement.flags.ToString("X"));
            WindowInfo wi = new WindowInfo{title = sb.ToString(), left = rect.Left, top = rect.Top, right = rect.Width, bottom = rect.Height, hwnd = hWnd.ToInt32()};
            queryList.Add(wi); 
        }
        return true;
    }
    
    public static void RestoreWindow(IDictionary<string,object> toRestore) {
        IntPtr hWnd = new IntPtr((Int32)toRestore["hwnd"]);
        WINDOWPLACEMENT placement = GetPlacement(hWnd);
        Rectangle rectangle = new Rectangle();
        Console.WriteLine("Restoring " + IntPtrToHex(hWnd) + " from " + rectangle.Left + "," + rectangle.Top + " " + rectangle.Width + "," + rectangle.Height);        
        // What an absolutely bloody ridiculous structure. "Left" is readonly, and we have swapped Width for right during read
        
        rectangle.X = (Int32)toRestore["left"];
        rectangle.Y = (Int32)toRestore["top"];
        rectangle.Width = (Int32)toRestore["right"];
        rectangle.Height = (Int32)toRestore["bottom"];
        placement.rcNormalPosition = rectangle;
        placement.showCmd = SW_SHOW;
        
        Console.WriteLine("Restoring " + IntPtrToHex(hWnd) + " to " + rectangle.Left + "," + rectangle.Top + " " + rectangle.Width + "," + rectangle.Height);
        bool success = SetWindowPlacement(hWnd, ref placement);
    }
    
    public async Task<object> Query(dynamic input) {
        if (queryList != null) {
            throw new InvalidOperationException("Cannot enumerate windows, enumeration already in progress - this implementation is not threadsafe");
        }
        ArrayList togo = queryList = new ArrayList();
        EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);
        queryList = null;
        Console.WriteLine("Returning " + togo.Count + " entries");
        return togo;
    }

    public async Task<object> Restore(dynamic input) {
        object[] restoreList = (object[]) input;
        foreach (object toRestore in restoreList) {
            Console.WriteLine("Restore " + toRestore);
            RestoreWindow((IDictionary<string,object>)toRestore);
        }
        return null;
    }
}
