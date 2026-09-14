# Phase 7 — Pen Stroke Quality Hardening

Ngày tạo: 2026-09-14

## 1. Mục tiêu

Đưa nét Pen bằng chuột từ mức “đã dùng được” lên mức có chất lượng đủ tốt để trở thành đặc điểm nổi bật của Screen Ink. Phase này là release gate bắt buộc trước Capture và Packaging.

Ưu tiên theo thứ tự:

1. Cảm giác bám chuột và độ trễ thấp.
2. Hình dáng nét tự nhiên, ổn định và dễ nhìn.
3. Đầu/cuối nét, chỗ đổi hướng và vòng kín sạch.
4. Kết quả nhất quán giữa loại chuột, polling rate và DPI.
5. Hiệu năng ổn định khi vẽ liên tục trên nhiều màn hình.

## 2. Baseline không được làm mất

Renderer Candidate B hiện tại là baseline rollback:

- Adaptive smoothing cho chuột.
- Filled outline thay cho polyline thô.
- Width biến đổi nhẹ theo vận tốc.
- Start/end taper.
- Round body và anti-aliased WPF geometry.
- Live preview tới raw endpoint hiện tại.

Trước khi sửa thuật toán, phải lưu raw traces, ảnh kết quả và số đo của baseline. Mọi candidate mới phải có thể bật/tắt để A/B test hoặc rollback mà không phục hồi mã thủ công.

## 3. Phạm vi

### 3.1. Input sampling và resampling

- Ghi timestamp, khoảng cách và vận tốc của raw mouse points.
- Kiểm tra chuột polling rate thấp/cao và touchpad.
- Resample để mật độ event không quyết định độ mượt hoặc độ rộng nét.
- Không dự đoán quá xa vị trí con trỏ chỉ để làm đường cong đẹp.

### 3.2. Smoothing

- So sánh baseline adaptive smoothing với ít nhất một candidate được kiểm soát bằng trace.
- Giữ góc có chủ ý, không biến zigzag thành đường cong vô nghĩa.
- Khoanh chậm không rung; gạch nhanh không kéo đuôi.
- Xác định giới hạn độ trễ có thể chấp nhận thay vì tăng smoothing vô hạn.

### 3.3. Outline, cap, join và taper

- Xử lý dot/nét cực ngắn thành hình tròn sạch.
- Không có blob khi dừng chuột hoặc nhả chuột.
- Không có mối nối nhọn ở vòng kín.
- Không lộ self-intersection bất thường ở chữ S/số 8.
- Taper không làm mất phần lớn nét ngắn và không tạo đuôi kim quá dài.

### 3.4. Live preview và finalization

- Live preview phải tới vị trí raw mới nhất.
- Final outline khi mouse-up không co, lệch hoặc nhảy rõ bằng mắt.
- Đo riêng thời gian input event → cập nhật geometry → WPF render.

### 3.5. DPI và kích thước

- Kiểm tra Small/Medium/Large tại 100%, 125%, 150%, 200%.
- Cursor preview và nét thật phải tương xứng.
- Cùng một gesture không thay đổi cảm giác quá mức khi chuyển monitor khác DPI.

## 4. Corpus kiểm thử bắt buộc

Mỗi trace gồm raw points + timestamp, cấu hình chuột/DPI và ảnh output:

- Dot, nét 5–20 px và click gần như không di chuyển.
- Đường ngang/dọc/chéo dài ở tốc độ chậm, thường và nhanh.
- Vòng tròn nhỏ/lớn theo hai chiều.
- Vòng kín với điểm nối ở trên, dưới, trái và phải.
- Chữ S, số 8, zigzag, góc 45°/90° và đổi hướng đột ngột.
- Gạch chân dài và khoanh liên tiếp nhiều vùng.
- Vẽ liên tục 30 giây và 100 nét persistent.

## 5. Số đo cần lưu

- Input-to-preview latency.
- p50/p95 frame time và dropped frames.
- Số raw points, số resampled points và số outline points.
- Thời gian build geometry theo số điểm.
- CPU/GPU/memory khi vẽ, fade và idle.
- Sai lệch endpoint giữa raw path, live preview và final geometry.

Kết quả trước/sau được ghi vào một báo cáo Phase 7 trong `AI_Context`; không chỉ báo “cảm giác nhanh hơn”.

## 6. Quy trình candidate

1. Chụp baseline Candidate B.
2. Chọn đúng một nhóm lỗi có trace tái hiện được.
3. Viết test geometry/metrics phù hợp trước hoặc cùng thay đổi.
4. Tạo candidate có feature flag hoặc cấu hình A/B.
5. Chạy corpus, performance test và kiểm tra regression.
6. Người dùng vẽ thử bằng chuột thật và chọn giữ/loại candidate.
7. Chỉ gộp candidate thắng; cập nhật Decision Log.

## 7. Ngoài phạm vi Phase 7

- Thêm nhiều brush trang trí.
- Pressure/tilt chuyên sâu cho stylus.
- Text, OCR, screenshot hoặc quay màn hình.
- Thay renderer chỉ vì sở thích kỹ thuật khi baseline WPF vẫn đạt mục tiêu.
- Tinh chỉnh Arrow recognition hoặc mở rộng shape recognition.

## 8. Exit criteria

Phase 7 chỉ hoàn thành khi:

- Các lỗi nét quan trọng có trace và visual regression case.
- Candidate cuối không tạo regression cho Highlighter, Eraser, Undo/Redo và temporary fade.
- Benchmark ở cấu hình mục tiêu không có frame stall gây khó chịu.
- Nét nhanh bám chuột, nét chậm không rung, vòng kín không có mối nối nhọn rõ ràng.
- Mouse-up không làm final geometry nhảy hình thấy được.
- Người dùng trực tiếp xác nhận chất lượng nét đạt yêu cầu.

## 9. Trạng thái triển khai

### Slice 7A — Baseline instrumentation và closed-loop seam

Trạng thái: Đã triển khai candidate, chờ thu mẫu/đánh giá trực tiếp.

- `StrokePointBuffer` giữ riêng toàn bộ raw samples, kể cả điểm bị minimum-distance filter loại khỏi filtered path.
- Opt-in recorder ghi một JSON cho mỗi stroke ở background; chế độ thường không khởi tạo timestamp/serialize/write cho recorder.
- Mẫu gồm raw/filtered points, tool, size, DPI, duration, closure distance, geometry bounds, số lần build geometry và total/max build time.
- Bật recorder bằng biến môi trường `SCREENINK_STROKE_DIAGNOSTICS=1` cho riêng process thử nghiệm.
- Nét vòng kín không áp dụng start/end taper và dùng tangent tuần hoàn ở seam để tránh đầu nối nhọn.
- Automated suite tăng từ 26 lên 28 test: raw-sample preservation và full-width closed seam.
