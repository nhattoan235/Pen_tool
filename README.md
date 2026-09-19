# Screen Ink

Screen Ink là ứng dụng Windows mouse-first để khoanh, vẽ và làm nổi bật nội dung trực tiếp trên màn hình. Annotation tạm thời mặc định tự biến mất sau khoảng hai giây.

Project hiện có beta installer candidate `0.9.0-beta.2`. Product context, quyết định thiết kế và báo cáo đóng gói nằm trong [`AI_Context`](AI_Context/README.md).

## Yêu cầu phát triển

- Windows 10/11.
- .NET SDK 10.0.302 hoặc patch tương thích mới hơn.
- PowerShell.

Project không cần package bên thứ ba ở Phase 0 và có thể restore/build offline từ SDK đã cài.

## Build

```powershell
.\build.ps1
```

## Test

```powershell
.\test.ps1
```

Test harness hiện không phụ thuộc NuGet test framework. Nó trả exit code khác 0 nếu có test thất bại, phù hợp cho local script và CI tối giản.

Sau khi build, có thể kiểm tra vòng đời ứng dụng bằng:

```powershell
.\smoke-test.ps1
```

## Chạy ứng dụng

```powershell
dotnet run --project .\src\ScreenInk.App\ScreenInk.App.csproj
```

Ứng dụng khởi động trong system tray. Double-click tray icon hoặc chọn **Open Screen Ink** để mở cửa sổ trạng thái; chọn **Exit** để thoát sạch.

Bản Release phát triển hiện có shortcut tại `C:\Users\ASUS\Desktop\Screen Ink.lnk`. Ứng dụng chỉ cho phép một process Screen Ink trong mỗi Windows user session.

Trong Draw mode, nhấn `Ctrl+Z` để xóa nét gần nhất hoặc `Delete` để xóa toàn bộ. Các lệnh tương tự cũng có trong menu tray.

Nét mặc định tồn tại 1,65 giây rồi fade trong 0,35 giây. Giữ `Shift` khi bắt đầu kéo chuột để tạo nét persistent không tự biến mất.

Để mở prototype và hiện ngay cửa sổ trạng thái:

```powershell
dotnet run --project .\src\ScreenInk.App\ScreenInk.App.csproj -- --show
```

## Publish và đóng gói beta

Tạo bản self-contained `win-x64` không yêu cầu máy đích cài sẵn .NET:

```powershell
.\publish.ps1
```

Sau khi cài Inno Setup 7, tạo installer single-user:

```powershell
.\package.ps1
```

Installer mặc định cài vào `%LOCALAPPDATA%\Programs\Screen Ink`, không yêu cầu quyền administrator, tạo shortcut Start Menu và cho phép chọn thêm shortcut Desktop.

## Cấu trúc

```text
AI_Context/                 Product, UX, architecture và implementation context
src/ScreenInk.App/          WPF shell, tray và Windows integration
src/ScreenInk.App/Assets/   App icon source PNG và multi-size Windows ICO
src/ScreenInk.Core/         Logic độc lập với UI
tests/ScreenInk.Core.Tests/ Zero-dependency test harness
```
