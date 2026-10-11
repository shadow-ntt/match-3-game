# TIẾN ĐỘ DỰ ÁN & QUY TẮC PHÁT TRIỂN (MATCH-3 GAME)

## 1. CÁC QUY TẮC & LƯU Ý KHI CODE (QUAN TRỌNG)
- **Chỉ comment dòng đơn `//`**: Tuyệt đối không dùng XML doc comment `///` hay block comment `/* */`.
- **Hạn chế debug**: Không viết các log rác `Debug.Log`, `Debug.LogWarning` bừa bãi khi không cần thiết.
- **Awake lấy dữ liệu**: Dùng `Awake()` để đọc dữ liệu (Load JSON, GetComponent, gán references cơ bản).
- **Start khởi tạo**: Dùng `Start()` để khởi tạo trạng thái runtime, khởi tạo các checker/manager và vẽ bàn cờ.
- **Đã là SerializeField thì khỏi check null**: Nếu biến đã là `[SerializeField]` (được gán hoặc kéo thả trong Inspector), không check null thừa thãi.
- **Hạn chế check null nếu không cần thiết**: Giữ luồng code gọn gàng, súc tích, tránh bọc điều kiện kiểm tra null tràn lan.
- **Singleton truy cập ở class khác**: Tạo field thành viên tương ứng và khởi tạo tại `Start()` (hoặc `Awake()` nếu cần load data sớm, ví dụ `movesManager = MovesManager.Instance;`), không gọi `.Instance` rải rác trong từng hàm và không check null thừa.
- **Tuyệt đối không fallback ngầm**: Đã là `[SerializeField]` hoặc Singleton thì truy cập trực tiếp; cấm dùng `FindAnyObjectByType` fallback ngầm để tránh che giấu lỗi khi debug.
---

## 2. TIẾN ĐỘ HIỆN TẠI

### 2.1. Chuẩn hóa Hệ tọa độ & Quản lý Bàn cờ (Board & Grid)
- **Chuẩn hóa hệ tọa độ (X, Y) toàn dự án**:
  - `X`: Cột (ngang, từ trái `0` sang phải `Width - 1`).
  - `Y`: Hàng (dọc, từ dưới `0` lên trên `Height - 1`).
  - Ma trận lưu trữ chuẩn: `grid[x, y]` (size: `[Width, Height]`).
  - `Vector2Int pos`: `pos.x` là $X$, `pos.y` là $Y$. Bỏ hoàn toàn việc đảo ngược `rows - 1 - row`.
- `Board.cs`:
  - Quản lý 4 tầng Tilemap & Grid: Board, Under, Normal, Overlay với kích thước `[Width, Height]`.
  - Tự động map dữ liệu JSON gốc sang `(x, y)` khi spawn (`x = c`, `y = rows - 1 - r`).
  - Cung cấp thuộc tính `Width`, `Height`, `Cols`, `Rows`, `IsInBounds(int x, int y)`, `IsInBounds(Vector2Int)`.

### 2.2. Tiện ích (Utils)
- `BoardItemUtils.cs`: Kiểm tra ô hợp lệ, nhận diện Normal Item (Red: 102 ... Pink: 108), Under/Overlay Item, Blank (-1), CanSwap.
- `InputUtils.cs`: Hướng vuốt ánh xạ trực tiếp sang offset `(dX, dY)` chuẩn Unity (`Right: (1, 0)`, `Left: (-1, 0)`, `Up: (0, 1)`, `Down: (0, -1)`).
- `GridUtils.cs`: Chuyển đổi trực tiếp và tinh gọn qua `GridToWorld(grid, x, y)` và `WorldToGrid(grid, worldPos)`.
- `TweenExtensions.cs`: Cung cấp extension method `ToUniTask()` cho mọi đối tượng `Tween` của DOTween, hỗ trợ `await` mượt mà không phụ thuộc gói bên ngoài.

### 2.3. Điều khiển & Input (HandleInput)
- `HandleInput.cs`:
  - Lắng nghe thao tác chạm/kéo của người chơi.
  - Vuốt tức thì ngay khi vượt ngưỡng `swipeThreshold` mà không cần chờ nhả chuột.
  - Hoán đổi vị trí hiển thị bằng DOTween và cập nhật ma trận `NormalGrid[x, y]`.
  - Cờ `isMovingSwap` ngăn nhận thêm thao tác trong thời gian hoạt ảnh chạy.
  - Gọi `CheckMatchManager.Instance.CheckMatch(priorityCenter)` sau khi hoán đổi; nếu `!CheckMatch` thì tự động hoán đổi lại 2 vị trí (Swap back). Nếu thành công thì kích hoạt `ItemFallManager.Instance.OnItemFall()`.

### 2.4. Thuật toán quét Match (LineScanMatchChecker)
- `MatchType.cs`: Định nghĩa các kiểu match (`Normal`, `HorizontalRocket`, `VerticalRocket`, `TNT`, `Missile`, `LightBall`).
- `MatchData.cs`: Lưu thông tin cụm match gồm `matchID`, `MatchType`, `centerCell` (tâm sinh booster), và danh sách `Matches`.
- `LineScanMatchChecker.cs`:
  - `ScanLines`: Quét gom các đoạn thẳng liên tục $\ge 3$ cho cả hàng ngang và hàng dọc.
  - `ProcessIntersectingMatches`: Tìm giao điểm chữ T, L, + cùng màu để tạo `TNT`.
  - `ProcessIndependentMatches`: Xử lý đoạn thẳng độc lập ($\ge 5 \rightarrow$ `LightBall`, $4 \rightarrow$ `Rocket`, $3 \rightarrow$ `Normal`).
  - `ScanSquareMatches`: Quét các khối ô vuông 2x2 để tạo `Missile`.
  - Tự động gán vị trí tâm booster `centerCell` (ưu tiên `priorityCenter` nếu người chơi chủ động vuốt).

### 2.5. Hệ thống Pool & Hiệu ứng
- `Pooltem.cs`: Pool quản lý các đối tượng `IBoardItem`, tái sử dụng khi sinh và thu hồi ngọc.
- `PoolBlockBreakEffect.cs`: Pool hạt ParticleSystem cho 7 màu ngọc (hỗ trợ cả dải `1-7` và `102-108`).
- `Assets/Prefabs/Effects/Particles/`: Đã hoàn thiện 7 Prefab hiệu ứng hạt nổ cho 7 màu:
  - 1: `ParticleRed.prefab` (Đỏ)
  - 2: `ParticleBlue.prefab` (Xanh dương)
  - 3: `ParticleGreen.prefab` (Xanh lá)
  - 4: `ParticleYellow.prefab` (Vàng)
  - 5: `ParticlePurple.prefab` (Tím)
  - 6: `ParticleOrange.prefab` (Cam)
  - 7: `ParticlePink.prefab` (Hồng)
