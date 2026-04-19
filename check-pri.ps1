Get-ChildItem 'C:\Users\Simbiosis\source\repos\App4\App4\App4\obj\Release\net8.0-windows10.0.19041.0\win-x64' -File |
    Where-Object { $_.Name -like '*.pri' -or $_.Name -like 'App4.*' -or $_.Name -like '*resource*' } |
    Format-Table Name, Length -AutoSize

Write-Host "`n=== Subfolders ==="
Get-ChildItem 'C:\Users\Simbiosis\source\repos\App4\App4\App4\obj\Release\net8.0-windows10.0.19041.0\win-x64' -Directory |
    Format-Table Name
