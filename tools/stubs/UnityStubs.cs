// Hand-written, minimal stand-ins for the Unity APIs used by this project. They exist ONLY to
// type-check our own C# under Mono (typos, wrong types, missing usings). They are written from
// memory of the real API, so they are NOT proof that the real Unity compiler accepts the code.
#pragma warning disable 0067, 0169, 0649, 0414
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void DestroyImmediate(Object o) { }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object { return null; }
        public static implicit operator bool(Object o) { return o != null; }
    }
    public enum FindObjectsSortMode { None, InstanceID }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum LightType { Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public enum CameraClearFlags { Skybox, SolidColor }
    public enum Space { World, Self }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public enum SleepTimeout_ { }
    public static class SleepTimeout { public const int NeverSleep = -1; public const int SystemSetting = -2; }
    public enum KeyCode { A, D, S, W, E, Return, Escape, LeftArrow, RightArrow, UpArrow, DownArrow }
    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }
    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay }
    public enum ColorSpace { Gamma, Linear }
    public enum UIOrientation { Portrait, LandscapeLeft, AutoRotation }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public float magnitude { get { return 0; } }
        public float sqrMagnitude { get { return 0; } }
        public static Vector2 ClampMagnitude(Vector2 v, float m) { return v; }
        public static Vector2 operator /(Vector2 a, float d) { return a; }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0; }
        public static Vector3 zero { get { return new Vector3(); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public float magnitude { get { return 0; } }
        public float sqrMagnitude { get { return 0; } }
        public Vector3 normalized { get { return this; } }
        public void Normalize() { }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) { return v; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return a; }
        public static float Dot(Vector3 a, Vector3 b) { return 0; }
        public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s) { return c; }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float d) { return a; }
        public static Vector3 operator /(Vector3 a, float d) { return a; }
        public static bool operator ==(Vector3 a, Vector3 b) { return true; }
        public static bool operator !=(Vector3 a, Vector3 b) { return false; }
        public override bool Equals(object o) { return true; }
        public override int GetHashCode() { return 0; }
    }
    public struct Quaternion
    {
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
        public static Quaternion LookRotation(Vector3 f, Vector3 u) { return new Quaternion(); }
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) { return a; }
        public static Vector3 operator *(Quaternion q, Vector3 v) { return v; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
    }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public struct Ray { }
    public struct RaycastHit { public float distance; }
    public struct Touch { public int fingerId; public TouchPhase phase; public Vector2 position; public Vector2 deltaPosition; }

    public static class Mathf
    {
        public const float PI = 3.14159f;
        public static float Clamp01(float v) { return v; }
        public static float Clamp(float v, float a, float b) { return v; }
        public static float Lerp(float a, float b, float t) { return a; }
        public static float InverseLerp(float a, float b, float v) { return a; }
        public static float MoveTowards(float a, float b, float d) { return a; }
        public static float Sin(float f) { return f; }
        public static float Cos(float f) { return f; }
        public static float Sqrt(float f) { return f; }
        public static float Abs(float f) { return f; }
        public static float Max(float a, float b) { return a; }
        public static float Min(float a, float b) { return a; }
        public static int Max(int a, int b) { return a; }
        public static int FloorToInt(float f) { return 0; }
        public static int RoundToInt(float f) { return 0; }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public string tag;
        public T GetComponent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool inactive) { return null; }
    }
    public class Behaviour : Component { public bool enabled; public bool isActiveAndEnabled { get { return true; } } }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator e) { return null; }
        public void StopCoroutine(Coroutine c) { }
    }
    public class Coroutine { }
    public class GameObject : Object
    {
        public string tag;
        public Transform transform { get; }
        public bool isStatic;
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] comps) { }
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool inactive) { return null; }
        public void SetActive(bool b) { }
        public bool activeSelf { get { return true; } }
        public static GameObject CreatePrimitive(PrimitiveType t) { return null; }
    }
    public class Transform : Component, IEnumerable
    {
        public Vector3 position, localPosition, localScale, eulerAngles, forward;
        public Quaternion rotation, localRotation;
        public Transform parent;
        public int childCount { get; }
        public void SetParent(Transform p, bool worldPositionStays) { }
        public Transform GetChild(int i) { return null; }
        public Transform Find(string n) { return null; }
        public void Rotate(float x, float y, float z, Space s) { }
        public IEnumerator GetEnumerator() { return null; }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, offsetMin, offsetMax;
    }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 p, Camera c, out Vector2 local) { local = Vector2.zero; return true; }
    }
    public class Camera : Behaviour
    {
        public static Camera main { get; }
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public float nearClipPlane, farClipPlane;
    }
    public class AudioListener : Behaviour { }
    public class Light : Behaviour { public LightType type; public Color color; public float intensity; public LightShadows shadows; }
    public class Renderer : Behaviour { public Material sharedMaterial; public Material material; }
    public class Collider : Component { public bool enabled; }
    public class BoxCollider : Collider { public Vector3 size; }
    public class CapsuleCollider : Collider { public float height, radius; public Vector3 center; }
    public class CharacterController : Collider
    {
        public float height, radius, stepOffset, slopeLimit; public Vector3 center;
        public bool isGrounded { get; }
        public CollisionFlags Move(Vector3 m) { return 0; }
    }
    public enum CollisionFlags { None }
    public static class Physics
    {
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q) { hit = new RaycastHit(); return false; }
    }
    public class Shader : Object { public static Shader Find(string n) { return null; } }
    public class Material : Object
    {
        public Color color;
        public Material(Shader s) { }
    }
    public class Texture2D : Object
    {
        public TextureWrapMode wrapMode;
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixel(int x, int y, Color c) { }
        public void Apply() { }
    }
    public class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) { return null; }
    }
    public class Font : Object { public static Font CreateDynamicFontFromOSFont(string n, int s) { return null; } }
    public class TextAsset : Object { public string text; }
    public static class Resources { public static T Load<T>(string p) where T : Object { return null; } }
    public static class Application
    {
        public static string persistentDataPath { get; }
        public static int targetFrameRate;
        public static bool isBatchMode { get; }
        public static void Quit() { }
    }
    public static class Screen { public static int width; public static float dpi; public static int sleepTimeout; }
    public static class Time { public static float deltaTime, unscaledDeltaTime, timeScale; }
    public static class Input
    {
        public static int touchCount;
        public static Touch GetTouch(int i) { return new Touch(); }
        public static bool GetKey(KeyCode k) { return false; }
        public static bool GetKeyDown(KeyCode k) { return false; }
        public static bool GetMouseButton(int b) { return false; }
        public static bool GetMouseButtonDown(int b) { return false; }
        public static Vector3 mousePosition;
    }
    public static class Debug
    {
        public static void Log(object o) { }
        public static void LogWarning(object o) { }
        public static void LogError(object o) { }
    }
    public static class JsonUtility
    {
        public static T FromJson<T>(string s) { return default(T); }
        public static string ToJson(object o) { return ""; }
    }
    public static class RenderSettings { public static Rendering.AmbientMode ambientMode; public static Color ambientLight; }
    public class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; }
    public class CanvasGroup : Behaviour { public float alpha; public bool blocksRaycasts; }
    public class CanvasRenderer : Component { }
    public class CanvasScalerHolder { }
    public static class PlayerSettingsHolder { }
    public class ContextMenu : Attribute { }
    public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    public class WaitHolder { }
}
namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox, Trilight, Flat }
    public enum GraphicsDeviceType { OpenGLES3 }
}
namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public class UnityEvent { public void AddListener(UnityAction a) { } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public bool isLoaded; public GameObject[] GetRootGameObjects() { return null; } }
    public static class SceneManager { public static void LoadScene(string n) { } public static Scene GetActiveScene() { return new Scene(); } }
}
namespace UnityEngine.EventSystems
{
    public class PointerEventData { public Vector2 position; public Camera pressEventCamera; }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData e); }
    public interface IDragHandler { void OnDrag(PointerEventData e); }
    public interface IPointerUpHandler { void OnPointerUp(PointerEventData e); }
    public class EventSystem : UnityEngine.Behaviour { public static EventSystem current; public bool IsPointerOverGameObject(int id) { return false; } }
    public class StandaloneInputModule : UnityEngine.Behaviour { }
}
namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.Behaviour { public UnityEngine.Color color; public bool raycastTarget; public UnityEngine.RectTransform rectTransform { get; } }
    public class Image : Graphic { public UnityEngine.Sprite sprite; }
    public class Text : Graphic
    {
        public UnityEngine.Font font; public int fontSize; public UnityEngine.TextAnchor alignment; public string text;
        public UnityEngine.HorizontalWrapMode horizontalOverflow; public UnityEngine.VerticalWrapMode verticalOverflow;
        public bool resizeTextForBestFit; public int resizeTextMinSize, resizeTextMaxSize;
    }
    public class Button : UnityEngine.Behaviour
    {
        public Graphic targetGraphic; public bool interactable; public ButtonClickedEvent onClick;
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
    }
    public class CanvasScaler : UnityEngine.Behaviour
    {
        public enum ScaleMode { ScaleWithScreenSize }
        public enum ScreenMatchMode { MatchWidthOrHeight }
        public ScaleMode uiScaleMode; public ScreenMatchMode screenMatchMode;
        public UnityEngine.Vector2 referenceResolution; public float matchWidthOrHeight;
    }
    public class GraphicRaycaster : UnityEngine.Behaviour { }
    public class HorizontalLayoutGroup : UnityEngine.Behaviour
    {
        public float spacing; public bool childForceExpandWidth, childForceExpandHeight, childControlWidth, childControlHeight, reverseArrangement;
    }
}
namespace UnityEditor
{
    public class MenuItem : Attribute { public MenuItem(string p) { } }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object { return null; }
        public static void CreateAsset(UnityEngine.Object o, string p) { }
        public static bool IsValidFolder(string p) { return true; }
        public static string CreateFolder(string parent, string n) { return ""; }
        public static void SaveAssets() { }
        public static void Refresh() { }
    }
    public static class EditorUtility { public static void SetDirty(UnityEngine.Object o) { } }
    public static class GameObjectUtility
    {
        public static void SetStaticEditorFlags(UnityEngine.GameObject g, StaticEditorFlags f) { }
        public static int GetMonoBehavioursWithMissingScriptCount(UnityEngine.GameObject g) { return 0; }
    }
    [Flags] public enum StaticEditorFlags { BatchingStatic = 4 }
    public class EditorBuildSettingsScene { public string path; public bool enabled; public EditorBuildSettingsScene(string p, bool e) { } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public static class EditorApplication { public static void Exit(int c) { } }
    public static class EditorUserBuildSettings { public static bool buildAppBundle; }
    public enum BuildTarget { Android }
    public enum BuildTargetGroup { Android }
    public enum BuildOptions { None }
    public enum ScriptingImplementation { IL2CPP }
    public enum ManagedStrippingLevel { Low }
    [Flags] public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2, X86 = 4, X86_64 = 8, All = -1 }
    public enum AndroidSdkVersions { AndroidApiLevel24, AndroidApiLevelAuto }
    public class BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildTargetGroup targetGroup; public BuildOptions options; }
    public static class BuildPipeline { public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) { return null; } }
    public static class PlayerSettings
    {
        public static string companyName, productName, bundleVersion;
        public static ColorSpaceHolder dummy;
        public static UnityEngine.ColorSpace colorSpace;
        public static UnityEngine.UIOrientation defaultInterfaceOrientation;
        public static bool allowedAutorotateToPortrait, allowedAutorotateToPortraitUpsideDown, allowedAutorotateToLandscapeLeft, allowedAutorotateToLandscapeRight;
        public static void SetApplicationIdentifier(Build.NamedBuildTarget t, string id) { }
        public static void SetScriptingBackend(Build.NamedBuildTarget t, ScriptingImplementation s) { }
        public static void SetManagedStrippingLevel(Build.NamedBuildTarget t, ManagedStrippingLevel l) { }
        public static void SetUseDefaultGraphicsAPIs(BuildTarget t, bool b) { }
        public static void SetGraphicsAPIs(BuildTarget t, UnityEngine.Rendering.GraphicsDeviceType[] a) { }
        public static class Android
        {
            public static int bundleVersionCode;
            public static AndroidSdkVersions minSdkVersion, targetSdkVersion;
            public static AndroidArchitecture targetArchitectures;
            public static bool forceInternetPermission, forceSDCardPermission;
        }
    }
    public class ColorSpaceHolder { }
}
namespace UnityEditor.Build
{
    public struct NamedBuildTarget { public static NamedBuildTarget Android { get { return new NamedBuildTarget(); } } }
}
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed }
    public class BuildSummary { public BuildResult result; public int totalErrors; public string outputPath; public ulong totalSize; public TimeSpan totalTime; }
    public class BuildReport { public BuildSummary summary; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene }
    public enum NewSceneMode { Single }
    public enum OpenSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) { return new UnityEngine.SceneManagement.Scene(); }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string p) { return true; }
        public static UnityEngine.SceneManagement.Scene OpenScene(string p, OpenSceneMode m) { return new UnityEngine.SceneManagement.Scene(); }
        public static UnityEngine.SceneManagement.Scene GetActiveScene() { return new UnityEngine.SceneManagement.Scene(); }
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; }
    }
}
