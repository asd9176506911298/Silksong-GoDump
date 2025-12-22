using BepInEx;
using UnityEngine.Windows;
using UnityEngine;
using Input = UnityEngine.Input;
using static UnityEngine.RectTransform;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using GODump;
using static GODump.SpriteDump;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace GoDump
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class GoDump : BaseUnityPlugin
    {
        public static GoDump Instance { get; private set; }

        private const string modGUID = "Yuki.GoDump";
        private const string modName = "GoDump";
        private const string modVersion = "1.0.0";

        private static string _spritePath = "";
        private static string _atlasPath = "";


        private List<tk2dSpriteCollectionData> clns;
        private List<tk2dSpriteAnimation> anims;
        private tk2dSpriteAnimation anim = null;
        private string[] animNames;
        private int num;

        private ConfigEntry<string> dumpAnimName;
        private ConfigEntry<string> exportPath;
        private ConfigEntry<bool> logCurrentClipId;
        private ConfigEntry<KeyCode> logClnKey;
        private ConfigEntry<KeyCode> dumpClnKey;
        public ConfigEntry<bool> isDrawRectBorder;

        private string currClipAndId = "";

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {modGUID} is loaded!");

            dumpAnimName = Config.Bind("", "dumpAnimName", "");
            exportPath = Config.Bind("", "exportPath", "");
            logCurrentClipId = Config.Bind("", "LogCurrentClipId", false);
            logClnKey = Config.Bind("Key", "logClnKey", KeyCode.F3);
            dumpClnKey = Config.Bind("Key", "dumpClnKey", KeyCode.F4);
            isDrawRectBorder = Config.Bind("", "isDrawRectBorder", true);
        }

        private void Start()
        {
            Instance = this;

            clns = new List<tk2dSpriteCollectionData>();
            anims = new List<tk2dSpriteAnimation>();
            animNames = new string[] { };
        }

        private void Update()
        {
            if(Input.GetKeyDown(logClnKey.Value))
            {
                Logger.LogInfo("Log tk2dSpriteAnimation Start");
                Logger.LogInfo("= = = = = = = = = = = = = = = =");
                foreach (var x in Resources.FindObjectsOfTypeAll<tk2dSpriteAnimation>())
                {
                    Logger.LogInfo(x.name);
                    
                }
                Logger.LogInfo("= = = = = = = = = = = = = = = =");
                Logger.LogInfo("Log tk2dSpriteAnimation End");
            }

            if (Input.GetKeyDown(dumpClnKey.Value))
            {
                if (exportPath.Value == "")
                {
                    Logger.LogInfo($"export Path is Empty");
                    return;
                }
                if (!Directory.Exists(exportPath.Value))
                {
                    Logger.LogInfo($"Export Path: {exportPath.Value} not exist");
                    return;
                }
                clns.Clear();
                anims.Clear();
                //var animation = HeroController.instance.GetComponent<tk2dSpriteAnimator>().Library;
                var animation = getAnimation(dumpAnimName.Value);
                anims.Add(animation);
                animNames = anims.Select(a => a.name).ToArray();
                StartCoroutine(HornetSprite());
            }

            if (HeroController.instance && logCurrentClipId.Value)
            {
                if (HeroController.instance.GetComponent<tk2dSpriteAnimator>())
                {
                    var newClipAndId = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name + HeroController.instance.GetComponent<tk2dSprite>().spriteId;
                    if (currClipAndId != newClipAndId)
                    {
                        currClipAndId = newClipAndId;
                        Logger.LogInfo(newClipAndId);
                    }
                }
            }
        }

        private IEnumerator HornetSprite()
        {
            num = 0;
            foreach (var animL in anims)
            {
                if (animNames.Contains(animL.name))
                {
                    int i = 0;
                    SpriteInfo spriteInfo = new SpriteInfo();
                    Logger.LogInfo("Begin Dumping sprites in tk2dSpriteAnimator [" + animL.name + "].");
                    foreach (tk2dSpriteAnimationClip clip in animL.clips)
                    {
                        Logger.LogInfo(clip.name);
                        //if (clip.name == "Dash")
                        //    break;

                        i++;
                        int j = -1;
                        float Xmax = -10000f;
                        float Ymax = -10000f;
                        float Xmin = 10000f;
                        float Ymin = 10000f;
                        foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                        {
                            tk2dSpriteDefinition tk2DSpriteDefinition = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                            Vector3[] pos = tk2DSpriteDefinition.positions;

                            float xmin = pos.Min(v => v.x);
                            float ymin = pos.Min(v => v.y);
                            float xmax = pos.Max(v => v.x);
                            float ymax = pos.Max(v => v.y);

                            Xmin = Xmin < xmin ? Xmin : xmin;
                            Ymin = Ymin < ymin ? Ymin : ymin;
                            Xmax = Xmax > xmax ? Xmax : xmax;
                            Ymax = Ymax > ymax ? Ymax : ymax;

                        }
                        foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                        {
                            j++;

                            if (clip.name == "")
                                continue;


                            //if (clip.name == "")
                            //{
                            //    clip.name = "Focus Air";
                            //}

                            tk2dSpriteDefinition tk2DSpriteDefinition = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                            Vector2[] uv = tk2DSpriteDefinition.uvs;
                            Vector3[] pos = tk2DSpriteDefinition.positions;
                            Texture texture = tk2DSpriteDefinition.material.mainTexture;
                            Texture2D texture2D = SpriteDump.TextureReadHack((Texture2D)texture);

                            string collectionname = frame.spriteCollection.spriteCollectionName + "_" + tk2DSpriteDefinition.material.mainTexture.name;
                            string path = exportPath.Value + "/sprites/" + animL.name + "/0.Atlases/" + collectionname + ".png";
                            string path0 = exportPath.Value + "/sprites/" + animL.name + "/" + String.Format("{0:D3}", i) + "." + clip.name + "/" + collectionname + ".png";
                            string path1 = exportPath.Value + "/sprites/" + animL.name + "/" + String.Format("{0:D3}", i) + "." + clip.name + "/" + String.Format("{0:D3}", i) + "-" + String.Format("{0:D2}", j) + "-" + String.Format("{0:D3}", frame.spriteId) + "_position.png";
                            string path2r = animL.name + "/" + String.Format("{0:D3}", i) + "." + clip.name + "/" + String.Format("{0:D3}", i) + "-" + String.Format("{0:D2}", j) + "-" + String.Format("{0:D3}", frame.spriteId) + ".png";
                            string path2 = exportPath.Value + "/sprites/" + path2r;

                            bool flipped = tk2DSpriteDefinition.flipped == tk2dSpriteDefinition.FlipMode.Tk2d;

                            float xmin = pos.Min(v => v.x);
                            float ymin = pos.Min(v => v.y);
                            float xmax = pos.Max(v => v.x);
                            float ymax = pos.Max(v => v.y);



                            int x1 = (int)(uv.Min(v => v.x) * texture2D.width);
                            int y1 = (int)(uv.Min(v => v.y) * texture2D.height);
                            int x2 = (int)(uv.Max(v => v.x) * texture2D.width);
                            int y2 = (int)(uv.Max(v => v.y) * texture2D.height);

                            // symmetry transformation
                            int x11 = x1;
                            int y11 = y1;
                            int x22 = x2;
                            int y22 = y2;
                            if (flipped)
                            {
                                x22 = y2 + x1 - y1;
                                y22 = x2 - x1 + y1;
                            }

                            int x3 = (int)((Xmin - Xmin) / tk2DSpriteDefinition.texelSize.x);
                            int y3 = (int)((Ymin - Ymin) / tk2DSpriteDefinition.texelSize.y);
                            int x4 = (int)((Xmax - Xmin) / tk2DSpriteDefinition.texelSize.x);
                            int y4 = (int)((Ymax - Ymin) / tk2DSpriteDefinition.texelSize.y);

                            int x5 = (int)((xmin - Xmin) / tk2DSpriteDefinition.texelSize.x);
                            int y5 = (int)((ymin - Ymin) / tk2DSpriteDefinition.texelSize.y);
                            int x6 = (int)((xmax - Xmin) / tk2DSpriteDefinition.texelSize.x);
                            int y6 = (int)((ymax - Ymin) / tk2DSpriteDefinition.texelSize.y);

                            RectP uvpixel = new RectP(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
                            RectP posborder = new RectP(x11 - x5 + x3, y11 - y5 + y3, x4 - x3 + 1, y4 - y3 + 1);
                            RectP uvpixelr = new RectP(x5 - x3, y5 - y3, x22 - x11 + 1, y22 - y11 + 1);


                            if (!File.Exists(path) && true) // GODump.instance.GlobalSettings.DumpAtlasOnce
                            {
                                SpriteDump.SaveTextureToFile(texture2D, path);
                                num++;
                            }
                            if (!File.Exists(path0) && false) // GODump.instance.GlobalSettings.DumpAtlasAlways
                            {
                                SpriteDump.SaveTextureToFile(texture2D, path0);
                                num++;
                            }
                            if (!File.Exists(path1) && false) // GODump.instance.GlobalSettings.DumpPosition
                            {
                                Texture2D subposition2D = SpriteDump.SubTexturePosition(texture2D, uvpixel);
                                SpriteDump.SaveTextureToFile(subposition2D, path1);
                                num++;
                                UnityEngine.Object.DestroyImmediate(subposition2D);
                            }
                            if (true) // GODump.instance.GlobalSettings.DumpSpriteInfo
                            {
                                spriteInfo.Add(frame.spriteId, x1, y1, uvpixelr.x, uvpixelr.y, uvpixelr.width, uvpixelr.height, collectionname, path2r, flipped);
                            }
                            if (!File.Exists(path2))
                            {
                                try
                                {
                                    // 檢查 uvpixel 是否超出 texture2D 範圍
                                    if (uvpixel.x < 0 || uvpixel.y < 0 ||
                                        uvpixel.x + uvpixel.width > texture2D.width ||
                                        uvpixel.y + uvpixel.height > texture2D.height)
                                    {
                                        Logger.LogWarning($"Texture rectangle out of bounds for {path2r}. Adjusting uvpixel.");
                                        // 裁剪 uvpixel 範圍以確保不超出紋理尺寸
                                        uvpixel.x = Mathf.Max(0, uvpixel.x);
                                        uvpixel.y = Mathf.Max(0, uvpixel.y);
                                        uvpixel.width = Mathf.Min(uvpixel.width, texture2D.width - uvpixel.x);
                                        uvpixel.height = Mathf.Min(uvpixel.height, texture2D.height - uvpixel.y);

                                        // 如果裁剪後寬高無效，跳過處理
                                        if (uvpixel.width <= 0 || uvpixel.height <= 0)
                                        {
                                            Logger.LogError($"Invalid texture rectangle after adjustment for {path2r}. Skipping.");
                                            continue;
                                        }
                                    }

                                    Texture2D subtexture2D = SpriteDump.SubTexture(texture2D, uvpixel);
                                    if (flipped)
                                    {
                                        SpriteDump.Tk2dFlip(ref subtexture2D);
                                    }

                                    if (true) // SpriteSizeFix
                                    {
                                        Texture2D fixedtexture2D = SpriteDump.SpriteSizeFix(subtexture2D, uvpixelr, posborder);
                                        SpriteDump.SaveTextureToFile(fixedtexture2D, path2);
                                        UnityEngine.Object.DestroyImmediate(fixedtexture2D);
                                    }
                                    else
                                    {
                                        SpriteDump.SaveTextureToFile(subtexture2D, path2);
                                    }
                                    UnityEngine.Object.DestroyImmediate(subtexture2D);
                                    num++;
                                }
                                catch (Exception ex)
                                {
                                    Texture2D subtexture2D = SpriteDump.SubTexture(texture2D, uvpixel);
                                    if (flipped)
                                    {
                                        SpriteDump.Tk2dFlip(ref subtexture2D);
                                    }

                                    Texture2D fixedtexture2D = SpriteDump.SpriteSizeFixError(subtexture2D, uvpixelr, posborder);
                                    SpriteDump.SaveTextureToFile(fixedtexture2D, path2);
                                    UnityEngine.Object.DestroyImmediate(fixedtexture2D);
                                    Logger.LogInfo($"{ex.ToString()} {path2r}");
                                }
                            }
                            UnityEngine.Object.DestroyImmediate(texture2D);
                        }
                        //yield return new WaitForSeconds(1.0f);
                    }

                    string spriteinfopath = exportPath.Value + "/sprites/" + animL.name + "/0.Atlases/SpriteInfo.json";
                    if (!File.Exists(spriteinfopath) && true) // GODump.instance.GlobalSettings.DumpSpriteInfo
                    {
                        using (FileStream fileStream = File.Create(spriteinfopath))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(fileStream))
                            {
                                string value = JsonUtility.ToJson(spriteInfo, true);
                                streamWriter.Write(value);
                            }
                        }
                    }


                    Logger.LogInfo("End Dumping sprites in tk2dSpriteAnimator [" + animL.name + "].");

                }

            }


            Logger.LogInfo("End Dumping Sprite.png " + num + " sprites dumped.");
            yield break;
        }

        private List<tk2dSpriteAnimation> GetUsedIns(tk2dSpriteCollectionData cln, List<tk2dSpriteAnimation> anims)
        {
            List<tk2dSpriteAnimation> used = new List<tk2dSpriteAnimation>();
            foreach (tk2dSpriteAnimation anim in anims)
            {
                foreach (tk2dSpriteAnimationClip clip in anim.clips)
                {
                    foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                    {
                        if (frame.spriteCollection.name == cln.name && !used.Contains(anim))
                        {
                            used.Add(anim);
                        }
                    }
                }
            }
            return used;
        }

        private tk2dSpriteAnimation getAnimation(string name)
        {
            foreach (var x in Resources.FindObjectsOfTypeAll<tk2dSpriteAnimation>())
            {
                //Logger.LogInfo(x.name);
                if (x.name == name)
                    anim = x;
            }

            return anim;
        }

        public static void logInfo(string msg)
        {
            Instance.Logger.LogInfo(msg);
        }
    }
}
