using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingUtils
{
    private Texture2D texture;
    private Color color;
    private int brushSize;

    public DrawingUtils(Texture2D texture, Color color, int brushSize)
    {
        this.texture = texture;
        this.color = color;
        this.brushSize = brushSize;
    }

    public void DrawPoint(Vector2Int position)
    {
        if (brushSize == 1)
        {
            texture.SetPixel(position.x, position.y, color);
        }
        // ブラシの大きさが偶数の場合と奇数の場合で処理を分ける
        else if (brushSize % 2 == 0)
        {
            int halfSize = brushSize / 2;
            for (int dx = -halfSize + 1; dx <= halfSize; dx++)
            {
                for (int dy = -halfSize + 1; dy <= halfSize; dy++)
                {
                    int px = position.x + dx;
                    int py = position.y + dy;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
        else if (brushSize % 2 == 1)
        {
            int halfSize = brushSize / 2;
            for (int dx = -halfSize; dx <= halfSize; dx++)
            {
                for (int dy = -halfSize; dy <= halfSize; dy++)
                {
                    int px = position.x + dx;
                    int py = position.y + dy;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    // Bresenhamの直線アルゴリズム
    public void DrawLine(Vector2Int start, Vector2Int end)
    {
        int dx = Mathf.Abs(end.x - start.x), sx = start.x < end.x ? 1 : -1;
        int dy = -Mathf.Abs(end.y - start.y), sy = start.y < end.y ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            DrawPoint(start);
            if (start == end) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; start.x += sx; }
            if (e2 <= dx) { err += dx; start.y += sy; }
        }
    }

    // Bresenhamの楕円アルゴリズム
    public void DrawCircle(Vector2Int start, Vector2Int end)
    {
        int centerX = (start.x + end.x) / 2;
        int centerY = (start.y + end.y) / 2;
        int radiusX = Mathf.Abs(centerX - start.x);
        int radiusY = Mathf.Abs(centerY - start.y);

        int x, y;
        float dx, dy, d1, d2;

        x = 0;
        y = radiusY;

        // 第一区間(x増加,y一定)
        d1 = (radiusY * radiusY) - (radiusX * radiusX * radiusY) + (0.25f * radiusX * radiusX);
        dx = 2 * radiusY * radiusY * x;
        dy = 2 * radiusX * radiusX * y;

        while (dx < dy)
        {
            DrawPoint(new Vector2Int(centerX + x, centerY + y));
            DrawPoint(new Vector2Int(centerX - x, centerY + y));
            DrawPoint(new Vector2Int(centerX + x, centerY - y));
            DrawPoint(new Vector2Int(centerX - x, centerY - y));

            x++;
            dx += 2 * radiusY * radiusY;
            if (d1 < 0)
            {
                d1 += dx + radiusY * radiusY;
            }
            else
            {
                y--;
                dy -= 2 * radiusX * radiusX;
                d1 += dx - dy + radiusY * radiusY;
            }
        }
        // 第二区間(x一定,y減少)
        d2 = ((radiusY * radiusY) * ((x + 0.5f) * (x + 0.5f))) + ((radiusX * radiusX) * ((y - 1) * (y - 1))) - (radiusX * radiusX * radiusY * radiusY);

        while (y >= 0)
        {
            DrawPoint(new Vector2Int(centerX + x, centerY + y));
            DrawPoint(new Vector2Int(centerX - x, centerY + y));
            DrawPoint(new Vector2Int(centerX + x, centerY - y));
            DrawPoint(new Vector2Int(centerX - x, centerY - y));

            y--;
            dy -= 2 * radiusX * radiusX;
            if (d2 > 0)
            {
                d2 += radiusX * radiusX - dy;
            }
            else
            {
                x++;
                dx += 2 * radiusY * radiusY;
                d2 += dx - dy + radiusX * radiusX;
            }
        }
    }

    public void DrawRectangle(Vector2Int start, Vector2Int end)
    {
        int xMin = Mathf.Min(start.x, end.x);
        int xMax = Mathf.Max(start.x, end.x);
        int yMin = Mathf.Min(start.y, end.y);
        int yMax = Mathf.Max(start.y, end.y);

        for (int x = xMin; x <= xMax; x++)
        {
            DrawPoint(new Vector2Int(x, start.y));
            DrawPoint(new Vector2Int(x, end.y));
        }
        for (int y = yMin; y <= yMax; y++)
        {
            DrawPoint(new Vector2Int(start.x, y));
            DrawPoint(new Vector2Int(end.x, y));
        }
    }

    // 塗りつぶしアルゴリズム
    public void FloodFill(RawImage rawImage, Vector2 position)
    {
        Rect rect = rawImage.rectTransform.rect;

        // ローカル座標をTexture2Dの座標に変換
        int x = Mathf.FloorToInt((position.x - rect.x) / rect.width * texture.width);
        int y = Mathf.FloorToInt((position.y - rect.y) / rect.height * texture.height);

        FloodFillPixel(x, y);
    }

    public void FloodFillPixel(int x, int y)
    {
        // 範囲外は何もしない（安全）
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            return;

        Color targetColor = texture.GetPixel(x, y);

        // クリックした場所の色と塗りつぶし色が同じ場合は何もしない
        if (targetColor == color) return;

        Queue<Vector2Int> pixelQueue = new Queue<Vector2Int>();
        pixelQueue.Enqueue(new Vector2Int(x, y));

        while (pixelQueue.Count > 0)
        {
            Vector2Int p = pixelQueue.Dequeue();

            if (p.x < 0 || p.x >= texture.width || p.y < 0 || p.y >= texture.height)
                continue;

            if (texture.GetPixel(p.x, p.y) != targetColor)
                continue;

            texture.SetPixel(p.x, p.y, color);

            pixelQueue.Enqueue(new Vector2Int(p.x + 1, p.y));
            pixelQueue.Enqueue(new Vector2Int(p.x - 1, p.y));
            pixelQueue.Enqueue(new Vector2Int(p.x, p.y + 1));
            pixelQueue.Enqueue(new Vector2Int(p.x, p.y - 1));
        }
    }

}
