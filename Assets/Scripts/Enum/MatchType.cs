// Danh sach cac loai Match tren ban co Match-3
public enum MatchType
{
    Normal,             // Match 3 vien thong thuong (khong sinh booster)
    HorizontalRocket,   // Rocket ngang (sinh ra khi match 4 vien doc)
    VerticalRocket,     // Rocket doc (sinh ra khi match 4 vien ngang)
    TNT,                // Bom TNT (sinh ra khi match giao nhau chu T, L, +)
    Missile,            // Ten lua / May bay mini (sinh ra khi match o vuong 2x2)
    LightBall           // Cau cau vong / Cau sang (sinh ra khi match 5 vien thang hang)
}