- Bổ sung ContextMenu `"Tu dong tai 7 Prefabs hat"` trong `PoolBlockBreakEffect` để tự động kéo gán 7 Prefab vào Inspector.

### 2.6. Quản lý kiểm tra Match & Nổ liên hoàn (CheckMatchManager)
- Tích hợp **UniTask (v2.5.11)** hỗ trợ DOTween async và async/await zero-allocation.
- `CheckMatchManager.cs`:
  - Khởi tạo `LineScanMatchChecker` trong `Start()`.
  - Cung cấp `HasMatches(priorityCenter)` để thăm dò match nhanh.
  - Hàm `async UniTask<bool> CheckMatch(priorityCenter)`:
    - Kiểm tra ban đầu, nếu không có match $\rightarrow$ trả về `false` ngay (Swap back).
    - Vòng lặp `while (true)`:
      1. Quét tìm tất cả cụm match `_matchChecker.FindAllMatches(currentCenter)`. Nếu hết match $\rightarrow$ `break`.
      2. Switch theo `MatchType`, kích hoạt hạt nổ và xóa gem khỏi bàn cờ.
      3. Chờ hiệu ứng hạt nổ qua `await UniTask.Delay(explosionDelay)`.
      4. Kích hoạt và đợi gem trên cao rơi xuống qua `await ItemFallManager.Instance.OnItemFallAsync()`.
      5. Xóa `currentCenter` và lặp lại quét match mới được tạo thành sau khi ngọc rơi.
    - Trả về `true` khi kết thúc toàn bộ chuỗi cascade.

### 2.7. Trạng thái ô cờ & Quản lý Rơi Item (BoardCell & ItemFallManager)
- `EnumStateBoardCell.cs`: Định nghĩa các trạng thái của ô cờ (`Empty`, `Occupied`, `Falling`).
- `BoardCell.cs`: 
  - Quản lý `State`, `IsEmpty`, tọa độ `X`, `Y`.
  - Bổ sung `IsGettingFilled`: Đánh dấu ô đang được "đặt chỗ" bởi ngọc đang trên đường rơi đến để triệt tiêu hoàn toàn race condition (tránh việc 2 ngọc cùng chọn 1 ô đích).
  - Tự động reset `IsGettingFilled = false` trong `OnSpawn`, `OnDespawn`, và khi tween hoàn tất.
- `ItemFallManager.cs` (Kiến trúc Rơi Thẳng & Rơi Chéo hoàn chỉnh):
  - **Duyệt đáy lên trên (`y = 1 → height - 1`)**: Xét từ đáy lên để các ô bên dưới được lấp trước, giúp các ô bên trên rơi đồng loạt.
  - **Phase 1 - Rơi Thẳng (`FindStraightTarget`)**: Ưu tiên 1, tìm ô đích thấp nhất trong cùng cột `x`. Cập nhật `NormalGrid` ngay lập tức và di chuyển bằng `DOMove`.
  - **Phase 2 - Rơi Chéo (`FindDiagonalTarget`)**: Kích hoạt khi không thể rơi thẳng. Kiểm tra ô trống lân cận `(x ± 1, y - 1)`. Chọn ngẫu nhiên 50/50 hướng Trái/Phải để triệt tiêu thiên vị hướng. Rơi tiếp xuống đáy `destY` nếu bên dưới còn trống.
  - **Animation Đa Điểm (`DOPath`)**: Sử dụng Linear Path với các waypoint (điểm vào chéo `entryPos` → điểm đáy `finalPos`) giúp đường cong rơi mượt mà, tự nhiên.
  - Vòng lặp ổn định UniTask giải quyết rơi liên hoàn cho đến khi không còn viên nào rơi được.

### 2.8. Sinh ngọc mới từ trên trời (SpawnItemFromSky)
- `SpawnItemFromSky.cs` (`Singleton<SpawnItemFromSky>`):
  - **Cơ chế Spawn Thẳng (`CanSpawnStraight`)**: Tìm đường thông suốt từ đỉnh cột `topY` xuống `targetY` không bị cản bởi vật cản, lỗ hổng hay gem tĩnh.
  - **Cơ chế Spawn Chéo từ cột lân cận (`CanSpawnDiagonal` & `FindSpawnPath`)**: Nếu cột thẳng bị chặn (do vật cản hoặc mép bàn cờ lẹm), tự động tìm cột lân cận `srcX = targetX ± 1` (random 50/50 Trái/Phải) để sinh ngọc trên trời cột lân cận rồi rơi thẳng xuống `targetY + 1` trước khi trượt chéo vào ô đích `(targetX, targetY)`.
  - **Animation DOPath Waypoints**: Di chuyển ngọc từ vị trí xếp hàng trên trời `spawnPos` → điểm rẽ chéo `entryPos` → đích `targetPos`.
  - Phân bổ `spawnOffset` độc lập cho từng cột `srcX` để các viên ngọc xếp hàng trên không không bao giờ đè lên nhau.
  - Cập nhật ma trận `NormalGrid` và đặt cờ `IsGettingFilled` tức thì trước khi khởi chạy tween để chống trùng lặp.
  - Tích hợp nhịp nhàng với vòng cascade của `CheckMatchManager`.

### 2.9. Sinh Booster khi có Match đặc biệt (Booster Spawning)
- `EnumItemBoard.cs`: Bổ sung các định danh Booster (`HorizontalRocket: 301`, `VerticalRocket: 302`, `TNT: 303`, `Missile: 304`, `LightBall: 305`).
- `BoardItemUtils.cs`:
  - Bổ sung `IsBoosterItem(...)` để nhận diện các item trong dải `301-305`.
  - Cập nhật `CanSwap(...)` cho phép hoán đổi cả gem thường lẫn booster.
- `LineScanMatchChecker.cs`: Thêm guard `IsBoosterItem` trong `IsCellMatchable` để đảm bảo booster không bị quét nhầm thành match ngọc thông thường.
- `BoosterItem.cs`: Class cơ sở kế thừa `NormalLayerItem` dành cho các booster trên bàn cờ.
- `ItemFallManager.cs`: Cập nhật `IsObstacle` để không coi `BoosterItem` là vật cản, cho phép booster rơi xuống các ô trống bên dưới như ngọc bình thường.
- `CheckMatchManager.cs`:
  - Phân tách `ExplodeMatches` thành `HandleNormalMatch` và `HandleSpecialMatch`.
  - Trong `HandleSpecialMatch`: Phá hủy và phát hiệu ứng hạt nổ cho các gem trong cụm match, giải phóng ô, và tự động gọi `Pooltem.Instance.SpawnBoardItem(...)` để sinh booster tương ứng tại tọa độ `centerCell`.

---

