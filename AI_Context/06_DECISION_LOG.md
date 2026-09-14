# Decision Log

Ngày cập nhật: 2026-09-14

## D-001 — Windows là nền tảng đầu tiên

Trạng thái: Chốt.

Lý do: nhu cầu là vẽ trên màn hình desktop hiện tại; Windows cung cấp mô hình topmost transparent overlay và global hotkey phù hợp.

Hệ quả: macOS/Linux chưa nằm trong kiến trúc MVP.

## D-002 — Thiết kế mouse-first

Trạng thái: Chốt theo xác nhận trực tiếp của người dùng.

Lý do: người dùng thao tác chủ yếu bằng chuột.

Hệ quả:

- Mọi workflow cốt lõi phải dùng được với chuột hai nút và con lăn.
- Không phụ thuộc pressure, tilt, touch hay side buttons.
- Smoothing được tune cho mouse input trước stylus.

## D-003 — Apple Markup là nguồn cảm hứng UX chính

Trạng thái: Chốt.

Áp dụng:

- Toolbar di chuyển và tự thu gọn.
- Chọn lại công cụ để mở line weight/opacity.
- Vẽ rồi giữ để snap thành shape.
- Pixel eraser và object eraser.
- Lasso để thao tác trên object.

Không áp dụng:

- Sao chép nguyên giao diện/icon/visual identity.
- Đưa toàn bộ công cụ tài liệu như sticker, signature vào MVP.

## D-004 — Không dùng toolbar dài kiểu Epic Pen

Trạng thái: Chốt.

Lý do: người dùng đã trải nghiệm Epic Pen và đánh giá toolbar dài, xấu, khó sử dụng.

Thiết kế thay thế:

- Collapsed dot.
- Quick capsule năm nút.
- Các tính năng ít dùng nằm trong popover/menu thứ hai.

## D-005 — Temporary ink là mặc định và tồn tại khoảng 2 giây

Trạng thái: Chốt.

Baseline:

- 1.650 ms hiển thị đầy đủ.
- 350 ms fade.
- Stroke cách nhau dưới 400 ms được nhóm và reset lifetime chung.

Hệ quả: persistent annotation là hành vi chủ động qua pin hoặc mode riêng.

## D-006 — Chất lượng nét là product gate

Trạng thái: Chốt.

Lý do: nét xấu là một trong các vấn đề chính của công cụ hiện có.

Hệ quả: phải hoàn thành và xác nhận stroke-rendering spike trước khi đầu tư vào nhiều công cụ phụ.

## D-007 — Right-click và Esc là lối thoát nhanh khỏi Draw mode

Trạng thái: Đề xuất đã ghi vào spec, cần xác nhận qua prototype thực tế.

Lý do: mouse-first cần một thao tác nhanh để trả quyền click cho ứng dụng bên dưới.

## D-008 — WPF là UI shell ban đầu

Trạng thái: Đề xuất kỹ thuật, chưa khóa renderer.

Lý do: phù hợp Windows desktop, tray, transparent windows và Win32 interop.

Renderer cuối cùng sẽ được quyết định giữa custom WPF geometry và SkiaSharp sau Phase 2.

## D-009 — Offline và privacy-first

Trạng thái: Chốt cho MVP.

Lý do: annotation không cần dịch vụ mạng và có thể xuất hiện trên nội dung nhạy cảm.

Hệ quả: không chụp hoặc ghi nội dung màn hình nếu người dùng chưa chủ động gọi Capture.

## D-010 — Candidate A centerline stroke bị loại

Trạng thái: Chốt theo đánh giá trực tiếp và ảnh chụp của người dùng.

Lý do: adaptive filtering + cubic Bézier trên một centerline có độ dày cố định chỉ làm đường nối cong hơn; chưa cải thiện rõ cảm giác bút, taper, độ sống của nét hoặc hình vẽ bằng chuột.

Hướng thay thế Candidate B:

- Giữ centerline smoothing nhưng mạnh hơn cho chuột.
- Sinh polygon outline quanh centerline.
- Mô phỏng pressure nhẹ từ vận tốc.
- Taper đầu và cuối nét.
- Render outline bằng quadratic curves và fill anti-aliased.

