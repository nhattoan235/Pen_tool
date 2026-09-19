# Technical Architecture

Ngày cập nhật: 2026-09-14

Tài liệu này mô tả kiến trúc đề xuất. Những lựa chọn liên quan tới renderer sẽ được xác nhận bằng technical spike trước khi khóa.

## 1. Stack đề xuất

- Ngôn ngữ: C#.
- Runtime: .NET 8 hoặc bản LTS phù hợp tại thời điểm triển khai.
- UI shell: WPF.
- Native interop: Win32 qua P/Invoke.
- Unit tests: xUnit hoặc NUnit; quyết định khi scaffold solution.
- Logging: structured local logging, không gửi telemetry mặc định.

Lý do chọn WPF cho MVP:

- Phù hợp ứng dụng Windows desktop chạy nền.
- Dễ dựng transparent topmost window và toolbar.
- Có sẵn stylus/mouse input primitives.
- Tích hợp Win32 thuận tiện cho global hotkey, click-through và multi-monitor.

## 2. Quyết định renderer cần spike

Không dùng mặc định `InkCanvas` như kết quả hình ảnh cuối cùng nếu chất lượng nét không đạt yêu cầu. Hai hướng cần prototype và so sánh:

### Hướng A — WPF custom DrawingVisual/Geometry

- Ít dependency.
- Tích hợp trực tiếp với WPF.
- Dễ đóng gói.
- Cần tự xây variable-width outline và tối ưu invalidation.

### Hướng B — SkiaSharp surface trong WPF

- Kiểm soát stroke và anti-aliasing tốt.
- Thuận lợi cho path/vector và render hiệu năng cao.
- Thêm native dependency và tăng độ phức tạp đóng gói.

Spike phải đo:

- Độ trễ cảm nhận khi vẽ.
- Chất lượng vòng tròn vẽ chậm bằng chuột.
- CPU/GPU khi vẽ liên tục trên màn hình 4K.
- Hành vi trên DPI 100–200%.
- Độ phức tạp của fade animation nhiều stroke.

## 3. Kiến trúc process

Một process chạy trong user session:

```text
ScreenInk.App
├─ TrayHost
├─ HotkeyService
├─ DisplayTopologyService
├─ OverlayManager
│  └─ OverlayWindow[per monitor]
├─ InputRouter
├─ StrokeEngine
├─ ShapeRecognitionService
├─ AnnotationDocument
├─ ToolbarHost
├─ CaptureService
├─ SettingsService
└─ DiagnosticsService
```

## 4. Overlay windows

- Tạo một borderless window trên mỗi monitor thay vì một window khổng lồ phủ virtual desktop.
- Window luôn topmost trong Draw/Select nhưng không cố vượt qua secure desktop hoặc exclusive fullscreen.
- Nền trong suốt theo pixel.
- Pointer mode bật click-through bằng extended window styles/hit testing.
- Draw/Select mode tắt click-through và nhận input.
- Toolbar có thể là window riêng để vẫn nhận click khi canvas đang pass-through.
- Không xuất hiện trong Alt+Tab hoặc taskbar.

### DPI

- Process khai báo Per-Monitor V2 awareness.
- Annotation lưu theo tọa độ logic gắn với monitor và có transform rõ ràng sang physical pixels.
- Lắng nghe thay đổi display topology và DPI.
- Không lưu raw absolute coordinates mà thiếu monitor identity/version.

## 5. Input routing

### Mouse path

1. Overlay nhận pointer down trong Draw mode.
2. InputRouter tạo stroke session.
3. Mouse move được đưa vào bộ thu thập điểm.
4. StrokeEngine render preview ngay lập tức.
5. Mouse up hoàn tất geometry và tạo AnnotationObject.
6. Lifetime service gán object vào temporary group.

### An toàn input

- `Esc` và emergency hotkey được xử lý ở cấp app/global hotkey.
- Right-click trong Draw/Select chuyển về Pointer.
- Khi có unhandled exception ở overlay/input, fallback phải xóa input-capturing style hoặc đóng overlay.
- Low-level mouse hook xử lý modifier+wheel trong Draw, phát hiện click ngoài toolbar trong Pointer và chỉ enqueue yêu cầu mở cửa sổ click-through cho wheel thường. Callback tuyệt đối không đổi window style, không gọi `SendInput` và không làm I/O. UI dispatcher mở pass-through trong 350 ms; các wheel tiếp theo trong burst được Windows hit-test tự nhiên tới ứng dụng bên dưới.

