Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class U32 {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
}
public struct RECT { public int Left, Top, Right, Bottom; }
'@
$p = Get-Process Lines.Windows -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { 'NO PROCESS'; exit 1 }
"title: [$($p.MainWindowTitle)] responding: $($p.Responding)"
$r = New-Object RECT
[U32]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
"rect: ${w}x${h}"
$b = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($b)
$hdc = $g.GetHdc()
$ok = [U32]::PrintWindow($p.MainWindowHandle, $hdc, 2)
$g.ReleaseHdc($hdc)
$b.Save($args[0])
$g.Dispose(); $b.Dispose()
"printwindow: $ok"
