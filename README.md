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
     - **`MatchType.Normal`**: Phát hạt nổ `PoolBlockBreakEffect.Play(...)`, thu hồi GameObject về `Pooltem`, xóa khỏi `MidGrid[x, y]`, đặt `BoardCell.State = Empty`.
     - **Các loại Booster**: Sinh item booster tương ứng tại `centerCell` trên `MidGrid`.
  3. Chờ hiệu ứng nổ hiển thị qua `await UniTask.Delay(explosionDelay)`.
  4. Kích hoạt và chờ toàn bộ item có sẵn rơi xuống đáy qua `await ItemFallManager.Instance.OnItemFallAsync()`.
  5. Sinh các viên ngọc mới từ Spawner rơi xuống lấp đầy các ô trống còn lại qua `await SpawnItemFromSky.Instance.SpawnFromSkyAsync()`.
  6. Reset `currentCenter = null` và lặp lại bước 1 cho đến khi không còn match nào (bàn cờ ổn định).
- Trả về `true` khi hoàn thành chuỗi nổ.

---

### 2.3. Cơ chế Rơi ngọc nâng cao (ItemFallManager)
- Hàm `async UniTask<bool> OnItemFallAsync()`:
  - **Thuật toán nhiều Pass (`StepFallAsync`)**: Lặp lại từng bước rơi cho đến khi không còn item nào có thể rơi tiếp.
  - **Rơi thẳng (Straight Fall)**: Ưu tiên rơi thẳng xuống các ô `Empty` bên dưới trong cùng cột.
  - **Rơi chéo (Diagonal Fall)**: Khi gặp vật cản bên dưới, item tự động trượt chéo sang trái hoặc phải vào các ô trống nếu thỏa mãn điều kiện luồng rơi không bị chặn.
  - **Quản lý trạng thái ô cờ**: Cập nhật logic ma trận `MidGrid` ngay lập tức và chuyển đổi trạng thái `BoardCell.State` (`Empty` $\rightarrow$ `Falling` $\rightarrow$ `Occupied`) với cờ `IsGettingFilled` tránh tình trạng ngọc rơi đè lên nhau.
  - **Chuẩn hóa tốc độ rơi**: Hàm `CalculateFallDuration` tính toán thời gian rơi tỉ lệ với căn bậc hai khoảng cách ô ($t = \text{base} \times \sqrt{\Delta y}$) tạo cảm giác gia tốc vật lý tự nhiên.

---

### 2.4. Cơ chế Sinh ngọc mới từ Spawner (SpawnItemFromSky)
- Singleton `SpawnItemFromSky`:
  - **Nhận diện Spawner**: Tự động tìm kiếm tọa độ các ô sinh ngọc (`ItemId == EnumItemBoard.Spawn`) ở đỉnh mỗi cột.
  - **Xếp hàng sinh ngọc**: Đếm số ô trống cần lấp đầy dưới mỗi Spawner và tính toán `spawnOffset` để các viên ngọc sinh ra liên tiếp rơi xuống có khoảng cách đều đặn, không chồng lấn lên nhau.
  - **Quản lý Pool**: Lấy ngẫu nhiên các màu ngọc hợp lệ (`allowedColors`) từ `Pooltem` và gán vào `MidTilemap`.
  - Phối hợp nhịp nhàng trong vòng lặp Cascade của `CheckMatchManager`.

---

### 2.5. Hệ thống Booster & Combo Resolver

#### A. 5 Loại Booster cơ bản
1. **Horizontal Rocket (301)**: Nổ quét sạch toàn bộ hàng ngang $Y$.
2. **Vertical Rocket (302)**: Nổ quét sạch toàn bộ cột dọc $X$.
3. **TNT (303)**: Kích nổ diện rộng vùng $3 \times 3$ xung quanh tâm.
4. **Missile (304)**: Tên lửa mini tự động tìm mục tiêu: ưu tiên các vật cản (Overlay, Under) hoặc phá một gem ngẫu nhiên.
5. **LightBall (305)**: Quả cầu sáng; khi hoán đổi với một viên ngọc màu nào, sẽ kích nổ quét sạch tất cả các viên ngọc cùng màu đó trên toàn bàn cờ.

