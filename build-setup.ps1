# =============================================================================
#  Simbiosis Leak Test App -- Setup Build Script
#
#  Ne yapar:
#    1) Eski publish/dist klasorlerini temizler
#    2) dotnet publish ile self-contained x64 Release ciktisi uretir
#    3) WebView2 Runtime installer'i indirir (yoksa)
#    4) Inno Setup ile Setup .exe uretir
#
#  Kullanim:
#    powershell -ExecutionPolicy Bypass -File build-setup.ps1
#    powershell -ExecutionPolicy Bypass -File build-setup.ps1 -SkipPublish
#    powershell -ExecutionPolicy Bypass -File build-setup.ps1 -Configuration Debug
# =============================================================================

param(
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64",
    [switch]$SkipPublish,
    [switch]$SkipInstaller,
    [switch]$KeepPublishDir,
    # Versiyon davranisi:
    #   -BumpVersion  : patch numarasini 1 artir (default: ON - her build yeni versiyon)
    #   -NoBump       : versiyonu oldugu gibi birak (hotfix / re-build icin)
    #   -SetVersion "1.2.3" : elle versiyon belirle
    [switch]$NoBump,
    [string]$SetVersion = ""
)

$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date

# --- Renk yardimcilari ---
function Write-Step([string]$msg)  { Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)    { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-Warn([string]$msg)  { Write-Host "  !!  $msg" -ForegroundColor Yellow }
function Write-Err([string]$msg)   { Write-Host "  XX  $msg" -ForegroundColor Red }

# --- Yollar ---
$RepoRoot      = $PSScriptRoot
$CsprojPath    = Join-Path $RepoRoot "App4\App4\App4.csproj"
$PublishDir    = Join-Path $RepoRoot "publish"
$DistDir       = Join-Path $RepoRoot "dist"
$InstallerDir  = Join-Path $RepoRoot "installer"
$IssFile       = Join-Path $InstallerDir "SimbiosisLeakTestApp.iss"
$RedistDir     = Join-Path $InstallerDir "redist"
$WebView2Exe   = Join-Path $RedistDir "MicrosoftEdgeWebView2RuntimeInstaller.exe"
$WebView2Url   = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"

# --- Inno Setup bul ---
$IsccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$ISCC = $null
foreach ($c in $IsccCandidates) {
    if (Test-Path -LiteralPath $c) { $ISCC = $c; break }
}

Write-Step "============================================================"
Write-Step " Simbiosis Leak Test App -- Setup Builder"
Write-Step "============================================================"
Write-Host "  Repo root     : $RepoRoot"
Write-Host "  Config        : $Configuration"
Write-Host "  Runtime       : $Runtime"
Write-Host "  Publish dir   : $PublishDir"
Write-Host "  Dist dir      : $DistDir"
Write-Host "  ISCC.exe      : $(if ($ISCC) { $ISCC } else { '(bulunamadi)' })"

# --- On kontroller ---
if (-not (Test-Path -LiteralPath $CsprojPath)) {
    Write-Err "csproj bulunamadi: $CsprojPath"
    exit 1
}
if (-not $SkipInstaller -and -not $ISCC) {
    Write-Err "Inno Setup kurulu degil. Yuklemek icin:"
    Write-Err "    winget install -e --id JRSoftware.InnoSetup"
    exit 1
}

# =============================================================================
# VERSIYON YONETIMI
#   .iss dosyasindaki "#define AppVersion "x.y.z"" satirini okur:
#     -SetVersion "1.2.3"  -> elle yaz
#     -NoBump              -> olani koru
#     (default)            -> patch +1 (1.0.0 -> 1.0.1 -> 1.0.2 ...)
#   Her setup dosya adi otomatik yeni versiyonu alir (OutputBaseFilename).
# =============================================================================
if (-not $SkipInstaller -and (Test-Path -LiteralPath $IssFile)) {
    $issContent = Get-Content -LiteralPath $IssFile -Raw
    $versionRegex = '#define\s+AppVersion\s+"(\d+)\.(\d+)\.(\d+)"'
    $match = [regex]::Match($issContent, $versionRegex)
    if (-not $match.Success) {
        Write-Warn "AppVersion tanimi .iss'te bulunamadi - versiyon bump atlaniyor."
    } else {
        $curVersion = "$($match.Groups[1].Value).$($match.Groups[2].Value).$($match.Groups[3].Value)"
        $newVersion = $curVersion

        if (-not [string]::IsNullOrWhiteSpace($SetVersion)) {
            if ($SetVersion -notmatch '^\d+\.\d+\.\d+$') {
                Write-Err "-SetVersion formati hatali: '$SetVersion' (beklenen: x.y.z)"
                exit 1
            }
            $newVersion = $SetVersion
        } elseif (-not $NoBump) {
            $major = [int]$match.Groups[1].Value
            $minor = [int]$match.Groups[2].Value
            $patch = [int]$match.Groups[3].Value + 1
            $newVersion = "$major.$minor.$patch"
        }

        if ($newVersion -ne $curVersion) {
            Write-Step "Versiyon: $curVersion -> $newVersion"
            $issContent = [regex]::Replace(
                $issContent, $versionRegex, "#define AppVersion `"$newVersion`"")
            Set-Content -LiteralPath $IssFile -Value $issContent -Encoding UTF8 -NoNewline
            Write-Ok "ISS guncellendi (AppVersion=$newVersion)"
        } else {
            Write-Ok "Versiyon korundu: $curVersion"
        }
    }
}

# --- WebView2 Runtime indir (eksikse) ---
if (-not (Test-Path -LiteralPath $WebView2Exe)) {
    Write-Step "WebView2 Runtime installer indiriliyor..."
    try {
        New-Item -ItemType Directory -Force -Path $RedistDir | Out-Null
        Invoke-WebRequest -Uri $WebView2Url -OutFile $WebView2Exe -UseBasicParsing
        $size = [math]::Round((Get-Item $WebView2Exe).Length / 1MB, 2)
        Write-Ok "Indirildi: $WebView2Exe ($size MB)"
    } catch {
        Write-Err "Indirme hatasi: $_"
        Write-Warn "Manuel indirmek icin: $WebView2Url"
        Write-Warn "Dosyayi $RedistDir klasorune koyun ve tekrar calistirin."
        exit 1
    }
} else {
    Write-Ok "WebView2 Runtime installer mevcut."
}

# --- Temizlik ---
if (-not $SkipPublish) {
    if (Test-Path -LiteralPath $PublishDir) {
        Write-Step "Eski publish klasoru temizleniyor..."
        Remove-Item -LiteralPath $PublishDir -Recurse -Force
        Write-Ok "Silindi: $PublishDir"
    }
}

# --- PUBLISH ---
if (-not $SkipPublish) {
    Write-Step "dotnet publish baslatiliyor..."
    $publishArgs = @(
        "publish", $CsprojPath,
        "-c", $Configuration,
        "-r", $Runtime,
        "--self-contained",
        "-o", $PublishDir,
        "--nologo",
        "-v", "minimal"
    )
    Write-Host "  Komut : dotnet $($publishArgs -join ' ')"
    $publishStart = Get-Date
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Err "dotnet publish basarisiz (exit $LASTEXITCODE)"
        exit $LASTEXITCODE
    }
    $publishDuration = ((Get-Date) - $publishStart).TotalSeconds
    Write-Ok ("Publish tamamlandi ({0:N1} sn)" -f $publishDuration)

    # Publish ciktisi dogrulama
    $publishedExe = Join-Path $PublishDir "SimbiosisLeakTestApp.exe"
    if (-not (Test-Path -LiteralPath $publishedExe)) {
        Write-Err "SimbiosisLeakTestApp.exe publish cikti klasorunde bulunamadi: $publishedExe"
        exit 1
    }
    $sizeBytes = (Get-ChildItem -LiteralPath $PublishDir -Recurse -File |
                  Measure-Object -Property Length -Sum).Sum
    $sizeMB = [math]::Round($sizeBytes / 1MB, 1)
    $fileCount = (Get-ChildItem -LiteralPath $PublishDir -Recurse -File).Count
    Write-Ok "Publish boyutu : $sizeMB MB ($fileCount dosya)"
} else {
    Write-Warn "Publish atlandi (-SkipPublish)"
    if (-not (Test-Path -LiteralPath (Join-Path $PublishDir "SimbiosisLeakTestApp.exe"))) {
        Write-Err "Publish klasorunde SimbiosisLeakTestApp.exe yok. -SkipPublish parametresini kaldirin."
        exit 1
    }
}

# --- INNO SETUP DERLEME ---
if (-not $SkipInstaller) {
    if (-not (Test-Path -LiteralPath $IssFile)) {
        Write-Err "ISS dosyasi bulunamadi: $IssFile"
        exit 1
    }

    Write-Step "Inno Setup derleniyor..."
    New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

    $isccStart = Get-Date
    & $ISCC $IssFile
    if ($LASTEXITCODE -ne 0) {
        Write-Err "ISCC basarisiz (exit $LASTEXITCODE)"
        exit $LASTEXITCODE
    }
    $isccDuration = ((Get-Date) - $isccStart).TotalSeconds
    Write-Ok ("ISCC tamamlandi ({0:N1} sn)" -f $isccDuration)

    # Ciktiyi bul
    $setupFile = Get-ChildItem -LiteralPath $DistDir -Filter "Setup_*.exe" |
                 Sort-Object LastWriteTime -Descending |
                 Select-Object -First 1
    if ($setupFile) {
        $setupSize = [math]::Round($setupFile.Length / 1MB, 2)
        Write-Ok "Setup olusturuldu:"
        Write-Host "    $($setupFile.FullName)  ($setupSize MB)" -ForegroundColor White
    }
}

# --- Opsiyonel: Publish klasorunu temizle ---
if (-not $KeepPublishDir -and -not $SkipInstaller) {
    Write-Step "Publish ara klasoru siliniyor (-KeepPublishDir ile korunabilir)..."
    Remove-Item -LiteralPath $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Ok "Silindi: $PublishDir"
}

# --- Ozet ---
$totalDuration = ((Get-Date) - $script:StartTime).TotalSeconds
Write-Step "============================================================"
Write-Ok ("Tamamlandi. Toplam sure: {0:N1} sn" -f $totalDuration)
Write-Step "============================================================"
