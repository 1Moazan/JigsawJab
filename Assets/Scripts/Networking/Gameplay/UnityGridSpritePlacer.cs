using System.Collections.Generic;
using UnityEngine;

namespace Networking.Gameplay
{
    public class UnityGridSpritePlacer : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridWidth = 5;  
        public int gridHeight = 5;
        public float widthOffset = 0.1f;

        [SerializeField] private Grid grid; 
        
        public float targetScale;
        public void CreateGrid(List<Sprite> sprites)
        {
            if (sprites == null || sprites.Count == 0)
            {
                Debug.LogError("No sprites provided!");
                return;
            }

            // Calculate screen width in world units
            float screenWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
            float usableWidth = screenWidth * (1 - widthOffset); // Apply offset

            // Get the size of the first sprite
            float spriteWidth = sprites[0].bounds.size.x;
            float spriteHeight = sprites[0].bounds.size.y;

            // Calculate dynamic sprite scale to fit the grid within the usable width
            float targetSpriteWidth = usableWidth / gridWidth;
            float scale = targetSpriteWidth / spriteWidth;
            targetScale = scale;

            // Adjust grid cell size based on the new sprite scale
            float adjustedCellWidth = spriteWidth * scale;
            float adjustedCellHeight = spriteHeight * scale;
            grid.cellSize = new Vector3(adjustedCellWidth, adjustedCellHeight, 0);

            // Calculate the total grid dimensions in world space
            float totalGridWidth = gridWidth * adjustedCellWidth;
            float totalGridHeight = gridHeight * adjustedCellHeight;

            // Center the grid in the screen
            Vector3 screenCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            screenCenter.z = 0; // Ensure z is set to 0 for 2D purposes

            // Offset the grid to align its center with the screen center
            Vector3 gridCenterOffset = new Vector3(-totalGridWidth / 2, -totalGridHeight / 2, 0);
            this.transform.position = screenCenter + gridCenterOffset;

            // Generate the grid cells
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int rowIndex = gridHeight - 1 - y; // Reverse the row index
                    int spriteIndex = (x + rowIndex * gridWidth) % sprites.Count;
                    CreateCell(x, y, sprites[spriteIndex], scale);
                }
            }
        }



        
        private void CreateCell(int x, int y, Sprite sprite, float scale)
        {
            GameObject cell = new GameObject($"Cell_{x}_{y}");
            cell.transform.SetParent(this.transform);
            cell.transform.localPosition = grid.CellToLocal(new Vector3Int(x, y, 0));

            SpriteRenderer spriteRenderer = cell.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            cell.transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}
