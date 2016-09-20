#pragma warning disable RECS0018

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;
using AttachPoint = tk2dSpriteDefinition.AttachPoint;

public delegate void ComponentHook(Component component);
public static partial class ETGMod {

    /// <summary>
    /// ETGMod object configuration.
    /// </summary>
    public static class Objects {

        public static int FramesToHandleAllObjectsIn = 16;
        public static int FramesToHandleAllSpritesIn = 16;

        public static Dictionary<Type, ComponentHook> Hooks = new Dictionary<Type, ComponentHook>() {
            { typeof(tk2dBaseSprite),       _HookTK2DSprite },
            { typeof(tk2dClippedSprite),    _HookTK2DSprite },
            { typeof(tk2dSlicedSprite),     _HookTK2DSprite },
            { typeof(tk2dSprite),           _HookTK2DSprite },
            { typeof(tk2dTiledSprite),      _HookTK2DSprite },

            { typeof(dfSprite),         _HookDFSprite },
            { typeof(dfRadialSprite),   _HookDFSprite },
            { typeof(dfSlicedSprite),   _HookDFSprite },
            { typeof(dfTiledSprite),    _HookDFSprite },
        };

        private static void _HookTK2DSprite(Component c) {
            ((tk2dBaseSprite) c).HandleAuto();
        }

        private static void _HookDFSprite(Component c) {
            ((dfSprite) c).Atlas.HandleAuto();
        }

        public static void HookUnity() {
            ETGModUnityEngineHooks.Construct = HandleObjectAutoConstruct;
            ETGModUnityEngineHooks.Instantiate = HandleObjectAutoInstantiate;
            ETGModUnityEngineHooks.AddComponent = HandleObjectAutoAddComponent;
        }

        public static void AddHook<T>(ComponentHook hook) where T : Component {
            AddHook(typeof(T), hook);
        }
        public static void AddHook(Type type, ComponentHook hook) {
            ComponentHook all;
            if (Hooks.TryGetValue(type, out all)) {
                all += hook;
            } else {
                all = hook;
            }
            Hooks[type] = hook;
        }

        public static ComponentHook GetHooks<T>() where T : Component {
            return GetHooks(typeof(T));
        }
        public static ComponentHook GetHooks(Type type) {
            ComponentHook all;
            if (Hooks.TryGetValue(type, out all)) {
                return all;
            }
            return null;
        }

        public static void HandleGameObject(GameObject go, bool recursive = true) {
            if (go == null) return;

            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++) {
                HandleComponent(components[i]);
            }

            if (!recursive) return;
            int children = go.transform.childCount;
            for (int i = 0; i < children; i++) {
                HandleGameObject(go.transform.GetChild(i).gameObject, true);
            }
        }

        public static void HandleComponent(Component c) {
            GetHooks(c.GetType())?.Invoke(c);
        }

        public static void HandleObject(UnityEngine.Object o) {
            if (o == null) return;

            if (o is GameObject) {
                HandleGameObject((GameObject) o, true);
                return;
            }

            if (o is Component) {
                HandleComponent((Component) o);
                return;
            }
        }

        public static void HandleAll() {
            StartGlobalCoroutine(HandleAllObjects());
            StartGlobalCoroutine(HandleAllSprites());
        }
        private static IEnumerator HandleAllObjects() {
            Transform[] transforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            int handleUntilYield = transforms.Length / FramesToHandleAllObjectsIn;
            int handleUntilYieldM1 = handleUntilYield - 1;
            for (int i = 0; i < transforms.Length; i++) {
                try {
                    HandleGameObject(transforms[i]?.gameObject, false);
                } catch (NullReferenceException) { /* Unity shit itself internally. */ }
                if (i % handleUntilYield == handleUntilYieldM1) yield return null;
            }
        }
        private static IEnumerator HandleAllSprites() {
            tk2dSpriteCollectionData[] atlases = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>();
            int handleUntilYield = atlases.Length / FramesToHandleAllSpritesIn;
            int handleUntilYieldM1 = handleUntilYield - 1;
            for (int i = 0; i < atlases.Length; i++) {
                Assets.HandleSprites(atlases[i]);
                if (i % handleUntilYield == handleUntilYieldM1) yield return null;
            }
        }

    }

    // Extension methods

    public static void HandleObject(this UnityEngine.Object obj) {
        Objects.HandleObject(obj);
    }
    public static void HandleObjectAutoConstruct(this UnityEngine.Object obj) {
        _HandleAuto(obj.HandleObject);
    }
    public static UnityEngine.Object HandleObjectAutoInstantiate(this UnityEngine.Object obj) {
        _HandleAuto(obj.HandleObject);
        return obj;
    }
    public static Component HandleObjectAutoAddComponent(this Component c) {
        _HandleAuto(c.HandleObject);
        return c;
    }

    private static void _HandleAuto(Action a) {
        if (ETGModGUI.TestTexture == null && false) {
            StartGlobalCoroutine(_HandleAutoCoroutine(a));
            return;
        }
        a();
    }
    private static IEnumerator _HandleAutoCoroutine(Action a) {
        yield return new WaitUntil(() => ETGModGUI.TestTexture != null);

        a();
    }

}
