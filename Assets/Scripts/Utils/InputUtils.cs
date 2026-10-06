using UnityEngine;

namespace Utils
{
    // Tien ich xu ly Input cu chi (Swipe/Drag) cho game Match-3
    public static class InputUtils
    {
        // Xac dinh huong vuot (left, right, up, down) giua diem truoc (before) va diem sau (after)
        public static Vector2 GetSwipeDirection(Vector2 before, Vector2 after, float threshold = 0f)
        {
            Vector2 delta = after - before;
            return GetSwipeDirection(delta, threshold);
        }

        // Xac dinh huong vuot dua vao vector do doi delta (sau - truoc)
        public static Vector2 GetSwipeDirection(Vector2 delta, float threshold = 0f)
        {
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            // Kiem tra nguong toi thieu de loc rung tay
            if (absX <= threshold && absY <= threshold)
            {
                return Vector2.zero;
            }

            if (absX > absY)
            {
                return delta.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                return delta.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        // Anh xa huong vuot Vector2 sang do lech toa do Grid (dX, dY)
        public static Vector2Int SwipeDirectionToOffset(Vector2 swipeDir)
        {
            if (swipeDir == Vector2.right) return Vector2Int.right;
            if (swipeDir == Vector2.left) return Vector2Int.left;
            if (swipeDir == Vector2.up) return Vector2Int.up;
            if (swipeDir == Vector2.down) return Vector2Int.down;
            return Vector2Int.zero;
        }

        public static Vector2Int SwipeDirectionToMatrixOffset(Vector2 swipeDir)
        {
            return SwipeDirectionToOffset(swipeDir);
        }
    }
}
