namespace ZoomButton
{
    public class ModConfig
    {
        public static ModConfig Loaded { get; set; } = new ModConfig();
        public float zoomInTimeSec = 0.5F;
        public float zoomOutTimeSec = 0.1F;
        public int fieldOfView = 20;
        public float mouseSensitivityFactor = 0.5F;
        public bool changeMouseSmoothing = false;
        public float mouseSmoothing = 0.0F;
        public bool vignetteShaderEnabled = true;
    }
}
