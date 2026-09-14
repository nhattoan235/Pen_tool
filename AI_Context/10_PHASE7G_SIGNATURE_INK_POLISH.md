# Phase 7G — Signature Ink Polish

Ngày tạo: 2026-09-14

## 1. Mục tiêu

Biến chất lượng nét đã ổn định của Natural Pen 7F thành dấu ấn thị giác riêng của Screen Ink: bám chuột, sạch như Apple Markup nhưng có chiều sâu và nhịp mực tự nhiên hơn một đường vector phẳng.

Phase 7G là nhánh polish của Color & Ink Appearance gate trong Phase 9. Đây không phải yêu cầu viết lại smoothing hoặc thêm brush trang trí. Natural Pen 7F luôn là baseline rollback và mọi candidate phải chứng minh rằng cảm giác điều khiển không kém baseline.

Thứ tự ưu tiên:

1. Cú chạm đầu tiên và nét cực ngắn đẹp ngay lập tức.
2. Nhịp độ dày/sắc độ phản ánh chuyển động chuột mà không phô hiệu ứng.
3. Góc cua, đổi hướng và chỗ chồng nét sạch, có chiều sâu.
4. Màu có tương phản tốt trên nền sáng, xám và tối.
5. Kết quả ổn định, tái lập được và không tăng độ trễ cảm nhận.

## 2. Baseline bị khóa

Natural Pen 7F được giữ nguyên làm Candidate A:

- Adaptive smoothing đã được người dùng xác nhận bám chuột tốt.
- Velocity pressure hiện tại và ba size preset.
- Filled outline với round cap sáu phân đoạn.
- Symmetric quadratic closure và open-seam cap overlap.
- Endpoint raw tham gia live preview và final geometry.
- Natural Ink B dual geometry hiện tại là baseline appearance để so sánh.

Không được âm thầm thay đổi đường đi, smoothing, topology, cap hoặc width response đã khóa khi chỉ đang thử appearance. Candidate mới phải có thể rollback bằng cấu hình/constant rõ ràng, không khôi phục mã thủ công.

## 3. Các trục tối ưu

### 3.1. First-contact response

- Click tạo dot tròn, rõ, không thành giọt nước hoặc tam giác.
- Nét 5–20 px không mất thân vì taper/pressure transition.
- Trong 50–100 ms đầu, width và ink coverage tăng liên tục, không bật bậc.
- Mực xuất hiện ngay dưới cursor; không đánh đổi latency để làm đầu nét đẹp hơn.

### 3.2. Natural ink depth

- Đoạn chậm có thể đầy và sâu màu hơn nhẹ.
- Đoạn nhanh nhẹ hơn nhưng không teo, bạc màu hoặc mất tương phản.
- Chỗ đổi hướng có thể tích mực rất nhẹ dựa trên curvature/speed đã làm mượt.
- Biến thiên nằm trong thân nét; không dùng drop-shadow, glow hay gradient theo bounding box.

### 3.3. Corner và overlap behavior

- Chữ `S`, `3`, `4`, `5`, số `8` và zigzag không tạo blob ở góc.
- Đổi hướng gấp vẫn giữ ý định của chuột, không tự bo quá mức.
- Hai đoạn chồng nhau không lộ seam hoặc lõi màu bị đứt.
- Đầu/cuối gặp nhau không làm điểm đầu đổi hình hay bị cắt.

### 3.4. Micro-variation có kiểm soát

- Chỉ thử biến thiên coverage/sắc độ rất nhỏ; không làm rung outline.
- Không random theo frame, theo thời gian hệ thống hoặc theo repaint.
- Nếu dùng variation, seed phải được suy ra xác định từ stroke data để Undo/Redo, capture và redraw cho cùng một kết quả.
- Không tạo texture giấy, hạt noise nhìn thấy rõ hoặc hiệu ứng cọ khô.

### 3.5. Color và contrast

- Đánh giá riêng ít nhất màu đỏ và xanh trên nền trắng, xám trung tính và tối.
- Giữ hue identity của palette; không để lớp core làm màu bẩn hoặc gần đen.
- Fade phải giảm alpha sạch, không chuyển nét sang xám trước khi biến mất.
- Không tự lấy mẫu hoặc đổi màu theo nội dung màn hình trong phase này.

## 4. Candidate A/B/C

### Candidate A — Locked baseline

Natural Pen 7F + Natural Ink B hiện tại. Không thay đổi; dùng làm đối chứng và rollback.

### Candidate B — Depth và corner pooling

- Giữ nguyên geometry/path của Candidate A.
- Tinh chỉnh mapping speed → ink coverage.
- Thử tăng sắc độ rất nhẹ tại vùng giảm tốc/đổi hướng, có giới hạn chống blob.
- Không thêm micro-texture.

Trạng thái triển khai: corner pooling đã được người dùng đánh giá tự nhiên. Candidate B2 đang chờ A/B cho high-speed tail. Trace thật cho thấy nét nhanh đạt `2,4–4,0 px/ms`, trong khi response cũ bão hòa từ `0,85 px/ms`, nên mọi tốc độ cao bị ép về cùng một width.

B2 giữ centerline, smoothing, cap và topology nhưng mở rộng width response đến `3,2 px/ms`. Người dùng thấy tốt hơn một chút; trace tiếp theo phát hiện các sample quét rất nhanh đạt `8–28 px/ms`, vì vậy B2 vẫn clamp sớm.

B3 thay hard clamp bằng open-ended response `v^e / (1 + v^e)`. Outline chính dùng `Thinning 0,72`, midpoint `0,65 px/ms`, exponent `0,80`, smoothing `0,42`; core dùng `1,00 / 0,60 / 0,78 / 0,46`. Width tiếp tục giảm tiệm cận ở mọi tốc độ nhưng có đáy an toàn; không nắn centerline và không dùng end taper nhọn. Corner pooling giữ nguyên tối đa 14%.

