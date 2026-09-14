# Project Status

Ngày cập nhật: 2026-09-14

## Trạng thái tổng thể

**Phase 8 được tạm hoãn sau full-screen capture; Phase 9 — Beta readiness đang thực hiện, với Color Gate bắt buộc trước packaging.**

Phase 0–5 hoàn thành ở mức MVP. Phase 6 đã có shape snap và Lasso selection; Arrow giữ nguyên, Recolor/Pin selection được hoãn. Phase 7 mới được thêm riêng cho chất lượng nét Pen. Capture và Packaging lần lượt chuyển thành Phase 8 và Phase 9.

## Đã hoàn thành

- [x] Xác định bài toán screen annotation overlay.
- [x] Khảo sát nhóm tính năng của Apple Markup, Samsung Screen Write, Windows Snipping Tool, Epic Pen, ZoomIt và Adobe Acrobat.
- [x] Chốt Windows-first.
- [x] Chốt mouse-first.
- [x] Chốt Apple Markup là nguồn cảm hứng UX chính.
- [x] Chốt compact/auto-minimizing toolbar.
- [x] Chốt temporary ink khoảng 2 giây.
- [x] Xác định stroke quality là product gate.
- [x] Viết product brief, UX spec, kiến trúc, implementation plan, test plan và decision log.
- [x] Scaffold .NET 10 solution với WPF app, core library và zero-dependency test harness.
- [x] Build scripts và offline NuGet configuration.
- [x] System tray lifecycle và local file logging.
- [x] Per-monitor transparent overlay.
- [x] Pointer/Draw state machine và click-through styles.
- [x] `Ctrl+Shift+D`, click phải và `Esc` safety paths.
- [x] Raw polyline input proof of concept.
- [x] Build sạch 0 warning/0 error; 5/5 tests pass; smoke-test pass.
- [x] Sửa layered-window alpha hit-testing dựa trên log thực tế.
- [x] Người dùng xác nhận overlay nhận và vẽ được bằng chuột.
- [x] `Delete`, tray Clear drawings và `Ctrl+Z`/Undo last stroke.
- [x] Phase 2 WPF smooth-stroke Candidate A.
- [x] Adaptive smoothing, micro-jitter filtering và Bézier geometry.
- [x] Candidate A bị loại dựa trên ảnh và đánh giá trực tiếp của người dùng.
- [x] Candidate B: filled outline, velocity-based width và start/end taper.
- [x] Build sạch; 11/11 tests pass; lifecycle smoke-test pass.
- [x] Người dùng đánh giá Candidate B là ổn; chọn WPF renderer cho MVP.
- [x] Temporary lifetime manager và grouping 400 ms.
- [x] Fade 1,65 giây + 0,35 giây.
- [x] Shift+drag persistent stroke.
- [x] Phase 3 build sạch; 16/16 tests pass.
- [x] Người dùng chấp nhận timing/fade temporary ink của Phase 3.
- [x] Phase 4 compact capsule: Pointer, Pen, Color, Undo và More.
- [x] Collapsed dot 38 px và auto-collapse khi stroke bắt đầu.
- [x] Drag/snap cạnh trái-phải và lưu display/vị trí dọc.
- [x] Phase 4 prototype build sạch; 17/17 tests và lifecycle smoke-test pass.
- [x] Phím số `1`–`5` và numpad chọn trực tiếp 5 màu khi Draw mode hoạt động.
- [x] Collapsed dot phản ánh ngay màu hiện tại.
- [x] Color popover hiển thị đủ palette và số tắt tương ứng.
- [x] Theme sáng/tối và lưu màu/theme gần nhất.
- [x] Phase 4 Release build sạch; 18/18 automated tests pass.
- [x] Người dùng xác nhận Phase 4 và đổi màu bằng phím số hoạt động ổn.
- [x] Phase 5 Pen/Highlighter với ba cỡ nét.
- [x] Tool popover không kéo dài capsule năm nút.
- [x] Temporary/Persistent mode.
- [x] Pixel Eraser và Object Eraser.
- [x] Unified Undo/Redo history cho nét và thao tác tẩy.
- [x] Cursor preview theo màu/cỡ/công cụ.
- [x] Phase 5 Release build sạch; 20/20 automated tests pass.
- [x] Sửa discoverability: nút Current Tool luôn mở danh sách và có chevron.
- [x] Bỏ drop-shadow hoàn toàn trong light theme theo phản hồi ảnh thực tế.
- [x] Cursor Eraser hai lớp tương phản cao, có accent màu theo palette.
- [x] Pixel Eraser cursor vuông `+`; Object Eraser cursor bo tròn `×`, không nhầm với Highlighter.
- [x] `Shift+wheel` chuyển tool hai chiều trong Draw; không chặn tổ hợp ở Pointer.
- [x] Phase 6 slice A: hold-to-snap Line/Ellipse/Rectangle và Undo snap về freehand.
- [x] Shape recognizer có positive/negative tests; toàn suite 25/25 đạt.
- [x] Người dùng xác nhận hold-to-snap Phase 6 slice A hoạt động ổn.
- [x] Thay modifier-wheel WPF bằng `WH_MOUSE_LL` do overlay không focus.
- [x] Auto-collapse khi click ngoài toolbar ở Pointer mode.
- [x] Nút Redo hiển thị cạnh Undo; toolbar hiện rộng 224 px.
- [x] `Delete`/Clear drawings là một composite history action; `Ctrl+Z` phục hồi và `Ctrl+Y` xóa lại.
- [x] Batch modifier-wheel thật bằng timer 45 ms; debounce preferences 350 ms.
- [x] Chỉ render cursor/feedback trên overlay đang có chuột; overlay khác cập nhật lazy khi chuột đi vào.
- [x] Chuyển ghi toolbar preferences sang async I/O ngoài UI thread.
- [x] Phase 6 Arrow: giữ implementation hiện tại, không ưu tiên tinh chỉnh theo quyết định người dùng.
- [x] Arrow positive test nâng toàn suite lên 26/26 tests.
- [x] Lasso là tool thứ năm trong menu và vòng `Shift+wheel`.
- [x] Khoanh chọn nhiều stroke, viền selection và kéo di chuyển.
- [x] `Delete` ưu tiên xóa selection; Move/Delete tham gia Undo/Redo.
- [x] Temporary lifetime tạm dừng khi được chọn và chạy lại khi bỏ chọn.
- [x] Bản hiện tại build sạch 0 warning/0 error; 26/26 automated tests và lifecycle smoke-test đạt.
- [x] Người dùng xác nhận Lasso cơ bản hoạt động tốt và yêu cầu tạm dừng.
- [x] Thêm Phase 7 riêng cho Pen Stroke Quality Hardening như một release gate.
- [x] Phase 7A: giữ raw mouse samples và opt-in JSON recorder chạy background.
- [x] Phase 7A: closed-loop seam không còn bị taper thành mũi nhọn; có regression test.
- [x] Phase 7A Release build sạch 0 warning/0 error; 28/28 automated tests đạt.
- [x] Thu và tổng hợp baseline thật: 105 stroke, event trung vị 8 ms, geometry outlier lớn nhất 3,363 ms.
- [x] Phase 7B được đo trên 56 Pen stroke và bị loại: build/raw vẫn 1,00, worst build 3,37 ms; đã rollback coalescing.
- [x] Xác định smoothing lag là nút thắt chính: 4,96 px median, 10,65 px p95, 33,20 px max.
- [x] Phase 7C candidate: tuning adaptive smoothing theo tốc độ chuột thật, dự kiến giảm lag p95 còn khoảng 4,07 px.
- [x] Thu 30 Pen stroke Candidate 7C: lag 1,71 px median, 2,00 px p95; lần lượt giảm 65,5% và 81,2% so với 7B.
- [x] Candidate 7C đạt 29/29 automated tests và lifecycle smoke-test.
- [x] Người dùng xác nhận cảm giác bám chuột của 7C khá tốt.
- [x] Phase 7D: bỏ taper nhọn, thêm round cap thật và đóng quadratic geometry đối xứng.
- [x] Phase 7D đạt 30/30 automated tests và lifecycle smoke-test trước khi gắn nhãn recorder.
- [x] Phase 7E: bỏ auto closed-topology; seam dùng hai round cap chồng mực ổn định.
- [x] Phase 7E Natural Pen: velocity pressure scale dự kiến mở từ 1,05–1,11× thành 0,80–1,21× trên corpus thật.
- [x] Phase 7E đạt 31/31 automated tests và lifecycle smoke-test.
- [x] Người dùng đánh giá Natural Pen 7E khá tốt; yêu cầu tăng nhẹ độ nhạy độ dày.
- [x] Phase 7F được replay trên 90 stroke: chọn mức width range trung vị 0,41× thay vì candidate mạnh hơn 0,43–0,46×.
- [x] Người dùng xác nhận 7F là chất lượng nét tốt nhất và cho phép chuyển phase.
- [x] Khóa Natural Pen 7F: smoothing bám chuột, round caps, open-seam overlap và velocity width.
- [x] Phase 8 slice A: Copy full screen vào clipboard và Save full screen PNG trong More.
- [x] Capture dùng toàn virtual desktop, hỗ trợ tọa độ monitor âm và tự ẩn toolbar/ContextMenu trước khi chụp.
- [x] Phase 8 slice A build sạch; 31/31 automated tests và lifecycle smoke-test đạt.
- [x] Người dùng xác nhận full-screen Copy/Save của Phase 8 hoạt động ổn và yêu cầu tạm hoãn phần còn lại.
- [x] Phase 9A: Settings chọn bốn hotkey preset; đăng ký mới thất bại sẽ tự phục hồi hotkey cũ.
- [x] Phase 9A: Start with Windows theo HKCU, mặc định tắt và lưu riêng trong app preferences.
- [x] App preferences ghi qua file tạm rồi replace để giảm nguy cơ file cấu hình dở dang.
- [x] Cửa sổ trạng thái được nâng thành Settings với nhãn accessibility cơ bản.
- [x] Phase 9A build sạch; 31/31 automated tests và lifecycle smoke-test đạt.
- [x] Tạo app icon bằng built-in ImageGen và lưu source PNG + multi-size ICO trong project.
- [x] Gắn icon vào EXE, MainWindow và system tray.
- [x] Tạo shortcut `C:\Users\ASUS\Desktop\Screen Ink.lnk` trỏ tới bản Release.
- [x] Single-instance mutex; kiểm tra mở hai lần chỉ còn đúng một Release process.
- [x] Crash-safe session marker; clean exit xóa marker, phiên lỗi quay về Pointer và bỏ volatile overlay state.
- [x] Debug + Release build sạch 0 warning/0 error; 31/31 tests và smoke-test đạt sau thay đổi Phase 9.
- [x] Color Gate chọn hướng Natural theo quyết định người dùng; không thêm swatch hoặc tự lấy mẫu nền.
- [x] Natural Ink candidate: base + pressure-sensitive ink core, sắc độ chênh nhẹ và không dùng shadow.
- [x] Ink core tham gia fade, Pixel Eraser, Lasso move, shape snap, detach/restore và Undo/Redo.
- [x] Collapsed toolbar dot có cơ chế click để mở và drag để di chuyển/snap cạnh; đang chờ xác nhận lần cuối trên máy thật.
- [x] Natural Ink candidate build sạch; 31/31 tests và lifecycle smoke-test đạt.
- [x] Candidate A bị đánh giá quá nhẹ; collapsed dot dùng WPF DragMove không hoạt động thực tế.
- [x] Natural Ink B tăng rõ ink-core depth/coverage; collapsed drag dùng physical cursor + HWND positioning.
- [x] Candidate B build sạch; 31/31 tests và lifecycle smoke-test đạt.
- [x] PreviewMouseMove/DragMove của collapsed Button bị loại sau hai lần thất bại thực tế.
- [x] Collapsed drag chuyển sang low-level hook có physical HWND hit-test và chỉ track move/up cho đúng gesture.
- [x] Hook-based collapsed drag build sạch; 31/31 tests và lifecycle smoke-test đạt.
- [x] Hook-based drag bị loại sau khi vẫn thất bại thực tế; thay bằng native caption drag `WM_NCLBUTTONDOWN(HTCAPTION)`.
- [x] Xóa drop-shadow hoàn toàn cho cả dark/light toolbar, ở cả XAML lẫn theme runtime.
- [x] Native caption drag candidate build sạch; 31/31 tests và lifecycle smoke-test đạt.
- [x] Người dùng xác nhận native caption drag hoạt động tốt và toolbar dark không còn bóng gây khó chịu.
- [x] Đặc tả Phase 7G Signature Ink Polish với baseline A và kế hoạch Candidate B/C.
- [x] Candidate 7G-B: ink-core speed mapping nhạy hơn và corner pooling theo turn/slow-motion weight.
- [x] Candidate 7G-B giữ nguyên Natural Pen 7F main outline; thêm hai geometry regression tests.
- [x] Candidate 7G-B build sạch; 33/33 tests và lifecycle smoke-test đạt.
- [x] Người dùng xác nhận corner pooling của 7G-B cho góc cua tự nhiên.
- [x] Trace thật phát hiện speed mapping cũ bão hòa 0,85 px/ms trong khi thao tác đạt 2,4–4,0 px/ms.
- [x] Candidate 7G-B2 thêm nonlinear high-speed tail cho main outline/core, không nắn centerline.
- [x] Candidate 7G-B2 build sạch; 34/34 tests và lifecycle smoke-test đạt.
- [x] Người dùng đánh giá B2 tốt hơn một chút; trace mới cho thấy hard clamp 3,2 px/ms vẫn bão hòa ở sample 8–28 px/ms.
- [x] Candidate 7G-B3 chuyển sang open-ended asymptotic speed response, có width floor tự nhiên và giữ round cap.
- [x] B3 regression kiểm tra bốn mức tốc độ đến 12 px/ms; 34/34 tests và lifecycle smoke-test đạt.
- [x] Người dùng đánh giá B3 vẫn chưa khác rõ; replay preset 8 px cho khoảng 5,4–10,4 px.
- [x] Candidate 7G-B4 mở biên độ silhouette dự kiến khoảng 4,4–11,4 px, giữ round cap/core/corner behavior.
- [x] Người dùng đánh giá cảm giác B4 có vẻ ổn; giữ B4 làm baseline thắng tạm thời, không tăng width response thêm.
- [x] Candidate 7G-C1: dot threshold 1,25 px, short-stroke round capsule và deterministic core variation tối đa 2,4%.
- [x] Candidate C1 build sạch; 36/36 tests và lifecycle smoke-test đạt.
- [x] Người dùng xác nhận C1 ổn; khóa C1 trên nền B4 thành `7G-signature-ink-final`.
- [x] Phase 7G Signature Ink / Color & Ink Appearance gate hoàn thành.
- [x] Signature Ink final build sạch ở Debug và Release; 36/36 tests đạt ở cả hai cấu hình.
- [x] Debug/Release lifecycle smoke-test đạt; static regression xác nhận base/core cùng đi qua fade, Eraser, detach/restore, Lasso move và history snapshots.
- [x] Collapsed ink puck mới thay record-like dot; người dùng xác nhận tốt, drag/click và no-shadow giữ nguyên.
- [x] Publish self-contained single-file `win-x64` cho version `0.9.0-beta.1`; published smoke-test đạt.
- [x] Inno Setup 7 single-user installer candidate; silent install/smoke/uninstall cô lập đều exit 0 và cleanup sạch.
- [x] Ghi SHA-256/package report; installer hiện chưa ký Authenticode.