Thiết kế pipeline tham khảo khái niệm spline points → outline polygon của dự án MIT `perfect-freehand`, nhưng implementation C# trong Screen Ink được viết riêng cho kiến trúc và yêu cầu mouse-first của project.

## D-011 — Chọn WPF filled-outline renderer cho MVP

Trạng thái: Chốt sau đánh giá Candidate B của người dùng.

Lý do: Candidate B thể hiện cải thiện rõ về taper, thân nét và đường cong trong ảnh kiểm thử. WPF renderer hiện đáp ứng mức chất lượng cần thiết mà không thêm native dependency.

Điểm chưa hoàn hảo: freehand loop khép kín có thể lộ mối nối nhọn. Phase 6 sẽ xử lý trường hợp khoanh vùng bằng hold-to-snap ellipse/circle; performance tiếp tục được đo và tối ưu khi số lượng object tăng.

## D-012 — Shift+drag tạo persistent stroke

Trạng thái: Chốt cho prototype Phase 3.

Lý do: temporary là mặc định nhưng cần một đường tắt mouse-first để giữ annotation. Việc giữ `Shift` lúc bắt đầu drag không làm toolbar dài thêm và phù hợp đặc tả ban đầu.

## D-013 — Timing temporary ink được chấp nhận

Trạng thái: Chốt theo đánh giá trực tiếp của người dùng.

Baseline giữ nguyên: 1.650 ms hiển thị đầy đủ và 350 ms fade, tổng lifetime khoảng 2 giây. Các stroke cách nhau dưới 400 ms tiếp tục dùng chung lifetime group.

## D-014 — Toolbar mặc định là capsule năm nút và collapsed dot

Trạng thái: Prototype đang chờ đánh giá trực tiếp.

Thiết kế hiện tại:

- Quick capsule gồm Pointer, Pen, Color, Undo và More.
- More chỉ chứa tác vụ ít dùng: Clear drawings và Exit.
- Toolbar tự thu thành dot 38 px ngay khi bắt đầu một nét.
- Dot mở lại capsule bằng một click chuột.
- Grip nhỏ cho phép kéo; khi thả, toolbar snap về cạnh trái hoặc phải và lưu vị trí theo từng display.

## D-015 — Phím số chọn màu chỉ hoạt động trong Draw mode

Trạng thái: Chốt theo yêu cầu trực tiếp của người dùng.

Ánh xạ: `1` đỏ, `2` vàng, `3` xanh dương, `4` xanh lá, `5` trắng. Hỗ trợ cả hàng số chính và numpad.

Lý do giới hạn trong Draw mode: thao tác đổi màu phải rất nhanh khi vẽ, nhưng Screen Ink không được chiếm phím số khi người dùng đã trở về Pointer và đang nhập liệu ở ứng dụng khác.

## D-016 — Phase 5 giữ một nút Current Tool thay vì kéo dài toolbar

Trạng thái: Candidate đang chờ đánh giá trực tiếp.

Nút công cụ vẫn thực hiện một-click để vào Draw từ Pointer. Khi Draw đang bật, bấm lại nút này mở popover gồm Pen, Highlighter, Pixel Eraser, Object Eraser, ba cỡ nét và Temporary/Persistent. Cách này giữ nguyên capsule năm nút của Phase 4.

Highlighter dùng một filled geometry opacity 34% cho mỗi stroke để các segment nội bộ không tạo vệt tối. Pixel Eraser dùng geometry exclusion; Object Eraser loại toàn bộ stroke. Cả hai tạo history action để Undo/Redo.

Usability correction: bản đầu chỉ mở popover khi Draw đang hoạt động nên người dùng không phát hiện các tool ở Pointer mode. Nút Current Tool hiện luôn mở popover ở mọi mode và có chevron làm affordance. Chọn một tool trong menu sẽ bật Draw rồi tự thu toolbar.

Light theme không dùng drop-shadow; đường viền mảnh là đủ để tách toolbar khỏi nền mà không tạo mảng xám thiếu tự nhiên.

## D-017 — Cursor Eraser dùng tương phản hai lớp và hình dạng riêng

Trạng thái: Candidate theo phản hồi trực tiếp của người dùng.

Cursor tẩy màu trắng đơn sắc bị mất trên nền trắng và hình tròn dễ nhầm với Highlighter. Pixel Eraser được đổi thành ô vuông có dấu `+`; Object Eraser là ô bo tròn có dấu `×`. Cả hai có viền ngoài tối luôn nhìn thấy và viền trong theo màu palette đang chọn, kể cả khi màu hiện tại là trắng.

