# Test Plan

Ngày cập nhật: 2026-09-14

## 1. Chiến lược

Chất lượng sản phẩm phụ thuộc nhiều vào cảm giác tương tác, vì vậy cần kết hợp:

- Unit tests cho state, timing, grouping, geometry và settings.
- Integration tests cho window styles, hotkeys và monitor transforms khi khả thi.
- Visual regression cho stroke/shape output.
- Manual usability tests bằng chuột thật.
- Performance traces trên màn hình độ phân giải cao.

## 2. Unit tests bắt buộc

### Temporary lifetime

- Stroke đơn hết hạn sau đúng lifecycle.
- Stroke thứ hai trong 400 ms gia nhập nhóm và reset timer.
- Stroke sau ngưỡng tạo nhóm mới.
- Nhóm được pin không hết hạn.
- Chọn object tạm dừng expiration đúng đặc tả.
- Undo trong visible/fade/remove state.
- Frame skip không kéo dài lifetime.

### State machine

- Pointer → Draw → Pointer.
- Draw → Select → Pointer.
- Bất kỳ state nào + `Esc` → Pointer.
- Input error → Pointer/fail-safe.
- Display rebuild → Pointer.

### Geometry

- Không tạo NaN/invalid geometry với click không di chuyển.
- Duplicate points được xử lý.
- Stroke rất ngắn có cap hợp lệ.
- Bounding box chứa toàn bộ outline.
- Transform logic ↔ physical coordinates round-trip trong sai số cho phép.

### Shape recognition

- Positive và negative cases cho line/arrow/ellipse/rectangle.
- Confidence thấp giữ freehand.
- Undo snap khôi phục đúng raw stroke.

## 3. Manual interaction scenarios

- Khoanh một icon nhỏ bằng chuột ở tốc độ chậm.
- Khoanh một khu vực lớn bằng chuyển động nhanh.
- Gạch chân một câu dài.
- Vẽ nhiều stroke liên tiếp thành một chú thích.
- Thoát Draw bằng click phải và lập tức click ứng dụng bên dưới.
- Dùng global hotkey khi app khác đang focus.
- Mở/đóng toolbar nhiều lần.
- Kéo toolbar qua các cạnh và giữa các màn hình.
- Undo khi nét đang fade.
- Ghim một nét trước khi hết 2 giây.
- Vẽ rồi giữ để snap shape; undo về freehand.

## 4. Ma trận màn hình

Tối thiểu:

| Cấu hình | Các trường hợp |
|---|---|
| Một màn hình | 1920×1080 tại 100%, 125%, 150% |
| Một màn hình HiDPI | 2560×1440 hoặc 4K tại 150%, 200% |
| Hai màn hình ngang | Cùng DPI và khác DPI |
| Hai màn hình dọc/ngang | Monitor phụ nằm trái, phải, trên hoặc dưới |
| Tọa độ âm | Monitor phụ ở bên trái/trên primary |
| Runtime change | Cắm/rút màn hình, đổi scale, đổi primary |

## 5. Ma trận chuột

- Chuột văn phòng polling rate thấp.
- Chuột gaming polling rate cao.
- Touchpad giả lập mouse input.
- Chuột không có side buttons.
- Chuột có Mouse 4/5 nếu hỗ trợ mapping.
- Con trỏ Windows ở các mức speed khác nhau.
- Enhance pointer precision bật và tắt.

## 6. Hiệu năng

Đo trong các trường hợp:

- Vẽ liên tục 30 giây.
- Nhiều stroke đang fade đồng thời.
- 100 object persistent.
- Một màn hình 4K.
- Hai màn hình khác DPI.
- App idle trong Pointer mode.

Theo dõi:

- Frame time và dropped frames.
- Độ trễ input tới pixels.
- CPU/GPU.
- Memory tăng theo số stroke.
- Cleanup sau khi temporary objects hết hạn.

## 7. Fail-safe và regression

- Kill/crash renderer không được để input bị chặn sau khi process đóng.
- Hotkey conflict phải hiển thị lỗi và cho phép đổi.
- Sleep/resume và lock/unlock Windows.
- Explorer restart.
- Remote Desktop connect/disconnect nếu có môi trường test.
- Fullscreen app thông thường.
- Screen sharing/recording phổ biến: xác nhận annotation và toolbar xuất hiện như mong đợi.

## 8. Privacy checks

- Không có network request khi sử dụng MVP.
- Không tạo screenshot file nếu chỉ vẽ.
- Clipboard chỉ thay đổi khi người dùng chủ động capture/copy.
- Log không chứa hình ảnh màn hình.

## 9. Acceptance test của người dùng

Trước khi coi stroke engine và toolbar là hoàn thành, cần người dùng trực tiếp xác nhận:

- Nét chuột đẹp hơn trải nghiệm họ không hài lòng ở Epic Pen.
- Smoothing không gây cảm giác trễ.
- Toolbar đủ ngắn và không gây khó chịu.
- Click phải/Esc để thoát Draw là tự nhiên.
- Thời gian 2 giây phù hợp khi đang nói và khoanh liên tục.

## 10. Phase gate riêng cho chất lượng nét Pen

Phase 7 phải lưu được cả raw input và ảnh render cuối cho cùng một gesture để phân biệt lỗi từ thiết bị, smoothing, outline hay WPF composition.

Bộ gesture tối thiểu:

- Dot và nét cực ngắn.
- Đường ngang/dọc nhanh và chậm.
- Vòng tròn nhỏ/lớn, cùng chiều và ngược chiều kim đồng hồ.
- Vòng kín có điểm nối ở bốn hướng khác nhau.
- Chữ S, số 8, zigzag và đổi hướng gấp.
- Khoanh liên tục nhiều object như khi đang thuyết trình.

Mỗi candidate phải được so sánh trên:

- Sai lệch giữa raw cursor path, live preview và final outline.
- Độ ổn định chiều rộng, cap, join và taper.
- Input-to-preview latency và p95 frame time.
- CPU/GPU/memory khi vẽ liên tục và khi idle.
- DPI 100%, 125%, 150%, 200% và ít nhất hai loại chuột thật.

Không khóa Phase 7 chỉ bằng automated tests. Candidate cuối phải được người dùng vẽ thử và chấp nhận trực tiếp; renderer hiện tại luôn được giữ làm baseline rollback.
