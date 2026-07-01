# ScreenshotTool

Take and store screenshots of the game's screen, an entire room or chapter.

> [!WARNING]  
> This mod generates a lot of lag when exporting, you should consider
> temporarily disabling [LagPauser](https://gamebanana.com/mods/591485) in its
> mod options if you use it.

## Gallery

The first screen of 1C, with a transparent background
![The first screen of 1C with a transparent background](https://raw.githubusercontent.com/d-002/ScreenshotTool/refs/heads/main/images/1c.png)

The entire Strawberry Jam Grandmaster lobby as a merged room scan (later
downscaled so it takes less disk space)
![The entire Strawberry Jam Grandmaster lobby](https://raw.githubusercontent.com/d-002/ScreenshotTool/refs/heads/main/images/gm.png)

## Exported files

Files are stored in

    [everest path]/[custom export path]/[map name]/[side]/[room name]/*.png

Standalone screenshots are named with the current date.
For room scans (and by extension chapter scans which just move the player to all
the chapter's rooms then run a room scan in each of them), see the next section.

## Room scanning

When taking screenshots of a room or chapter, multiple screenshots will be taken
so as to capture the entirety of the room.
They will be named with an index and the relative position in the room, to make
the construction of a large image possible.

For example, a file might be named `0000,dx=160,dy=86.png` :

- The `dx` and `dy` values map the position of the center of the camera relative
  to the room's location (`+x` is right, `+y` is down).
- The index at the start is just here so that image viewers sort the images in a
  reliable order, it does not carry any extra useful information.

To help with merging, upon exporting a room the export directory contains a JSON
file with the following information:

```json
{
    "roomPosition": [528, -88],
    "roomSize": [944, 180],
    "cameraSize": [320, 180],
    "viewPort": [1920, 1080]
}
```

For example, this file was generated when scanning the second room of 1C in
vanilla Celeste.

A sample room merge script (merge.py) can be found in the code to better
understand what is meant by merging room screenshots.
Simply place it in a room's export directory then run it with Python.

Keep in mind some image viewers (like your browser) might crash when trying to
display images of very large rooms (like Farewell Farewell).

## Additional room/chapter scan features

When scanning a room, the player is hidden and has no collision, to avoid side
effects like pufferfishes exploding.

It is also possible to remove the foreground effects to avoid parallax issues
when merging, make the background transparent, or freeze time.

**Freezing time can lead to unexpected behavior** as things like unloaded
lightning, spinners, cutscene triggers may update incorrectly.
This can also happen when the time is not frozen, which is why an option is
available to wait some time in between screenshots and allow the game to catch
up.

Taking a screenshot with a transparent background is, as far as I know,
impossible.
To circumvent that, when scanning a room with transparent background enabled,
two screenshots are taken at a time, with differently colored backgrounds.
They are then compared to infer transparency.
This means the render might not be 100% accurate.

For now, the game is captured as-is, this means UI elements like the timer
remain visible in screenshots.
You are therefore currently advised to turn this off when scanning entire rooms.

## Mods

Mods should be supported, but since they are free to code whatever they want
some features of ScreenshotTool might not work properly.

For example, while taking the Strawberry Jam lobby image I noticed they added
decorative bubbles in the water, that are coded differently from other entities:
when enabling ScreenshotTool's Time freeze, their animation was not halted,
introducinng jitteriness in the merged image.

I will **not** be explicitely making other mods' features compatible, since it
would require more effort than I am willing to give to please all requests, read
and interface with the code of other mods (that is usually not designed for
this) and keep up with the updates and breaking changes in the code of such
mods.

If you believe a feature is missing or broken, you are however welcome to
opening an issue.

Happy screenshotting!

## Known bugs / backlog

- Crash at the end of 5A with the big eye