Người dùng đánh giá B3 gần như không khác rõ bằng mắt. Replay chính trace B3 cho preset 8 px cho khoảng quan sát khoảng `5,4–10,4 px`, nhưng độ tương phản silhouette trên nét dài vẫn quá dè dặt. B4 là candidate expressive có chủ ý: main `Thinning 1,00`, response smoothing `0,52`, các tham số open-ended còn lại và toàn bộ corner/core giữ nguyên. Replay dự kiến khoảng `4,4–11,4 px`; cap vẫn tròn và width floor test hạ có kiểm soát xuống 4,2 px cho preset 8 px. B4 dùng để tạo A/B đủ khác; nếu quá mạnh sẽ nội suy giữa B3/B4 thay vì tiếp tục đổi công thức.

### Candidate C — First contact và deterministic micro-variation

- Kế thừa candidate thắng giữa A/B cho thân nét.
- Tối ưu dot, nét 5–20 px và transition 50–100 ms đầu.
- Thử micro-variation cực nhẹ, xác định và chỉ nằm trong coverage/core.
- Có công tắc tắt độc lập để xác định variation thực sự cải thiện hay chỉ gây nhiễu.

Trạng thái triển khai: C1 đã được người dùng xác nhận ổn và được khóa thành Signature Ink cuối. Ngưỡng coi chuyển động là dot được tách khỏi size và đặt `1,25 px`, nên click vẫn tròn nhưng nét 5–20 px trở thành capsule có hai round cap thay vì một dot phình. Chỉ ink core nhận deterministic coverage variation tối đa `2,4%`, wavelength `18 px`; tín hiệu suy ra từ cumulative distance + tọa độ đầu, không phụ thuộc frame/thời gian và không làm rung main outline. B4 vẫn là rollback nếu một regression phần cứng xuất hiện.

Chỉ đưa từng candidate cho người dùng sau khi automated regression và replay corpus đạt. Không gộp đồng thời nhiều thay đổi không thể tách nguyên nhân.

## 5. Corpus và A/B trực tiếp

Mỗi candidate phải được vẽ bằng chuột thật với cùng size/màu:

- Dot, nét 5 px, 10 px và 20 px.
- Gạch ngang nhanh/chậm; đường cong dài tăng rồi giảm tốc.
- Chữ `S`, `3`, `4`, `5`, `8` và zigzag 45°/90°.
- Vòng tròn nhỏ/lớn và vòng có đầu-cuối chồng nhau.
- Hai nét cắt nhau và nhiều vòng khoanh liên tục như khi thuyết trình.
- Màu đỏ và xanh trên nền trắng, xám và tối.

Đánh giá mù A/B nếu có thể; không nói candidate nào “mới hơn” trước khi người dùng chọn. Ghi lại lựa chọn và lý do bằng cảm giác thực tế, không chỉ dựa vào screenshot phóng lớn.

## 6. Đo lường và guardrail

- Input-to-preview và smoothing lag không kém Natural Pen 7F có thể cảm nhận.
- p95/max geometry build không tạo frame stall mới.
- Width range không vượt mức khiến chữ viết bằng chuột biến thành calligraphy.
- Dot/nét ngắn, round caps và open seam tiếp tục qua geometry tests.
- Cùng stroke data render lặp lại phải cho geometry/appearance tương đương xác định.
- Highlighter không nhận ink core/variation của Pen.
- Pixel Eraser, Object Eraser, Lasso move, Delete, Undo/Redo, temporary fade và capture xử lý toàn bộ lớp mực đúng cách.

## 7. Những điều cố ý không làm

- Không thêm nhiều brush hoặc palette chỉ để tăng số lượng lựa chọn.
- Không texture giấy, cọ khô, noise rõ hoặc wobble ngẫu nhiên ở viền.
- Không mô phỏng pressure quá mạnh từ tốc độ chuột.
- Không làm đầu nét nhọn kiểu calligraphy.
- Không background sampling hoặc tự đổi màu theo nền.
- Không thêm shadow/glow cho nét hay toolbar.

## 8. Exit criteria

Phase 7G chỉ được khóa khi:

- Người dùng trực tiếp chọn candidate thắng sau khi vẽ bằng chuột thật.
- Cú chạm đầu, nét ngắn, góc cua và overlap đều không có lỗi thị giác nổi bật.
- Nét có chiều sâu hơn baseline nhưng không bị nhận ra như một “hiệu ứng” phủ lên trên.
- Màu đỏ/xanh giữ tương phản và bản sắc trên ba nhóm nền kiểm thử.
- Không regression về latency, geometry, eraser, selection, history, fade hoặc capture.
- Automated suite, lifecycle smoke test và manual regression đều đạt.

Sau khi đạt gate này, renderer/color được đóng băng cho bản beta và Phase 9 chuyển sang packaging/installer.

## 9. Kết quả khóa

Trạng thái: Hoàn thành theo xác nhận trực tiếp của người dùng.

Signature Ink cuối gồm B4 expressive open-ended velocity response + corner pooling của B + first-contact capsule và deterministic core variation của C1. Không tiếp tục tuning appearance trước beta trừ khi manual regression phát hiện lỗi P0/P1 có thể tái hiện.

Debug và Release đều build sạch; 36/36 automated tests chạy đạt ở cả hai cấu hình, cùng lifecycle smoke test. Static integration audit xác nhận base/core dùng chung fade multiplier, Pixel Eraser geometry subtraction, Object Eraser detach, Lasso transform và Undo/Redo geometry snapshots. Manual UI regression trên bản final vẫn thuộc Phase 9, không mở lại tuning Signature Ink nếu không có lỗi cụ thể.
