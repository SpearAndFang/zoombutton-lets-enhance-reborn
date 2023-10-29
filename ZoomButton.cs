//using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

[assembly: ModInfo("ZoomButton")]

namespace ZoomButton
{
    public class ZoomButton : ModSystem
    {
        private static readonly string HOTKEY_CODE = "zoombutton";
        private static readonly string FIELD_OF_VIEW_SETTING_NAME = "fieldOfView";
        private static readonly string MOUSE_SENSITIVITY_SETTING_NAME = "mouseSensivity"; // n.b. typo in Vintage Story's code!
        private static readonly string MOUSE_SMOOTHING_SETTING_NAME = "mouseSmoothing";
        private static readonly int MAX_FRAMERATE_MS = 1000 / 90;

        private ICoreClientAPI capi;
        private int originalFieldOfView;
        private int originalMouseSensivity;
        private int originalMouseSmoothing;
        private bool isZooming = false;
        private float zoomState = 0;
        private ModConfig config;
        private SquintOverlayRenderer renderer;

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;

            this.capi.Logger.Event("started 'ZoomButton' mod");

            // load config file or write it with defaults
            this.config = api.LoadModConfig<ModConfig>("zoombutton.json");
            if (this.config == null)
            {
                this.config = new ModConfig();
                api.StoreModConfig(this.config, "zoombutton.json");
            }

            api.Input.RegisterHotKey(HOTKEY_CODE, "Zoom in", GlKeys.Z, HotkeyType.CharacterControls);
            api.Event.RegisterGameTickListener(this.OnGameTick, MAX_FRAMERATE_MS);

            this.renderer = new SquintOverlayRenderer(api);
        }

        private void OnGameTick(float dt)
        {
            var isHotKeyPressed = this.capi.Input.KeyboardKeyState[this.capi.Input.GetHotKeyByCode(HOTKEY_CODE).CurrentMapping.KeyCode];

            // is the player currently zooming in?
            if (isHotKeyPressed && this.zoomState < 1)
            {
                // is this the start of a zoom?
                if (!this.isZooming)
                {
                    this.originalFieldOfView = this.capi.Settings.Int[FIELD_OF_VIEW_SETTING_NAME];
                    this.originalMouseSensivity = this.capi.Settings.Int[MOUSE_SENSITIVITY_SETTING_NAME];
                    this.originalMouseSmoothing = this.capi.Settings.Int[MOUSE_SMOOTHING_SETTING_NAME];
                    this.isZooming = true;
                }

                // advance zoomState
                this.zoomState += dt / this.config.zoomInTimeSec;
                if (this.zoomState > 1)
                {
                    this.zoomState = 1; // clamp to 0..1
                    isHotKeyPressed = false;
                }
                this.UpdateSettings();
            }
            // is the player currently zooming out?
            else if (!isHotKeyPressed && this.zoomState > 0)
            {
                // advance zoomState
                this.zoomState -= dt / this.config.zoomOutTimeSec;
                if (this.zoomState < 0)
                {
                    this.zoomState = 0; // clamp to 0..1
                    this.isZooming = false; // go back to initial state, which allows us to capture any player changes to settings
                }
                this.UpdateSettings();
            }
            if (this.config.vignetteShaderEnabled)
            {
                this.renderer.PercentZoomed = this.zoomState;
            }
            // otherwise we are already zoomed all the way in or out: nothing to do
        }

        private void UpdateSettings()
        {
            // update fov and mouse sensitivity via linear interpolation based on zoomState 0..1
            this.capi.Settings.Int[FIELD_OF_VIEW_SETTING_NAME] = this.lerp(this.originalFieldOfView, this.config.fieldOfView, this.zoomState);
            this.capi.Settings.Int[MOUSE_SENSITIVITY_SETTING_NAME] = this.lerp(this.originalMouseSensivity, this.originalMouseSensivity * this.config.mouseSensitivityFactor, this.zoomState);
            if (this.config.changeMouseSmoothing)
            {
                this.capi.Settings.Int[MOUSE_SMOOTHING_SETTING_NAME] = this.lerp(this.originalMouseSmoothing, this.config.mouseSmoothing, this.zoomState);
            }
        }

        private int lerp(float a, float b, float t)
        {
            return (int)System.Math.Round(a + ((b - a) * t));
        }
    }
}
