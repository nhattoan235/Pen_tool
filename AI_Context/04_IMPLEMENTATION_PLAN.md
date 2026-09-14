# Implementation Plan

Ngày cập nhật: 2026-09-14

Kế hoạch ưu tiên giảm rủi ro trải nghiệm cốt lõi trước. Không xây nhiều công cụ cho tới khi overlay, chuyển mode và chất lượng nét đạt yêu cầu.

## Phase 0 — Project foundation

Mục tiêu: có solution sạch, chạy được và có nền tảng kiểm thử.

Deliverables:

- [x] Tạo .NET solution và project structure.
- [x] App chạy system tray và thoát sạch.
- [x] Thiết lập formatting/analyzers.
- [x] Unit-test project.
- [x] Logging cục bộ tối thiểu.
- [x] CI hoặc script build/test lặp lại được.
- [x] README dành cho developer.

Exit criteria:

- Build và test chạy bằng một lệnh.
- App khởi động/thoát không để process treo.

## Phase 1 — Overlay, state machine và input safety

Mục tiêu: chứng minh overlay có thể vẽ và trả quyền click an toàn.

Deliverables:

- [x] Enumerate monitors.
- [x] Tạo transparent overlay per monitor.
- [x] Pointer mode click-through.
- [x] Draw mode nhận mouse events.
- [x] Global hotkey Pointer ↔ Draw.
- [x] Right-click và `Esc` trở về Pointer.
- [x] Emergency fallback khi overlay/input gặp lỗi.
- [x] Per-Monitor V2 DPI setup.

Exit criteria:

- Có thể bật vẽ, tạo raw polyline, thoát Draw mode và thao tác ứng dụng bên dưới.
- Không lệch con trỏ trên ma trận DPI thử nghiệm cơ bản.
- Không có tình huống overlay chặn chuột mà không thể thoát bằng bàn phím.

Trạng thái: implementation và automated smoke-test đã hoàn thành; chờ manual acceptance trên máy người dùng trước khi khóa phase.

## Phase 2 — Stroke rendering spike

Mục tiêu: chọn renderer và thuật toán tạo nét.

Deliverables:

- [x] Prototype WPF custom geometry.
- [x] Đánh giá nhu cầu SkiaSharp: chưa cần vì WPF outline Candidate B đã được người dùng chấp nhận.
- [x] Bộ test dữ liệu chuột: micro-jitter, slow/fast smoothing và released endpoint.
- [ ] Đo latency, CPU và frame behavior.
- [ ] So sánh ảnh ở 100%, 150%, 200% DPI và 4K.
- [x] Chốt WPF filled-outline renderer trong Decision Log; performance hardening tiếp tục ở các phase sau.

Trạng thái: Candidate A bị loại. Candidate B dùng adaptive smoothing + filled outline + simulated pressure/taper đã được người dùng đánh giá là ổn; WPF renderer được chọn cho MVP.

Exit criteria:

- Nét chuột được người dùng xác nhận là đủ mượt và đẹp.
- Không có jitter/góc gãy rõ ràng trong các mẫu chính.
- Hiệu năng phù hợp trên màn hình mục tiêu.

Gate: không chuyển sang xây nhiều công cụ nếu Phase 2 chưa đạt.

## Phase 3 — Temporary ink 2 giây

Mục tiêu: hoàn thiện hành vi đặc trưng của sản phẩm.

Deliverables:

- [x] Annotation registry và object IDs.
- [x] Nhóm stroke theo khoảng 400 ms.
- [x] Visible 1.650 ms + fade 350 ms.
- [x] Monotonic timing.
- [x] Undo trong mọi trạng thái lifetime; redo được thực hiện ở Phase 5.
- [x] Chuyển temporary object sang persistent bằng `Shift+drag`.
- [x] Unit tests cho grouping và expiration.

Exit criteria:

- Nhóm nhiều stroke biến mất đồng thời.
- Lifetime đúng trong sai số animation hợp lý ngay cả khi frame bị trễ.
- Undo/redo không tạo ghost stroke.

Trạng thái: hoàn thành và được người dùng chấp nhận. Automated suite hiện có 17/17 tests; lifecycle smoke-test đạt.

## Phase 4 — Compact toolbar

Mục tiêu: đạt trải nghiệm tối giản lấy cảm hứng từ Apple Markup.

Deliverables:

- [x] Collapsed dot.
- [x] Quick capsule năm nút.
- [x] Drag/snap vào cạnh màn hình.
- [x] Auto-minimize khi bắt đầu vẽ.
- [x] Popover màu tầng hai và menu More cho Clear, theme và Exit.
- [x] Hover labels.
- [x] Theme sáng/tối.
- [x] Lưu cạnh màn hình và vị trí dọc gần nhất.
- [x] Lưu màu và theme gần nhất; mode luôn khởi động an toàn ở Pointer.
- [x] Phím `1`–`5` và numpad `1`–`5` chọn màu trực tiếp chỉ trong Draw mode.

