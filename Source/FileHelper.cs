using System.IO;

namespace Celeste.Mod.ScreenshotTool;

public static class FileHelper
{
    public static string GetExistingPath(Player self, string fileName)
    {
        string roomName = self.level.Session.Level;
        string mapName = self.level.Session.Area.GetSID();
        char sideSuffix = (char)('A' + (int)self.level.Session.Area.Mode);
        string filePath = Path.Combine(Everest.PathGame, ScreenshotToolModule.Settings.ExportPath, mapName,
            sideSuffix.ToString(), roomName, fileName);

        string directory = Path.GetDirectoryName(filePath) ?? ".";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return filePath;
    }
}
