$exePath = 'C:\Users\Simbiosis\source\repos\App4\publish\App4.exe'
$workDir = 'C:\Users\Simbiosis\source\repos\App4\publish'

# Eski crash log varsa sil
Remove-Item 'C:\Simbiosis\SimbiosisLeakTestApp\startup-crash.log' -ErrorAction SilentlyContinue

$proc = Start-Process $exePath -WorkingDirectory $workDir -PassThru
Start-Sleep -Seconds 10
try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch {}
Get-Process -Name 'App4' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$file = 'C:\Simbiosis\SimbiosisLeakTestApp\Config\Automation_Settings.json'
$crashLog = 'C:\Simbiosis\SimbiosisLeakTestApp\startup-crash.log'

if (Test-Path $file) {
    Write-Host "OK - Automation_Settings.json olusturuldu:"
    Write-Host (Get-Item $file).Length "bytes"
} else {
    Write-Host "UYARI - Automation_Settings.json YOK"
}

if (Test-Path $crashLog) {
    Write-Host "`n=== CRASH LOG ==="
    Get-Content $crashLog -Raw
}
