# AI Context — Screen Annotation Tool

Thư mục này là nguồn thông tin chung cho toàn bộ quá trình thiết kế và phát triển công cụ vẽ trực tiếp lên màn hình.

Mục tiêu của bộ tài liệu là giúp mọi phiên làm việc sau này trả lời nhanh các câu hỏi:

- Sản phẩm đang giải quyết vấn đề gì?
- Những quyết định nào đã được chốt với người dùng?
- Trải nghiệm chuột phải hoạt động như thế nào?
- Kiến trúc kỹ thuật dự kiến là gì?
- Làm theo thứ tự nào và khi nào một giai đoạn được coi là hoàn thành?
- Phải kiểm thử những trường hợp nào trước khi phát hành?

## Thứ tự đọc

1. `01_PRODUCT_BRIEF.md` — mục tiêu, phạm vi và tiêu chí thành công.
2. `02_UX_INTERACTION_SPEC.md` — hành vi chi tiết của chuột, toolbar và nét vẽ.
3. `03_TECHNICAL_ARCHITECTURE.md` — kiến trúc Windows và các module dự kiến.
4. `04_IMPLEMENTATION_PLAN.md` — kế hoạch triển khai theo giai đoạn.
5. `05_TEST_PLAN.md` — chiến lược kiểm thử và ma trận môi trường.
6. `06_DECISION_LOG.md` — các quyết định đã chốt và lý do.
7. `07_STATUS.md` — trạng thái hiện tại và bước tiếp theo.
8. `08_PEN_STROKE_QUALITY_PHASE.md` — hợp đồng chất lượng, corpus, số đo và exit criteria riêng cho Phase 7.
9. `09_PHASE7_BASELINE_REPORT.md` — số đo chuột thật của Candidate B và giả thuyết tối ưu cho candidate kế tiếp.

## Tóm tắt đã chốt

- Nền tảng đầu tiên: Windows desktop.
- Thiết bị nhập chính: chuột; stylus chỉ là hỗ trợ bổ sung.
- Sản phẩm là lớp overlay trong suốt, vẽ trực tiếp trên mọi ứng dụng.
- Trải nghiệm lấy cảm hứng từ những điểm mạnh của Apple Markup, không sao chép giao diện theo pixel.
- Toolbar phải ngắn, đẹp, tự thu gọn; không lặp lại trải nghiệm toolbar dài của Epic Pen.
- Chế độ mặc định là nét tạm thời.
- Nét tạm thời tồn tại tổng cộng khoảng 2 giây sau khi kết thúc nhóm nét.
- Chất lượng và cảm giác của nét vẽ là ưu tiên cấp cao nhất.
- Ứng dụng hoạt động offline và không chụp màn hình nếu người dùng chưa chủ động yêu cầu.

## Quy ước cập nhật

- Khi một quyết định sản phẩm thay đổi, cập nhật cả tài liệu liên quan và `06_DECISION_LOG.md`.
- Khi hoàn thành một giai đoạn, cập nhật `07_STATUS.md` và checklist trong `04_IMPLEMENTATION_PLAN.md`.
- Không coi một tính năng là hoàn thành nếu chưa đạt tiêu chí kiểm thử tương ứng trong `05_TEST_PLAN.md`.
- Những con số có nhãn “đề xuất ban đầu” phải được xác nhận bằng prototype hoặc usability test trước khi khóa.
