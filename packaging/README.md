# Screen Ink packaging

## Build

```powershell
.\package.ps1
```

Pipeline tạo self-contained single-file `win-x64`, sau đó biên dịch installer bằng Inno Setup 7. `packaging/NuGet.Publish.Config` chỉ mở nuget.org cho runtime packs cần trong publish; `NuGet.Config` ở root vẫn giữ build/test thường ngày offline.

Có thể chỉ định compiler và phiên bản:

```powershell
.\package.ps1 -Version 0.9.0-beta.1 -InnoCompiler 'C:\Path\To\ISCC.exe'
```

Output nằm trong `artifacts/installer` và bị loại khỏi Git. Trước khi phân phối, ghi SHA-256, chạy install/smoke/uninstall test và ký Authenticode nếu có certificate.
