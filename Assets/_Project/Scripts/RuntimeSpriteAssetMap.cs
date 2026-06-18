// Надає безпечні методи завантаження текстур і вирізання спрайтів за координатами атласу.
using UnityEngine;

public static class RuntimeSpriteAssetMap
{
    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0.05f);

    public static Texture2D LoadTexture(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        PreparePixelTexture(texture);
        return texture;
    }

    public static Sprite SpriteFromTopLeft(
        Texture2D texture,
        float x,
        float y,
        float width,
        float height,
        float pixelsPerUnit,
        Vector2 pivot)
    {
        if (texture == null) return null;
        Rect rect = RectFromTopLeft(texture, x, y, width, height);
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    public static Sprite TileFromGrid(Texture2D texture, int cellX, int cellY, int cellSize, float pixelsPerUnit)
    {
        return SpriteFromTopLeft(
            texture,
            cellX * cellSize,
            cellY * cellSize,
            cellSize,
            cellSize,
            pixelsPerUnit,
            Center);
    }

    public static Sprite FullSprite(Texture2D texture, float pixelsPerUnit, Vector2 pivot)
    {
        if (texture == null) return null;
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            pivot,
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    public static Sprite LoadSprite(string resourcePath, float pixelsPerUnit, Vector2 pivot)
    {
        Texture2D texture = LoadTexture(resourcePath);
        return FullSprite(texture, pixelsPerUnit, pivot);
    }

    public static Rect RectFromTopLeft(Texture2D texture, float x, float y, float width, float height)
    {
        return new Rect(x, texture.height - y - height, width, height);
    }

    public static void PreparePixelTexture(Texture2D texture)
    {
        if (texture == null) return;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
    }
}
