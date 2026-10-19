// Minimal API stubs so mcs can type-check the game scripts outside Unity. Not runtime-accurate.
using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public string name; public static void Destroy(Object o) {} public static void DontDestroyOnLoad(Object o) {} public static T FindAnyObjectByType<T>() where T : Object => null; public static T FindAnyObjectByType<T>(FindObjectsInactive i) where T : Object => null; public static T FindObjectOfType<T>() where T : Object => null; public static T FindFirstObjectByType<T>() where T : Object => null; public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object => new T[0]; public static T Instantiate<T>(T o) where T : Object => o; public static implicit operator bool(Object o) => o != null; }
    public class Component : Object { public GameObject gameObject; public Transform transform; public T GetComponent<T>() => default(T); public T AddComponent<T>() => default(T); public T GetComponentInChildren<T>() => default(T); }
    public class Behaviour : Component { public bool enabled; }
    public enum FindObjectsSortMode { None, InstanceID }
    public class AudioClip : Object {}
    public class AudioSource : Behaviour { public bool playOnAwake, loop, isPlaying; public AudioClip clip; public float volume, panStereo, pitch; public void Play() {} public void Stop() {} }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = default(Color); return true; } }
    public enum FindObjectsInactive { Exclude, Include }
    public class CanvasGroup : Behaviour { public bool blocksRaycasts, interactable; public float alpha; }
    public class MonoBehaviour : Behaviour { public Coroutine StartCoroutine(IEnumerator e) => null; public void Invoke(string s, float t) {} }
    public class Coroutine {}
    public class GameObject : Object { public GameObject() {} public GameObject(string n) {} public GameObject(string n, params Type[] t) {} public Transform transform; public T AddComponent<T>() where T : Component => default(T); public T GetComponent<T>() => default(T); public T GetComponentInChildren<T>() => default(T); public void SetActive(bool b) {} public bool activeSelf; public bool activeInHierarchy; public static GameObject Find(string n) => null; }
    public class Transform : Component, IEnumerable { public Vector3 TransformPoint(Vector3 v) => v; public Vector3 position; public Vector3 localPosition; public Vector3 localScale; public Quaternion rotation; public Quaternion localRotation; public Transform parent; public void SetParent(Transform p, bool w = true) {} public void SetParent(Transform p) {} public int childCount; public Transform GetChild(int i) => null; public IEnumerator GetEnumerator() => null; public void SetAsLastSibling() {} public void SetAsFirstSibling() {} }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot, sizeDelta, anchoredPosition; }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero, one, up, down, left, right; public static Vector2 operator +(Vector2 a, Vector2 b) => a; public static Vector2 operator -(Vector2 a, Vector2 b) => a; public static Vector2 operator *(Vector2 a, float b) => a; public static implicit operator Vector3(Vector2 v) => new Vector3(); }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; } public static Vector3 zero, one, up, down, left, right, forward; public static Vector3 operator *(Vector3 a, float b) => a; public static Vector3 operator *(float b, Vector3 a) => a; public static Vector3 operator +(Vector3 a, Vector3 b) => a; public static Vector3 operator -(Vector3 a, Vector3 b) => a; public static implicit operator Vector2(Vector3 v) => new Vector2(); public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a; }
    public struct Vector2Int { public int x, y; public Vector2Int(int x, int y) { this.x = x; this.y = y; } }
    public struct Quaternion { public static Quaternion identity; public static Quaternion Euler(float x, float y, float z) => identity; }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white, black, red, green, blue, yellow, clear, gray, grey, cyan, magenta; public static Color Lerp(Color a, Color b, float t) { t = t < 0 ? 0 : t > 1 ? 1 : t; return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); } public static Color operator *(Color a, float b) => new Color(a.r * b, a.g * b, a.b * b, a.a * b); public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
    public struct Color32 { public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; } }
    public struct Rect { public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } public float x, y, width, height; }
    public static class Random { static readonly System.Random R = new System.Random(7); public static float value => (float)R.NextDouble(); public static int Range(int a, int b) => b > a ? R.Next(a, b) : a; public static float Range(float a, float b) => a + (float)R.NextDouble() * (b - a); }
    public static class Mathf { public static float Pow(float a, float b) => (float)System.Math.Pow(a, b); public static float Ceil(float f) => (float)System.Math.Ceiling(f); public static float Floor(float f) => (float)System.Math.Floor(f); public static float Repeat(float t, float l) => t - (float)System.Math.Floor(t / l) * l; public static float Clamp01(float v) => System.Math.Clamp(v, 0f, 1f); public static float Clamp(float v, float a, float b) => System.Math.Clamp(v, a, b); public static int Clamp(int v, int a, int b) => System.Math.Clamp(v, a, b); public static int FloorToInt(float f) => (int)System.Math.Floor(f); public static int RoundToInt(float f) => (int)System.Math.Round(f, System.MidpointRounding.ToEven); public static int CeilToInt(float f) => (int)System.Math.Ceiling(f); public static float Sign(float f) => f >= 0f ? 1f : -1f; public static float Abs(float f) => System.Math.Abs(f); public static int Abs(int f) => System.Math.Abs(f); public static float Max(float a, float b) => System.Math.Max(a, b); public static int Max(int a, int b) => System.Math.Max(a, b); public static float Min(float a, float b) => System.Math.Min(a, b); public static int Min(int a, int b) => System.Math.Min(a, b); public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t); public static float Sqrt(float f) => (float)System.Math.Sqrt(f); public static float Exp(float f) => (float)System.Math.Exp(f); public const float PI = 3.14159265f; public static float Sin(float f) => (float)System.Math.Sin(f); public static float Cos(float f) => (float)System.Math.Cos(f); public static float Round(float f) => (float)System.Math.Round(f, System.MidpointRounding.ToEven); public static float Atan2(float y, float x) => (float)System.Math.Atan2(y, x); public const float Deg2Rad = PI / 180f, Rad2Deg = 180f / PI; public static float MoveTowards(float c, float t, float d) => System.Math.Abs(t - c) <= d ? t : c + System.Math.Sign(t - c) * d; }
    public static class Debug { public static void Log(object o) {} public static void LogWarning(object o) {} public static void LogError(object o) {} public static void LogException(Exception e) {} }
    public static class Time { public static float deltaTime, time, unscaledDeltaTime, unscaledTime; }
    public static class Application { public static string persistentDataPath; public static void Quit() {} public static bool isPlaying; }
    public static class Resources { public static System.Func<string, System.Type, Object> Hook; public static T Load<T>(string p) where T : Object => Hook?.Invoke(p, typeof(T)) as T; public static T[] FindObjectsOfTypeAll<T>() where T : Object => new T[0]; }
    public class TextAsset : Object { public byte[] bytes; public string text; }
    public enum TextureWrapMode { Repeat, Clamp }
    public static class ImageConversion
    {
        // test-only format: "RAW1" + int32 w + int32 h + RGBA rows bottom-up (what Unity's GetPixels32 returns)
        public static bool LoadImage(this Texture2D t, byte[] data)
        {
            if (data == null || data.Length < 12 || data[0] != 'R') return false;
            t.width = System.BitConverter.ToInt32(data, 4); t.height = System.BitConverter.ToInt32(data, 8);
            t.px = new Color32[t.width * t.height];
            for (int i = 0; i < t.px.Length; i++) t.px[i] = new Color32(data[12 + i * 4], data[13 + i * 4], data[14 + i * 4], data[15 + i * 4]);
            return true;
        }
    }
    public enum SpriteMeshType { FullRect, Tight }
    public class Sprite : Object { public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => null; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m) => null; public Texture2D texture; public Rect rect; }
    public class Texture : Object { public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode; }
    public class Texture2D : Texture { public Color32[] px; public Texture2D(int w, int h) { width = w; height = h; } public Texture2D(int w, int h, TextureFormat f, bool m) { width = w; height = h; } public void SetPixel(int x, int y, Color c) {} public void Apply() {} public Color GetPixel(int x, int y) => default(Color); public Color32[] GetPixels32() => px; public void SetPixels32(Color32[] p) { px = p; } }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public class Renderer : Component { public int sortingOrder; public bool enabled; }
    public enum SpriteDrawMode { Simple, Sliced, Tiled }
    public class SpriteRenderer : Renderer { public Sprite sprite; public Color color; public bool flipX, flipY; public SpriteDrawMode drawMode; public Vector2 size; }
    public class Camera : Behaviour { public static Camera main; public bool orthographic; public float orthographicSize; public Color backgroundColor; public Vector3 ScreenToWorldPoint(Vector3 v) => v; public Vector3 WorldToScreenPoint(Vector3 v) => v; public float aspect; }
    public class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; public Camera worldCamera; public float scaleFactor = 1f; }
    public static class RectTransformUtility { public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 p, Camera c) => false; }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Font : Object {}
    public static class PlayerPrefs { public static string GetString(string k, string d = "") => d; public static void SetString(string k, string v) {} public static int GetInt(string k, int d = 0) => d; public static void SetInt(string k, int v) {} public static void Save() {} public static bool HasKey(string k) => false; public static void DeleteKey(string k) {} }
    public static class JsonUtility { public static string ToJson(object o, bool p = false) => ""; public static T FromJson<T>(string s) => default(T); }
    public class SerializeField : Attribute {}
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) {} }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() {} public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, SubsystemRegistration }
    public class TextAnchor {}
    public static class Screen { public static int width, height; }
}
namespace UnityEngine.EventSystems { public class EventSystem : UnityEngine.MonoBehaviour { public static EventSystem current; public bool IsPointerOverGameObject() => false; } public class StandaloneInputModule : UnityEngine.MonoBehaviour {} }
namespace UnityEngine.InputSystem
{
    public class Keyboard { public static Keyboard current; public KeyControl this[Key k] => null; public KeyControl escapeKey, spaceKey, enterKey, tabKey, iKey, jKey, mKey, rKey, eKey, fKey, gKey, qKey, cKey, lKey, kKey, oKey, pKey, sKey, tKey, wKey, aKey, dKey, bKey, nKey, vKey, xKey, zKey, hKey, yKey, uKey, digit1Key, digit2Key, digit3Key, digit4Key, digit5Key, f5Key, f8Key, f9Key, leftArrowKey, rightArrowKey, upArrowKey, downArrowKey, f1Key; }
    public class KeyControl { public bool wasPressedThisFrame, isPressed; }
    public enum Key { None }
    public class Mouse { public static Mouse current; public ButtonControl leftButton, rightButton; public Vector2Control position, scroll; }
    public class ButtonControl { public bool wasPressedThisFrame, isPressed; }
    public class Vector2Control { public UnityEngine.Vector2 ReadValue() => default(UnityEngine.Vector2); }
}
namespace UnityEngine.InputSystem.UI { public class InputSystemUIInputModule : UnityEngine.MonoBehaviour {} }
namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.Behaviour { public UnityEngine.Color color; public bool raycastTarget; public UnityEngine.RectTransform rectTransform; }
    public class Image : Graphic { public UnityEngine.Sprite sprite; public bool preserveAspect; }
    public class Text : Graphic { public string text; }
    public class Selectable : UnityEngine.Behaviour { public bool interactable; public Graphic targetGraphic; }
    public class Button : Selectable { public ButtonClickedEvent onClick = new ButtonClickedEvent(); public class ButtonClickedEvent { public void AddListener(Action a) {} public void RemoveAllListeners() {} } }
    public class LayoutGroup : UnityEngine.Behaviour { public RectOffset padding; public UnityEngine.TextAnchor childAlignment; }
    public class HorizontalOrVerticalLayoutGroup : LayoutGroup { public bool childForceExpandHeight, childForceExpandWidth, childControlHeight, childControlWidth; public float spacing; }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup {}
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup {}
    public class LayoutElement : UnityEngine.Behaviour { public float preferredHeight, preferredWidth, minHeight, flexibleWidth, flexibleHeight; }
    public class ContentSizeFitter : UnityEngine.Behaviour { public FitMode verticalFit, horizontalFit; public enum FitMode { Unconstrained, PreferredSize, MinSize } }
    public class CanvasScaler : UnityEngine.Behaviour { public ScaleMode uiScaleMode; public UnityEngine.Vector2 referenceResolution; public float matchWidthOrHeight; public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize } }
    public class GraphicRaycaster : UnityEngine.Behaviour {}
    public class ScrollRect : UnityEngine.Behaviour { public UnityEngine.RectTransform content; public bool horizontal, vertical; public float verticalNormalizedPosition; }
    public class Mask : UnityEngine.Behaviour { public bool showMaskGraphic; }
    public class RectMask2D : UnityEngine.Behaviour {}
    public class RectOffset { public RectOffset() {} public RectOffset(int a, int b, int c, int d) {} }
}
namespace UnityEngine.Rendering
{
    public class SortingGroup : UnityEngine.Behaviour { public int sortingOrder; }
    // Batch 35: just enough of URP's Volume framework for ColorGrade
    public class RenderPipelineAsset : UnityEngine.ScriptableObject {}
    public static class GraphicsSettings { public static RenderPipelineAsset currentRenderPipeline; }
    public class VolumeParameter<T> { public T value; public bool overrideState; }
    public class ClampedFloatParameter : VolumeParameter<float> {}
    public class FloatParameter : VolumeParameter<float> {}
    public class ColorParameter : VolumeParameter<UnityEngine.Color> {}
    public class Vector2Parameter : VolumeParameter<UnityEngine.Vector2> {}
    public class Vector4Parameter : VolumeParameter<UnityEngine.Vector4> {}
    public class VolumeComponent : UnityEngine.ScriptableObject { public bool active = true; }
    public class VolumeProfile : UnityEngine.ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent, new() => new T(); }
    public class Volume : UnityEngine.Behaviour { public bool isGlobal; public float priority; public VolumeProfile profile; }
}
namespace UnityEngine.Rendering.Universal
{
    public class ColorAdjustments : UnityEngine.Rendering.VolumeComponent { public UnityEngine.Rendering.FloatParameter postExposure = new UnityEngine.Rendering.FloatParameter(); public UnityEngine.Rendering.ClampedFloatParameter contrast = new UnityEngine.Rendering.ClampedFloatParameter(), hueShift = new UnityEngine.Rendering.ClampedFloatParameter(), saturation = new UnityEngine.Rendering.ClampedFloatParameter(); public UnityEngine.Rendering.ColorParameter colorFilter = new UnityEngine.Rendering.ColorParameter(); }
    public class LiftGammaGain : UnityEngine.Rendering.VolumeComponent { public UnityEngine.Rendering.Vector4Parameter lift = new UnityEngine.Rendering.Vector4Parameter(), gamma = new UnityEngine.Rendering.Vector4Parameter(), gain = new UnityEngine.Rendering.Vector4Parameter(); }
    public class Vignette : UnityEngine.Rendering.VolumeComponent { public UnityEngine.Rendering.ColorParameter color = new UnityEngine.Rendering.ColorParameter(); public UnityEngine.Rendering.Vector2Parameter center = new UnityEngine.Rendering.Vector2Parameter(); public UnityEngine.Rendering.ClampedFloatParameter intensity = new UnityEngine.Rendering.ClampedFloatParameter(), smoothness = new UnityEngine.Rendering.ClampedFloatParameter(); }
    public class UniversalAdditionalCameraData : UnityEngine.MonoBehaviour { public bool renderPostProcessing; }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this UnityEngine.Camera c) => new UniversalAdditionalCameraData(); }
}
namespace TMPro
{
    public class TMP_Text : UnityEngine.UI.Graphic { public TMP_FontAsset font; public float characterSpacing, outlineWidth; public UnityEngine.Color32 outlineColor; public string text; public float fontSize; public TextAlignmentOptions alignment; public bool enableWordWrapping; public TextOverflowModes overflowMode; public FontStyles fontStyle; public bool richText; public bool textWrappingMode_dummy; public TextWrappingModes textWrappingMode; }
    public class TextMeshProUGUI : TMP_Text {}
    public class TMP_FontAsset : UnityEngine.Object { public static TMP_FontAsset CreateFontAsset(UnityEngine.Font f) => null; }
    public enum TextAlignmentOptions { Left, Center, Right, TopLeft, Top, TopRight, MidlineLeft, Midline, BottomLeft, Bottom, CenterGeoAligned, Justified }
    public enum TextOverflowModes { Overflow, Ellipsis, Truncate, ScrollRect }
    public enum TextWrappingModes { NoWrap, Normal }
    public enum FontStyles { Normal, Bold, Italic }
}
