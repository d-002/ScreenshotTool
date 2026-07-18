#!/bin/sh

TARGET=ScreenshotTool.zip
rm -f "$TARGET"
zip "$TARGET" bin/ScreenshotTool.dll bin/ScreenshotTool.pdb everest.yaml