Trạng thái: implementation hoàn thành; Release build sạch và 18/18 automated tests đạt. Chờ đánh giá trực tiếp về kích thước, vị trí và thao tác chuột trước khi khóa phase.

Acceptance: người dùng xác nhận Phase 4 ổn sau khi thử toolbar và phím màu trực tiếp. Bổ sung sau acceptance: auto-collapse khi click ngoài ở Pointer và cặp Undo/Redo cạnh nhau; capsule tăng từ 200 lên 224 px.

Exit criteria:

- Thao tác thường dùng không cần mở settings.
- Toolbar thu gọn không che vùng làm việc đáng kể.
- Toolbar hoạt động đúng trên mọi cạnh và DPI test.

## Phase 5 — Core tools

Mục tiêu: hoàn thiện bộ công cụ đủ cho sử dụng thực tế.

Deliverables:

- [x] Pen.
- [x] Highlighter render thành một geometry với opacity 34%, không tự chồng tối giữa các segment cùng stroke.
- [x] Color popover và ba width preset; `Ctrl+wheel` chỉnh width.
- [x] Undo/redo/clear; redo qua `Ctrl+Y` hoặc `Ctrl+Shift+Z`.
- [x] Persistent mode trong tool popover; `Shift+drag` vẫn là override nhanh.
- [x] Pixel eraser bằng geometry exclusion.
- [x] Object eraser xóa toàn stroke.
- [x] Cursor preview phản ánh công cụ, màu và kích thước.

Trạng thái: implementation candidate đã build sạch và 20/20 automated tests đạt; chờ manual acceptance về cảm giác Highlighter, Eraser và cursor.

Exit criteria:

- Mỗi công cụ có visual và cursor nhất quán.
- Highlighter không tạo banding/tối bất thường trong cùng stroke.
- Tẩy không để artifact.

## Phase 6 — Shape recognition và selection

Mục tiêu: đưa các tương tác mạnh nhất của Markup vào workflow chuột.

Deliverables:

- [x] Detect hold khoảng 280–300 ms ở cuối stroke.
- [x] Line, arrow, ellipse và rectangle recognition.
- [ ] Snap animation; feedback label 650 ms đã có, animation hình học còn lại.
- [x] Undo snap về freehand stroke.
- [x] Lasso selection.
- [ ] Move/delete/recolor/pin selection: Move/Delete đã có; Recolor/Pin còn lại.

Trạng thái: Phase 6 slice A đã được người dùng xác nhận. Arrow recognition được giữ nguyên nhưng không ưu tiên tinh chỉnh theo quyết định của người dùng. Slice Lasso Selection đã build sạch với 26/26 automated tests; chờ manual acceptance cho khoanh/chọn, move, Delete và Undo/Redo.

Hardening đã hoàn thành trong slice hiện tại: Clear/Delete tham gia unified Undo/Redo dưới dạng một composite action; modifier-wheel được gom nhịp và preferences được lưu trễ để giữ tương tác chuyển tool mượt.

Exit criteria:

- Confidence thấp không làm biến dạng nét người dùng.
- Shape snap đủ dễ dự đoán.
- Undo sau snap đúng hai cấp: snap → freehand → remove.

## Phase 7 — Pen stroke quality hardening

Mục tiêu: đưa chất lượng nét Pen bằng chuột thành release gate độc lập, với đánh giá cảm giác, visual và hiệu năng thay vì chỉ dựa trên việc “vẽ được”.

Deliverables:

- [ ] Thu corpus raw input từ chuột văn phòng, chuột polling-rate cao và touchpad ở các thao tác: recorder opt-in đã có; chờ thu mẫu thật.
- [ ] Chuẩn hóa resampling theo khoảng cách/thời gian để mật độ mouse event khác nhau không làm thay đổi hình dáng nét quá mức.
- [x] Tinh chỉnh adaptive smoothing với giới hạn độ trễ rõ ràng; 7C giảm lag p95 từ 10,65 xuống 2,00 px.
- [x] Loại seam cắt, cap phẳng và taper nhọn bằng round-cap overlap + symmetric geometry; Natural Pen 7F thêm velocity width.
- [x] Đồng bộ live preview với final geometry; endpoint raw luôn tham gia preview và final.
- [ ] Kiểm tra ba cỡ Pen và cursor preview ở DPI 100%, 125%, 150%, 200%.
- [ ] Tạo visual-regression corpus/golden images cho các gesture chuẩn và các lỗi đã từng xuất hiện: raw/filtered JSON schema đã có; golden renderer còn lại.
- [ ] Đo input-to-pixel latency, frame time, dropped frames, CPU/GPU và memory ở 1080p, 1440p/4K và hai màn hình.
- [ ] Stress test vẽ liên tục 30 giây, 100 nét persistent và nhiều nhóm temporary đang fade.
- [x] So sánh trực tiếp 7B–7F bằng chuột thật; giữ đường rollback về renderer Candidate B trong lịch sử thay đổi.
- [x] Người dùng trực tiếp xác nhận Natural Pen 7F là candidate tốt nhất và cho phép chuyển phase.