### 2.10. Kích hoạt Booster khi vuốt (Booster Activation)
- `IBoosterActivatable.cs`: Interface định nghĩa phương thức `GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)`.
- `BoosterItem.cs`: Implement `IBoosterActivatable` làm lớp cơ sở với phương thức `GetAffectedCells` dạng `virtual`.
- 5 Class con kế thừa `BoosterItem` và implement cơ chế nổ riêng biệt:
  - `HorizontalRocketItem.cs`: Quét và xóa toàn bộ hàng ngang `Y` của ô booster.
  - `VerticalRocketItem.cs`: Quét và xóa toàn bộ cột dọc `X` của ô booster.
  - `TNTItem.cs`: Quét và xóa vùng nổ diện rộng `3×3` xung quanh tâm booster.
  - `MissileItem.cs`: Tìm vật cản (trên `OverlayGrid`, `UnderGrid`, hoặc Normal grid) để phá hủy; nếu không có thì chọn ngẫu nhiên 1 gem trên bàn cờ.
  - `LightBallItem.cs`: Quét và xóa toàn bộ gem cùng màu với viên ngọc bị hoán đổi (`swapTarget`).
- `BoosterActivationManager.cs` (`Singleton<BoosterActivationManager>`):
  - Nhận diện booster và thu thập danh sách ô bị ảnh hưởng qua `IBoosterActivatable`.
  - Phá hủy viên booster trước, sau đó phát particle nổ `PoolBlockBreakEffect` cho các ô bị ảnh hưởng và thu hồi về `Pooltem`.
  - Đặt lại trạng thái `BoardCell.State = Empty` và `IsGettingFilled = false` cho các ô bị xóa.
- `HandleInput.cs`:
  - Bổ sung tham chiếu `BoosterActivationManager`.
  - Cập nhật `AnimateSwapAndCheck`: Sau khi hoàn tất animation trượt đổi vị trí, kiểm tra nếu một trong hai item là booster thì kích hoạt `ActivateBoosterAsync` với `swapTarget` là gem bị vuốt cùng.
  - Sau khi booster nổ, kích hoạt chuỗi ngọc rơi (`ItemFallManager`) và sinh ngọc mới từ trời (`SpawnItemFromSky`), tiếp nối cascade tự động nếu có match mới hình thành.
  - Đảm bảo cơ chế hoán đổi ngược lại (swap back) hoạt động chính xác khi thao tác không hợp lệ.

---

### 2.11. Kết hợp 2 Booster (Booster + Booster Combo)
- `BoardItemUtils.cs`: Cập nhật `CanSwap(IBoardItem, IBoardItem)` cho phép hoán đổi 2 booster với nhau kể cả khi cùng `ItemId` (ví dụ 2 Rocket hoặc 2 quả TNT).
- `BoosterComboResolver.cs`: Module chuyên biệt tính toán danh sách ô bị ảnh hưởng (`GetComboAffectedCells`) cho tất cả các cặp kết hợp:
  - **LightBall + LightBall**: Xóa toàn bộ ngọc và item có trên bàn cờ.
  - **LightBall + Booster khác**: Tìm màu ngọc thường xuất hiện nhiều nhất trên bàn cờ (`PickDominantColor`), bắn tia sáng đổi toàn bộ ngọc màu đó thành booster cùng loại với booster đang kết hợp (kèm animation pop nảy), sau đó kích hoạt nổ dây chuyền tất cả các booster vừa được tạo.
  - **TNT + TNT**: Vùng nổ siêu lớn `5×5` xung quanh tâm.
  - **TNT + Rocket (Ngang/Dọc)**: Tạo hiệu ứng Mega Rocket gồm `3 hàng ngang + 3 cột dọc` bao quanh tâm vuốt.
  - **HorizontalRocket + HorizontalRocket**: Quét sạch `3 hàng ngang` liên tiếp (`Y - 1, Y, Y + 1`).
  - **VerticalRocket + VerticalRocket**: Quét sạch `3 cột dọc` liên tiếp (`X - 1, X, X + 1`).
  - **HorizontalRocket + VerticalRocket**: Tạo dấu thập `+` quét toàn bộ hàng ngang và cột dọc tại tâm.
  - **Missile + Missile**: Bắn 3 tên lửa truy tìm đồng thời 3 mục tiêu đặc biệt (vật cản / ngọc ngẫu nhiên).
  - **Missile + Rocket / TNT**: Kích hoạt hiệu ứng diện rộng tại tâm đồng thời kích hoạt thêm tại vị trí mục tiêu mà tên lửa tìm được.
- `BoosterActivationManager.cs`:
  - Thêm phương thức `ActivateBoosterComboAsync`: Thu thập danh sách ô bị ảnh hưởng từ `BoosterComboResolver`, xóa cả 2 viên booster tham gia combo, phát particle hạt nổ cho các ô bị ảnh hưởng, trả ô về `Empty` và thu hồi item về pool.
- `HandleInput.cs`:
  - Trong `AnimateSwapAndCheck`, bổ sung nhánh `if (isBoosterA && isBoosterB)` ưu tiên trước các nhánh kích hoạt booster đơn lẻ. Tự động kích hoạt chuỗi rơi ngọc và cascade tiếp nối ngay sau khi vụ nổ combo hoàn tất.

---

### 2.12. Kích hoạt dây chuyền (Chain Activation)
- `LightBallItem.cs`:
  - Bổ sung cơ chế fallback `PickRandomColor`: Nếu `LightBallItem` bị kích hoạt dây chuyền (không qua thao tác vuốt với `swapTarget`), nó sẽ tự động chọn ngẫu nhiên một màu ngọc thường hiện có trên bàn cờ để quét sạch màu đó.
- `BoosterActivationManager.cs`:
  - **Tách luồng nội bộ `ActivateBoosterInternalAsync`**:
    - Sử dụng `HashSet<Vector2Int> visited` và tham số `depth` (giới hạn tối đa 15 cấp) để ngăn chặn hoàn toàn vòng lặp vô tận (Infinite Loop) hoặc trùng lặp ô khi các booster kích hoạt lẫn nhau.
    - Nhận diện các Booster khác nằm trong vùng nổ `affectedCells` trước khi xóa ô thường, đưa vào danh sách `chainBoosters` và giữ nguyên trên bàn cờ để animation và logic nổ dây chuyền hiển thị rõ ràng, mượt mà.
    - Xóa các ô thường, chờ độ trễ `explosionDelay`, sau đó đệ quy kích hoạt lần lượt từng booster trong chuỗi.
  - **Hỗ trợ nổ dây chuyền cho cả Booster đơn lẻ và Booster Combo**: Cả `ActivateBoosterAsync` và `ActivateBoosterComboAsync` đều kích hoạt dây chuyền cho tất cả các booster nằm trong phạm vi ảnh hưởng.
  - **Cung cấp API `TriggerChainBoostersAsync(List<Vector2Int> cells)`**: Cho phép kích hoạt dây chuyền từ bất kỳ danh sách tọa độ nào từ bên ngoài nếu cần.

