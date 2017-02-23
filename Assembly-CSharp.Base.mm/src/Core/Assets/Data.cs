#pragma warning disable RECS0018

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;
using AttachPoint = tk2dSpriteDefinition.AttachPoint;

public class AssetDirectory { private AssetDirectory() { } }

public struct AssetSpriteData {
    // Leshy SpriteSheet Tool - https://www.leshylabs.com/apps/sstool/
    public string name;

    public int x;
    public int y;
    public int width;
    public int height;

    // Custom extensions for tk2d compatibility
    public int flip;

    public AttachPoint[] attachPoints;

    public static void ToTK2D(List<AssetSpriteData> list, out string[] names, out Rect[] regions, out Vector2[] anchors, out AttachPoint[][] attachPoints) {
        names = new string[list.Count];
        regions = new Rect[list.Count];
        anchors = new Vector2[list.Count];
        attachPoints = new AttachPoint[list.Count][];
        for (int i = 0; i < list.Count; i++) {
            // TODO
            AssetSpriteData item = list[i];
            names[i] = item.name;
            regions[i] = new Rect(item.x, item.y, item.width, item.height);
            anchors[i] = new Vector2(item.width / 2f, item.height / 2f);
            attachPoints[i] = item.attachPoints;
        }
    }

    public static List<AssetSpriteData> FromTK2D(tk2dSpriteCollectionData sprites) {
        List<AssetSpriteData> list = new List<AssetSpriteData>(sprites.spriteDefinitions.Length);
        for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
            tk2dSpriteDefinition frame = sprites.spriteDefinitions[i];
            list.Add(FromTK2D(sprites, frame));
        }
        return list;
    }

    public static AssetSpriteData FromTK2D(tk2dSpriteCollectionData sprites, tk2dSpriteDefinition frame, bool separate = false) {
        if (sprites.materials[0]?.mainTexture == null) {
            return new AssetSpriteData {
                name = separate ? "INCOMPLETE" : (frame.name + "_INCOMPLETE")
            };
        }
        int texWidth = sprites.materials[0].mainTexture.width;
        int texHeight = sprites.materials[0].mainTexture.height;
        return new AssetSpriteData {
            name = separate ? null : frame.name,

            x = separate ? 0 : (int) Math.Floor(texWidth * frame.uvs[0].x),
            y = separate ? 0 : (int) Math.Floor(texHeight * frame.uvs[0].y),
            width = (int) Math.Ceiling(texWidth * (frame.uvs[3].x - frame.uvs[0].x)),
            height = (int) Math.Ceiling(texHeight * (frame.uvs[3].y - frame.uvs[0].y)),

            flip = frame.uvs[0].x == frame.uvs[1].x ? 1 : 0,

            attachPoints = sprites.GetAttachPoints(sprites.spriteDefinitions.IndexOf(frame)) ?? new AttachPoint[0]
        };
    }

}

public class JSONAttachPointRule : JSONValueTypeBaseRule<AttachPoint> { }
