param(
    [string]$OutputRoot = "AssetSlices"
)

Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"

function New-CleanDirectory {
    param([string]$Path)
    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Get-SafeName {
    param([string]$Name)
    return ($Name -replace '[^\p{L}\p{Nd}_\-]+', '_').Trim('_')
}

function Save-Crop {
    param(
        [System.Drawing.Bitmap]$Source,
        [System.Drawing.Rectangle]$Rect,
        [string]$Path
    )

    if ($Rect.Width -le 0 -or $Rect.Height -le 0) { return }

    $crop = New-Object System.Drawing.Bitmap $Rect.Width, $Rect.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($crop)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.DrawImage($Source, (New-Object System.Drawing.Rectangle 0, 0, $Rect.Width, $Rect.Height), $Rect, [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.Dispose()
    $crop.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $crop.Dispose()
}

function Test-HasVisiblePixel {
    param(
        [System.Drawing.Bitmap]$Image,
        [System.Drawing.Rectangle]$Rect
    )

    for ($y = $Rect.Top; $y -lt $Rect.Bottom; $y++) {
        for ($x = $Rect.Left; $x -lt $Rect.Right; $x++) {
            if ($Image.GetPixel($x, $y).A -gt 8) { return $true }
        }
    }

    return $false
}

function Slice-Grid {
    param(
        [string]$SourcePath,
        [string]$Group,
        [int]$CellWidth,
        [int]$CellHeight,
        [bool]$SkipEmpty = $true
    )

    if (!(Test-Path $SourcePath)) { return @() }

    $safeGroup = Get-SafeName $Group
    $target = Join-Path $OutputRoot $safeGroup
    New-Item -ItemType Directory -Force -Path $target | Out-Null

    $image = [System.Drawing.Bitmap]::FromFile((Resolve-Path $SourcePath))
    $items = New-Object System.Collections.Generic.List[object]
    $index = 0
    $cols = [Math]::Floor($image.Width / $CellWidth)
    $rows = [Math]::Floor($image.Height / $CellHeight)

    for ($row = 0; $row -lt $rows; $row++) {
        for ($col = 0; $col -lt $cols; $col++) {
            $rect = New-Object System.Drawing.Rectangle ($col * $CellWidth), ($row * $CellHeight), $CellWidth, $CellHeight
            if ($SkipEmpty -and !(Test-HasVisiblePixel $image $rect)) { continue }

            $fileName = "{0:D3}__r{1:D2}_c{2:D2}__x{3}_y{4}_w{5}_h{6}.png" -f $index, $row, $col, $rect.X, $rect.Y, $rect.Width, $rect.Height
            $outPath = Join-Path $target $fileName
            Save-Crop $image $rect $outPath

            $items.Add([PSCustomObject]@{
                Group = $safeGroup
                Index = $index
                Row = $row
                Col = $col
                X = $rect.X
                Y = $rect.Y
                Width = $rect.Width
                Height = $rect.Height
                File = $outPath
            })
            $index++
        }
    }

    $image.Dispose()
    return $items
}

function Add-Point {
    param(
        [System.Drawing.Rectangle]$Rect,
        [int]$X,
        [int]$Y
    )

    $left = [Math]::Min($Rect.Left, $X)
    $top = [Math]::Min($Rect.Top, $Y)
    $right = [Math]::Max($Rect.Right, $X + 1)
    $bottom = [Math]::Max($Rect.Bottom, $Y + 1)
    return New-Object System.Drawing.Rectangle $left, $top, ($right - $left), ($bottom - $top)
}

function Merge-CloseRects {
    param(
        [System.Collections.Generic.List[System.Drawing.Rectangle]]$Rects,
        [int]$Gap = 3
    )

    $changed = $true
    while ($changed) {
        $changed = $false
        for ($i = 0; $i -lt $Rects.Count; $i++) {
            for ($j = $i + 1; $j -lt $Rects.Count; $j++) {
                $a = $Rects[$i]
                $b = $Rects[$j]
                $expanded = New-Object System.Drawing.Rectangle ($a.X - $Gap), ($a.Y - $Gap), ($a.Width + $Gap * 2), ($a.Height + $Gap * 2)
                if ($expanded.IntersectsWith($b)) {
                    $left = [Math]::Min($a.Left, $b.Left)
                    $top = [Math]::Min($a.Top, $b.Top)
                    $right = [Math]::Max($a.Right, $b.Right)
                    $bottom = [Math]::Max($a.Bottom, $b.Bottom)
                    $Rects[$i] = New-Object System.Drawing.Rectangle $left, $top, ($right - $left), ($bottom - $top)
                    $Rects.RemoveAt($j)
                    $changed = $true
                    break
                }
            }
            if ($changed) { break }
        }
    }
}

function Slice-AutoAlpha {
    param(
        [string]$SourcePath,
        [string]$Group,
        [int]$MinPixels = 12,
        [int]$Padding = 1,
        [int]$MergeGap = 2
    )

    if (!(Test-Path $SourcePath)) { return @() }

    $safeGroup = Get-SafeName $Group
    $target = Join-Path $OutputRoot $safeGroup
    New-Item -ItemType Directory -Force -Path $target | Out-Null

    $image = [System.Drawing.Bitmap]::FromFile((Resolve-Path $SourcePath))
    $visited = New-Object 'bool[,]' $image.Width, $image.Height
    $rects = New-Object 'System.Collections.Generic.List[System.Drawing.Rectangle]'

    for ($startY = 0; $startY -lt $image.Height; $startY++) {
        for ($startX = 0; $startX -lt $image.Width; $startX++) {
            if ($visited[$startX, $startY]) { continue }
            $visited[$startX, $startY] = $true
            if ($image.GetPixel($startX, $startY).A -le 8) { continue }

            $queue = New-Object 'System.Collections.Generic.Queue[System.Drawing.Point]'
            $queue.Enqueue((New-Object System.Drawing.Point $startX, $startY))
            $rect = New-Object System.Drawing.Rectangle $startX, $startY, 1, 1
            $pixels = 0

            while ($queue.Count -gt 0) {
                $point = $queue.Dequeue()
                $pixels++
                $rect = Add-Point $rect $point.X $point.Y

                $neighbors = @(
                    (New-Object System.Drawing.Point ($point.X + 1), $point.Y),
                    (New-Object System.Drawing.Point ($point.X - 1), $point.Y),
                    (New-Object System.Drawing.Point $point.X, ($point.Y + 1)),
                    (New-Object System.Drawing.Point $point.X, ($point.Y - 1))
                )

                foreach ($next in $neighbors) {
                    if ($next.X -lt 0 -or $next.Y -lt 0 -or $next.X -ge $image.Width -or $next.Y -ge $image.Height) { continue }
                    if ($visited[$next.X, $next.Y]) { continue }
                    $visited[$next.X, $next.Y] = $true
                    if ($image.GetPixel($next.X, $next.Y).A -gt 8) {
                        $queue.Enqueue($next)
                    }
                }
            }

            if ($pixels -ge $MinPixels) {
                $left = [Math]::Max(0, $rect.Left - $Padding)
                $top = [Math]::Max(0, $rect.Top - $Padding)
                $right = [Math]::Min($image.Width, $rect.Right + $Padding)
                $bottom = [Math]::Min($image.Height, $rect.Bottom + $Padding)
                $rects.Add((New-Object System.Drawing.Rectangle $left, $top, ($right - $left), ($bottom - $top)))
            }
        }
    }

    Merge-CloseRects $rects $MergeGap

    $items = New-Object System.Collections.Generic.List[object]
    $index = 0
    foreach ($rect in ($rects | Sort-Object Y, X)) {
        $fileName = "{0:D3}__x{1}_y{2}_w{3}_h{4}.png" -f $index, $rect.X, $rect.Y, $rect.Width, $rect.Height
        $outPath = Join-Path $target $fileName
        Save-Crop $image $rect $outPath
        $items.Add([PSCustomObject]@{
            Group = $safeGroup
            Index = $index
            Row = ""
            Col = ""
            X = $rect.X
            Y = $rect.Y
            Width = $rect.Width
            Height = $rect.Height
            File = $outPath
        })
        $index++
    }

    $image.Dispose()
    return $items
}

function New-ContactSheet {
    param(
        [object[]]$Items,
        [string]$Group
    )

    if ($Items.Count -eq 0) { return }

    $safeGroup = Get-SafeName $Group
    $previewDir = Join-Path $OutputRoot "_contact_sheets"
    New-Item -ItemType Directory -Force -Path $previewDir | Out-Null

    $cellW = 132
    $cellH = 132
    $cols = 6
    $rows = [Math]::Ceiling($Items.Count / $cols)
    $sheet = New-Object System.Drawing.Bitmap ($cols * $cellW), ($rows * $cellH), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($sheet)
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 31, 34, 48))

    $font = New-Object System.Drawing.Font "Arial", 9
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 230, 205, 120))
    $gridBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 48, 52, 72))

    for ($i = 0; $i -lt $Items.Count; $i++) {
        $item = $Items[$i]
        $col = $i % $cols
        $row = [Math]::Floor($i / $cols)
        $x = $col * $cellW
        $y = $row * $cellH
        $graphics.FillRectangle($gridBrush, $x + 2, $y + 2, $cellW - 4, $cellH - 4)

        $sprite = [System.Drawing.Bitmap]::FromFile((Resolve-Path $item.File))
        $scale = [Math]::Min(88 / [Math]::Max(1, $sprite.Width), 82 / [Math]::Max(1, $sprite.Height))
        $drawW = [Math]::Max(1, [int]($sprite.Width * $scale))
        $drawH = [Math]::Max(1, [int]($sprite.Height * $scale))
        $drawX = $x + [Math]::Floor(($cellW - $drawW) / 2)
        $drawY = $y + 8 + [Math]::Floor((88 - $drawH) / 2)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.DrawImage($sprite, $drawX, $drawY, $drawW, $drawH)
        $sprite.Dispose()

        $label = "{0:D3} x{1} y{2}" -f $item.Index, $item.X, $item.Y
        $graphics.DrawString($label, $font, $brush, $x + 8, $y + 98)
        $graphics.DrawString(("{0}x{1}" -f $item.Width, $item.Height), $font, $brush, $x + 8, $y + 113)
    }

    $outPath = Join-Path $previewDir ($safeGroup + ".png")
    $sheet.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $sheet.Dispose()
    $font.Dispose()
    $brush.Dispose()
    $gridBrush.Dispose()
}

