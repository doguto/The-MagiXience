namespace Project.Scenes.Battle.Scripts.Model
{
    public static class ScreenBoundsCache
    {
        public static float MinX { get; private set; }
        public static float MaxX { get; private set; }
        public static float MinY { get; private set; }
        public static float MaxY { get; private set; }

        // カメラは原点に置かれている前提。
        public static void Initialize(float orthographicSize, float aspect)
        {
            var halfWidth = orthographicSize * aspect;
            MinX = -halfWidth;
            MaxX = halfWidth;
            MinY = -orthographicSize;
            MaxY = orthographicSize;
        }
    }
}
