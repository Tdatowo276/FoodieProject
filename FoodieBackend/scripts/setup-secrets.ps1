# =====================================================================
#  Foodie API - Cấu hình bí mật cho môi trường Development (User Secrets)
#
#  Chạy 1 lần sau khi clone dự án (trong PowerShell, tại thư mục gốc dự án):
#      Set-ExecutionPolicy -Scope Process Bypass; .\FoodieBackend\scripts\setup-secrets.ps1
#
#  Bí mật được lưu NGOÀI thư mục dự án (%APPDATA%\Microsoft\UserSecrets\...),
#  nên không bao giờ bị commit lên Git.
# =====================================================================

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\FoodieAPI\FoodieAPI.csproj'

function Set-Secret([string]$key, [string]$value) {
    dotnet user-secrets set $key $value --project $project | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Không đặt được '$key'." }
}

Write-Host '=== Cấu hình bí mật cho Foodie API ===' -ForegroundColor Cyan

# 1. Chuỗi kết nối SQL Server
$defaultConn = 'Server=.\SQLEXPRESS;Database=FoodieDB;Trusted_Connection=True;TrustServerCertificate=True;'
Write-Host "`n[1/3] Chuỗi kết nối SQL Server"
Write-Host "      Mặc định: $defaultConn"
$conn = Read-Host '      Nhấn Enter để dùng mặc định, hoặc nhập chuỗi khác'
if ([string]::IsNullOrWhiteSpace($conn)) { $conn = $defaultConn }
Set-Secret 'ConnectionStrings:Default' $conn

# 2. Khóa ký JWT: sinh ngẫu nhiên 48 byte (mỗi máy một khóa riêng)
Write-Host "`n[2/3] Sinh khóa ký JWT ngẫu nhiên..."
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
Set-Secret 'Jwt:Key' ([Convert]::ToBase64String($bytes))
Write-Host '      Đã đặt Jwt:Key.'

# 3. Mật khẩu tài khoản admin được tạo lần đầu (admin@foodie.vn)
Write-Host "`n[3/3] Mật khẩu cho tài khoản admin (tối thiểu 8 ký tự)"
while ($true) {
    $p1 = Read-Host '      Nhập mật khẩu' -AsSecureString
    $p2 = Read-Host '      Nhập lại' -AsSecureString
    $s1 = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($p1))
    $s2 = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($p2))
    if ($s1 -ne $s2) { Write-Host '      Hai lần nhập không khớp, thử lại.' -ForegroundColor Yellow; continue }
    if ($s1.Length -lt 8) { Write-Host '      Mật khẩu quá ngắn, thử lại.' -ForegroundColor Yellow; continue }
    break
}
Set-Secret 'Seed:AdminPassword' $s1

Write-Host "`nHoàn tất! Các khóa đã cấu hình:" -ForegroundColor Green
dotnet user-secrets list --project $project | ForEach-Object { ($_ -split ' = ')[0] }
