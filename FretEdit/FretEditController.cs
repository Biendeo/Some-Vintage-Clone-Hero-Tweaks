using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Reflection;
using System.IO;
using BepInEx;

namespace FretEdit
{
    [BepInPlugin("com.biendeo.vintage.fretedit", "Fret Edit", "1.0.0")]
    class FretEditController : BaseUnityPlugin
    {
        public void Awake()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp").GetTypes();
            fretAnimatorType = (from t in types where t.FullName.Equals("Fret_Animator") select t).FirstOrDefault();
            fretSpeedField = fretAnimatorType.GetField(fretSpeedFieldKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fretBaseField = fretAnimatorType.GetField(fretBaseFieldKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fretTopField = fretAnimatorType.GetField(fretTopFieldKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            SceneManager.activeSceneChanged += (_, __) => sceneChanged = true;
        }

        public void Update()
        {
            if (!isSettingKey)
            {
                keyButtonText = "Change Open Keybind";
                if (Input.GetKeyDown(keyBind.Value))
                {
                    showMenu = !showMenu;
                    if (showMenu)
                        wasCursorVisible = Cursor.visible;
                    else
                        Cursor.visible = wasCursorVisible;
                }
            }
            if (isSettingKey)
            {
                keyButtonText = "Cancel";
            }
        }

        public void LateUpdate()
        {
            if (sceneChanged)
            {
                if (fretAnimatorType != null)
                {
                    var scene = SceneManager.GetActiveScene();
                    if (string.Equals(scene.name, "Gameplay"))
                    {
                        fretObjects = FindObjectsOfType(fretAnimatorType);
                        UpdateFrets();
                    }
                    else
                    {
                        fretObjects = null;
                    }
                    sceneChanged = false;
                }
            }
        }

        public void OnEnable()
        {
            ReadConfig();
            maxFretSpeedText = maxFretSpeed.ToString();
            maxFretHeightText = maxFretHeight.ToString();
        }

        public void OnDisable()
        {
            WriteConfig();
        }

        public void OnGUI()
        {
            if (windowBackgrounds == null)
            {
                windowBackgrounds = new Texture2D[8];
                windowBackgrounds[0] = FixWindowBackground(GUI.skin.window.normal.background);
                windowBackgrounds[1] = FixWindowBackground(GUI.skin.window.active.background);
                windowBackgrounds[2] = FixWindowBackground(GUI.skin.window.hover.background);
                windowBackgrounds[3] = FixWindowBackground(GUI.skin.window.focused.background);
                windowBackgrounds[4] = FixWindowBackground(GUI.skin.window.onNormal.background);
                windowBackgrounds[5] = FixWindowBackground(GUI.skin.window.onActive.background);
                windowBackgrounds[6] = FixWindowBackground(GUI.skin.window.onHover.background);
                windowBackgrounds[7] = FixWindowBackground(GUI.skin.window.onFocused.background);
            }
            if (showMenu)
            {
                var windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.normal.background = windowBackgrounds[0];
                windowStyle.active.background = windowBackgrounds[1];
                windowStyle.hover.background = windowBackgrounds[2];
                windowStyle.focused.background = windowBackgrounds[3];
                windowStyle.onNormal.background = windowBackgrounds[4];
                windowStyle.onActive.background = windowBackgrounds[5];
                windowStyle.onHover.background = windowBackgrounds[6];
                windowStyle.onFocused.background = windowBackgrounds[7];
                windowRect = GUILayout.Window(hashCode, windowRect, OnWindow, "FretEdit", windowStyle);
            }
        }

        void OnWindow(int id)
        {
            if (id == hashCode)
            {
                if (fretAnimatorType != null && fretSpeedField != null && fretBaseField != null && fretTopField != null)
                {
                    GUILayout.Label("Fret speed: " + fretSpeed.ToString());
                    GUILayout.BeginHorizontal();
                    fretSpeed = GUILayout.HorizontalSlider(fretSpeed, 0, maxFretSpeed);
                    maxFretSpeedText = GUILayout.TextField(maxFretSpeedText, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Fret height: " + fretHeight.ToString());
                    GUILayout.BeginHorizontal();
                    fretHeight = GUILayout.HorizontalSlider(fretHeight, 0, maxFretHeight);
                    maxFretHeightText = GUILayout.TextField(maxFretHeightText, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                    if (float.TryParse(maxFretSpeedText, out var maxFretSpeedDesired))
                    {
                        if (maxFretSpeedDesired > 0)
                        {
                            maxFretSpeed = maxFretSpeedDesired;
                            fretSpeed = Mathf.Clamp(fretSpeed, 0, maxFretSpeed);
                        }
                    }
                    if (float.TryParse(maxFretHeightText, out var maxFretHeightDesired))
                    {
                        if (maxFretHeightDesired > 0)
                        {
                            maxFretHeight = maxFretHeightDesired;
                            fretHeight = Mathf.Clamp(fretHeight, 0, maxFretHeight);
                        }
                    }
                    UpdateFrets();
                }
                else
                {
                    GUILayout.Label("Component not found.\nIncompatible tweak version?");
                }
                if (GUILayout.Button(keyButtonText))
                {
                    isSettingKey = !isSettingKey;
                }
                if (isSettingKey)
                {
                    GUILayout.Label("Press any key...");
                    if (Event.current.isKey)
                    {
                        keyBind.Value = Event.current.keyCode;
                        isSettingKey = false;
                    }
                }
                GUILayout.Label("Current Key: " + keyBind.Value);
                GUILayout.Label("BepInEx port by Biendeo");
            }
            GUI.DragWindow();
        }

        void UpdateFrets()
        {
            if (fretObjects != null)
            {
                foreach (var o in fretObjects)
                {
                    fretSpeedField.SetValue(o, fretSpeed);
                    fretTopField.SetValue(o, ((float)fretBaseField.GetValue(o)) + fretHeight);
                }
            }
        }

        public FretEditController()
        {
            configFile = new FileInfo(Path.Combine(Paths.ConfigPath, "FretEdit.cfg"));
            hashCode = GetHashCode();
            keyBind.Value = KeyCode.F2;
        }

        void ReadConfig()
        {
            try
            {
                using (var stream = configFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    keyBind.ReadConfig(stream);
                    using (var reader = new BinaryReader(new NoCloseStream(stream)))
                    {
                        maxFretSpeed = reader.ReadSingle();
                        fretSpeed = reader.ReadSingle();
                        maxFretHeight = reader.ReadSingle();
                        fretHeight = reader.ReadSingle();
                    }
                }
            }
            catch (IOException) { }
        }

        void WriteConfig()
        {
            using (var fstream = configFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                using (var stream = new MemoryStream())
                {
                    keyBind.WriteConfig(stream);
                    using (var writer = new BinaryWriter(new NoCloseStream(stream)))
                    {
                        writer.Write(maxFretSpeed);
                        writer.Write(fretSpeed);
                        writer.Write(maxFretHeight);
                        writer.Write(fretHeight);
                    }
                    fstream.SetLength(0);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fstream);
                }
            }
        }

        static Texture2D FixWindowBackground(Texture2D texture)
        {
            if (texture != null)
            {
                var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0);
                Graphics.Blit(texture, rt);
                var tex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
                var tmp = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = tmp;
                RenderTexture.ReleaseTemporary(rt);
                var pixels = tex.GetPixels();
                for (int i = 0; i < pixels.Length; ++i)
                {
                    var c = pixels[i];
                    if (c.r > 0.1 && c.r < 0.9 && c.g > 0.1 && c.g < 0.9 && c.b > 0.1 && c.b < 0.9)
                    {
                        c.a = 1;
                    }
                    pixels[i] = c;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }
            return null;
        }

        readonly FileInfo configFile;
        readonly int hashCode;
        KeyBind keyBind;
        bool wasCursorVisible;

        const string fretSpeedFieldKey = "\u0314\u0313\u031A\u0316\u0311\u0310\u0310\u0315\u031A\u0310\u0311";
        const string fretBaseFieldKey = "\u0311\u0319\u0313\u0311\u0313\u0314\u0316\u0319\u030F\u031C\u0311";
        const string fretTopFieldKey = "\u030F\u0312\u0313\u030E\u030F\u0317\u0313\u0311\u0318\u0311\u0315";
        Type fretAnimatorType;
        FieldInfo fretSpeedField;
        FieldInfo fretBaseField;
        FieldInfo fretTopField;
        object[] fretObjects;

        bool sceneChanged = false;
        string maxFretSpeedText;
        float maxFretSpeed = 1;
        float fretSpeed = 0.275f;
        string maxFretHeightText;
        float maxFretHeight = 0.075f;
        float fretHeight = 0.0375f;
        bool isSettingKey = false;
        bool showMenu = false;
        string keyButtonText = "Change Open Keybind";
        Rect windowRect = new Rect(10f, 10f, 300f, 200f);
        Texture2D[] windowBackgrounds;
    }
}