## D-018 — Modifier+wheel chuyển tool chỉ trong Draw mode

Trạng thái: Candidate theo đề xuất của người dùng.

Candidate đầu dùng `Alt+wheel`, nhưng kiểm thử thực tế cho thấy khi thả Alt, ứng dụng đang có keyboard focus có thể hiểu nó như một cú nhấn Alt đơn và kích hoạt menu. Wheel không phải phím bàn phím nên không vô hiệu hóa hành vi hệ thống này. Candidate được đổi sang `Shift+wheel`: Screen Ink chỉ xử lý tổ hợp trong Draw mode; Pointer mode trả nguyên wheel cho ứng dụng bên dưới. Cách này cũng tránh phải chặn Alt toàn cục hoặc inject một phím giả, vốn có nguy cơ ảnh hưởng Alt+Tab và accelerator của ứng dụng khác.

Wheel xuống chuyển theo Pen → Highlighter → Pixel Eraser → Object Eraser; wheel lên đi ngược. Tên tool hiện cạnh con trỏ trong 650 ms.

## D-019 — Modifier+wheel chuyển sang low-level mouse hook tối thiểu

Trạng thái: Candidate đang chờ xác nhận trực tiếp.

WPF `MouseWheel` không ổn định vì overlay dùng `ShowActivated=False` và không giành focus. Hook `WH_MOUSE_LL` được dùng để bắt đúng `Shift+wheel`/`Ctrl+wheel` trong Draw và phát hiện click ngoài toolbar trong Pointer. Callback không làm I/O, chỉ enqueue action rồi trả ngay; sự kiện không thuộc shortcut luôn được chuyển tiếp. Hook được tháo khi ứng dụng thoát.

Toolbar bổ sung Redo cạnh Undo bằng hai hit target hẹp, nâng chiều dài capsule từ 200 lên 224 px. Trong Pointer, click ngoài toolbar làm capsule thu thành dot; popup đang mở không bị đóng trước khi nhận click.

## D-020 — Clear là một history action và modifier-wheel được gom nhịp

Trạng thái: Candidate theo phản hồi trực tiếp của người dùng.

`Delete`/Clear drawings không còn xóa cứng stroke và history. Toàn bộ stroke hiện hữu được detach thành một composite history action; `Ctrl+Z` khôi phục cả lần clear và `Ctrl+Y` thực hiện clear lại. Trạng thái persistent/temporary và thứ tự hiển thị của từng stroke được giữ khi phục hồi.

Low-level hook chỉ tích lũy wheel delta. Một timer thread-pool 45 ms chốt toàn bộ burst thành đúng một UI action, còn việc lưu preferences được debounce 350 ms và ghi bất đồng bộ ngoài UI thread. Cursor style chỉ render ngay trên overlay đang chứa chuột; các overlay khác trì hoãn cập nhật đến khi chuột đi vào. Tool feedback cũng chỉ xuất hiện trên màn hình hiện hành. Nhờ đó một burst không còn dựng lại visual toàn màn hình hoặc ghi JSON ở từng nấc cuộn, và callback hook vẫn kết thúc nhanh.

## D-021 — Arrow snap dùng gesture một nét có hai cánh

Trạng thái: Giữ nguyên, không tiếp tục tinh chỉnh theo quyết định của người dùng. Người dùng có thể tự vẽ mũi tên và không xem auto-snap là tính năng cần ưu tiên.

Arrow được nhận dạng từ gesture: thân gần thẳng đi từ đuôi tới đầu, cánh thứ nhất quay về đầu, rồi cánh thứ hai nằm ở phía đối diện. Bộ nhận dạng kiểm tra straightness của thân, tỷ lệ chiều dài hai cánh, hướng cánh quay ngược thân và dấu cross-product đối nhau để tránh biến scribble thành arrow.

Kết quả snap dựng lại một mũi tên cân đối với round cap/join theo đúng cỡ Pen. Cơ chế history giữ nguyên: Undo đầu tiên trả về toàn bộ freehand gesture, Undo tiếp theo xóa stroke.

## D-022 — Lasso là tool thứ năm trong compact tool menu

