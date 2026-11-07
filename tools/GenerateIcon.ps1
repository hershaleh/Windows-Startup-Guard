$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path $PSScriptRoot '..\Assets'
New-Item -ItemType Directory -Force -Path $assetDir | Out-Null
$path = Join-Path $assetDir 'WindowsStartupGuard.ico'
$bitmap = [System.Drawing.Bitmap]::new(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)
$tile = [System.Drawing.Drawing2D.GraphicsPath]::new()
$tile.AddArc(2,2,16,16,180,90); $tile.AddArc(46,2,16,16,270,90)
$tile.AddArc(46,46,16,16,0,90); $tile.AddArc(2,46,16,16,90,90); $tile.CloseFigure()
$graphics.FillPath([System.Drawing.Brushes]::DodgerBlue, $tile)
$graphics.DrawPath([System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(190,225,255),2), $tile)
$shield = [System.Drawing.Drawing2D.GraphicsPath]::new()
$points = [System.Drawing.Point[]]@(
 [System.Drawing.Point]::new(32,8), [System.Drawing.Point]::new(52,15),
 [System.Drawing.Point]::new(51,34), [System.Drawing.Point]::new(44,46),
 [System.Drawing.Point]::new(32,56), [System.Drawing.Point]::new(20,46),
 [System.Drawing.Point]::new(13,34), [System.Drawing.Point]::new(12,15)
)
$shield.AddPolygon($points)
$graphics.FillPath([System.Drawing.Brushes]::MidnightBlue, $shield)
$graphics.DrawPath([System.Drawing.Pen]::new([System.Drawing.Color]::White,2), $shield)
$white = [System.Drawing.Brushes]::White
foreach($pt in @(@(20,19),@(34,19),@(20,32),@(34,32))) {
 $graphics.FillRectangle($white, [int]$pt[0], [int]$pt[1], 10, 9)
}
$graphics.FillEllipse([System.Drawing.Brushes]::MediumSeaGreen, 37, 37, 24, 24)
$graphics.DrawEllipse([System.Drawing.Pen]::new([System.Drawing.Color]::White,2), 37, 37, 24, 24)
$check = [System.Drawing.Pen]::new([System.Drawing.Color]::White,3)
$check.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$check.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLines($check, [System.Drawing.Point[]]@(
 [System.Drawing.Point]::new(42,49), [System.Drawing.Point]::new(47,53), [System.Drawing.Point]::new(56,43)
))
$pngStream = [System.IO.MemoryStream]::new()
$bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $pngStream.ToArray()
$iconStream = [System.IO.File]::Open($path, [System.IO.FileMode]::Create)
$writer = [System.IO.BinaryWriter]::new($iconStream)
$writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]1)
$writer.Write([byte]64); $writer.Write([byte]64); $writer.Write([byte]0); $writer.Write([byte]0)
$writer.Write([UInt16]1); $writer.Write([UInt16]32)
$writer.Write([UInt32]$pngBytes.Length); $writer.Write([UInt32]22)
$writer.Write($pngBytes)
$writer.Dispose(); $iconStream.Dispose(); $pngStream.Dispose()
$check.Dispose(); $tile.Dispose(); $shield.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
Write-Host "Generated $path"
