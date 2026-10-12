// Danh sach cac loai o va item tren ban co match-3
public enum EnumItemBoard
{
    Blank = -1,     // O trong (khong co gi)
    Board = 0,      // O ban co (BoardCell)
    Spawn = 1,       // Diem sinh item (Spawner)
    Red = 102,        // Item Do
    Blue = 103,       // Item Xanh Duong
    Green = 104,      // Item Xanh La
    Yellow = 105,     // Item Vang
    Purple = 106,     // Item Tim
    Orange = 107,     // Item Cam
    Pink = 108,       // Item Hong
    Stone = 301,    // Vat can Ground (Tang Overlay)
    Grass = 201,    // Vat can Grass (Tang Under)
    HorizontalRocket = 401, // Rocket ngang (sinh ra khi match 4 doc)
    VerticalRocket = 402,   // Rocket doc (sinh ra khi match 4 ngang)
    TNT = 403,              // Bom TNT (sinh ra khi match giao nhau chu T, L, +)
    Missile = 404,          // Ten lua / May bay mini (sinh ra khi match o vuong 2x2)
    LightBall = 405         // Cau cau vong / Cau sang (sinh ra khi match 5 vien thang hang)
}