Trạng thái: Candidate chờ đánh giá trực tiếp.

Lasso được thêm vào vòng `Shift+wheel` và popover Current Tool mà không kéo dài capsule. Kéo chuột tạo vùng chọn nét đứt; các stroke nằm trong hoặc giao với vùng được chọn. Viền xanh bao quanh selection là hit area để kéo toàn bộ selection.

Trong khi được chọn, temporary stroke được rút khỏi lifetime group để không biến mất giữa thao tác; khi bỏ chọn, lifetime 2 giây bắt đầu lại. `Delete` xóa selection nếu có, nếu không có selection thì giữ hành vi Clear All. Move và Delete đều tạo unified history action cho Ctrl+Z/Ctrl+Y.

## D-023 — Chất lượng nét Pen là một phase và release gate riêng

Trạng thái: Chốt theo yêu cầu trực tiếp của người dùng.

Phase 7 được dành riêng cho Pen Stroke Quality Hardening và đặt trước Capture/Packaging. Phase này không được xem là polish tùy chọn: Screen Ink chỉ chuyển sang hoàn thiện phát hành sau khi có corpus chuột thật, visual regression, số đo latency/frame-time và xác nhận trực tiếp của người dùng.

Candidate B hiện tại là baseline có thể rollback. Mọi thay đổi smoothing/resampling/outline phải chứng minh được cải thiện mà không tăng cảm giác trễ, không phụ thuộc một loại chuột và không gây regression cho Highlighter/Eraser.

## D-024 — Slice 7A đo opt-in và xử lý riêng closed-loop seam

Trạng thái: Candidate chờ đánh giá trực tiếp.

Recorder chỉ bật qua `SCREENINK_STROKE_DIAGNOSTICS=1`, ghi raw/filtered points và timing ở background sau khi stroke hoàn tất; không chụp nội dung màn hình và không tạo chi phí đo/ghi file trong chế độ dùng thường.

Vòng kín được nhận biết bằng khoảng cách đầu-cuối tương quan với chiều dài stroke và cỡ Pen. Với vòng kín, taper hai đầu bị tắt và normal tại seam dùng các điểm lân cận theo chu kỳ. Nét mở tiếp tục giữ nguyên thuật toán Candidate B để giảm phạm vi regression.

## D-025 — Natural Pen 7F là chuẩn nét bút được người dùng chấp nhận

Trạng thái: Chốt theo đánh giá trực tiếp của người dùng.

Renderer giữ smoothing 7C (`0,30–0,85`, ngưỡng `0,65 px/ms`), round cap sáu phân đoạn và symmetric quadratic closure. Freehand không tự đổi topology thành closed polygon; hai cap chồng mực khi đầu-cuối gặp nhau để tránh seam bị cắt hoặc đầu nét đổi hình.

Natural pressure dùng vận tốc chuột với `Thinning = 0,48`, `MaximumSpeed = 0,85 px/ms`, `PressureSmoothing = 0,36`. Trên 90 stroke thật, pressure scale dự kiến nằm chủ yếu trong `0,76–1,24×`. Người dùng xác nhận 7F là phiên bản tốt nhất và cho phép chuyển phase.

## D-026 — Capture toàn virtual desktop ẩn toolbar trước

Trạng thái: Candidate Phase 8 slice A.

Full-screen capture dùng physical bounds của Windows virtual desktop để bao phủ cả monitor có tọa độ âm. Toolbar và ContextMenu được ẩn, chờ một nhịp composition/DWM rồi mới copy pixels; các overlay annotation vẫn hiện trong ảnh. Cùng một bitmap pipeline phục vụ Clipboard PNG và Save PNG để hai đầu ra không sai khác.

## D-027 — Hoãn Phase 8, thực hiện Phase 9 với Color Gate trước packaging

Trạng thái: Chốt theo yêu cầu trực tiếp của người dùng.

Phase 8 dừng sau full-screen Copy/Save đã được xác nhận tốt; Capture vùng và hardening monitor/DPI nằm trong backlog. Phase 9 tiếp tục nhưng tạm loại reduced motion, license và third-party notices khỏi phạm vi hiện tại.

Trước khi tạo installer/package cuối, dự án phải có một Color & Ink Appearance gate riêng. Trọng tâm không phải thêm nhiều swatch mà là độ đậm nhạt, tương phản trên nền sáng/tối và khả năng biến thiên lượng mực trong một stroke. Không thay đổi renderer màu trước khi trao đổi và A/B trực tiếp với người dùng.