---

### 2.13. Hoạt ảnh Booster (Booster Animations)
- `IBoosterActivatable.cs` & `BoosterItem.cs`:
  - Bổ sung `UniTask PlayActivationAnimationAsync()` vào interface `IBoosterActivatable` và cài đặt mặc định dạng `virtual` trong `BoosterItem`.
- **Hoạt ảnh chuẩn bị (Pre-activation Animation) cho từng Booster**:
  - `HorizontalRocketItem.cs` & `VerticalRocketItem.cs`: Hoạt ảnh flash scale nhấp nháy phóng to thu nhỏ (`DOScale: 1.0 → 1.3 → 1.0`).
  - `TNTItem.cs`: Hoạt ảnh nhấp nháy đỏ-trắng (`DOColor: red`) kết hợp rung lắc bàn cờ (`DOShakePosition`).
  - `MissileItem.cs`: Hoạt ảnh nhấp nháy scale chuẩn bị cất cánh.
  - `LightBallItem.cs`: Hoạt ảnh pulse phóng to kết hợp xoay tròn 180° (`DOScale` + `DORotate`).
- **Hệ thống Projectiles & Pool**:
  - `RocketProjectile.cs`: Đối tượng đầu đạn tên lửa, bay lướt qua từng ô theo hàng/cột, tự động xoay góc theo hướng bay, kích hoạt callback phá hủy và phát particle nổ tại từng ô khi bay qua, sau đó vút ra ngoài mép bàn cờ.
  - `PoolRocketProjectile.cs` (`Singleton<PoolRocketProjectile>`): Pool quản lý tái sử dụng các đầu đạn tên lửa Rocket.
  - `MissileProjectile.cs`: Đối tượng tên lửa dẫn đường, bay theo đường cong uốn lượn mượt mà (`DOPath` CatmullRom), tự động xoay đầu đạn theo tiếp tuyến hướng di chuyển đến ô mục tiêu.
  - `PoolMissileProjectile.cs` (`Singleton<PoolMissileProjectile>`): Pool quản lý tái sử dụng tên lửa dẫn đường Missile.
- `BoosterActivationManager.cs`:
  - Tích hợp `await booster.PlayActivationAnimationAsync()` trước khi xóa booster khỏi bàn cờ.
  - **Điều phối hiệu ứng theo từng loại booster**:
    - **HorizontalRocket**: Bắn song song 2 đầu đạn bay sang Trái và Phải, phá hủy ô khi đầu đạn lướt qua.
    - **VerticalRocket**: Bắn song song 2 đầu đạn bay Xuống và Lên, phá hủy ô khi đầu đạn lướt qua.
    - **Missile**: Bắn tên lửa bay đường cong tới ô mục tiêu, chạm đích rồi phát nổ.
    - **LightBall**: Làn sóng nổ lan tỏa theo khoảng cách từ tâm tới các gem cùng màu.
    - **TNT**: Phát nổ diện rộng 3×3 đồng loạt.
  - **Cơ chế Fallback an toàn**: Nếu chưa gán Prefab trong Editor, hệ thống tự động fallback về cơ chế phá ô và phát particle mặc định, không gây crash hoặc gián đoạn luồng chơi.

---

### 2.14. Hoàn thiện Hoạt ảnh Booster (Phase 6 Animation Polish)
- `CameraShakeService.cs` (`Singleton<CameraShakeService>`):
  - Dịch vụ quản lý rung màn hình Camera sử dụng DOTween (`DOShakePosition`) và UniTask.
  - Tự động lưu và khôi phục vị trí gốc `originalLocalPos` sau mỗi lần shake để tránh tình trạng trôi camera (camera drift).
  - Cung cấp preset:
    - `ShakeTNT()`: Rung vừa phải (duration: 0.25s, strength: 0.15) cho TNT đơn lẻ hoặc combo TNT thường.
    - `ShakeMega()`: Rung dữ dội (duration: 0.4s, strength: 0.3) cho combo TNT + TNT hoặc Mega Combo.
    - Được gọi bất đồng bộ fire-and-forget (`.Forget()`) không chặn gameplay.
- **Hệ thống Tia sáng LightBall Beam**:
  - `LightBallBeam.cs`: Quản lý 1 tia sáng LineRenderer kết nối từ LightBall đến từng viên ngọc mục tiêu trước khi nổ. Tự động fade alpha mượt mà bằng DOTween (`DOTween.To`) và tự thu hồi sau thời gian quy định.
  - `PoolLightBallBeam.cs` (`Singleton<PoolLightBallBeam>`): Quản lý object pool cho các tia sáng LightBallBeam, khởi tạo sẵn và tái sử dụng.
  - Tích hợp vào `BoosterActivationManager.PlayLightBallEffectAsync`:
    - Tự động nhận diện màu của ngọc mục tiêu để đồng bộ màu sắc tia sáng (Red, Blue, Green, Yellow, Purple, Orange, Pink).
    - Bắn đồng loạt các tia sáng từ LightBall tới tất cả các viên ngọc đích (`UniTask.WhenAll`), sau đó kích nổ đồng thời.
    - Fallback an toàn: Nếu chưa cấu hình prefab beam trong Editor, hệ thống tự động fallback về cơ chế nổ lan tỏa theo khoảng cách cũ.
- **Hoạt ảnh Hợp nhất Combo Booster (Combo Merge Animation)**:
  - Tích hợp `PlayComboMergeAnimationAsync` trong `BoosterActivationManager` được kích hoạt ngay trước khi giải phóng 2 booster tham gia combo:
    - **Rocket + Rocket**: Hai quả tên lửa thu nhỏ (`DOScale: 0.5`), lao vào nhau tại tâm (`DOMove`) và flash trắng mở rộng (`DOScale: 1.8`).
    - **TNT + TNT / TNT + Rocket**: Cả 2 nhấp nháy đỏ rực (`DOColor: red`) kết hợp rung lắc (`DOShakePosition`) và phồng to phát nổ.
    - **LightBall + Booster khác**: LightBall phóng to xoay tròn 360°, hút booster đối tác thu nhỏ dần vào tâm trước khi bùng nổ.
    - **Missile + Missile**: Cả 2 tên lửa xoay 360° ngược chiều nhau (`DORotate`) và nhấp nháy phóng to thu nhỏ.
    - **Missile + Rocket / TNT**: Missile vọt lên cao (`DOMove Y+1.5`) rồi lao thẳng xuống tâm va chạm với booster còn lại.
    - **Default Combo**: Hiệu ứng pulse đồng thời cho 2 booster.
  - Kích hoạt rung camera tương ứng nếu combo có chứa TNT (`ShakeMega` cho TNT+TNT, `ShakeTNT` cho TNT+khác).

---

