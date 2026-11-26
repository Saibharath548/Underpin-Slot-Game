using UnityEngine;

namespace Arrows.UI
{
    /// <summary>
    /// Utility for procedurally generating crisp UI sprites (anti-aliased hearts,
    /// rounded dialog cards, circles) without external image asset dependencies.
    /// </summary>
    public static class UISpriteFactory
    {
        private static Sprite cachedFilledHeart;
        private static Sprite cachedEmptyHeart;
        private static Sprite cachedCard;
        private static Sprite cachedButton;

        public static Sprite GetFilledHeartSprite()
        {
            if (cachedFilledHeart == null)
            {
                cachedFilledHeart = CreateHeartSprite(64, filled: true, new Color(1f, 0.23f, 0.28f, 1f)); // Vibrant Crimson/Red
            }
            return cachedFilledHeart;
        }

        public static Sprite GetEmptyHeartSprite()
        {
            if (cachedEmptyHeart == null)
            {
                cachedEmptyHeart = CreateHeartSprite(64, filled: false, new Color(0.7f, 0.7f, 0.75f, 0.4f)); // Faded slate
            }
            return cachedEmptyHeart;
        }

        public static Sprite GetCardSprite()
        {
            if (cachedCard == null)
            {
                cachedCard = CreateRoundedRectSprite(128, 128, 20f, Color.white);
            }
            return cachedCard;
        }

        public static Sprite GetButtonSprite()
        {
            if (cachedButton == null)
            {
                cachedButton = CreateRoundedRectSprite(128, 64, 16f, Color.white);
            }
            return cachedButton;
        }

        /// <summary>
        /// Creates a crisp, anti-aliased heart sprite using distance field sampling.
        /// </summary>
        public static Sprite CreateHeartSprite(int size, bool filled, Color tint)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];

            float invSize = 1f / size;
            for (int y = 0; y < size; y++)
            {
                float ny = (y * invSize - 0.48f) * 2.4f; // Centered vertical
                for (int x = 0; x < size; x++)
                {
                    float nx = (x * invSize - 0.5f) * 2.4f; // Centered horizontal

                    // Heart SDF: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    // Scaled and oriented so lobes are up and tip is down
                    float hx = nx;
                    float hy = ny + 0.35f;

                    // Heart equation F(x, y) = x^2 + (y - sqrt(|x|))^2 - 1
                    float sqrtAbsX = Mathf.Sqrt(Mathf.Abs(hx));
                    float dy = hy - sqrtAbsX;
                    float distSq = hx * hx + dy * dy;

                    // Interior is distSq <= 1.0
                    float dist = Mathf.Sqrt(distSq) - 1.0f;

                    float alpha;
                    if (filled)
                    {
                        // Filled heart with soft 1-pixel AA boundary
                        alpha = Mathf.Clamp01(0.5f - dist * 8f);
                    }
                    else
                    {
                        // Outline heart: border width ~ 0.25
                        float borderDist = Mathf.Abs(dist) - 0.18f;
                        alpha = Mathf.Clamp01(0.5f - borderDist * 8f);
                    }

                    colors[y * size + x] = new Color(tint.r, tint.g, tint.b, tint.a * alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Creates a rounded rectangle sprite suitable for cards, panels, and buttons.
        /// </summary>
        public static Sprite CreateRoundedRectSprite(int width, int height, float cornerRadius, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[width * height];
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            float innerW = halfW - cornerRadius;
            float innerH = halfH - cornerRadius;

            for (int y = 0; y < height; y++)
            {
                float dy = Mathf.Abs(y - halfH);
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Abs(x - halfW);

                    float qx = Mathf.Max(dx - innerW, 0f);
                    float qy = Mathf.Max(dy - innerH, 0f);
                    float dist = Mathf.Sqrt(qx * qx + qy * qy) - cornerRadius;

                    float alpha = Mathf.Clamp01(0.5f - dist);
                    colors[y * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Vector4 border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}

