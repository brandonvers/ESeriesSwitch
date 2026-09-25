param([string]$OutDir)
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$ErrorActionPreference = 'Stop'

function New-Brush($c1, $c2) {
    $b = New-Object System.Windows.Media.LinearGradientBrush
    $b.StartPoint = New-Object System.Windows.Point(0, 0)
    $b.EndPoint = New-Object System.Windows.Point(1, 1)
    $b.GradientStops.Add((New-Object System.Windows.Media.GradientStop([System.Windows.Media.ColorConverter]::ConvertFromString($c1), 0)))
    $b.GradientStops.Add((New-Object System.Windows.Media.GradientStop([System.Windows.Media.ColorConverter]::ConvertFromString($c2), 1)))
    $b.Freeze(); $b
}

function P($x, $y) { New-Object System.Windows.Point($x, $y) }

function Add-Arrow($dc, $cx, $cy, $r, $a0, $a1, $pen, $fill, $headLen, $headW) {
    $rad0 = $a0 * [Math]::PI / 180; $rad1 = $a1 * [Math]::PI / 180
    $start = P ($cx + $r * [Math]::Cos($rad0)) ($cy + $r * [Math]::Sin($rad0))
    $end = P ($cx + $r * [Math]::Cos($rad1)) ($cy + $r * [Math]::Sin($rad1))
    $g = New-Object System.Windows.Media.StreamGeometry
    $ctx = $g.Open()
    $ctx.BeginFigure($start, $false, $false)
    $ctx.ArcTo($end, (New-Object System.Windows.Size($r, $r)), 0, $false, [System.Windows.Media.SweepDirection]::Clockwise, $true, $false)
    $ctx.Close()
    $dc.DrawGeometry($null, $pen, $g)

    # NyÃ­lhegy az Ã­v vÃ©gÃ©n, Ã©rintÅ‘ irÃ¡nyba
    $tx = -[Math]::Sin($rad1); $ty = [Math]::Cos($rad1)
    $nx = [Math]::Cos($rad1); $ny = [Math]::Sin($rad1)
    $tip = P ($end.X + $tx * $headLen) ($end.Y + $ty * $headLen)
    $b1 = P ($end.X + $nx * $headW) ($end.Y + $ny * $headW)
    $b2 = P ($end.X - $nx * $headW) ($end.Y - $ny * $headW)
    $h = New-Object System.Windows.Media.StreamGeometry
    $hc = $h.Open()
    $hc.BeginFigure($tip, $true, $true)
    $hc.LineTo($b1, $true, $true)
    $hc.LineTo($b2, $true, $true)
    $hc.Close()
    $dc.DrawGeometry($fill, $null, $h)
}

function Render([int]$size) {
    $dv = New-Object System.Windows.Media.DrawingVisual
    $dc = $dv.RenderOpen()
    $s = $size / 256.0
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform($s, $s)))

    $small = $size -lt 48
    $margin = if ($small) { 4 } else { 10 }
    $side = 256 - 2 * $margin
    $rect = [System.Windows.Rect]::new($margin, $margin, $side, $side)
    $clip = New-Object System.Windows.Media.RectangleGeometry($rect, 52, 52)
    $dc.PushClip($clip)

    $orange = New-Brush '#FF8A3D' '#D9480F'
    $green = New-Brush '#34D399' '#15803D'
    $white = [System.Windows.Media.Brushes]::White

    # ÃtlÃ³ mentÃ©n kettÃ©osztva: bal-fent narancs (E-szÃ©ria), jobb-lent zÃ¶ld (ISTA+)
    $dc.DrawRectangle($green, $null, $rect)
    $tri = New-Object System.Windows.Media.StreamGeometry
    $tc = $tri.Open()
    $tc.BeginFigure((P 0 0), $true, $true)
    $tc.LineTo((P 256 0), $true, $false)
    $tc.LineTo((P 0 256), $true, $false)
    $tc.Close()
    $dc.DrawGeometry($orange, $null, $tri)
    $dc.Pop()

    $typeface = New-Object System.Windows.Media.Typeface(
        (New-Object System.Windows.Media.FontFamily('Segoe UI')),
        [System.Windows.FontStyles]::Normal, [System.Windows.FontWeights]::Black, [System.Windows.FontStretches]::Normal)

    function Draw-Text($text, $em, $cx, $cy) {
        $ft = New-Object System.Windows.Media.FormattedText($text, [Globalization.CultureInfo]::InvariantCulture,
            [System.Windows.FlowDirection]::LeftToRight, $typeface, $em, $white, 1.0)
        $geo = $ft.BuildGeometry((P 0 0))
        $bb = $geo.Bounds
        $geo.Transform = New-Object System.Windows.Media.TranslateTransform(($cx - $bb.X - $bb.Width / 2), ($cy - $bb.Y - $bb.Height / 2))
        $dc.DrawGeometry($white, $null, $geo)
    }

    if ($small) {
        Draw-Text 'E' 150 88 88
        Draw-Text '+' 150 176 176
    }
    else {
        Draw-Text 'E' 100 68 66
        Draw-Text '+' 108 190 192
        $pen = New-Object System.Windows.Media.Pen($white, 15)
        $pen.StartLineCap = 'Round'; $pen.EndLineCap = 'Flat'
        Add-Arrow $dc 128 128 40 200 318 $pen $white 22 17
        Add-Arrow $dc 128 128 40 20 138 $pen $white 22 17
    }

    $dc.Pop()
    $dc.Close()
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($dv)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $ms = New-Object System.IO.MemoryStream
    $enc.Save($ms)
    , $ms.ToArray()
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = @{}
foreach ($sz in $sizes) {
    $pngs[$sz] = Render $sz
    [IO.File]::WriteAllBytes((Join-Path $OutDir "icon_$sz.png"), $pngs[$sz])
}

# ICO Ã¶sszerakÃ¡sa PNG bejegyzÃ©sekbÅ‘l
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
foreach ($sz in $sizes) {
    $d = if ($sz -ge 256) { 0 } else { $sz }
    $bw.Write([byte]$d); $bw.Write([byte]$d); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$pngs[$sz].Length); $bw.Write([UInt32]$offset)
    $offset += $pngs[$sz].Length
}
foreach ($sz in $sizes) { $bw.Write($pngs[$sz]) }
$bw.Flush()
[IO.File]::WriteAllBytes((Join-Path $OutDir 'app.ico'), $ms.ToArray())
"ok"

