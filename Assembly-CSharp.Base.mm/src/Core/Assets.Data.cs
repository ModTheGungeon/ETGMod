#pragma warning disable RECS0018

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;

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

    public int anchorX;
    public int anchorY;

    public static void ToTK2D(List<AssetSpriteData> list, out string[] names, out Rect[] regions, out Vector2[] anchors) {
        names = new string[list.Count];
        regions = new Rect[list.Count];
        anchors = new Vector2[list.Count];
        for (int i = 0; i < list.Count; i++) {
            // TODO
            AssetSpriteData item = list[i];
            names[i] = item.name;
            regions[i] = new Rect(item.x, item.y, item.width, item.height);
            anchors[i] = new Vector2(item.anchorX, item.anchorY);
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
        int texWidth = sprites.materials[0].mainTexture.width;
        int texHeight = sprites.materials[0].mainTexture.height;
        return new AssetSpriteData {
            name = separate ? null : frame.name,

            x = separate ? 0 : (int) Math.Floor(texWidth * frame.uvs[0].x),
            y = separate ? 0 : (int) Math.Floor(texHeight * frame.uvs[0].y),
            width = (int) Math.Ceiling(texWidth * (frame.uvs[3].x - frame.uvs[0].x)),
            height = (int) Math.Ceiling(texHeight * (frame.uvs[3].y - frame.uvs[0].y)),

            flip = frame.uvs[0].x == frame.uvs[1].x ? 1 : 0,

            anchorX = (int) Math.Round(frame.boundsDataCenter.x * texWidth * (frame.uvs[3].x - frame.uvs[0].x)),
            anchorY = (int) Math.Round(frame.boundsDataCenter.y * texHeight * (frame.uvs[3].y - frame.uvs[0].y))
        };
    }

}