### 2.15. Hoạt ảnh Kích hoạt Combo Booster (Phase 7 Combo Activation Animations)
- **Hệ thống Helper Bắn & Nổ Tái sử dụng**:
  - `PlayRocketRowAsync`: Bắn 1 cặp đầu đạn Rocket bay sang Trái và Phải trên 1 hàng cụ thể, tự động phá hủy ô xuất phát và quét sạch các ô trên hàng đó.
  - `PlayRocketColumnAsync`: Bắn 1 cặp đầu đạn Rocket bay Xuống và Lên trên 1 cột cụ thể, tự động phá hủy ô xuất phát và quét sạch các ô trên cột đó.
  - `PlayWaveExplosionAsync`: Tạo sóng nổ mở rộng theo từng vòng (ring) từ tâm ra ngoài bán kính `radius` với delay `0.05s` giữa các vòng, tự động bao quát cả các ô xa ở rìa.
  - `PlayLightBallBeamsAsync`: Bắn đồng loạt các tia sáng LineRenderer từ điểm phát tới danh sách ngọc mục tiêu trước khi kích nổ.
  - `FlySingleMissileAsync`: Bắn 1 tên lửa dẫn đường theo đường cong uốn lượn tới mục tiêu và phá hủy ô đó khi chạm đích.
- **Tái cấu trúc (Refactor) các luồng Booster Đơn lẻ**:
  - `PlayHorizontalRocketEffectAsync`: Tái sử dụng `PlayRocketRowAsync`.
  - `PlayVerticalRocketEffectAsync`: Tái sử dụng `PlayRocketColumnAsync`.
  - `PlayLightBallEffectAsync`: Tái sử dụng `PlayLightBallBeamsAsync`.
  - TNT đơn lẻ trong `ActivateBoosterInternalAsync`: Tái sử dụng `PlayWaveExplosionAsync`.
- **Hoạt ảnh Phá hủy Riêng biệt cho Từng loại Combo (`PlayComboActivationEffectAsync`)**:
  - **Nhóm LightBall**:
    - **LightBall + LightBall**: Rung camera `ShakeMega`, bắn tia sáng toàn màn hình tới tất cả các ô trên bàn cờ và kích nổ đồng loạt.
    - **LightBall + HorizontalRocket**: Bắn tia sáng tới tất cả các viên ngọc cùng màu ngẫu nhiên song song với bắn cặp Rocket quét sạch hàng ngang tại tâm (`UniTask.WhenAll`).
    - **LightBall + VerticalRocket**: Bắn tia sáng tới tất cả các viên ngọc cùng màu ngẫu nhiên song song với bắn cặp Rocket quét sạch cột dọc tại tâm.
    - **LightBall + TNT**: Rung camera `ShakeTNT`, bắn tia sáng tới tất cả các viên ngọc cùng màu ngẫu nhiên song song với sóng nổ 3×3 tại tâm.
    - **LightBall + Missile**: Tự động nhận diện màu ngọc phổ biến nhất và phân tách mục tiêu, bắn tia sáng tới các viên ngọc cùng màu song song với phóng 3 tên lửa dẫn đường truy tìm 3 mục tiêu đặc biệt.
  - **Nhóm Missile**:
    - **Missile + Missile**: Phóng 3 tên lửa dẫn đường đồng thời bay tới 3 mục tiêu độc lập.
    - **Missile + TNT**: Nổ sóng 3×3 tại tâm kèm rung camera → phóng Missile bay tới vị trí mục tiêu → tiếp tục nổ sóng 3×3 tại mục tiêu kèm rung camera.
    - **Missile + HorizontalRocket**: Bắn cặp Rocket nổ hàng ngang tại tâm → phóng Missile bay tới hàng mục tiêu → bắn cặp Rocket nổ toàn bộ hàng ngang tại mục tiêu.
    - **Missile + VerticalRocket**: Bắn cặp Rocket nổ cột dọc tại tâm → phóng Missile bay tới cột mục tiêu → bắn cặp Rocket nổ toàn bộ cột dọc tại mục tiêu.
  - **Nhóm TNT**:
    - **TNT + TNT**: Rung camera `ShakeMega`, kích hoạt sóng nổ 5×5 cực mạnh lan tỏa từ tâm ra ngoài (radius 2).
    - **Rocket + TNT (Mega Rocket)**: Rung camera `ShakeTNT`, bắn đồng loạt 3 cặp Rocket ngang (3 hàng liên tiếp) và 3 cặp Rocket dọc (3 cột liên tiếp) tỏa ra ngoài từ tâm.
  - **Nhóm Rocket + Rocket**:
    - **HorizontalRocket + HorizontalRocket**: Bắn đồng loạt 3 cặp Rocket ngang quét sạch 3 hàng liên tiếp (`cy-1, cy, cy+1`).
    - **VerticalRocket + VerticalRocket**: Bắn đồng loạt 3 cặp Rocket dọc quét sạch 3 cột liên tiếp (`cx-1, cx, cx+1`).
    - **HorizontalRocket + VerticalRocket**: Bắn đồng thời 1 cặp Rocket ngang và 1 cặp Rocket dọc tạo hình chữ thập `+` quét sạch cả hàng và cột tại tâm.
- **Cơ chế Fallback & An toàn**:
  - Toàn bộ helper và sub-method đều có fallback phá hủy ô trực tiếp nếu pool chưa gán Prefab trong Editor, không gây lỗi logic hay đứng game.
  - Vòng quét dọn cuối cùng bảo đảm không bao giờ bỏ sót bất kỳ ô nào trong `affectedCells`.
  - Giới hạn thời gian hoạt ảnh mỗi combo ≤ 0.5s, duy trì nhịp độ gameplay nhanh và cuốn hút.

---

### 2.16. Xóa Item Đa Tầng (OverlayGrid, UnderGrid & MidGrid)
- **Chuẩn hóa thao tác xóa item trên Board**:
  - `Board.cs`: Bổ sung `RemoveMidItem(x, y)`, `RemoveOverlayItem(x, y)` và `RemoveUnderItem(x, y)`:
    - Thu hồi GameObject về `Pooltem`.
    - Gán `null` cho ô tương ứng trên ma trận grid (`_midGrid`, `_overlayGrid`, `_underGrid`).
    - Cập nhật trạng thái `BoardCell.State = Empty` và `IsGettingFilled = false` khi ô không còn chứa bất kỳ item nào, cho phép ngọc trên cao rơi xuống hoặc sinh mới từ trời.
