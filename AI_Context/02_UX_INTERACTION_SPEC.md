# UX & Interaction Specification

Ngày cập nhật: 2026-09-14

## 1. Mô hình trạng thái

Ứng dụng có bốn trạng thái tương tác chính:

### Pointer

- Overlay vẫn hiển thị annotation nhưng không nhận mouse input.
- Click đi xuyên xuống ứng dụng bên dưới.
- Toolbar thu gọn vẫn có một vùng hit-test riêng để có thể mở lại.

### Draw

- Overlay nhận mouse input.
- Kéo chuột trái tạo stroke bằng công cụ hiện tại.
- Click phải thoát ngay về Pointer.
- `Esc` cũng thoát về Pointer.

### Select

- Click chọn một object.
- Kéo lasso chọn nhiều object.
- Có thể di chuyển, đổi màu, ghim hoặc xóa selection.
- Click phải hoặc `Esc` trở về Pointer.

### Capture

- Người dùng kéo chọn vùng hoặc chọn toàn màn hình.
- Toolbar được ẩn khỏi ảnh đầu ra.
- Kết quả có thể copy vào clipboard hoặc lưu file.

Luồng ưu tiên:

```text
Pointer → global hotkey → Draw → left-drag → annotation → right-click/Esc → Pointer
```

## 2. Toolbar

### 2.1. Trạng thái thu gọn

```text
[ ● ]
```

- Là một nút nhỏ nằm sát cạnh màn hình.
- Hiển thị màu của công cụ đang chọn.
- Click mở thanh nhanh.
- Kéo để đổi vị trí.
- Lưu vị trí riêng theo từng màn hình.

### 2.2. Thanh nhanh

```text
[ Pointer ][ Current tool ][ Color ][ Undo ][ More ]
```

- Mục tiêu chiều dài: 160–200 px ở scale 100%.
- Tự điều chỉnh theo DPI.
- Không hiển thị label thường trực; label chỉ xuất hiện khi hover.
- Sau khi chọn công cụ hoặc bắt đầu stroke, toolbar tự thu gọn.

### 2.3. Tầng tùy chọn thứ hai

Click lại công cụ đang chọn để mở popover nhỏ gồm:

- Loại công cụ.
- Độ dày.
- Opacity nếu phù hợp.
- Temporary/Persistent.
- Kiểu tẩy nếu công cụ hiện tại là Eraser.

Không mở cửa sổ settings đầy đủ cho các thay đổi nhanh này.

## 3. Ánh xạ chuột mặc định

| Thao tác | Hành vi |
|---|---|
| Global hotkey | Chuyển Pointer ↔ Draw |
| Left-drag trong Draw | Vẽ stroke |
| Left-click toolbar | Chọn hoặc mở công cụ |
| Right-click trong Draw/Select | Trở về Pointer |
| `Esc` | Trở về Pointer |
| `Ctrl+Z` | Undo |
| `1`–`5` trong Draw | Chọn nhanh màu Pen; hỗ trợ cả hàng số chính và numpad |
| `Ctrl+Shift+Z` hoặc `Ctrl+Y` | Redo |
| `Shift` khi vẽ | Giữ nét thành Persistent |
| `Ctrl + mouse wheel` | Chỉnh độ dày công cụ hiện tại |
| `Shift + mouse wheel` trong Draw | Chuyển tool; wheel xuống tới tool kế, wheel lên quay lại |
| Giữ cuối stroke khoảng 300 ms | Thử snap thành shape |
| `Delete` khi có selection | Xóa selection |

Global hotkey ban đầu được đề xuất là `Ctrl+Shift+D`, nhưng phải cho phép đổi vì có thể xung đột với phần mềm khác.

Mouse 4/5 là mapping tùy chọn, không phải yêu cầu để sử dụng sản phẩm.

`Shift+wheel` chỉ được Screen Ink xử lý khi Draw mode đang hoạt động. Modifier phải chính xác là `Shift`; trong Pointer mode tổ hợp được trả nguyên cho ứng dụng đang dùng. `Ctrl+wheel` tiếp tục điều chỉnh cỡ công cụ.

Ánh xạ màu nhanh đã chốt cho palette MVP:

| Phím | Màu |
|---|---|
| `1` | Đỏ |
| `2` | Vàng |
| `3` | Xanh dương |
| `4` | Xanh lá |
| `5` | Trắng |

Các phím này chỉ được giữ bởi Screen Ink khi Draw mode đang hoạt động. Trong Pointer mode, chúng tiếp tục đi vào ứng dụng đang sử dụng bình thường.

## 4. Temporary ink — đặc tả thời gian

Một nhóm annotation có vòng đời:

```text
đang vẽ → pen/mouse up → giữ nguyên 1.650 ms → fade 350 ms → bị loại khỏi canvas
```

Tổng thời gian từ khi kết thúc nhóm nét tới khi biến mất hoàn toàn: **2.000 ms**.