#### B. Kích hoạt Booster (`BoosterActivationManager`)
- Xử lý kích hoạt khi người chơi click/tap trực tiếp hoặc hoán đổi với ngọc khác.
- Kích nổ hạt particle tương ứng, xóa item khỏi `MidGrid`, trả về `Pooltem` và cập nhật lại trạng thái ô cờ về `Empty`.

#### C. Hệ thống Kết hợp Combo giữa 2 Booster (`BoosterComboResolver`)
Hỗ trợ tương tác đặc biệt khi hoán đổi 2 Booster vào nhau:
- **Rocket + Rocket**: Nổ dấu cộng hình chữ thập (cả hàng ngang và cột dọc tại tâm).
- **Rocket + TNT**: Nổ chữ thập mở rộng quy mô lớn (quét đồng thời 3 hàng ngang và 3 cột dọc).
- **TNT + TNT**: Đại bác nổ cực lớn với bán kính vùng $5 \times 5$.
- **Missile + Missile**: Phân tách thành 3 quả tên lửa mini bay đi tiêu diệt 3 mục tiêu khác nhau.
- **Missile + Rocket / TNT**: Tên lửa mang theo đầu đạn Rocket hoặc TNT bay đến mục tiêu rồi kích hoạt hiệu ứng nổ tại đích đến.
- **LightBall + Booster**: Biến đổi tất cả các viên ngọc thường cùng màu với gem chỉ định thành Booster đó và kích nổ đồng loạt trên khắp bàn cờ.
- **LightBall + LightBall**: Kích nổ tối thượng, quét sạch toàn bộ item trên toàn bộ bàn cờ.

---

### 2.6. Chuẩn hóa Kiến trúc Tầng giữa (Mid Layer)
- **Đồng bộ khái niệm**: Chuyển đổi toàn diện từ `NormalLayer` sang `MidLayer` trên toàn hệ thống (`Board`, `LevelData`, `GetLevelFromTileMap`, `SaveManager`).
- **Lý do**: Tầng giữa trên bàn cờ Match-3 thực tế chứa cả **Ngọc thông thường (102–108)** lẫn **Booster đặt sẵn (301–305)**.
- **`BoardItemUtils.IsMidLayer`**: Cung cấp bộ hàm tiện ích kiểm tra nhanh một item có thuộc tầng Mid Layer hay không, giúp bàn cờ spawn đầy đủ cả ngọc và booster khi nạp màn chơi.

---

### 2.7. Điều khiển & Hoán đổi (HandleInput)
- Lắng nghe thao tác chạm/vuốt của người chơi.
- Vuốt vượt ngưỡng `swipeThreshold` là hoán đổi ngay lập tức qua DOTween.
- Phương thức `AnimateSwapAndCheck` sử dụng `UniTask`:
  - `await UniTask.WhenAll(tweenA.ToUniTask(), tweenB.ToUniTask())`: Chạy mượt mà hoạt ảnh hoán đổi 2 viên ngọc.
  - Phân luồng:
    - Nếu hoán đổi giữa 2 Booster: Kích hoạt `ActivateBoosterComboAsync`.
    - Nếu 1 trong 2 là Booster (như LightBall): Kích hoạt `ActivateBoosterAsync`.
    - Nếu là 2 viên ngọc thường: Kiểm tra Match thường; nếu không khớp thì Swap back vị trí cũ.
  - Khóa input người chơi trong suốt chuỗi nổ liên hoàn cho đến khi bàn cờ dừng hẳn.

---

### 2.8. Hệ thống Pool & Hiệu ứng
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

