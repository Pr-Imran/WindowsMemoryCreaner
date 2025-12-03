Add-Type -AssemblyName System.Drawing

$size = 64
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$c1 = [System.Drawing.Color]::DeepSkyBlue
$c2 = [System.Drawing.Color]::DodgerBlue
$rectF = New-Object System.Drawing.RectangleF 0, 0, $size, $size

$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rectF, $c1, $c2, 45.0
$g.FillEllipse($brush, 2, 2, 60, 60)

$white = [System.Drawing.Color]::White
$pen = New-Object System.Drawing.Pen $white, 4
$g.DrawEllipse($pen, 10, 10, 44, 44)

# Fix Font constructor - use string for family
$family = "Arial"
# 1 = Bold
$style = [System.Drawing.FontStyle]::Bold 
# 2 = Pixel unit (GraphicsUnit.Pixel)
$unit = [System.Drawing.GraphicsUnit]::Pixel
$font = New-Object System.Drawing.Font $family, 32, $style, $unit

$brushText = [System.Drawing.Brushes]::White
$stringFormat = New-Object System.Drawing.StringFormat
$stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
$stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

$g.DrawString("O", $font, $brushText, $rectF, $stringFormat)

$g.Dispose()

# Save as PNG in memory
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $ms.ToArray()
$ms.Dispose()
$bmp.Dispose()

# Write ICO
$fs = [System.IO.File]::Create("$PSScriptRoot\OptiRam.ico")
$fs.Write([byte[]](0,0, 1,0, 1,0), 0, 6)
$fs.WriteByte($size)
$fs.WriteByte($size)
$fs.WriteByte(0)
$fs.WriteByte(0)
$fs.Write([byte[]](1,0, 32,0), 0, 4)
$lenBytes = [BitConverter]::GetBytes([int]$pngBytes.Length)
$fs.Write($lenBytes, 0, 4)
$offsetBytes = [BitConverter]::GetBytes([int]22)
$fs.Write($offsetBytes, 0, 4)
$fs.Write($pngBytes, 0, $pngBytes.Length)
$fs.Close()

Write-Host "Icon generated: OptiRam.ico"