Nguyên tắc:

- Không thêm hiệu ứng “đẹp” nếu làm tăng cảm giác trễ.
- Không tối ưu riêng cho một polling rate hoặc một mức DPI.
- Pen là trọng tâm; Highlighter/Eraser phải qua regression để không bị ảnh hưởng ngoài ý muốn.
- Mọi thay đổi thuật toán phải có trace hoặc visual case tái hiện được.

Exit criteria:

- Khoanh chậm bằng chuột không thấy rung, góc gãy hoặc mối nối nhọn rõ ràng.
- Gạch nhanh vẫn bám con trỏ, không bị kéo đuôi hoặc bo mất ý định.
- Nhả chuột không tạo thay đổi hình học có thể nhận ra bằng mắt.
- Không có frame stall gây khó chịu trên cấu hình mục tiêu; số đo trước/sau được lưu trong `AI_Context`.
- Người dùng xác nhận nét Pen đạt chất lượng mong muốn so với trải nghiệm không hài lòng ở Epic Pen.

Trạng thái: Natural Pen 7F đã được người dùng chấp nhận và khóa làm chuẩn sản phẩm. Các mục hardware/DPI/golden/stress matrix còn lại được giữ làm hardening liên phase, không chặn bắt đầu Phase 8.

Slice 7B đã coalesce geometry build theo WPF composition frame, giữ force-update ở mouse-down/mouse-up và không thay đổi point/smoothing math. Baseline 105 stroke đã được tổng hợp trong `09_PHASE7_BASELINE_REPORT.md`; candidate chờ A/B thực tế.

## Phase 8 — Capture và multi-monitor hardening

Mục tiêu: xuất kết quả đúng và ổn định trên cấu hình thực tế.

Trạng thái: Tạm hoãn theo yêu cầu người dùng sau khi full-screen Copy/Save được xác nhận hoạt động tốt. Capture vùng và multi-monitor/DPI hardening được giữ trong backlog.

Deliverables:

- [ ] Capture vùng.
- [x] Capture toàn virtual desktop — slice A candidate.
- [x] Copy PNG vào clipboard — slice A candidate.
- [x] Save PNG — slice A candidate.
- [x] Ẩn toolbar và ContextMenu khỏi capture — slice A candidate.
- [ ] Test tọa độ âm và monitor đặt theo chiều dọc.
- [ ] Test hot-plug và đổi DPI runtime.

Exit criteria:

- Ảnh xuất khớp đúng annotation trên mọi monitor test.
- Thay đổi display topology không làm app chặn input hoặc crash.

## Phase 9 — Packaging và beta readiness

Mục tiêu: tạo bản dùng thử ổn định.

Deliverables:

- [ ] Single-user installer/package.
- [x] Auto-start tùy chọn, mặc định tắt — Phase 9A candidate.
- [x] Settings cho hotkeys với rollback khi tổ hợp bị chiếm — Phase 9A candidate.
- [x] Crash-safe recovery candidate: session marker, clean-exit cleanup và volatile-state reset.
- [x] Single-instance protection để shortcut không tạo nhiều overlay.
- [x] Accessibility cơ bản cho Settings; reduced motion tạm hoãn theo yêu cầu người dùng.
- [ ] License/third-party notices — tạm hoãn theo yêu cầu người dùng.
- [ ] Manual regression pass.
- [ ] Color & ink appearance gate trước packaging: sắc độ, tương phản và biến thiên lượng mực dọc nét.

Thứ tự Phase 9 đã chốt:

1. Settings hotkey + Start with Windows.
2. Crash-safe recovery và single-instance protection. — candidate đã triển khai
3. Accessibility cơ bản + manual regression.
4. Thảo luận, prototype và khóa Color & Ink Appearance. — Natural Ink candidate đang A/B
5. Packaging/installer cuối; reduced motion và notices chưa nằm trong scope hiện tại.

Exit criteria:

- Cài đặt, nâng cấp và gỡ sạch.
- Không cần quyền administrator cho sử dụng thông thường.
- Toàn bộ P0 và P1 được xác nhận hoặc có lý do hoãn được ghi lại.

## Thứ tự ưu tiên khi có xung đột

1. Không chặn hoặc phá thao tác của người dùng.
2. Độ trễ và chất lượng nét.
3. Chuyển Pointer/Draw nhanh và dễ hiểu.
4. Toolbar gọn.
5. Tính đúng của temporary lifetime.
6. Số lượng công cụ.
