using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class SpriteDividerHelper : MonoBehaviour
    {
        public static List<Sprite> DivideSprite(Sprite sprite)
        {
            // Get the texture and validate its size
            Texture2D texture = sprite.texture;
            int textureSize = texture.width; // Assuming the texture is square

            if (textureSize != texture.height || textureSize % 4 != 0 || (textureSize & (textureSize - 1)) != 0)
            {
                Debug.LogError("Sprite's texture must be square, a power of 2, and divisible by 4.");
                return null;
            }

            List<Sprite> sprites = new List<Sprite>();
            int spriteSize = textureSize / 4; // Each grid cell size (e.g., for 4x4, divide into 4 parts)
            int numberOfSpritesPerRow = textureSize / spriteSize;

            for (int y = 0; y < numberOfSpritesPerRow; y++)
            {
                for (int x = 0; x < numberOfSpritesPerRow; x++)
                {
                    // Flip the y-coordinate
                    int flippedY = numberOfSpritesPerRow - 1 - y;

                    Rect spriteRect = new Rect(
                        sprite.rect.x + x * spriteSize,
                        sprite.rect.y + flippedY * spriteSize,
                        spriteSize,
                        spriteSize
                    );

                    Sprite subSprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
                    sprites.Add(subSprite);
                }
            }

            return sprites;
        }
    }
}