Phase 9A cung cấp hotkey preset có rollback an toàn nếu `RegisterHotKey` thất bại, cùng tùy chọn Start with Windows theo người dùng hiện tại và mặc định tắt.

## D-028 — Icon nét mực và shortcut Release

Trạng thái: Candidate Phase 9 theo yêu cầu trực tiếp của người dùng.

Nhận diện Screen Ink dùng tile than tối bo tròn, một nét coral có pressure variation và cursor-tip trắng tích hợp. Icon không dùng chữ, toolbar hoặc hình cây bút chung chung; silhouette được ưu tiên để nhận ra ở 16 px.

Nguồn raster được tạo bằng built-in ImageGen, lưu tại `src/ScreenInk.App/Assets/screenink-icon-source.png`, sau đó chuyển thành ICO đa kích thước `16–256 px` tại `src/ScreenInk.App/Assets/screenink.ico`. ICO được nhúng vào EXE, MainWindow và system tray.

Shortcut phát triển `Screen Ink.lnk` được tạo trên Desktop, trỏ tới bản Release và lấy icon từ EXE. Installer cuối sẽ chịu trách nhiệm tạo shortcut chính thức sau Color Gate.

## D-029 — Single instance và crash marker không phục hồi overlay dễ lỗi

Trạng thái: Candidate Phase 9.

Named mutex theo user session ngăn chạy chồng nhiều overlay; kiểm tra thực tế mở shortcut rồi mở EXE lần hai vẫn chỉ có một process. Mỗi phiên tạo `running.marker`; clean exit xóa marker. Nếu phiên trước crash/ bị kill, lần chạy sau ghi nhận recovery và luôn bắt đầu Pointer với overlay/history volatile rỗng, thay vì phục hồi trạng thái có nguy cơ chặn chuột.

## D-030 — Natural Ink dùng dual geometry, không dùng background sampling

Trạng thái: Candidate Color & Ink Appearance A/B.

Pen dùng hai outline đồng bộ trên cùng point stream: lớp nền sáng hơn màu palette 3%, và ink core rộng 52% với pressure response mạnh hơn, màu sâu hơn 18% ở opacity 28%. Core coverage thay đổi theo velocity width nên vùng chậm/ôm góc nhận nhiều sắc đậm hơn, vùng nhanh nhẹ hơn. Hiệu ứng không lấy mẫu pixel nền, không dùng shadow, texture hoặc gradient theo bounding box.

Ink core là thành phần thật của stroke: cùng fade, detach/restore, Pixel Eraser, Lasso move, shape snap và Undo/Redo. Highlighter không có core để giữ màu phẳng và tránh chồng opacity nội bộ.

Collapsed toolbar dot phân biệt click và drag bằng ngưỡng drag chuẩn của Windows. Click tiếp tục mở capsule; drag di chuyển window rồi snap cạnh/lưu placement.

Kết quả Candidate A: người dùng gần như không nhận thấy thay đổi màu; cơ chế `Window.DragMove()` cũng không chiếm lại được chuột từ collapsed Button. Candidate B tăng ink core từ 52%/28% opacity lên 64%/42% opacity, tăng dark blend từ 18% lên 28%. Kéo collapsed dot chuyển sang cập nhật HWND trực tiếp theo tọa độ con trỏ vật lý, rồi snap khi mouse-up.

Kết quả kéo HWND qua WPF PreviewMouseMove vẫn thất bại trên máy thật vì collapsed Button/no-activate window không cung cấp chuỗi move đáng tin cậy. Candidate tiếp theo mở rộng low-level mouse hook sẵn có: chỉ khi left-down hit đúng physical HWND bounds của collapsed dot mới theo dõi move/up, dispatch tọa độ sang UI thread và di chuyển HWND. Drag ngoài toolbar không bị theo dõi nên không tăng tải cho ứng dụng khác.

Kết quả low-level hook vẫn thất bại thực tế do callback hook và UI dispatcher không tạo được một native drag loop ổn định cho no-activate window. Phương án này bị loại. Candidate kế tiếp dùng `ReleaseCapture` + `WM_NCLBUTTONDOWN(HTCAPTION)` ngay tại PreviewMouseDown của dot: Windows trực tiếp sở hữu vòng kéo; khi nhả chuột, app chỉ phân biệt click/drag để mở hoặc snap cạnh. Phần hook trở lại đúng phạm vi shortcut/click-outside ban đầu.