Quy tắc nhóm:

- Stroke mới bắt đầu trong vòng 400 ms sau stroke trước thuộc cùng nhóm.
- Khi thêm stroke vào nhóm, đồng hồ 2 giây của cả nhóm được đặt lại.
- Nhóm đang được chọn hoặc hover trong Select mode không tự biến mất.
- Ghim một object sẽ chuyển nó sang Persistent và hủy expiration timer.
- Undo trong lúc fade phải xóa object ngay, không chạy thêm animation.

Các con số 1.650/350/400 ms là baseline đã chốt cho prototype đầu tiên; có thể tinh chỉnh sau test nhưng tổng lifetime mặc định vẫn khoảng 2 giây theo yêu cầu người dùng.

## 5. Stroke quality cho chuột

### 5.1. Mục tiêu cảm giác

- Nét bám sát con trỏ.
- Không rung rõ khi vẽ chậm.
- Không bo tròn quá mức làm mất ý đồ của người vẽ.
- Đầu và cuối stroke thu nhẹ, không bị cắt phẳng.
- Vòng tròn và đường cong không có góc gãy do tần số mouse event thấp.

### 5.2. Pipeline đề xuất

```text
raw pointer events
→ loại điểm trùng/nhiễu rất nhỏ
→ resample theo khoảng cách
→ smoothing phụ thuộc vận tốc
→ nội suy đường cong
→ tạo outline có độ rộng
→ anti-aliased render
```

Yêu cầu quan trọng:

- Không đợi toàn bộ stroke hoàn tất mới render.
- Render phần đầu bằng đường dự đoán/preview độ trễ thấp.
- Khi stroke hoàn tất, thay bằng geometry đã làm mượt mà không được “nhảy” hình rõ rệt.
- Mouse dùng smoothing mạnh hơn stylus.
- Velocity chỉ ảnh hưởng nhẹ tới độ rộng; không giả lập pressure quá mức.

### 5.3. Pen mặc định

- Màu: đỏ dễ nhìn trên cả nền sáng và tối, giá trị chính xác sẽ chọn qua visual test.
- Độ dày: đề xuất ban đầu 4 px vật lý tương đương ở 100% DPI.
- Opacity: 100%.
- Round cap và round join.

### 5.4. Highlighter

- Đầu dẹt.
- Opacity đề xuất 35%.
- Các segment trong cùng một stroke không tự làm tối lẫn nhau.
- Blend ổn định trên cả nền sáng và tối.

## 6. Shape recognition

Shape recognition chỉ kích hoạt khi người dùng giữ nút chuột ở cuối stroke khoảng 300 ms.

Ưu tiên nhận dạng:

1. Straight line.
2. Arrow.
3. Ellipse/circle.
4. Rectangle/square.
5. Triangle.
6. Orthogonal polyline.

Quy tắc an toàn:

- Nếu confidence thấp, giữ stroke tự do.
- Snap có animation ngắn để người dùng hiểu hệ thống vừa sửa hình.
- `Ctrl+Z` ngay sau snap trả lại stroke tự do trước khi undo toàn bộ object.
- Không tự snap nếu người dùng nhấc chuột ngay.

## 7. Con trỏ và phản hồi

- Draw mode dùng cursor tròn phản ánh gần đúng kích thước và màu nét.
- Highlighter dùng preview hình chữ nhật/dẹt.
- Pixel Eraser dùng cursor vuông hai lớp; Object Eraser dùng cursor bo tròn có dấu `×`.
- Cursor Eraser luôn có viền tối để thấy trên nền trắng và một viền accent theo màu palette hiện tại; phím `1`–`5` cũng đổi accent này.
- Pointer mode dùng cursor hệ thống bình thường.
- Khi chuyển mode, toolbar và cursor phải phản hồi tức thì.
- Không phát âm thanh mặc định.
- Animation ngắn và có thể tắt trong Accessibility.

## 8. Nguyên tắc visual

- Capsule nhỏ, góc bo mềm và shadow nhẹ.
- Tương phản tốt nhưng không quá nổi bật so với nội dung.
- Icon đơn sắc, hình học rõ ràng.
- Trạng thái được chọn phải phân biệt bằng cả màu và nền, không dựa duy nhất vào màu.
- Khoảng hit target tối thiểu 32×32 px cho chuột; có thể tăng khi phát hiện touch.
- Hỗ trợ light/dark dựa trên nền hệ thống hoặc tùy chọn người dùng.

## 9. Hành vi khi có sự cố

- Nếu overlay mất focus hoặc module input lỗi, tự chuyển về Pointer.
- Có emergency hotkey luôn hoạt động để vô hiệu hóa Draw mode.
- Khi thay đổi cấu hình màn hình, tạm dừng input, dựng lại overlay rồi trở về Pointer.
- Không được để một overlay trong suốt chặn chuột mà không có cách thoát bằng bàn phím.
