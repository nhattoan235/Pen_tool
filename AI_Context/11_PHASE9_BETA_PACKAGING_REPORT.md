# Phase 9 — Beta Packaging Report

Ngày tạo: 2026-09-14

## 1. Candidate

- Product: Screen Ink
- Version: `0.9.0-beta.1`
- Target: Windows x64, Windows 10 build 17763 trở lên
- Publish: self-contained, single-file .NET 10 WPF
- Installer: Inno Setup 7.1.0 x64, single-user/non-admin
- Install path: `%LOCALAPPDATA%\Programs\Screen Ink`

## 2. Artifact

- File: `artifacts/installer/ScreenInk-Setup-0.9.0-beta.1-win-x64.exe`
- Size: 50,967,388 bytes (48.61 MiB)
- SHA-256: `BA12503E13E03844E85534D1DC40E9AD948E8ECEB32364BE8E2083964FE09E92`
- Authenticode: chưa ký; có thể bị Windows SmartScreen cảnh báo trên máy chưa có reputation.

Artifact build không được commit vào Git. Source of truth là `publish.ps1`, `package.ps1`, `packaging/ScreenInk.iss` và `packaging/NuGet.Publish.Config`.

## 3. Hành vi installer

- `PrivilegesRequired=lowest`; không yêu cầu administrator.
- Cài dưới Local AppData theo user hiện tại.
- Tạo Start Menu shortcut; Desktop shortcut là lựa chọn, mặc định bỏ chọn.
- Cho phép đóng `ScreenInk.exe` khi nâng cấp nếu file đang được dùng.
- Uninstall xóa giá trị `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ScreenInk` nếu app đã bật Start with Windows.
- Post-install có lựa chọn chạy Screen Ink; silent install không tự chạy.

## 4. Kết quả xác minh

- Self-contained publish tạo đúng một `ScreenInk.exe`, 173,314,215 bytes.
- Metadata EXE: Product `Screen Ink`, product version `0.9.0-beta.1`, file version `0.9.0.0`.
- Published EXE lifecycle smoke test: pass.
- Inno compiler parse/verification/compression: pass.
- Silent isolated install: exit code 0.
- Smoke test từ executable đã cài: exit code 0.
- Silent uninstall: exit code 0.
- Thư mục cài test còn lại sau uninstall: false.
- Startup registry value còn lại sau uninstall: không có.
- Debug/Release core suite: 36/36 pass.

## 5. Còn lại trước beta distribution

- Manual UI regression do người dùng thực hiện: Pixel/Object Eraser, Lasso move/Delete, Undo/Redo, temporary fade, Copy/Save capture, hotkeys và toolbar theme/drag.
- Code signing chưa có; chấp nhận cảnh báo SmartScreen cho private beta hoặc bổ sung certificate trước public beta.
- License/notices và reduced motion vẫn hoãn theo quyết định người dùng.
- Installer chỉ build `win-x64`; Arm64 chưa nằm trong candidate này.