Drop-shadow của toolbar bị xóa hoàn toàn ở cả dark và light theme. Theme chỉ còn surface, border và foreground để capsule/dot không tạo quầng giả trên nền màn hình.

## D-031 — Phase 7G tạo Signature Ink nhưng không viết lại Natural Pen 7F

Trạng thái: Kế hoạch đã chốt, chưa triển khai renderer.

Natural Pen 7F tiếp tục là baseline điều khiển và rollback. Phase 7G chỉ polish dấu ấn thị giác theo ba candidate: A là 7F + Natural Ink B hiện tại; B thử depth mapping và tích mực nhẹ ở vùng giảm tốc/đổi hướng; C tiếp tục tối ưu dot/nét ngắn, 50–100 ms đầu và micro-variation xác định.

Ưu tiên là first contact, chiều sâu mực, góc cua/overlap và tương phản đỏ/xanh trên nền trắng/xám/tối. Cấm texture/noise rõ, random theo frame, shadow/glow, calligraphy pressure quá mức, background sampling hoặc thay smoothing/path đã khóa chỉ để tăng hiệu ứng.

Phase chỉ hoàn thành sau A/B bằng chuột thật, regression Eraser/Lasso/history/fade/capture và xác nhận trực tiếp của người dùng. Candidate thắng sẽ đóng băng renderer/color cho beta trước packaging.

## D-032 — Candidate 7G-B chỉ biến đổi coverage của ink core

Trạng thái: Candidate đã triển khai, chờ đánh giá trực tiếp.

Centerline, smoothing, outline chính, cap và width response của Natural Pen 7F được giữ nguyên. Ink core nhạy với speed hơn (`Thinning 0,84`, `MaximumSpeed 0,78`, smoothing `0,44`) và có corner pooling tối đa lý thuyết 14%. Pooling được dẫn động bởi góc đổi hướng và trọng số chuyển động chậm, vì vậy góc chậm sâu mực hơn nhưng góc nhanh không phình mạnh; đường thẳng nhận pooling bằng 0.

Không thêm Path/layer, random, texture, shadow hoặc opacity animation mới. Hai geometry test khóa hành vi: góc chậm phải tăng coverage nhìn thấy nhưng không vượt anti-blob limit; đường thẳng phải không bị thay đổi bởi riêng corner pooling.

## D-033 — High-speed tail không được bão hòa ở tốc độ viết thường

Trạng thái: Candidate 7G-B2, chờ đánh giá trực tiếp.

Người dùng xác nhận góc cua của 7G-B tự nhiên nhưng nét sau không nhỏ thêm khi tiếp tục tăng tốc. Trace 14 nét thật đo `p90` đến `2,73 px/ms` và max khoảng `4,02 px/ms`; mapping cũ clamp từ `0,85 px/ms`, đúng với hiện tượng thị giác.

B2 dùng speed response phi tuyến `pow(clamp(speed/max), exponent)` với max khoảng `3 px/ms`, thay vì clamp tuyến tính sớm. Width giảm liên tục qua vùng nhanh vừa → nhanh → quét rất nhanh. Renderer không tự biến curve thành line vì điều đó làm sai ý định; centerline/smoothing giữ nguyên, chỉ outline/core width thay đổi. Automated test khóa thứ tự `slow width > fast width > very-fast width` và đáy width để nét nhanh không trở nên quá mảnh, dễ vỡ.

## D-034 — Candidate 7G-B3 dùng speed response đuôi mở

Trạng thái: Candidate chờ đánh giá trực tiếp.

B2 được người dùng đánh giá tốt hơn một chút nhưng trace mới có sample `8–28 px/ms`, vượt xa clamp `3,2 px/ms`. Không tiếp tục nâng một hard limit vì sẽ làm vùng viết chậm kém nhạy và vẫn thất bại ở polling pattern khác.