- **Tích hợp vào hệ thống nổ & kích hoạt Booster**:
  - `BoosterActivationManager.cs`:
    - Bổ sung `RemoveOverlayItem(x, y)` và `RemoveUnderItem(x, y)`.
    - Tối ưu hóa `ExplodeAndRemoveCell(cx, cy)`:
      1. Ưu tiên 1: Nếu ô có vật cản trên `OverlayGrid` (đá, băng tuyet, dây xích...) $\rightarrow$ phát hiệu ứng hạt nổ và xóa `OverlayItem`.
      2. Ưu tiên 2: Nếu ô có vật cản trên `UnderGrid` (không phải Spawner) $\rightarrow$ phát hiệu ứng hạt nổ và xóa `UnderItem`.
      3. Ưu tiên 3: Nếu ô không còn vật cản tầng trên/dưới $\rightarrow$ phát hiệu ứng hạt nổ và xóa ngọc/booster trên `MidGrid`.
    - Đồng bộ `PlayComboLightBallAsync` (LightBall + LightBall) quét sạch toàn bộ cả `OverlayGrid` và `MidGrid`.
- **Hỗ trợ Booster nhận diện và phá hủy vật cản đa tầng**:
  - `MissileItem.cs`:
    - `FindSpecialTarget`: Tìm vật cản gần nhất trên `OverlayGrid` (hoặc `UnderGrid`, hoặc vật cản `MidGrid`), không nhắm vào điểm sinh `Spawn` (Spawner).
    - Phá hủy thành công vật cản `OverlayGrid` (như Đá 200) và giải phóng ô cờ.
  - `HorizontalRocketItem.cs`, `VerticalRocketItem.cs`, `TNTItem.cs`:
    - Cập nhật `GetAffectedCells` nhận diện cả các ô chỉ có vật cản trên `OverlayGrid` để quét sạch triệt để.
  - `BoosterComboResolver.cs`:
    - Bổ sung kiểm tra `OverlayGrid` trong `AddAllBoardItems`.
    - Loại trừ Spawner khi tìm kiếm mục tiêu đặc biệt trên `UnderGrid`.

---

### 2.17. Hệ thống Dữ liệu Mục tiêu & Lượt đi (Phase 1 Data Layer)
- `GoalEntry.cs`: Model lưu trữ từng mục tiêu thu thập hoặc phá hủy (`itemId`, `amount`) tuần tự hóa qua Odin Serializer.
- `LevelData.cs`:
  - Bổ sung `movesLimit` (mặc định 25), `targetScore` (mặc định 0), `goals` (`List<GoalEntry>`).
  - Cung cấp constructor tương thích ngược và constructor mở rộng.
  - Cơ chế fallback an toàn: Tự động khởi tạo danh sách rỗng và fallback `movesLimit` khi tải các level JSON cũ, triệt tiêu `NullReferenceException`.
- `GetLevelFromTileMap.cs`:
  - Thêm trường `movesLimit`, `targetScore`, `levelGoals` cấu hình trực tiếp từ Inspector của `BoardGrid`.
  - Đóng gói dữ liệu mục tiêu và lượt đi vào `LevelData` khi trích xuất và lưu level thành JSON.
- `SaveManager.cs`:
  - Cập nhật hàm test `TestSaveDemoLevel1` và `TestLoadDemoLevel1` hỗ trợ ghi và đọc đầy đủ `movesLimit`, `targetScore`, `goals`.

---

### 2.18. Hệ thống Quản lý Logic Nghiệp vụ (Phase 2 Core Managers)
- `GameEventBus.cs`:
  - Static Event Bus làm trung tâm kết nối toàn bộ hệ thống (`OnScoreChanged`, `OnMovesChanged`, `OnGoalUpdated`, `OnGoalCompleted`, `OnLevelComplete`, `OnLevelFailed`).
  - Hỗ trợ `ClearAllEvents()` dọn dẹp bộ nhớ khi chuyển đổi scene.
- `ScoreManager.cs` (`Singleton<ScoreManager>`):
  - Quản lý điểm số người chơi: `baseGemScore = 20`, `obstacleScore = 50`, `boosterScore = 40`.
  - Tính toán combo cascade multiplier: Tăng `+0.5` mỗi vòng cascade nổ liên hoàn, tối đa `3.0x`.
  - Tự động reset combo khi bắt đầu lượt vuốt mới và tăng khi ngọc rơi tạo match mới trong `CheckMatchManager`.
- `MovesManager.cs` (`Singleton<MovesManager>`):
  - Quản lý số lượt di chuyển còn lại `movesLeft` / `movesTotal`.
  - Trừ 1 lượt khi người chơi hoán đổi hợp lệ (`ConsumeMove`) từ `HandleInput`.
  - Hỗ trợ cờ `isLevelEnded` và `AddBonusMoves`.
- `GoalTracker.cs` (`Singleton<GoalTracker>`):
  - Theo dõi tiến độ từng mục tiêu trong màn chơi (`GoalProgress`: `itemId`, `current`, `required`).
  - Lắng nghe ghi nhận item bị phá hủy qua `RegisterDestroyed(itemId, count)` từ cả `CheckMatchManager` và `BoosterActivationManager`.
  - Tự động kích hoạt `GameEventBus.RaiseLevelComplete()` khi toàn bộ mục tiêu hoàn thành.
- `GameFlowController.cs` (`Singleton<GameFlowController>`):
  - Nhận dữ liệu `LevelData` từ `Board.cs` và khởi tạo đồng bộ `MovesManager`, `GoalTracker`, `ScoreManager`.
  - Lắng nghe `OnLevelComplete` và `OnLevelFailed` để vô hiệu hóa input (`HandleInput.EnableInput = false`) và khóa số lượt đi.
- **Tích hợp sâu vào Game Loop**:
  - `CheckMatchManager.cs`: Ghi nhận mục tiêu và cộng điểm khi xóa ngọc thường hoặc special match; quản lý cấp độ combo cascade.
  - `BoosterActivationManager.cs`: Ghi nhận mục tiêu và cộng điểm khi kích hoạt booster đơn lẻ, combo booster hoặc phá hủy các ô đa tầng (`OverlayGrid`, `UnderGrid`, `MidGrid`).
  - `HandleInput.cs`: Trừ 1 lượt đi khi hoán đổi tạo match hoặc kích hoạt booster; kiểm tra điều kiện thua nếu hết lượt mà chưa đạt mục tiêu; chặn thao tác khi game kết thúc hoặc hết lượt.
  - `Board.cs`: Loại bỏ toàn bộ fallback ngầm `FindAnyObjectByType` và cơ chế khởi tạo chéo; giao toàn quyền khởi tạo Manager cho `GameFlowController`.
- **Chuẩn hóa Kiến trúc & Refactor Toàn diện (Tuân thủ Quy tắc Phát triển)**:
  - Tất cả các Singleton khi được truy cập ở các class khác đều được tạo dưới dạng **Field thành viên** và khởi tạo tập trung tại `Start()` (như `movesManager`, `goalTracker`, `scoreManager`, `pooltem`, `checkMatchManager`, `boosterActivationManager`...).
  - Xóa bỏ 100% các đoạn code fallback ngầm `FindAnyObjectByType` và các kiểm tra `null` thừa thãi đối với các biến `[SerializeField]` trong toàn bộ codebase (`GameFlowController`, `Board`, `HandleInput`, `CheckMatchManager`, `BoosterActivationManager`, `SpawnItemFromSky`, `ItemFallManager`), đảm bảo lỗi được phát hiện tức thì khi debug thay vì fallback âm thầm.

