# Phase 7 — Baseline Report 7A

Ngày đo: 2026-09-14

## Phạm vi dữ liệu

Baseline được thu trực tiếp từ chuột của người dùng với renderer Candidate B + closed-loop seam candidate, DPI scale 100% trên display đang vẽ.

- Tổng số mẫu: 105 stroke.
- Pen: 98 stroke.
- Highlighter: 7 stroke.
- Mẫu chỉ chứa tọa độ/timestamp nét và timing geometry; không chứa screenshot hoặc nội dung màn hình.

## Kết quả Pen baseline

| Chỉ số | Giá trị |
|---|---:|
| Raw points trung vị | 33 |
| Raw points p95 | 102 |
| Khoảng cách thời gian event trung vị | 8 ms |
| Khoảng cách thời gian event p95 | 16 ms |
| Số lần build geometry trung vị | 34 |
| Số lần build geometry p95 | 103 |
| Tổng thời gian build/stroke trung vị | 0,863 ms |
| Max build-time/stroke trung vị | 0,059 ms |
| Max build-time/stroke p95 | 0,178 ms |
| Frame build chậm nhất quan sát được | 3,363 ms |

## Kết luận 7A

- Mouse stream thực tế chủ yếu ở khoảng 8 ms/event, tương đương khoảng 125 Hz; một phần event ở 16 ms.
- Filter hiện giữ gần như toàn bộ điểm có chuyển động. Chưa có đủ bằng chứng để tăng minimum-distance vì điều đó có thể làm endpoint/live preview kém bám chuột.
- Geometry build thông thường nhanh, nhưng việc rebuild toàn bộ outline ở từng mouse event tạo nhiều allocation và có outlier 3,363 ms trên nét dài.
- Hướng tối ưu an toàn cho Slice 7B: giữ nguyên raw/filtered geometry, coalesce việc build theo frame composition và luôn force final build ở mouse-up.
- Closed-loop seam có 5 gesture thực tế phù hợp điều kiện nhận dạng ban đầu; cần tiếp tục đánh giá bằng mắt trên candidate mới.

## Candidate 7B

Thay đổi:

- Mouse event chỉ cập nhật point buffer và đánh dấu geometry dirty.
- `CompositionTarget.Rendering` dựng geometry tối đa một lần mỗi frame.
- Mouse-down dựng dot đầu ngay; mouse-up force bản cuối ngay trước khi hoàn tất stroke.
- Không thay đổi smoothing alpha, outline math, pressure, taper của nét mở hoặc danh sách filtered points.
- Recorder luôn tính mouse-up là một raw input sample riêng để raw count không nhỏ hơn filtered count.

Tiêu chí đánh giá tiếp theo:

- Hình dáng nét phải không khác Candidate B ngoài closed-loop seam.
- Cảm giác không được trễ hơn khi khoanh/gạch nhanh.
- Số geometry builds phải giảm trên stream 125 Hz trong khi final endpoint giữ nguyên.
- Outlier max build và cảm giác stall phải không tệ hơn baseline.

## Kết quả Candidate 7B

Candidate 7B được đo bằng 56 Pen stroke thật, tổng cộng 7.354 điểm nội bộ dùng cho phân tích độ trễ.

- Tỷ lệ geometry build/raw point trung bình vẫn là `1,00`; composition rate gần bằng mouse event rate nên coalescing không giảm số lần dựng hình.
- Geometry build chậm nhất là `3,37 ms`, gần như không cải thiện so với baseline `3,363 ms`.
- Cơ chế composition làm tăng độ phức tạp vòng đời nhưng không đem lại lợi ích đo được, vì vậy đã bị loại và rollback hoàn toàn.
- Recorder, sửa raw mouse-up và closed-loop seam vẫn được giữ lại.

## Phân tích độ trễ smoothing

Trên 56 Pen stroke của Candidate 7B:

