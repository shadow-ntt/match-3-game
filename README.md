# Match-3 Game

Dự án game Match-3 được phát triển bằng Unity (Universal Render Pipeline - 2D).

## 1. Yêu cầu Hệ thống / Môi trường Phát triển
- **Unity Version**: `6000.5.7f1` (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP - 2D)
- **Plugins**: DOTween, OdinSerializer

---

## 2. Kiến trúc Hệ thống & Gameplay

### 2.1. Chuẩn hóa Hệ tọa độ & Cơ chế Adapter lúc Spawn

#### A. Bản chất & Nguyên lý Adapter lúc Spawn
- **Dữ liệu Level JSON gốc**: Vẫn giữ nguyên cấu trúc ma trận 2D `[row, col]` (hàng `0` ở đỉnh màn hình, xuất từ công cụ thiết kế level). Không cần convert hay re-export lại hàng loạt file level JSON có sẵn.
- **Cơ chế Adapter tại `Board.SpawnLayer`**: Chỉ đảo trục $Y$ **duy nhất 1 lần** lúc đọc dữ liệu để nạp vào ma trận runtime:
  ```csharp
  int x = c;
  int y = height - 1 - r;
  targetGrid[x, y] = obj;
  ```
- **Đổi tên biến chuẩn hóa**: Thay thế toàn bộ các biến gây nhầm lẫn như `rows/cols` trong runtime thành `width/height` và luôn truy xuất theo thứ tự chuẩn `[x, y]`.

#### B. Lợi ích cho toàn bộ Gameplay Logic
- **Đồng bộ hoàn toàn với Unity World Space**: Tọa độ $(X, Y)$ trong grid map $1:1$ với trục $X$ (ngang) và trục $Y$ (dọc) của Unity:
  - **`X`**: Cột (từ trái `0` sang phải `Width - 1`).
  - **`Y`**: Hàng (từ đáy `0` lên đỉnh `Height - 1`).
  - **Ma trận runtime**: `grid[x, y]` với kích thước `[width, height]`.
- **Trọng lực rơi tự nhiên (Falling Logic)**: Ô bên dưới ô `(x, y)` đơn giản là `(x, y - 1)`. `ItemFallManager` chỉ cần kiểm tra `(x, y - 1)` mà không phải làm bất kỳ phép tính nghịch đảo nào.
- **Tính toán World Position tinh gọn**: `GridUtils.GridToWorld(grid, x, y)` và `GridUtils.WorldToGrid(grid, worldPos)` hoàn toàn không còn phép nghịch đảo `rows - 1 - row` nào.
- **Input & Vuốt trực quan**: Hướng vuốt từ `InputUtils` ánh xạ trực tiếp sang vector offset chuẩn: `Right (1, 0)`, `Left (-1, 0)`, `Up (0, 1)`, `Down (0, -1)`.
- **Loại bỏ triệt để nguy cơ nhầm lẫn hàng/cột**: Tất cả các hệ thống (`LineScanMatchChecker`, `ItemFallManager`, `CheckMatchManager`, `HandleInput`) đều dùng chung chuẩn `(x, y)`.

---

### 2.2. Kiểm tra & Xử lý Match (CheckMatchManager & LineScanMatchChecker)

#### A. Thuật toán quét Match (`LineScanMatchChecker`)
- **Quét đoạn thẳng**: Dùng chung logic `ScanLines` quét các đoạn thẳng liên tục $\ge 3$ cùng màu (ngang và dọc).
- **Phân tích hình học**:
  - Giao nhau chữ T, L, + $\rightarrow$ `TNT` (gán tâm tại giao điểm hoặc ô vừa vuốt).
  - Đoạn thẳng $\ge 5$ $\rightarrow$ `LightBall`.
  - Đoạn thẳng $4$ $\rightarrow$ `Rocket` (ngang sinh VerticalRocket, dọc sinh HorizontalRocket).
  - Đoạn thẳng $3$ $\rightarrow$ `Normal`.
  - Khối vuông 2x2 $\rightarrow$ `Missile`.

#### B. Quản lý xử lý Match & Nổ liên hoàn (`CheckMatchManager`)
Hàm `async UniTask<bool> CheckMatch(priorityCenter)`:
- Kiểm tra ban đầu: nếu không có match nào thì trả về `false` ngay (để `HandleInput` kích hoạt Swap back).
- **Vòng lặp `while (true)` xử lý nổ liên hoàn (Cascade / Combo)**:
  1. Quét tìm tất cả các match trên bàn cờ. Nếu không còn match nào $\rightarrow$ `break` thoát vòng lặp.
  2. Duyệt qua từng match và switch theo `MatchType`:
     - **`MatchType.Normal`**: Phát hạt nổ `PoolBlockBreakEffect.Play(...)`, thu hồi GameObject về `Pooltem`, xóa khỏi `NormalGrid[x, y]`, đặt `BoardCell.State = Empty`.
     - **Các loại Booster**: Sẽ sinh item booster tương ứng tại `centerCell`.
  3. Chờ hiệu ứng nổ hiển thị qua `await UniTask.Delay(explosionDelay)`.
  4. Kích hoạt và chờ toàn bộ item có sẵn rơi xuống đáy qua `await ItemFallManager.Instance.OnItemFallAsync()`.
  5. Sinh các viên ngọc mới từ trên trời rơi xuống lấp đầy các ô trống còn lại ở đỉnh mỗi cột qua `await SpawnItemFromSky.Instance.SpawnFromSkyAsync()`.
  6. Reset `currentCenter = null` và lặp lại bước 1 cho đến khi không còn match nào (bàn cờ ổn định).
