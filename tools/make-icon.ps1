# Recreates the approved LightShaper package icon from its 1024px master.
# Editable vector source: tools/assets/icon.svg.
param(
    [string]$Out = (Join-Path $PSScriptRoot '..\package\icon.png'),
    [ValidateRange(16, 4096)][int]$Size = 256
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$master = Join-Path $PSScriptRoot 'assets\icon-1024.png'
if (-not (Test-Path -LiteralPath $master)) {
    throw "Missing icon master: $master"
}
$source = [System.Drawing.Image]::FromFile($master)
$bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $attributes = New-Object System.Drawing.Imaging.ImageAttributes
    try {
        $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $destination = New-Object System.Drawing.Rectangle 0, 0, $Size, $Size
        $graphics.DrawImage($source, $destination, 0, 0, $source.Width, $source.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)
    } finally {
        $attributes.Dispose()
    }
    $full = [System.IO.Path]::GetFullPath($Out)
    $bitmap.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Icon: $full ($Size x $Size)"
} finally {
    $graphics.Dispose()
    $bitmap.Dispose()
    $source.Dispose()
}