| Chỉ số | Giá trị |
|---|---:|
| Khoảng cách raw → filtered trung vị | 4,96 px |
| Khoảng cách raw → filtered p95 | 10,65 px |
| Khoảng cách raw → filtered p99 | 19,13 px |
| Lệch lớn nhất quan sát được | 33,20 px |
| Độ lệch chuẩn hóa theo bề rộng nét, trung vị | 0,62× |
| Độ lệch chuẩn hóa theo bề rộng nét, p95 | 1,33× |
| Tốc độ chuột trung vị | 0,25 px/ms |
| Tốc độ chuột p95 | 1,152 px/ms |

Nguyên nhân: ngưỡng `FastMovementSpeed = 2,4 px/ms` quá cao so với corpus chuột thật, khiến phần lớn chuyển động chỉ dùng alpha gần nhánh làm mượt chậm.

## Candidate 7C — mouse-adaptive response

- Trả renderer về cập nhật trực tiếp theo mouse event.
- Giữ lọc micro-jitter `0,65 px`.
- Đổi alpha chậm từ `0,22` lên `0,30`, alpha nhanh từ `0,68` lên `0,85`.
- Đổi ngưỡng đạt nhánh nhanh từ `2,4` xuống `0,65 px/ms`, dựa trên corpus thật.
- Replay định lượng dự kiến đưa lag về khoảng `2,26 px` trung vị, `4,07 px` p95 và `12,51 px` tối đa trên cùng dữ liệu đầu vào.
- Candidate này cần A/B bằng mắt với nét chậm, vòng tròn nhỏ, chữ viết và flick nhanh trước khi khóa.

## Kết quả Candidate 7C

Người dùng đã vẽ 30 Pen stroke thật, tạo 2.169 điểm nội bộ hợp lệ để so sánh raw → filtered.

| Chỉ số | 7B | 7C | Thay đổi |
|---|---:|---:|---:|
| Raw → filtered trung vị | 4,96 px | 1,71 px | giảm 65,5% |
| Raw → filtered p95 | 10,65 px | 2,00 px | giảm 81,2% |
| Raw → filtered p99 | 19,13 px | 4,61 px | giảm 75,9% |
| Độ lệch chuẩn hóa theo bề rộng nét p95 | 1,33× | 0,25× | giảm 81,2% |
| Chiều dài filtered/raw trung vị | 95,3% | 99,4% | ít co đường hơn |
| Geometry build/raw point | 1,00 | 1,00 | không đổi |
| Worst geometry build quan sát được | 3,372 ms | 1,809 ms | corpus 7C ngắn hơn, chỉ tham khảo |

Kết luận định lượng:

- Candidate 7C giải quyết đúng smoothing lag; p95 hiện chỉ bằng một phần tư bề rộng Pen 8 px.
- Endpoint và toàn bộ raw sample tiếp tục được giữ; không có sample nào bị mất trong corpus này.
- Path retention tăng cho thấy nét ít bị co góc/vòng cung hơn. Đây là ưu điểm về fidelity nhưng cần người dùng xác nhận bằng mắt rằng lượng damping còn đủ tự nhiên với chuột.
- Chưa khóa tham số 7C cho đến khi người dùng xác nhận cảm giác mượt, độ rung, vòng tròn nhỏ và chữ viết.

## Candidate 7D — round caps và symmetric closure

Phản hồi bằng mắt trên 7C: độ bám và cảm giác tổng thể tốt, nhưng đầu/cuối nét nhọn như tam giác và đầu nét đổi hình khi endpoint tiến vào ngưỡng closed-loop.

Nguyên nhân kỹ thuật:

- Pen vẫn dùng taper `10 px` ở đầu và `14 px` ở cuối, với bán kính tối thiểu chỉ `0,45 px`.
- Polygon geometry bắt đầu tại một outline vertex rồi đóng bằng một đoạn line riêng, khiến phép làm tròn không đối xứng quanh điểm bắt đầu.
- Khi nhận diện closed-loop bật, taper bị gỡ tức thời nên đầu nét nở từ `0,45 px` lên toàn bề rộng.

