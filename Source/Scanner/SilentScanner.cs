using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ScreenshotTool.Scanner;

public static class SilentScanner
{
    // saved data
    private static Vector2 _prevPosition;
    private static Collider _prevCollider;
    private static Entity _timeStopEntity;
    private static BackdropRenderer _background;
    private static BackdropRenderer _foreground;

    public static void BeforeScan(Player self)
    {
        _prevPosition = self.Position;
        _prevCollider = self.Collider;

        self.Visible = false;
        self.Collidable = false;
        self.Collider = new Hitbox(0, 0);
        SaveData.Instance.Assists.Invincible = true;
        self.level.CameraLockMode = Level.CameraLockModes.None;

        if (ScreenshotToolModule.Settings.FreezeTime)
        {
            _timeStopEntity =
            [
                new TimeRateModifier(0)
            ];
            self.level.Add(_timeStopEntity);
        }

        if (ScreenshotToolModule.Settings.RemoveBackground)
        {
            _background = self.level.Background;
            self.level.Background = new BackdropRenderer
            {
                Visible = false
            };
        }

        if (ScreenshotToolModule.Settings.RemoveForeground)
        {
            _foreground = self.level.Foreground;
            self.level.Foreground = new BackdropRenderer
            {
                Visible = false
            };
        }

        // store extra metadata to a file and make sure the directory exists
        string filePath = FileHelper.GetExistingPath(self, "room.json");

        Rectangle bounds = self.level.Bounds;
        using StreamWriter writer = new StreamWriter(filePath);
        float camW = self.level.Camera.Right - self.level.Camera.Left;
        float camH = self.level.Camera.Bottom - self.level.Camera.Top;

        // probably a better way to do this but oh well
        writer.WriteLine("{");
        writer.WriteLine($"    \"roomPosition\": [{bounds.Left}, {bounds.Top}],");
        writer.WriteLine($"    \"roomSize\": [{bounds.Width}, {bounds.Height}],");
        writer.WriteLine($"    \"cameraSize\": [{camW}, {camH}],");
        writer.WriteLine($"    \"viewPort\": [{Engine.ViewWidth}, {Engine.ViewHeight}]");
        writer.WriteLine("}");
    }

    public static void AfterScan(Player self)
    {
        self.Visible = true;
        self.Collidable = true;
        self.Collider = _prevCollider;
        self.Position = _prevPosition;
        SaveData.Instance.Assists.Invincible = false;

        if (ScreenshotToolModule.Settings.FreezeTime)
            self.level.Remove(_timeStopEntity);

        if (ScreenshotToolModule.Settings.RemoveBackground)
            self.level.Background = _background;
        if (ScreenshotToolModule.Settings.RemoveForeground)
            self.level.Foreground = _foreground;
    }

    public static void StartScanRoom(Player self)
    {
        if (RoomsScanner.IsScanning)
            return;

        RoomsScanner.IsScanning = true;

        RoomsScanner.RoomQueue.Enqueue(self.level.Session.Level);
        RoomsScanner.RunScanCoroutine(self);
    }

    public static void StopScan()
    {
        RoomsScanner.RoomQueue.Clear();
        RoomsScanner.IsScanning = false;
    }

    public static void StartScanChapter(Player self)
    {
        if (RoomsScanner.IsScanning)
            return;

        RoomsScanner.IsScanning = true;

        List<LevelData> allRooms = self.level.Session.MapData.Levels;

        foreach (LevelData room in allRooms)
        {
            Logger.Info("ScreenshotTool", $"Found room: {room.Name}");
            RoomsScanner.RoomQueue.Enqueue(room.Name);
        }

        RoomsScanner.RunScanCoroutine(self);
    }

    public static void TakeStandaloneScreenshot(Player self)
    {
        RoomsScanner.TriggerRender(self, -1, null, true);
    }

    private static void UpdateWhitelist(On.Celeste.Level.orig_Update orig, Level self)
    {
        if (!ScreenshotToolModule.Settings.FreezeTime || !RoomsScanner.IsScanning || !RoomsScanner.IsTransitioning)
        {
            orig(self);
            return;
        }

        try
        {
            Type[] whitelisted =
            [
                typeof(Player), typeof(LightningRenderer),
                typeof(CrystalStaticSpinner), typeof(DustStaticSpinner),
                typeof(DustRotateSpinner), typeof(DustTrackSpinner),
                typeof(BladeRotateSpinner), typeof(BladeTrackSpinner),
                typeof(StarTrackSpinner), typeof(StarRotateSpinner)
            ];

            foreach (Type type in whitelisted)
                if (self.Tracker.Entities.TryGetValue(type, out var trackerEntity))
                    foreach (Entity entity in trackerEntity)
                        entity.Update();
        }
        catch (Exception e)
        {
            Logger.Error("ScreenshotTool",
                $"Error in UpdateWhitelist, while scanning with time frozen: {e.Message}");
        }
    }

    private static bool PlayerColliderHook(On.Celeste.PlayerCollider.orig_Check orig, PlayerCollider self,
        Player player)
    {
        return !RoomsScanner.IsScanning && orig(self, player);
    }

    private static void PufferUpdateHook(On.Celeste.Puffer.orig_Update orig, Puffer self)
    {
        if (!RoomsScanner.IsScanning)
            orig(self);
    }

    private static void BadelineUpdateHook(On.Celeste.BadelineOldsite.orig_Update orig, BadelineOldsite self)
    {
        if (!RoomsScanner.IsScanning)
            orig(self);
    }

    private static void BirdUpdateHook(On.Celeste.FlingBird.orig_Update orig, FlingBird self)
    {
        if (!RoomsScanner.IsScanning)
            orig(self);
    }

    public static void Load()
    {
        // to freeze time when needed
        On.Celeste.Level.Update += UpdateWhitelist;

        // to make the player really invisible from things
        On.Celeste.PlayerCollider.Check += PlayerColliderHook;
        On.Celeste.Puffer.Update += PufferUpdateHook;
        On.Celeste.BadelineOldsite.Update += BadelineUpdateHook;
        On.Celeste.FlingBird.Update += BirdUpdateHook;
    }

    public static void Unload()
    {
        On.Celeste.Level.Update -= UpdateWhitelist;

        On.Celeste.PlayerCollider.Check -= PlayerColliderHook;
        On.Celeste.Puffer.Update -= PufferUpdateHook;
        On.Celeste.BadelineOldsite.Update -= BadelineUpdateHook;
        On.Celeste.FlingBird.Update -= BirdUpdateHook;
    }
}