---

---

### 2.19. Hệ thống Giao diện Người dùng (Phase 3 UI Layer)
- `ScoreView.cs`:
  - Lắng nghe `GameEventBus.OnScoreChanged`.
  - Hiệu ứng đếm số tăng dần mượt mà qua DOTween (`DOVirtual.Int`).
  - Hiệu ứng nảy scale nhẹ (`DOPunchScale`) mỗi khi cộng điểm.
- `MovesView.cs`:
  - Lắng nghe `GameEventBus.OnMovesChanged`.
  - Hiển thị số lượt di chuyển còn lại.
  - Tự động đổi màu đỏ (`warningColor`) và rung nảy cảnh báo khi `movesLeft <= warningThreshold` (mặc định 5 lượt).
  - Rung lắc nhẹ (`DOShakePosition`) khi về 0 lượt.
- `GoalItemView.cs`:
  - Đại diện cho từng mục tiêu đơn lẻ trên UI: hiển thị icon `Sprite`, số lượng còn lại (`required - current`).
  - Hiệu ứng scale punch khi số lượng giảm.
  - Khi hoàn thành mục tiêu: ẩn số lượng, hiển thị checkmark xanh với hoạt ảnh phóng to xoay nảy (`DOScale` OutBack).
- `GoalView.cs`:
  - Lắng nghe `GameEventBus.OnGoalUpdated`.
  - Quản lý danh sách các `GoalItemView` trong container.
  - Hỗ trợ ánh xạ `GoalIconConfig` (`itemId` -> `Sprite`) cấu hình trực tiếp từ Inspector.
  - Tự động sinh `GoalItemView` tương ứng với các mục tiêu trong `LevelData`.
- `LevelResultPopup.cs`:
  - Lắng nghe `GameEventBus.OnLevelComplete` và `GameEventBus.OnLevelFailed`.
  - Hiển thị Popup Chiến Thắng (kèm tổng điểm đạt được, nút Tiếp Tục / Next Level) hoặc Popup Thất Bại (kèm nút Thử Lại / Retry).
  - Tích hợp trực tiếp với `GameFlowController` để chuyển hoặc load lại level.

---

### 2.20. Hệ thống Âm thanh Tách biệt (Audio System - Phase 1 Core Infrastructure)
- **Kiến trúc Kết hợp Event-Driven (GameEventBus) & ScriptableObjects (AudioEventSO)**:
  - Tách rời 100% giữa Gameplay và Audio: Gameplay chỉ raise event thuần túy qua `GameEventBus`, không chứa bất kỳ reference hay code Audio nào.
  - Toàn bộ tham số âm thanh (clips, volume, pitch variation, mixer group, cooldown) được đóng gói dạng Data-Driven trong ScriptableObject.
- `GameEventBus.cs`:
  - Mở rộng các sự kiện gameplay: `OnItemSwapped`, `OnMatchExploded`, `OnBoosterCreated`, `OnBoosterActivated`, `OnBoosterComboActivated`, `OnObstacleDestroyed`, `OnUIButtonClicked`.
  - Cập nhật hàm `ClearAllEvents()` dọn dẹp toàn bộ subscriber mới.
- `AudioEventSO.cs`:
  - ScriptableObject quản lý mảng `AudioClip[]`, volume, `Vector2 pitchRange` (ngẫu nhiên hóa cao độ), `AudioMixerGroup` và `cooldown` chống clipping khi nhiều ngọc cùng nổ.
  - Cung cấp hàm `Play(AudioSource, pitchMultiplier)` và `PlayLoop(AudioSource)`.
- `AudioConfigSO.cs`:
  - ScriptableObject trung tâm gom 23 sự kiện âm thanh (BGM, Swap, Match, Boosters, Combos, Obstacles, UI).
- `AudioManager.cs` (`SingletonDontDestroy<AudioManager>`):
  - Cung cấp `bgmSource`, `sfxPrefab`, `sfxParent`, `sfxPoolSize` dạng `[SerializeField]` từ Inspector và tự động Instantiate pool trong `Awake()`.
  - Tự động đăng ký lắng nghe toàn bộ event từ `GameEventBus` trong `OnEnable`/`OnDisable`.
  - Hỗ trợ **Dynamic Combo Pitch Escalation**: tăng cao độ pitch theo nhịp combo `cascadeStep` tạo hiệu ứng thỏa mãn cho người chơi.
- `AudioSetupTool.cs`:
  - Công cụ Editor (`Tools/Audio/Tu Dong Tao Audio Assets & Config`) tự động quét clips và tạo toàn bộ 23 `.asset` AudioEventSO cùng file `AudioConfig.asset` liên kết sẵn AudioMixer.
  - Bổ sung menu (`Tools/Audio/Tao AudioManager GameObject Trong Scene`) tự động sinh GameObject AudioManager, tạo Prefab `SFX_Source`, wire sẵn `sfxPrefab` và `sfxParent` vào Inspector.

---

### 2.21. Tích hợp Sự kiện Âm thanh vào Gameplay & UI (Audio System - Phase 2 Audio Integration)
- **Hoàn tất tích hợp Event-Driven (Zero Coupling với Audio)**:
  - `HandleInput.cs`:
    - Bắn `GameEventBus.RaiseItemSwapped(false)` khi bắt đầu tween hoán đổi ngọc.
    - Bắn `GameEventBus.RaiseItemSwapped(true)` khi hoán đổi thất bại và tween hoàn lại vị trí cũ (Revert).
  - `CheckMatchManager.cs`:
    - Truyền `cascadeStep` và bắn `GameEventBus.RaiseMatchExploded(count, cascadeStep)` trong `HandleNormalMatch`, giúp pitch âm thanh tăng dần theo nhịp combo liên hoàn.
    - Bắn `GameEventBus.RaiseBoosterCreated(boosterId)` trong `HandleSpecialMatch` khi booster đặc biệt được sinh trên bàn cờ.
  - `BoosterActivationManager.cs`:
    - Bắn `GameEventBus.RaiseBoosterActivated(bItem.ItemId)` khi booster đơn lẻ được kích hoạt.
    - Bắn `GameEventBus.RaiseBoosterComboActivated(boosterA.ItemId, boosterB.ItemId)` khi kích hoạt combo giữa 2 booster.
    - Bắn `GameEventBus.RaiseObstacleDestroyed(obstacleId)` khi phá vỡ chướng ngại vật (Stone/Ground) trên `OverlayGrid` và `UnderGrid`.
  - `LevelResultPopup.cs`:
    - Bắn `GameEventBus.RaiseUIButtonClicked()` khi người chơi click nút Next Level hoặc Retry.
  - `UIButtonAudio.cs`:
    - Component tiện ích tự động bắt `Button.onClick` và bắn `GameEventBus.RaiseUIButtonClicked()` cho bất kỳ Button UI nào trong game.