Thay đổi 7D:

- Tắt start/end taper cho Pen; giữ velocity-based thinning ở thân nét.
- Thêm cap bán nguyệt sáu phân đoạn vượt qua đúng hai endpoint cho mọi nét mở.
- Dựng quadratic closed geometry từ midpoint cuối–đầu và dùng mọi outline vertex làm control point; không còn đoạn đóng đặc biệt.
- Nét kín vẫn dùng tangent tuần hoàn và không thêm cap thừa tại seam.
- Thêm regression test xác nhận cap tròn vượt qua cả hai endpoint; suite hiện 30/30 đạt.

Tiêu chí chấp nhận trực tiếp:

- Đầu và cuối nét tròn, không còn mũi kim/tam giác.
- Khi khép vòng, đầu nét không nở hoặc đổi hình rõ rệt.
- Nét ngắn/click vẫn là dot tròn; thân nét và độ bám 7C không suy giảm.

## Candidate 7E — Natural Pen và open-seam stability

Phản hồi bằng mắt trên 7D:

- Khi endpoint quay về start point vẫn xuất hiện một vết cắt nhỏ tại seam.
- Dù hình dạng gesture khác nhau, thân Pen gần như monoline và thiếu cảm giác mực/bút thật.

Đo trên 44 Pen stroke 7D cho thấy pressure scale hiện chỉ nằm chủ yếu trong khoảng `1,05–1,11×`: ngưỡng `MaximumSpeed = 2,4 px/ms` quá cao và `Thinning = 0,22` quá nhẹ, nên nhánh mảnh hầu như không xuất hiện.

Thay đổi 7E:

- Không tự đổi topology của freehand thành closed polygon nữa. Nét luôn giữ danh tính open stroke; khi hai đầu gặp nhau, hai round cap chồng mực thay vì thay tangent/taper và cắt seam.
- Pen dùng `Thinning = 0,42`, `MaximumSpeed = 0,9 px/ms`, `PressureSmoothing = 0,32`.
- Replay trên corpus 7D dự kiến tạo pressure scale p05–p95 khoảng `0,80–1,21×`, thay cho `1,05–1,11×`.
- Chuyển động nhanh tạo nét thanh hơn; chuyển động chậm và ôm góc tạo nét đậm hơn. Cap vẫn tròn và smoothing tọa độ 7C không đổi.
- Highlighter tiếp tục dùng `Thinning = 0`, nên không bị biến thành Natural Pen ngoài ý muốn.
- Suite tăng lên 31/31: thêm kiểm thử seam được round-cap overlap phủ kín và kiểm thử đoạn chậm rộng hơn đoạn nhanh.

7E là candidate đầu tiên nhắm đồng thời ba thuộc tính: bám chuột, cap/seam ổn định và độ sống của thân nét. Cần đánh giá bằng mắt trước khi khóa biên độ pressure.

## Candidate 7F — tăng độ nhạy pressure

Người dùng đánh giá 7E khá tốt và yêu cầu độ dày nhạy thêm một chút. Replay 90 Pen stroke 7E được dùng để tránh tăng theo cảm tính:

| Cấu hình | Pressure scale p05–p95 | Width range/stroke trung vị |
|---|---:|---:|
| 7E | 0,79–1,21× | 0,34× |
| 7F chọn | 0,76–1,24× | 0,41× |
| Candidate mạnh hơn, chưa chọn | 0,75–1,25× | 0,43× |

7F thay đổi `Thinning 0,42 → 0,48`, `MaximumSpeed 0,90 → 0,85 px/ms`, `PressureSmoothing 0,32 → 0,36`. Mục tiêu là tăng khoảng 20% biến thiên nội bộ và phản hồi nhanh hơn một chút, không làm chữ nhỏ phồng–xẹp quá rõ.
