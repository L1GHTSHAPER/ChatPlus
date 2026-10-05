# Renders package\icon.png (256x256, required by Thunderstore) with System.Drawing.
param([string]$Out = (Join-Path $PSScriptRoot '..\package\icon.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)

function New-RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}
function C([int]$a, [int]$r, [int]$gr, [int]$b) { [System.Drawing.Color]::FromArgb($a, $r, $gr, $b) }
function P([float]$x, [float]$y) { New-Object System.Drawing.PointF $x, $y }

# Background (same palette as the other LightShaper mods)
$bg = New-RoundedRect 0 0 256 256 44
$bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 0, 256), (C 255 46 62 92), (C 255 22 30 48)
$g.FillPath($bgBrush, $bg)

# Chat window in the game's colours: cream frame, brown outline, grey message area
$ink = C 255 106 77 82
$cream = New-Object System.Drawing.SolidBrush (C 255 244 237 225)
$outline = New-Object System.Drawing.Pen $ink, 7
$frame = New-RoundedRect 30 44 168 168 26
$g.FillPath($cream, $frame)
$g.DrawPath($outline, $frame)
$area = New-RoundedRect 46 60 136 104 14
$g.FillPath((New-Object System.Drawing.SolidBrush (C 255 142 134 135)), $area)
$g.DrawPath((New-Object System.Drawing.Pen $ink, 4), $area)
# Input field
$g.FillPath((New-Object System.Drawing.SolidBrush (C 255 237 222 205)), (New-RoundedRect 46 176 136 22 11))

# Message lines: a time stamp (gold) in front of each line
$gold = New-Object System.Drawing.SolidBrush (C 255 242 196 109)
$text = New-Object System.Drawing.SolidBrush (C 255 245 237 225)
$y = 74
foreach ($w in 84, 62, 74) {
    $g.FillPath($gold, (New-RoundedRect 56 $y 22 12 6))
    $g.FillPath($text, (New-RoundedRect 84 $y $w 12 6))
    $y += 26
}

# Resize arrow at the top-right corner
$arrowPen = New-Object System.Drawing.Pen (C 255 140 217 255), 12
$arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$arrowPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawLine($arrowPen, 176, 66, 222, 20)
$g.DrawLines($arrowPen, [System.Drawing.PointF[]]@((P 196 20), (P 222 20), (P 222 46)))

$g.Dispose()
$full = [System.IO.Path]::GetFullPath($Out)
$bmp.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Icon: $full"
