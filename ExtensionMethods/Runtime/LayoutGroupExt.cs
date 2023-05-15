using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.Core.Runtime
{
    public static class LayoutGroupExt
    {
        /// <summary>
        /// Works properly only on Flexible group without Size Fitter
        /// </summary>
        /// <param name="layoutGroup"></param>
        /// <returns>Returns max vertical and horizontal cells count</returns>
        public static Vector2Int GetFlexibleDimensions(this GridLayoutGroup layoutGroup)
        {
            var size = layoutGroup.GetComponent<RectTransform>().rect.size;
            var spacing = layoutGroup.spacing; 
            var cellSize = layoutGroup.cellSize;
            var padding = layoutGroup.padding;
            
            int GetCellCountOnAxis(float length, float _spacing, float _cellSize, float _padding)
            {
                return Mathf.Max(1, Mathf.FloorToInt((length - _padding + _spacing + 0.001f) / (_cellSize + _spacing)));
            } 

            return new Vector2Int(GetCellCountOnAxis(size.x, spacing.x, cellSize.x, padding.horizontal), 
                GetCellCountOnAxis(size.y, spacing.y, cellSize.y, padding.vertical));
        }
    }
}