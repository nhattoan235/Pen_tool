# Product Brief

Ngày cập nhật: 2026-09-14

Tên làm việc: **Screen Ink**. Tên thương mại sẽ được quyết định sau.

## 1. Vấn đề cần giải quyết

Người dùng cần khoanh vùng, gạch chân hoặc trỏ nhanh vào nội dung đang hiển thị trong lúc nói, thuyết trình, quay hướng dẫn hoặc giải thích từ xa.

Các sản phẩm hiện có thường gặp một hoặc nhiều vấn đề:

- Toolbar dài và chiếm diện tích màn hình.
- Chuyển đổi giữa vẽ và điều khiển ứng dụng thiếu tự nhiên.
- Nét vẽ bằng chuột rung, thô hoặc có cảm giác trễ.
- Quá nhiều tính năng ít dùng khiến thao tác chính bị chậm.
- Annotation tồn tại quá lâu, buộc người dùng phải xóa thủ công.

## 2. Tuyên bố sản phẩm

Screen Ink là một ứng dụng Windows nhẹ, cho phép vẽ trực tiếp lên mọi ứng dụng bằng chuột với nét mượt, toolbar tối giản và annotation tạm thời tự biến mất sau khoảng 2 giây.

## 3. Người dùng mục tiêu

Nhóm chính:

- Người thường xuyên giải thích nội dung trên màn hình.
- Người quay tutorial hoặc demo phần mềm.
- Giáo viên, người hướng dẫn và presenter.
- Người họp hoặc chia sẻ màn hình từ xa.

Thiết bị nhập mặc định là chuột phổ thông. Không được giả định người dùng có màn hình cảm ứng, stylus hoặc nút chuột phụ.

## 4. Nguyên tắc sản phẩm

### 4.1. Nội dung luôn quan trọng hơn công cụ

Toolbar phải biến mất khỏi sự chú ý khi không sử dụng. Công cụ không được che khu vực người dùng đang giải thích.

### 4.2. Một thao tác cho một ý định

Các hành động thường dùng phải thực hiện được bằng một click, một drag hoặc một phím tắt. Tùy chọn nâng cao nằm ở tầng thứ hai.

### 4.3. Nét vẽ phải tạo cảm giác dễ chịu

Độ trễ thấp, đường cong mượt, đầu nét đẹp và phản hồi trực tiếp quan trọng hơn số lượng công cụ.

### 4.4. Tạm thời là mặc định

Annotation chủ yếu dùng để dẫn mắt người xem trong lúc nói. Sau khi hoàn thành vai trò, nó tự biến mất.

### 4.5. Lấy tinh thần Apple Markup, không sao chép hình thức

Áp dụng các nguyên tắc tốt: toolbar di chuyển và tự thu gọn, chọn lại công cụ để chỉnh thuộc tính, giữ cuối stroke để snap thành hình, lasso chỉnh object và tẩy theo pixel/object. Icon, bố cục và visual identity sẽ là thiết kế riêng.

## 5. Phạm vi MVP

### P0 — bắt buộc

- Chạy nền qua system tray.
- Global hotkey bật/tắt Draw mode.
- Overlay trong suốt trên một hoặc nhiều màn hình.
- Pointer mode cho phép click xuyên xuống ứng dụng bên dưới.
- Pen tối ưu cho chuột.
- Highlighter.
- Undo, redo và clear.
- Temporary ink: giữ nét 1,65 giây và fade 0,35 giây.
- Nhóm các stroke liên tiếp để chúng biến mất cùng nhau.
- Toolbar dạng capsule ngắn, có thể kéo và tự thu gọn.
- Màu và độ dày nét.
- Lưu thiết lập cục bộ.
- Hoạt động chính xác với nhiều mức DPI.

### P1 — nên có trước bản beta công khai

- Nhận dạng line, arrow, ellipse và rectangle bằng thao tác vẽ rồi giữ.
- Object eraser và pixel eraser.
- Persistent/pinned annotation.
- Lasso chọn, di chuyển và xóa object.
- Chụp vùng/toàn màn hình kèm annotation.
- Copy ảnh vào clipboard.
- Gán phím tắt và nút chuột tùy chỉnh.
- Spotlight/laser pointer.

### P2 — sau khi MVP ổn định

- Text và đánh số bước.
- OCR vùng được chọn.
- Zoom và whiteboard/blackboard.
- Quay màn hình có annotation.
- Gắn annotation với một cửa sổ cụ thể.
- Profile cho giảng dạy, họp và quay tutorial.
- Hỗ trợ sâu hơn cho stylus, pressure, tilt và barrel buttons.

## 6. Ngoài phạm vi ban đầu

- macOS, Linux, iOS và Android.
- Đồng bộ đám mây hoặc tài khoản người dùng.
- Cộng tác annotation thời gian thực qua mạng.
- Sticker, chữ ký điện tử và chỉnh sửa tài liệu/PDF đầy đủ.
- Nhận dạng chữ viết tay.
- Can thiệp vào các cửa sổ bảo mật hoặc nội dung DRM.

## 7. Tiêu chí thành công của MVP

- Người dùng mới có thể bật Draw mode, khoanh một vùng và trở lại điều khiển ứng dụng mà không cần đọc hướng dẫn dài.
- Toolbar thu gọn không che đáng kể nội dung.
- Nét tròn vẽ bằng chuột không có rung nhìn thấy rõ ở tốc độ sử dụng bình thường.
- Cảm giác từ di chuyển chuột tới hiển thị nét không gây nhận biết về độ trễ.
- Temporary annotation biến mất nhất quán sau 2 giây tính từ khi kết thúc nhóm nét.
- Không lệch tọa độ trên các màn hình có DPI khác nhau.
- Khi ứng dụng lỗi hoặc thoát, người dùng không bị mất quyền click vào các ứng dụng khác.

