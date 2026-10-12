# Match-3 Game

Dự án game Match-3 2D hoàn chỉnh được phát triển trên nền tảng Unity 6 với Universal Render Pipeline (URP - 2D), tối ưu hóa kiến trúc theo chuẩn Clean Code, Design Patterns (Singleton, Event Bus, State, Object Pooling) và lập trình bất đồng bộ bằng UniTask & DOTween.

---

## 1. Môi trường Phát triển & Thư viện
- **Unity Version**: `6000.5.7f1` (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP - 2D)
- **Thư viện bên thứ ba**:
  - [UniTask](https://github.com/Cysharp/UniTask): Lập trình bất đồng bộ hiệu năng cao không cấp phát GC.
  - [DOTween (HOTween v2)](http://dotween.demigiant.com/): Điều khiển chuyển động và hoạt ảnh UI/Gameplay.
  - [OdinSerializer](https://github.com/TeamSirenix/odin-serializer): Tuần tự hóa và lưu trữ dữ liệu JSON (Level Data & Player Data).
  - [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest): Hiển thị văn bản chất lượng cao.

---

## 2. Cấu trúc Dự án
```text
Assets/
├── Audio/              # Audio Clips, AudioMixer và ScriptableObjects cấu hình âm thanh
├── Data/               # File dữ liệu JSON (level_1.json, level_2.json, level_3.json, player_data.json)
├── Prefabs/            # Prefabs của bàn cờ, các loại ngọc, booster, vật cản, UI và hiệu ứng
├── Scenes/             # Các Scene chính: StartScene (menu chính), MainScene (gameplay), ForCreator
├── Scripts/
│   ├── Audio/          # AudioManager, AudioEventSO, AudioConfigSO
│   ├── Core/           # Data models (PlayerData, LevelData, GoalEntry) và GameEventBus
│   ├── Enum/           # Hệ thống Enum chuẩn hóa (EnumItemBoard, EnumBoardLayer, AudioTargetType...)
│   ├── Game/
│   │   ├── Base/       # Interfaces (IDestructible, IMatchable, ISwappable, IBoardItem)
│   │   ├── Board/      # Board, HandleInput, BoardCell
│   │   ├── BoardItems/ # BoardItemBase, MatchItem, ObstacleItem, BoosterItem, Normal/Under/OverLayerItem
│   │   ├── Manager/    # GameFlowController, CheckMatchManager, ItemDestructionHandler, ItemFallManager...
│   │   ├── Matching/   # LineScanMatchChecker (thuật toán quét Match-3, 4, 5, T/L, 2x2)
│   │   └── Pools/      # Pooltem, PoolBlockBreakEffect
│   ├── UI/             # MainMenuController, ScoreView, MovesView, GoalView, LevelResultPopup, SettingsBar...
│   └── Utils/          # GridUtils, BoardItemUtils, BoosterComboGridUtils, SingletonDontDestroy...
└── Tilemaps/           # Bảng vẽ và Tileset thiết kế màn chơi
```

---

## 3. Kiến trúc Hệ thống & Gameplay

### 3.1. Hệ tọa độ Chuẩn hóa & Adapter Spawn
- **Dữ liệu Level JSON gốc**: Lưu trữ dạng ma trận 2D `[row, col]` (hàng `0` ở đỉnh màn hình, tương thích bộ thiết kế level).
- **Cơ chế Adapter tại `Board.SpawnLayer`**: Chỉ đảo trục $Y$ duy nhất 1 lần khi đọc file JSON vào ma trận runtime:
  ```csharp
  int x = c;
  int y = height - 1 - r;
  targetGrid[x, y] = obj;
  ```
- **Đồng bộ trực quan với Unity World Space**:
  - `X`: Cột từ trái (`0`) sang phải (`width - 1`).
  - `Y`: Hàng từ đáy (`0`) lên đỉnh (`height - 1`).
  - `grid[x, y]` giúp loại bỏ hoàn toàn các phép tính nghịch đảo hàng/cột phức tạp trong logic gameplay (rơi ngọc, vuốt đổi chỗ, quét match).

---

### 3.2. Kiến trúc Hướng Đối Tượng cho Board Items
Nhằm đảm bảo tính mở rộng cao và tuân thủ nguyên lý Single Responsibility & Interface Segregation:
- **Interfaces chuyên biệt**:
  - [`IDestructible`](Assets/Scripts/Game/Base/IDestructible.cs): Định nghĩa item có thể bị phá hủy, trừ máu (`TakeDamageAsync`), kiểm tra trạng thái sống/chết.
  - [`IMatchable`](Assets/Scripts/Game/Base/IMatchable.cs): Định nghĩa viên ngọc có màu sắc (`ItemColor`), tham gia vào cụm Match-3.
  - [`ISwappable`](Assets/Scripts/Game/Base/ISwappable.cs): Xác định item có thể hoán đổi với item lân cận hay không.
- **Phân cấp lớp kế thừa**:
  - [`BoardItemBase`](Assets/Scripts/Game/BoardItems/BoardItemBase.cs): Lớp cơ sở trừu tượng chuẩn hóa vị trí, tọa độ grid và hiển thị.
  - [`MatchItem`](Assets/Scripts/Game/BoardItems/MatchItem.cs): Viên ngọc thông thường (triển khai `IMatchable`, `ISwappable`, `IDestructible`).
  - [`ObstacleItem`](Assets/Scripts/Game/BoardItems/ObstacleItem.cs): Chướng ngại vật đa tầng (Đá, Cỏ, Băng...) có máu và chống hoán đổi.
  - [`BoosterItem`](Assets/Scripts/Game/BoardItems/BoosterItem.cs): Các vật phẩm đặc biệt có khả năng kích hoạt hiệu ứng nổ.

---

### 3.3. Xử lý Tiêu hủy Tập trung (`ItemDestructionHandler`)
Toàn bộ logic phá hủy item trên bàn cờ được quy về [`ItemDestructionHandler`](Assets/Scripts/Game/Manager/ItemDestructionHandler.cs), loại bỏ code trùng lặp:
- **`DestroyUnderItem(board, x, y)`**: Xử lý phá hủy vật cản nền (Cỏ - Grass) khi viên ngọc đè bên trên nổ hoặc khi Booster quét qua.
- **`ExplodeByBooster(board, x, y)`**: Quét và tiêu diệt đồng thời cả 3 tầng (`OverlayGrid`, `MidGrid`, `UnderGrid`) trong phạm vi ảnh hưởng của Booster.
- **`ExplodeAdjacentObstacle(board, x, y)`**: Tự động nhận diện và phá hủy chướng ngại vật ở rìa 4 hướng trực giao quanh cụm Match thường.
- Tự động đồng bộ việc giải phóng ô trên `Board`, phát hạt nổ qua `PoolBlockBreakEffect`, phát âm thanh qua `GameEventBus` và cộng điểm/tiến độ màn chơi.

---

### 3.4. Thuật toán Quét & Xử lý Match (`LineScanMatchChecker` & `CheckMatchManager`)
- **Quét đoạn thẳng (`LineScanMatchChecker`)**: Quét các đường thẳng $\ge 3$ cùng màu theo trục ngang và dọc:
  - Giao nhau chữ T, L, + $\rightarrow$ Sinh **TNT**.
  - Đoạn thẳng $\ge 5$ viên $\rightarrow$ Sinh **LightBall**.
  - Đoạn thẳng $4$ viên $\rightarrow$ Sinh **Rocket** (ngang sinh VerticalRocket, dọc sinh HorizontalRocket).
  - Khối vuông 2x2 $\rightarrow$ Sinh **Missile**.
  - 3 viên thẳng hàng $\rightarrow$ Nổ thường (**Normal Match**).
- **Vòng lặp Nổ Liên Hoàn (Cascade Combo)**:
  - `CheckMatchManager` điều phối chu kỳ: *Quét match $\rightarrow$ Nổ & sinh Booster $\rightarrow$ Rơi ngọc (`ItemFallManager`) $\rightarrow$ Sinh ngọc mới (`SpawnItemFromSky`) $\rightarrow$ Quét tiếp* cho đến khi bàn cờ ổn định.
  - Tự động tích lũy hệ số điểm combo (`cascadeStep`) tăng dần.

---

### 3.5. Trọng lực & Rơi ngọc Nâng cao (`ItemFallManager`)
- **Thuật toán Nhiều Pass (`StepFallAsync`)**:
  - **Rơi thẳng (Straight Fall)**: Ưu tiên rơi thẳng vào ô trống bên dưới trong cùng cột.
  - **Rơi chéo (Diagonal Fall)**: Khi gặp vật cản bên dưới, ngọc tự động trượt chéo sang trái hoặc phải vào các ô trống nếu luồng rơi thông suốt.
  - **Quản lý trạng thái ô cờ**: Chuyển đổi trạng thái `BoardCell.State` (`Empty` $\rightarrow$ `Falling` $\rightarrow$ `Occupied`) với cờ chống rơi đè.
  - **Gia tốc vật lý**: Thời gian rơi tính theo căn bậc hai khoảng cách ($t = \text{base} \times \sqrt{\Delta y}$) tạo cảm giác chuyển động tự nhiên.

---

### 3.6. Hệ thống Booster & Combo Resolver
Bao gồm 5 loại Booster cơ bản và cơ chế kết hợp giữa 2 Booster bất kỳ:
1. **Horizontal Rocket**: Nổ quét sạch hàng ngang.
2. **Vertical Rocket**: Nổ quét sạch cột dọc.
3. **TNT**: Kích nổ diện rộng vùng $3 \times 3$.
4. **Missile**: Tự động bay tìm và tiêu diệt mục tiêu (ưu tiên vật cản hoặc ngọc ngẫu nhiên).
5. **LightBall**: Quét sạch tất cả viên ngọc cùng màu trên toàn bàn cờ.
- **Tương tác Combo giữa 2 Booster**:
  - **Rocket + Rocket**: Nổ dấu cộng chữ thập (ngang + dọc).
  - **Rocket + TNT**: Quét chữ thập khổng lồ (3 hàng ngang + 3 cột dọc).
  - **TNT + TNT**: Vụ nổ đại bác bán kính cực đại $5 \times 5$.
  - **Missile + Missile**: Tách thành 3 quả tên lửa mini bay tới 3 mục tiêu.
  - **Missile + Rocket / TNT**: Tên lửa mang đầu đạn bay tới đích và phát nổ hiệu ứng tương ứng.
  - **LightBall + Booster**: Biến đổi tất cả ngọc cùng màu thành Booster đó và kích nổ đồng loạt.
  - **LightBall + LightBall**: Kích nổ tối thượng quét sạch toàn bộ bàn cờ.

---

### 3.7. Luồng Trò chơi, Điểm số, Lượt đi & Mục tiêu
- **`GameEventBus`**: Static Event Bus làm trung tâm kết nối toàn bộ hệ thống (Score, Moves, Goals, Win/Lose, Audio, UI) với mức độ coupling tối thiểu.
- **`ScoreManager`**: Quản lý điểm số, tính điểm ngọc thường, vật cản, booster và hệ số nhân combo cascade liên hoàn.
- **`MovesManager`**: Đếm số lượt di chuyển còn lại; cảnh báo khi sắp hết lượt.
- **`GoalTracker`**: Theo dõi danh sách mục tiêu màn chơi (`GoalEntry`); tự động bắn sự kiện thắng khi hoàn thành tất cả mục tiêu.
- **`GameFlowController`**: Điều phối nạp dữ liệu level, khởi tạo các manager, xử lý thắng/thua và ngắt input khi kết thúc game.

---

### 3.8. Hệ thống Âm thanh Tách biệt (Data & Event-Driven)
- Tách rời 100% giữa Gameplay và Audio qua `GameEventBus`.
- **ScriptableObjects**:
  - `AudioEventSO`: Quản lý danh sách audio clips, volume, pitch ngẫu nhiên, mixer group và cooldown chống méo tiếng.
  - `AudioConfigSO`: Danh mục cấu hình toàn bộ 23 sự kiện âm thanh (BGM, Swap, Match, Boosters, Combos, Obstacles, UI).
- **`AudioManager`**: Quản lý pool nguồn phát âm thanh SFX, nhạc nền BGM, hỗ trợ Dynamic Combo Pitch Escalation (tăng dần cao độ theo nhịp combo nổ liên tiếp) và điều khiển qua AudioMixer (0 dB / -80 dB).

---

### 3.9. Giao diện Người dùng (UI Layer) & Thanh Cài Đặt
- **Màn hình chính (`StartScene`)**: Quản lý bởi `MainMenuController`, hiển thị nút chơi kèm số level hiện tại của người chơi (`Play level X`), tự động chuyển scene mượt mà.
- **HUD Gameplay (`MainScene`)**:
  - `ScoreView`: Đếm số tăng dần mượt mà và hiệu ứng nảy scale bằng DOTween.
  - `MovesView`: Hiển thị số lượt đi, tự động đổi màu và rung cảnh báo khi còn ít lượt.
  - `GoalView` & `GoalItemView`: Hiển thị icon mục tiêu, số lượng cần thu thập và hiệu ứng checkmark khi hoàn thành.
  - `LevelResultPopup`: Popup Chiến Thắng / Thất Bại với nút Next Level và Retry (kèm hiệu ứng punch scale và reload scene).
- **Thanh Cài Đặt Bung Ngang (`ExpandableSettingsBar`)**:
  - Đặt tại góc màn hình, bung mở mượt mà 3 nút chức năng (BGM, SFX, Exit).
  - `AudioOnOffButton`: Nút bật/tắt BGM và SFX với icon gạch chéo hiển thị trạng thái chuẩn xác.
  - `ButtonExit`: Nút thoát thông minh (tự động chuyển về Start Scene hoặc thoát ứng dụng).

---

### 3.10. Hệ thống Lưu trữ & Dữ liệu (`SaveManager`)
- **`SaveManager` (Static Class)**: Quản lý nạp/lưu toàn bộ file JSON trong thư mục `Assets/Data/` bằng Odin Serializer.
- **`PlayerData`**: Lưu trữ tiến trình của người chơi (`CurrentLevel`, `HighestUnlockedLevel`, trạng thái âm thanh `isBgmOn`, `isSfxOn`).
- **`GetMaxLevel()`**: Tự động quét toàn bộ file level trong thư mục `Assets/Data` để xác định trần level cao nhất, chống lỗi nạp level vượt quá giới hạn dữ liệu.

---

## 4. Hướng dẫn Chạy & Thao tác
1. Mở dự án bằng **Unity 6 (6000.5.7f1)**.
2. Mở Scene khởi đầu tại: `Assets/Scenes/StartScene.unity`.
3. Nhấn **Play** để trải nghiệm game.
4. Thao tác điều khiển:
   - **Click / Chạm và Vuốt**: Hoán đổi 2 viên ngọc liền kề hoặc kích hoạt Booster.
   - **Thanh Cài Đặt**: Bấm icon bánh răng ở góc màn hình để điều chỉnh âm thanh hoặc thoát về menu chính.
