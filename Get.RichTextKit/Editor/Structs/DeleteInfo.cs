﻿namespace Get.RichTextKit.Editor.Structs;

public enum DeleteModes : byte
{
    Selection = default,
    Forward,
    Backward
}
public record struct DeleteInfo(TextRange Range, DeleteModes DeleteMode)
{
}