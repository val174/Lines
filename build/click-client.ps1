param([int]$ClientX, [int]$ClientY)

Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Clicker2 {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int n);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
public struct RECT { public int Left, Top, Right, Bottom; }
'@

$p = Get-Process Lines.Windows | Select-Object -First 1
[Clicker2]::ShowWindow($p.MainWindowHandle, 9) | Out-Null # SW_RESTORE
Start-Sleep -Milliseconds 300
[Clicker2]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 200
$r = New-Object RECT
[Clicker2]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$frameX = [Clicker2]::GetSystemMetrics(32)   # SM_CXFRAME
$frameY = [Clicker2]::GetSystemMetrics(33)   # SM_CYFRAME
$caption = [Clicker2]::GetSystemMetrics(4)   # SM_CYCAPTION
$sx = $r.Left + $frameX + $ClientX
$sy = $r.Top + $frameY + $caption + $ClientY
"screen: ${sx},${sy} (window $($r.Left),$($r.Top) $($r.Right-$r.Left)x$($r.Bottom-$r.Top))"
[Clicker2]::SetCursorPos($sx, $sy) | Out-Null
Start-Sleep -Milliseconds 80
[Clicker2]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 80
[Clicker2]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