## Chưa hoàn thành

- [ ] Phase 6 tùy chọn: Snap animation, Recolor và Pin selection.
- [ ] Phase 7 hardening matrix còn lại: golden images, nhiều loại chuột/DPI và stress benchmark (không chặn Phase 8).
- [x] Phase 7G: C1/B4 đã được người dùng chọn và khóa Signature Ink/Color Gate.
- [ ] Phase 8 backlog: Capture vùng, kiểm thử output thật và hot-plug/DPI runtime.
- [ ] Phase 9: Signature Ink và packaging candidate hoàn thành; còn manual UI regression và quyết định code signing/phân phối.
- [ ] Phase 9 manual UI regression còn cần xác nhận trực tiếp cho Pixel/Object Eraser, Lasso, Undo/Redo, fade và capture trên bản final.
- [ ] Tạm hoãn theo yêu cầu: reduced motion, license và third-party notices.

## Bước tiếp theo đề xuất

Bước tiếp theo là người dùng chạy manual regression trên installer candidate: Pixel/Object Eraser, Lasso move/Delete, Undo/Redo, temporary fade, Copy/Save capture, hotkeys và toolbar theme/drag. Nếu đạt, beta sẵn sàng phân phối nội bộ; code signing là gate riêng cho public distribution.

## Câu hỏi chưa khóa

- Tên sản phẩm chính thức.
- Global hotkey mặc định cuối cùng.
- Màu và độ dày Pen mặc định.
- Toolbar có hiện trong phần mềm quay/chia sẻ màn hình hay không theo từng chế độ.
- Hành vi temporary group 400 ms có tự nhiên trong sử dụng thật hay cần điều chỉnh.