New-CleanDirectory $OutputRoot

$all = New-Object System.Collections.Generic.List[object]
$jobs = @(
    @{ Mode = "auto"; Path = "Assets/Resources/Visuals/Farm/House.png"; Group = "farm_house_objects"; Min = 18; Gap = 3 },
    @{ Mode = "auto"; Path = "Assets/Resources/Visuals/Farm/MapleTree.png"; Group = "farm_maple_objects"; Min = 8; Gap = 3 },
    @{ Mode = "auto"; Path = "Assets/Resources/Visuals/Farm/Woods.png"; Group = "farm_woods_objects"; Min = 20; Gap = 5 },
    @{ Mode = "auto"; Path = "Assets/Resources/Visuals/Props/AtlasProps.png"; Group = "ancient_ruins_props"; Min = 14; Gap = 3 },
    @{ Mode = "auto"; Path = "Assets/Resources/Visuals/Terrain/GrassIslands.png"; Group = "terrain_grass_objects"; Min = 8; Gap = 2 },
    @{ Mode = "auto"; Path = "Assets/Resources/Visuals/Terrain/StoneRuins.png"; Group = "terrain_stone_objects"; Min = 8; Gap = 2 },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Farm/House.png"; Group = "farm_house_16px_debug"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Farm/MapleTree.png"; Group = "farm_maple_16px_debug"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Farm/Woods.png"; Group = "farm_woods_16px_debug"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Props/AtlasProps.png"; Group = "ancient_ruins_16px_debug"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Terrain/GrassIslands.png"; Group = "terrain_grass_16px_debug"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Terrain/StoneRuins.png"; Group = "terrain_stone_16px_debug"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Farm/TilesetSpring.png"; Group = "tileset_spring_16px"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/Farm/Fence.png"; Group = "farm_fence_16px"; W = 16; H = 16; Skip = $true },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/UI/Icons1.png"; Group = "ui_icons1_32px"; W = 32; H = 32; Skip = $false },
    @{ Mode = "grid"; Path = "Assets/Resources/Visuals/UI/MainMenuIcons.png"; Group = "ui_main_menu_64x32"; W = 64; H = 32; Skip = $false }
)

foreach ($job in $jobs) {
    if ($job.Mode -eq "auto") {
        $items = Slice-AutoAlpha -SourcePath $job.Path -Group $job.Group -MinPixels $job.Min -MergeGap $job.Gap
    } else {
        $items = Slice-Grid -SourcePath $job.Path -Group $job.Group -CellWidth $job.W -CellHeight $job.H -SkipEmpty $job.Skip
    }

    foreach ($item in $items) { $all.Add($item) }
    New-ContactSheet -Items $items -Group $job.Group
}

$manifest = Join-Path $OutputRoot "manifest.csv"
$all | Export-Csv -Path $manifest -NoTypeInformation -Encoding UTF8
Write-Host "Generated $($all.Count) slices in $OutputRoot"
Write-Host "Manifest: $manifest"
