# Embedded XAML kontrolu - App4.dll icinde resources var mi?
$dll = 'C:\Users\Simbiosis\source\repos\App4\publish\App4.dll'
Write-Host "=== App4.dll boyutu: $([Math]::Round((Get-Item $dll).Length/1MB,2)) MB ==="

# PRI sorunu: MsixContent klasoru taranmis olmali
$msix = 'C:\Users\Simbiosis\source\repos\App4\App4\App4\obj\Release\net8.0-windows10.0.19041.0\win-x64\MsixContent'
if (Test-Path $msix) {
    Write-Host "`n=== MsixContent/ (obj klasoru) ==="
    Get-ChildItem $msix -Recurse -File | Format-Table Name, Directory, Length -AutoSize
}

# Publish klasorundeki tum .xbf ve .pri
Write-Host "`n=== Publish klasorunde xbf/pri ==="
Get-ChildItem 'C:\Users\Simbiosis\source\repos\App4\publish' -Recurse -Include '*.xbf','*.pri' -File |
    Select-Object Name, Directory |
    Format-Table -AutoSize

# App4 ile baslayan her sey publishde
Write-Host "`n=== Publish'de App4.* dosyalari ==="
Get-ChildItem 'C:\Users\Simbiosis\source\repos\App4\publish' -Filter 'App4*' -File |
    Format-Table Name, Length -AutoSize
