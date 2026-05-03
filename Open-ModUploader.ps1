$uploader = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\ModUploader.exe"

if (-not (Test-Path $uploader)) {
    throw "ModUploader.exe not found at $uploader"
}

Start-Process -FilePath $uploader
Write-Host "Opened: $uploader"
