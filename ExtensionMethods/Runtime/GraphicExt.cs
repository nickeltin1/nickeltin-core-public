using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.Core.Runtime
{
    public static class GraphicExt
    {
        private static Sprite _emptyTexture;
        public static Sprite TransparentTexture
        {
            get
            {
                if(_emptyTexture == null) _emptyTexture = Sprite.Create(new Texture2D(100, 100),
                    new Rect(-100, -100, 100, 100), Vector2.one / 2);
                return _emptyTexture;
            }
        }

        public static void SetEmptyTexture(this Image img) => img.sprite = TransparentTexture;
        public static void SetEmptyTexture(this SpriteRenderer img) => img.sprite = TransparentTexture;
    }
}