# Случайные клики по клеткам игрового поля до заполнения (Game Over).
# Параметры: -Clicks N (число кликов), -DelayMs пауза между кликами.
param([int]$Clicks = 150, [int]$DelayMs = 180)

Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Clicker3 {
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
[Clicker3]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
Start-Sleep -Milliseconds 300
[Clicker3]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 200

$r = New-Object RECT
[Clicker3]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$frameX = [Clicker3]::GetSystemMetrics(32)
$offsetY = [Clicker3]::GetSystemMetrics(33) + [Clicker3]::GetSystemMetrics(4)

# размер окна ~= 816x519 (клиент 808x488); раскладка как в BoardLayout
$W = $r.Right - $r.Left - 2 * $frameX
$H = $r.Bottom - $r.Top - $offsetY - 8
$panelH = [Math]::Max(48, [int]($H / 10))
$board = [Math]::Min($W, $H - $panelH)
$cell = [int]($board / 9)
$board = $cell * 9
$ox = [int](($W - $board) / 2)
$oy = $panelH + [int](($H - $panelH - $board) / 2)
"client ${W}x${H}, board origin ${ox},${oy} cell ${cell}"

$rnd = New-Object Random
for ($i = 0; $i -lt $Clicks; $i++) {
    $cx = $ox + $rnd.Next(9) * $cell + [int]($cell / 2)
    $cy = $oy + $rnd.Next(9) * $cell + [int]($cell / 2)
    $sx = $r.Left + $frameX + $cx
    $sy = $r.Top + $offsetY + $cy
    [Clicker3]::SetCursorPos($sx, $sy) | Out-Null
    Start-Sleep -Milliseconds 40
    [Clicker3]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [Clicker3]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $DelayMs
}
"готово: $Clicks кликов"
