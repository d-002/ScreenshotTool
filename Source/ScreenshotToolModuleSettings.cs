using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.ScreenshotTool;

public class ScreenshotToolModuleSettings : EverestModuleSettings
{
    [SettingSubText("Main (relative) directory path to export images to.")]
    [SettingMinLength(1)]
    [SettingMaxLength(256)]
    public string ExportPath { get; set; } = "ScreenshotTool_Exports";

    [SettingSubText("Attempt to freeze time when taking screenshots, can help with particles and other animations.\n" +
                    "Can cause issues with unloaded spinners and lightning.")]
    public bool FreezeTime { get; set; } = false;

    [SettingSubText("Whether to try to remove the backdrop and make it transparent.")]
    public bool RemoveBackground { get; set; } = false;
    [SettingSubText("Whether to try to remove foreground effects whose parallax can mess things up.")]
    public bool RemoveForeground { get; set; } = false;

    [SettingSubMenu]
    public class AdvancedSettingsSubMenu
    {
        [SettingSubText("Delay in between the frames, to allow the GPU to catch up.")]
        [SettingRange(min: 3, max: 600)]
        public int WaitDelay { get; set; } = 20;

        [SettingSubText("Shift in position (in tiles) between screenshots.")]
        [SettingRange(min: 1, max: 40)] public int HorizontalScreenshotOffset { get; set; } = 20;
        [SettingSubText("Shift in position (in tiles) between screenshots.")]
        [SettingRange(min: 1, max: 20)] public int VerticalScreenshotOffset { get; set; } = 15;
    }

    public AdvancedSettingsSubMenu AdvancedSettings { get; set; } = new();

    [SettingSubText("Take a single screenshot of the entire screen")]
    [DefaultButtonBinding(button: Buttons.LeftStick, key: Keys.P)]
    public ButtonBinding RecordScreen { get; set; }
    
    [SettingSubText("Take multiple screenshots of the current room in a way to cover it all")]
    public ButtonBinding RecordRoom { get; set; }
    
    [SettingSubText("Go to every room in the chapter sequentially, recording each of them fully")]
    public ButtonBinding RecordChapter { get; set; }
}