---

### 2.22. Quản lý Tiến độ Người chơi & Khởi tạo Start Scene (SaveManager Static & Scene Flow)
- **Chuẩn hóa SaveManager thành Static Class**:
  - Chuyển đổi `SaveManager` từ `SingletonDontDestroy<SaveManager>` thành `public static class SaveManager`.
  - Loại bỏ hoàn toàn sự phụ thuộc vào GameObject trong Scene và Singleton runtime; cho phép gọi trực tiếp `SaveManager.LoadLevel(...)`, `SaveManager.SaveLevel(...)` ở cả Runtime lẫn Editor Tools.
  - Bổ sung các `[MenuItem]` trong Editor: `Tools > Save > Reset Player Data (Level 1)`, `Tools > Save > Test Save Demo Level 1`, `Tools > Save > Test Load Demo Level 1`.
- **Hệ thống Lưu trữ Tiến độ Người chơi bằng JSON (PlayerData)**:
  - `PlayerData.cs` (`Assets/Scripts/Core/Data/PlayerData.cs`): Data model lưu trữ `CurrentLevel` và `HighestUnlockedLevel` tuần tự hóa bằng Odin Serializer.
  - `SaveManager.LoadPlayerData()` & `SaveManager.SavePlayerData(PlayerData)`: Đọc và ghi trực tiếp vào `Assets/Data/player_data.json`. Tự động khởi tạo `new PlayerData()` nếu chưa có file.
- **Tích hợp Luồng Gameplay & Lưu Tiến độ**:
  - `Board.cs`: Loại bỏ tham chiếu Singleton `SaveManager`, chuyển sang gọi trực tiếp `SaveManager.LoadLevel(levelNumber)`; tự động nạp `levelNumber = playerData.CurrentLevel` trong `Awake()`.
  - `GameFlowController.cs`: Nạp `levelToLoad` từ `SaveManager.LoadPlayerData().CurrentLevel` trong `Awake()`; gọi `board.DrawBoard()` khi chuyển level mới.
  - `LevelResultPopup.cs`: Tự động tăng `CurrentLevel++` và lưu vào `player_data.json` ngay khi người chơi chiến thắng trong `ShowWin()`.
- **Giao diện Start Scene & Điều phối Luồng**:
  - `MainMenuController.cs` (`Assets/Scripts/UI/MainMenuController.cs`):
    - Đọc `CurrentLevel` trong `Awake()` và cập nhật text nút: `$"Play level {playerData.CurrentLevel}"`.
    - Phát âm thanh click nút qua `GameEventBus.RaiseUIButtonClicked()`.
    - Hiệu ứng nút nảy nhẹ qua DOTween trước khi chuyển sang `MainScene`.
    - Tự động gọi `GameEventBus.ClearAllEvents()` và `DOTween.KillAll()` trước khi load scene.
  - `StartSceneSetupTool.cs` (`Assets/Scripts/UI/Editor/StartSceneSetupTool.cs`):
    - Công cụ 1-click tại menu `Tools > Scene > Tu Dong Tao StartScene & Build Settings`.
    - Tự động tạo `StartScene.unity` với Camera, EventSystem, Canvas 1920x1080, Background, Title Text, Play Button, `UIButtonAudio`, `MainMenuController` đã wire đầy đủ references.
    - Tự động cấu hình danh sách `EditorBuildSettings.scenes` (Index 0: `StartScene`, Index 1: `MainScene`).

---

### 2.23. Hệ thống Thanh Cài Đặt Bung Ngang (Settings Expand Bar - Phase 1 State-Driven & Animations)
- **Lưu trữ Trạng thái Âm thanh (PlayerData Persistence)**:
  - `PlayerData.cs`: Bổ sung `isBgmOn` và `isSfxOn` (mặc định `true`), tự động lưu/đọc qua Odin Serializer vào `player_data.json`.
- **Kiến trúc State-Driven Tường minh (Turn On / Turn Off)**:
  - `AudioManager.cs`:
    - Thay thế cơ chế Toggle mù bằng API tường minh: `TurnOnBgm()`, `TurnOffBgm()`, `SetBgm(bool)`, `TurnOnSfx()`, `TurnOffSfx()`, `SetSfx(bool)`.
    - Thuộc tính `IsBgmOn` và `IsSfxOn` cùng static event `OnBgmStateChanged`, `OnSfxStateChanged`.
    - Đọc trạng thái từ `PlayerData` trong `Awake()` và tự động lưu ngược lại mỗi khi thay đổi.
    - Điều khiển âm lượng tập trung qua **AudioMixer** (`BgmVolume` và `SfxVolume`: On = `0 dB`, Off = `-80 dB`), ngắt tiếng chuẩn xác trên toàn bộ Bus âm thanh.
- **Thành phần Điều khiển UI & Hoạt ảnh Bung Ngang**:
  - `AudioOnOffButton.cs`:
    - Component gắn trên nút BGM/SFX quản lý trạng thái `TurnOn()` / `TurnOff()`.
    - Tự động đồng bộ với `AudioManager` qua Event; cập nhật màu sắc và hiển thị lớp phủ gạch chéo (`Cross_Icon.png`).
    - Hoạt ảnh nảy nhẹ `DOPunchScale` khi click.
  - `ExpandableSettingsBar.cs`:
    - Quản lý thanh cài đặt ở góc dưới bên phải màn hình.
    - Khi bấm mở (`Open`): Nút chính xoay icon 180° (`DORotate`), 3 nút con (BGM, SFX, Exit) trượt ngang từ phải sang trái (`DOAnchorPosX`) cùng hàng Y với độ trễ so le (stagger), phóng to (`DOScale` OutBack) và hiện rõ `DOFade`.
    - Khi bấm đóng (`Close`): 3 nút con trượt ngược từ trái về phải thu về vị trí nút chính (`Ease.InBack`), nút chính xoay về 0°.
    - Cờ `isAnimating` chống click spam làm lỗi animation state.
  - `SettingsMenuSetupTool.cs` (`Assets/Scripts/UI/Editor/SettingsMenuSetupTool.cs`):
    - Công cụ Editor 1-click tại menu `Tools > UI > Tao Settings Expand Bar Trong Scene`: Tự động tìm Canvas, tạo toàn bộ GameObject, nạp sprite và wire references hoàn chỉnh.
    - Tích hợp trực tiếp vào `StartSceneSetupTool.cs` để tự động đính kèm vào `StartScene.unity`.

---