## 6. Data model

```text
AnnotationDocument
└─ AnnotationLayer[]
   └─ AnnotationObject[]
      ├─ StrokeObject
      ├─ ShapeObject
      └─ GroupObject
```

Thuộc tính chung:

- Stable ID.
- Monitor ID và coordinate space.
- Bounding box.
- Z-order.
- Color, opacity và blend mode.
- Temporary/Persistent.
- CreatedAt, ExpiresAt và fade state.
- Selected/locked state.

`StrokeObject` lưu raw points có giới hạn và geometry đã xử lý. Raw points giúp tinh chỉnh smoothing hoặc hoàn tác snap khi cần.

`ShapeObject` lưu semantic shape thay vì chỉ lưu path để có thể resize và chỉnh sửa đẹp.

## 7. Temporary lifetime service

- Dùng monotonic clock, không dùng wall-clock để tính animation.
- Timer logic độc lập với frame rate.
- Nhóm stroke theo khoảng cách thời gian 400 ms.
- Lifetime 2.000 ms bắt đầu sau khi nhóm được coi là kết thúc.
- 1.650 ms visible + 350 ms opacity easing về 0.
- Object hết hạn được loại khỏi document sau frame fade cuối.
- Khi app bị lag, chuyển trực tiếp tới opacity đúng theo thời gian thực thay vì kéo dài lifetime.

## 8. Stroke engine

Các bước dự kiến:

1. Thu thập timestamped pointer points.
2. Loại duplicate và micro-jitter.
3. Resample theo khoảng cách/thời gian.
4. Tính vận tốc cục bộ.
5. Adaptive smoothing: mạnh hơn khi chậm, ít hơn khi đổi hướng nhanh.
6. Tạo centerline curve.
7. Tạo outline/brush geometry.
8. Render preview và final geometry.

Không khóa thuật toán trước technical spike. Các ứng viên ban đầu:

- One Euro filter cho dữ liệu con trỏ.
- Catmull–Rom chuyển sang cubic Bézier cho centerline.
- Outline variable-width với round cap/join.

Các thuật toán phải được đánh giá bằng cảm giác sử dụng, không chỉ bằng ảnh tĩnh.

## 9. Capture

- Capture chỉ chạy sau thao tác chủ động.
- Tạm ẩn toolbar trước capture.
- Kết hợp ảnh màn hình và annotation bằng coordinate transform chính xác.
- Hỗ trợ clipboard PNG trước, lưu file sau.
- Không duy trì buffer ảnh màn hình nền khi không capture.

## 10. Settings và persistence

Lưu cục bộ:

- Hotkeys.
- Current tool, màu và độ dày.
- Temporary/Persistent preference.
- Toolbar position theo monitor.
- Theme và accessibility preferences.

Không lưu annotation tạm thời qua lần khởi động lại.

## 11. Hiệu năng mục tiêu

Các mục tiêu ban đầu để đo trong prototype:

- Render tương tác ở refresh rate của màn hình khi có thể.
- Không block UI thread bằng shape recognition hoặc export.
- Tránh full-screen redraw nếu chỉ một vùng nhỏ thay đổi.
- Không tăng CPU đáng kể khi ở Pointer/idle.
- Hỗ trợ ít nhất hai màn hình và một màn hình 4K trong test thực tế.

## 12. Bảo mật và riêng tư

- Chạy với quyền user thường.
- Không yêu cầu administrator.
- Không ghi hoặc gửi nội dung màn hình khi chưa có lệnh capture.
- Không có network dependency trong MVP.
- Log không chứa ảnh chụp hoặc tọa độ nội dung chi tiết mặc định.

## 13. Rủi ro kỹ thuật chính

- Transparent overlay và click-through khác nhau giữa cấu hình GPU/Windows.
- Multi-monitor có DPI và tọa độ âm.
- Một số exclusive fullscreen app có thể phủ trên topmost window.
- Toolbar/canvas có thể xuất hiện trong phần mềm quay màn hình tùy capture method.
- Smoothing quá mạnh gây lag; quá yếu làm nét rung.
- WPF renderer có thể không đủ đẹp/nhanh cho variable-width stroke ở 4K.

Mỗi rủi ro trên phải có test hoặc spike trước khi mở rộng tính năng.
