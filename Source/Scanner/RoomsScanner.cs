using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ScreenshotTool.Scanner;

public static class RoomsScanner
{
    // for persistence through rooms
    private static Entity _scannerHost;

    public static readonly Queue<string> RoomQueue = new();

    private const int TileSize = 8;

    // whether the room the player just got teleported in is a real room or just a filler
    private static bool _isRealRoom;

    public static bool IsScanning;
    public static bool IsTransitioning;

    public static void TriggerRender(Player self, int index, Vector2? relPos, bool isStandalone)
    {
        // If needed, render twice with different background colors to figure out transparency.
        // Also remove bloom from one of the screenshots, it being captured once is enough and fixes artifacts.
        if (ScreenshotToolModule.Settings.RemoveBackground)
        {
            float prevBloom = self.level.Bloom.Strength;
            self.level.Bloom.Strength = 0;
            self.level.BackgroundColor = Color.White;
            self.level.BeforeRender();
            self.level.Render();
            self.level.AfterRender();
            ScreenshotRenderer.TakeScreenshot(self, ScreenshotRenderer.Pass.White);
            self.level.Bloom.Strength = prevBloom;
        }

        self.level.BackgroundColor = Color.Black;
        self.level.BeforeRender();
        self.level.Render();
        self.level.AfterRender();
        ScreenshotRenderer.TakeScreenshot(self, ScreenshotRenderer.Pass.Black);

        ScreenshotRenderer.AllowSaving(index, relPos, isStandalone);
    }

    private static IEnumerator ChangeRoom(Level level, Player player, string roomName)
    {
        yield return null;
        IsTransitioning = true;

        // don't use TeleportTo, mimic its behavior instead to skip any animations
        level.Session.Level = roomName;
        level.Session.FirstLevel = false;
        level.Session.StartedFromBeginning = false;
        try
        {
            level.Session.RespawnPoint = level.Session.GetSpawnPoint(player.Position);
            _isRealRoom = true;
        }
        catch
        {
            _isRealRoom = false;
            yield break;
        }

        LevelData nextRoom = level.Session.MapData.Get(roomName);
        Vector2 spawnPoint = nextRoom.Spawns[0];

        // warnings are fine, the variables are not modified until the hook is done running
        level.OnEndOfFrame += () =>
        {
            player.Position = spawnPoint;
            level.TransitionTo(nextRoom, Vector2.Zero);
        };

        // wait for the room transition
        while (Engine.Scene is Level currentLevel && currentLevel.Session.Level != roomName)
            yield return null;

        // wait for the next room to stabilize
        while (Engine.Scene is not Level)
            yield return null;

        level = Engine.Scene as Level;

        while ((player = level?.Tracker.GetEntity<Player>()) == null)
            yield return null;

        // wait for the animation to play out
        int i = 30;
        while (i-- != 0)
            yield return null;

        // wait for the new player to spawn
        IsTransitioning = false;
    }

    private static IEnumerator ScanRoom(Player initialPlayer)
    {
        Player player = initialPlayer;
        Level level = player.level;

        string initialRoom = level.Session.Level;

        while (RoomQueue.Count != 0)
        {
            string roomName = RoomQueue.Dequeue();

            if (level.Session.Level != roomName)
            {
                yield return ChangeRoom(level, player, roomName);
                if (!_isRealRoom)
                    continue; // if just e.g. a filler room, skip to the next one
            }

            SilentScanner.BeforeScan(player);
            Rectangle bounds = level.Bounds;

            int w = bounds.Width / TileSize, h = bounds.Height / TileSize;
            int x = 0, y = 0;

            float camW = level.Camera.Right - level.Camera.Left;
            float camH = level.Camera.Bottom - level.Camera.Top;
            Vector2 camOffset = new Vector2(-camW / 2, -camH / 2);

            float prevTimeActive = level.TimeActive;
            float prevRawTimeActive = level.RawTimeActive;

            int index = 0;
            bool end = false;
            Vector2 prevPos = Vector2.Zero;
            while (!end && IsScanning)
            {
                // prepare position for the next screenshot and end detection (after the bottom right corner was reached)
                Vector2 relPos = new Vector2(x * TileSize + camW / 2, y * TileSize + camH / 2);
                float maxX = w * TileSize - camW / 2;
                float maxY = h * TileSize - camH / 2;

                end = true;
                if (relPos.X > maxX)
                {
                    x = 0;
                    y += ScreenshotToolModule.Settings.AdvancedSettings.VerticalScreenshotOffset;
                    relPos.X = maxX;
                }
                else
                {
                    x += ScreenshotToolModule.Settings.AdvancedSettings.HorizontalScreenshotOffset;
                    end = false;
                }

                if (relPos.Y > maxY)
                    relPos.Y = maxY;
                else
                    end = false;

                if (index != 0 && (int)relPos.X == (int)prevPos.X && (int)relPos.Y == (int)prevPos.Y)
                    continue;
                prevPos = relPos;

                // set up player and camera enforced positions
                Vector2 selfPos = bounds.Location.ToVector2() + relPos;
                Vector2 camPos = selfPos + camOffset;
                player.Position = selfPos;
                level.Camera.position = camPos;

                // wait for the game and the GPU to catch up
                for (int i = 0; i < ScreenshotToolModule.Settings.AdvancedSettings.WaitDelay; i++)
                {
                    yield return null;
                    player.Position = selfPos;
                    player.Speed.Y = 0;
                    level.Camera.position = camPos;
                }

                // Reset time if needed to hopefully freeze most animations.
                // Enforcing this every frame would give more consistent results, but it also breaks
                // some of the rendering // like hair, spinners and lightning (because of update groups) etc.
                if (ScreenshotToolModule.Settings.FreezeTime)
                {
                    level.TimeActive = prevTimeActive;
                    level.RawTimeActive = prevRawTimeActive;
                }

                TriggerRender(player, index++, relPos, false);

                // wait for the renderer to finish saving
                while (!ScreenshotRenderer.SavingFinished)
                    yield return null;
            }

            SilentScanner.AfterScan(player);

            yield return null;
        }

        if (initialRoom != level.Session.Level)
            yield return ChangeRoom(level, player, initialRoom);

        Audio.Play(SFX.game_gen_touchswitch_last_oneshot);
        IsScanning = false;
        _scannerHost = null;
    }

    public static void RunScanCoroutine(Player player)
    {
        if (_scannerHost?.Scene == null)
        {
            _scannerHost = new Entity
            {
                Tag = Tags.Persistent
            };
            player.level.Add(_scannerHost);
        }

        _scannerHost.Add(new Coroutine(RoomsScanner.ScanRoom(player)));
    }
}