B3 dùng hàm tiệm cận không clamp `v^e/(1+v^e)`. Nét tiếp tục mảnh dần từ nhanh vừa đến cú quét cực nhanh nhưng width không về 0. Main/core giữ round cap và centerline cũ; corner pooling đã được chấp nhận nên không đổi. Test hiện so sánh bốn mức `0,2 / 0,85 / 2,4 / 12 px/ms` và yêu cầu width giảm nghiêm ngặt nhưng nét 8 px vẫn rộng tối thiểu 5 px ở mức cực nhanh.

## D-035 — Candidate 7G-B4 phải đủ khác để A/B bằng mắt

Trạng thái: Candidate expressive chờ đánh giá trực tiếp.

B3 đúng về toán nhưng người dùng vẫn thấy không khác nhiều. Replay trace preset 8 px cho thấy width khoảng `5,4–10,4 px`; trên nét dài, thay đổi này chưa tạo dấu ấn thị giác rõ. B4 tăng main thinning từ `0,72` lên `1,00` và response smoothing `0,42 → 0,52`, dự kiến mở khoảng thành `4,4–11,4 px` trên cùng trace.

Đây là A/B biên độ, không phải thay topology: centerline, round cap, open-ended response, ink core và corner pooling giữ nguyên. Width floor regression của preset 8 px được điều chỉnh từ 5,0 xuống 4,2 px nhưng vẫn cấm nét biến mất/đứt. Nếu B4 quá biểu cảm, candidate cuối sẽ nội suy tham số giữa B3 và B4.

## D-036 — Candidate 7G-C1 chỉ polish first contact và core texture

Trạng thái: Candidate chờ đánh giá trực tiếp; B4 là rollback.

Người dùng đánh giá cảm giác B4 có vẻ ổn, vì vậy không tăng thinning/corner response thêm. C1 hạ dot threshold về độ dài vật lý 1,25 px thay vì bằng toàn bộ pen size: click/jitter rất nhỏ vẫn là dot, còn nét 5–20 px giữ phương hướng thành capsule có hai round cap.

Micro-variation chỉ thay coverage của ink core tối đa 2,4% theo hai sóng xác định từ cumulative distance và tọa độ bắt đầu. Main silhouette B4 không nhận variation; cùng input render lặp lại cho cùng kết quả và không có random theo frame. Hai test mới khóa short capsule cùng tính deterministic/bounded, nâng suite lên 36 test.

## D-037 — Signature Ink cuối là C1 trên nền B4

Trạng thái: Chốt theo xác nhận trực tiếp của người dùng.

Người dùng xác nhận C1 ổn sau khi thử dot, short capsule, chữ nhỏ và nét dài. Renderer cuối giữ B4 expressive open-ended velocity response, corner pooling 14%, dot threshold 1,25 px và deterministic core variation 2,4%/18 px. Nhãn recorder chuyển thành `7G-signature-ink-final`.

Color & Ink Appearance gate được khóa. Không tuning thêm trước beta nếu không có regression P0/P1 tái hiện được; B4 không micro-variation là rollback gần nhất.

## D-038 — Collapsed control dùng signature ink puck, không dùng record dot

Trạng thái: Chốt theo xác nhận trực tiếp của người dùng.

Collapsed button vẫn là puck tròn 38 px và giữ native caption drag, nhưng chấm tròn trung tâm bị thay bằng nét mực bất đối xứng theo màu đang chọn, facet nib trắng và inner disc phẳng. Thiết kế không dùng shadow/glow, tránh đọc nhầm thành nút Record và liên hệ trực tiếp với app icon. Người dùng xác nhận thiết kế mới tốt.

## D-039 — Beta installer là self-contained single-user Inno Setup

Trạng thái: Candidate đã build và xác minh install/uninstall.

`0.9.0-beta.1` publish single-file self-contained `win-x64`, không yêu cầu máy đích có .NET. Inno Setup 7 dùng `PrivilegesRequired=lowest`, cài dưới `%LOCALAPPDATA%\Programs\Screen Ink`, tạo Start Menu shortcut và để Desktop shortcut là tùy chọn.

Test cô lập đạt cho install → chạy published app → uninstall; không còn thư mục cài hoặc startup registry. Artifact 48,61 MiB có SHA-256 `BA12503E13E03844E85534D1DC40E9AD948E8ECEB32364BE8E2083964FE09E92`. Chưa có Authenticode certificate nên private beta có thể gặp SmartScreen; public distribution cần quyết định code signing.