- Trả về `true` khi hoàn thành chuỗi nổ.

---

### 2.3. Cơ chế Rơi ngọc (ItemFallManager)
- Hàm `OnItemFall(onComplete)` & `async UniTask<bool> OnItemFallAsync()`:
  - Duyệt từng cột `x` từ đáy lên đỉnh (`y` từ $1 \rightarrow Height - 1$).
  - Kiểm tra ô bên dưới `(x, y - 1)`: nếu là `IsEmpty`, tìm ô trống thấp nhất trong cột.
  - Cập nhật ma trận và trạng thái ô cờ (`Empty` $\rightarrow$ `Falling` $\rightarrow$ `Occupied`).
  - Dùng DOTween (`DOMove`) di chuyển ngọc rơi xuống mượt mà.
  - Hỗ trợ `OnItemFallAsync` cho phép `await` toàn bộ animation rơi của tất cả các cột hoàn tất.

---

### 2.4. Cơ chế Sinh ngọc mới từ trên trời (SpawnItemFromSky)
- Singleton `SpawnItemFromSky`:
  - Quét tìm các ô còn trống trong ma trận sau khi ngọc cũ dồn xuống đáy.
  - Xác định điểm sinh trên đỉnh mỗi cột `(x, topY + 1)`.
  - Sinh ngọc ngẫu nhiên từ `allowedColors` từ `Pooltem`.
  - Tạo hoạt ảnh rơi (`DOMove`, `Ease.InQuad`) từ ngoài viền màn hình xuống ô trống mục tiêu, xếp hàng mượt mà không chồng hình ảnh.
  - Cung cấp `async UniTask<bool> SpawnFromSkyAsync()` phối hợp trong chuỗi combo liên hoàn.

---

### 2.5. Điều khiển & Hoán đổi (HandleInput)
- Lắng nghe thao tác chạm/vuốt của người chơi.
- Vuốt vượt ngưỡng `swipeThreshold` là hoán đổi ngay lập tức qua DOTween.
- Phương thức `AnimateSwapAndCheck` sử dụng `UniTask`:
  - `await UniTask.WhenAll(tweenA.ToUniTask(), tweenB.ToUniTask())`: Chạy mượt mà hoạt ảnh hoán đổi 2 viên ngọc.
  - `bool matched = await checkMatchManager.CheckMatch(priorityCenter)`:
    - Nếu `!matched`: Tự động hoán đổi ngược lại vị trí cũ (Swap back) rồi mới mở khóa input.
    - Nếu `matched`: `CheckMatchManager` tự động xử lý toàn bộ chuỗi nổ liên hoàn và rơi ngọc trong vòng lặp `while`, khóa input người chơi cho đến khi toàn bộ combo kết thúc.

---

### 2.6. Hệ thống Pool & Hiệu ứng
- **`Pooltem`**: Pool quản lý nạp và thu hồi các `IBoardItem`.
- **`PoolBlockBreakEffect`**: Pool hiệu ứng hạt nổ cho đầy đủ 7 màu ngọc (`Red`, `Blue`, `Green`, `Yellow`, `Purple`, `Orange`, `Pink`) tương ứng với 7 Prefab trong `Assets/Prefabs/Effects/Particles/`.

---

## 3. Quy ước Lập trình Dự án (Coding Guidelines)
- **Chỉ sử dụng comment dòng đơn `//`**: Tuyệt đối không dùng XML doc comment `///` hay block comment `/* */`.
- **Hạn chế Debug log**: Không dùng `Debug.Log` / `Debug.LogWarning` bừa bãi khi không thực sự cần thiết.
- **Awake lấy dữ liệu**: Dùng `Awake()` để lấy dữ liệu, tham chiếu (`GetComponent`, đọc file cấu hình, gán references).
- **Start khởi tạo**: Dùng `Start()` để khởi tạo trạng thái runtime, khởi tạo các manager/checker và vẽ bàn cờ.
- **Không kiểm tra null cho `[SerializeField]`**: Đã là `[SerializeField]` (được gán từ Inspector) thì không check null dư thừa.
- **Hạn chế kiểm tra null không cần thiết**: Giữ luồng code gọn gàng, súc tích, tránh bọc điều kiện kiểm tra null tràn lan.

