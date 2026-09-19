# Phase 9 — Beta Packaging Report

Ngày tạo: 2026-09-19

## 1. Candidate

- Product: Screen Ink
- Version: `0.9.0-beta.2`
- Target: Windows x64, Windows 10 build 17763 trở lên
- Publish: self-contained, single-file .NET 10 WPF
- Installer: Inno Setup 7.1.0 x64, single-user/non-admin
- Install path: `%LOCALAPPDATA%\Programs\Screen Ink`

## 2. Artifact

- File: `artifacts/installer/ScreenInk-Setup-0.9.0-beta.2-win-x64.exe`
- Size: 50,960,478 bytes (48.60 MiB)
- SHA-256: `5B14CE97D253620A739C1C8FE8365C9A6C13F8F1648EC767A96230D1590A6EBE`
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
- Metadata EXE: Product `Screen Ink`, product version `0.9.0-beta.2`, file version `0.9.0.1`.
- Published EXE lifecycle smoke test: pass.
- Inno compiler parse/verification/compression: pass.
- Silent isolated install: exit code 0.
- Smoke test từ executable đã cài: exit code 0.
- Silent uninstall: exit code 0.
- Thư mục cài test còn lại sau uninstall: false.
- Startup registry value còn lại sau uninstall: không có.
- Debug/Release core suite: 36/36 pass.
- Beta.2 change: removed the rejected scroll-linked ink experiment; annotations are screen-fixed again while safe wheel pass-through remains.

## 5. Còn lại trước beta distribution

- Manual UI regression do người dùng thực hiện: Pixel/Object Eraser, Lasso move/Delete, Undo/Redo, temporary fade, Copy/Save capture, hotkeys và toolbar theme/drag.
- Code signing chưa có; chấp nhận cảnh báo SmartScreen cho private beta hoặc bổ sung certificate trước public beta.
- License/notices và reduced motion vẫn hoãn theo quyết định người dùng.
- Installer chỉ build `win-x64`; Arm64 chưa nằm trong candidate này.
