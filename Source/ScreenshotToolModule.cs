using System;
using Celeste.Mod.ScreenshotTool.Scanner;

namespace Celeste.Mod.ScreenshotTool;

public class ScreenshotToolModule : EverestModule {
    public static ScreenshotToolModule Instance { get; private set; }

    public override Type SettingsType => typeof(ScreenshotToolModuleSettings);
    public static ScreenshotToolModuleSettings Settings => (ScreenshotToolModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(ScreenshotToolModuleSession);
    public static ScreenshotToolModuleSession Session => (ScreenshotToolModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(ScreenshotToolModuleSaveData);
    public static ScreenshotToolModuleSaveData SaveData => (ScreenshotToolModuleSaveData) Instance._SaveData;

    public ScreenshotToolModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(ScreenshotToolModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(ScreenshotToolModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        On.Celeste.Player.Update += OnPlayerUpdate;
        
        SilentScanner.Load();
    }

    public override void Unload() {
        On.Celeste.Player.Update -= OnPlayerUpdate;
        
        SilentScanner.Unload();
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
    {
        if (Settings.RecordScreen.Pressed)
            SilentScanner.TakeStandaloneScreenshot(self);
        if (Settings.RecordRoom.Pressed)
            SilentScanner.StartScanRoom(self);
        if (Settings.RecordChapter.Pressed)
            SilentScanner.StartScanChapter(self);
        
        ScreenshotRenderer.OnPlayerUpdate(self);
        
        orig(self);
    }
}
