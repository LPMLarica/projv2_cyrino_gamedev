using System.IO;
using Nexus.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus.EditorTools
{
    public static class NexusBuilder
    {
        const string Root = "Assets/NexusCards";
        const string Gen = Root + "/Generated";
        const string MatDir = Gen + "/Materials";
        const string PrefabDir = Gen + "/Prefabs";
        const string AnimDir = Gen + "/Animations";
        const string SceneDir = Gen + "/Scenes";
        const string MenuScenePath = SceneDir + "/Menu.unity";
        const string ArenaScenePath = SceneDir + "/Arena.unity";

        static readonly Vector3 CamPos = new Vector3(0f, 13.5f, -7.5f);
        static readonly Vector3 CamLook = new Vector3(0f, 0f, 1f);
        const float PlayerRowZ = -0.5f;
        const float EnemyRowZ = 2f;
        const float SlotSpacing = 1.6f;

        class Kit
        {
            public Font title, body, heavy;
            public Sprite frame, gem, atk, hp, panel, panelGold, button, buttonPrimary, white, pill, glow, diamond, vignette, ornament;
            public Texture2D cardBack, table, slot, ring;
            public AudioClip menuMusic, battleMusic;
            public AudioClip[] sfx;
            public Material tableTop, tableBody, floor, gold, stone, cardBody, cardBackMat, nexusBlue, nexusRed, turretCrystal, gateEnergy, projectile, trail, slotPlayer, slotEnemy, ringMat;
            public Mesh crystal;
            public AnimatorController cardController, gateController, nexusController;
            public GameObject cardPrefab, projectilePrefab, popupPrefab, slotPrefab, deckPrefab, nexusPrefab, turretPrefab, audioPrefab;
        }

        [MenuItem("NEXUS/1 - Construir projeto completo", false, 0)]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("NEXUS", "Saia do modo Play antes de construir.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Progress("Pastas e importação", 0.05f);
                EnsureFolders();
                ArtDownloader.Download(false);
                ConfigureImporters();
                var kit = new Kit();
                Progress("Carregando recursos", 0.15f);
                LoadKit(kit);
                Progress("Materiais e malhas", 0.25f);
                CreateMaterials(kit);
                kit.crystal = CreateCrystalMesh();
                Progress("Animações", 0.35f);
                CreateAnimations(kit);
                Progress("Prefabs", 0.5f);
                CreatePrefabs(kit);
                Progress("Cena do menu", 0.65f);
                BuildMenuScene(kit);
                Progress("Cena da arena", 0.8f);
                BuildArenaScene(kit);
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MenuScenePath, true),
                    new EditorBuildSettingsScene(ArenaScenePath, true)
                };
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(MenuScenePath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog("NEXUS", "Projeto construído.\n\nCena aberta: Menu.\nAperte Play para jogar.", "Jogar!");
        }

        static void Progress(string info, float p)
        {
            EditorUtility.DisplayProgressBar("NEXUS — construindo", info, p);
        }

        static void EnsureFolders()
        {
            foreach (var dir in new[] { Gen, MatDir, PrefabDir, AnimDir, SceneDir, Root + "/Resources/ChampionArt" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ importação

        static void ConfigureImporters()
        {
            var borders = new System.Collections.Generic.Dictionary<string, Vector4>
            {
                { "panel", new Vector4(24, 24, 24, 24) },
                { "panel_gold", new Vector4(24, 24, 24, 24) },
                { "button", new Vector4(24, 24, 24, 24) },
                { "button_primary", new Vector4(24, 24, 24, 24) },
                { "pill", new Vector4(16, 15, 16, 15) },
            };
            foreach (var file in Directory.GetFiles(Root + "/Art/UI", "*.png"))
            {
                string path = file.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                Vector4 b;
                ti.spriteBorder = borders.TryGetValue(Path.GetFileNameWithoutExtension(path), out b) ? b : Vector4.zero;
                ti.SaveAndReimport();
            }
            foreach (var file in Directory.GetFiles(Root + "/Art/Textures"))
            {
                string path = file.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Default;
                ti.alphaIsTransparency = path.EndsWith(".png");
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 2048;
                ti.anisoLevel = 4;
                ti.SaveAndReimport();
            }
            foreach (var dir in new[] { Root + "/Resources/ChampionArt", Root + "/Resources/GeneratedArt" })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*.jpg"))
                {
                    var ti = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                    if (ti == null) continue;
                    ti.textureType = TextureImporterType.Default;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.maxTextureSize = 512;
                    ti.SaveAndReimport();
                }
            }
        }

        static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning("[NEXUS] Recurso não encontrado: " + path);
            return a;
        }

        static void LoadKit(Kit k)
        {
            k.title = Load<Font>(Root + "/Fonts/Cinzel-Bold.ttf");
            k.body = Load<Font>(Root + "/Fonts/Barlow-SemiBold.ttf");
            k.heavy = Load<Font>(Root + "/Fonts/Barlow-ExtraBold.ttf");
            string ui = Root + "/Art/UI/";
            k.frame = Load<Sprite>(ui + "card_frame.png");
            k.gem = Load<Sprite>(ui + "gem.png");
            k.atk = Load<Sprite>(ui + "badge_atk.png");
            k.hp = Load<Sprite>(ui + "badge_hp.png");
            k.panel = Load<Sprite>(ui + "panel.png");
            k.panelGold = Load<Sprite>(ui + "panel_gold.png");
            k.button = Load<Sprite>(ui + "button.png");
            k.buttonPrimary = Load<Sprite>(ui + "button_primary.png");
            k.white = Load<Sprite>(ui + "white.png");
            k.pill = Load<Sprite>(ui + "pill.png");
            k.glow = Load<Sprite>(ui + "glow.png");
            k.diamond = Load<Sprite>(ui + "diamond.png");
            k.vignette = Load<Sprite>(ui + "vignette.png");
            k.ornament = Load<Sprite>(ui + "ornament.png");
            k.cardBack = Load<Texture2D>(ui + "card_back.png");
            string tex = Root + "/Art/Textures/";
            k.table = Load<Texture2D>(tex + "table.jpg");
            k.slot = Load<Texture2D>(tex + "slot.png");
            k.ring = Load<Texture2D>(tex + "ring.png");
            string au = Root + "/Audio/";
            k.menuMusic = Load<AudioClip>(au + "music_menu.wav");
            k.battleMusic = Load<AudioClip>(au + "music_battle.wav");
            string[] names = { "draw", "play", "attack", "hit", "block", "gate", "combo", "turret", "click", "death", "victory", "defeat" };
            k.sfx = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++) k.sfx[i] = Load<AudioClip>(au + "sfx_" + names[i] + ".wav");
        }

        // ------------------------------------------------------------------ materiais

        static bool UsesSrp { get { return GraphicsSettings.currentRenderPipeline != null; } }

        static Material SaveMat(Material m, string name)
        {
            string path = MatDir + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material Lit(string name, Color color, float metallic, float smoothness, Texture tex = null, Color? emission = null)
        {
            Shader sh = UsesSrp ? Shader.Find("Universal Render Pipeline/Lit") : null;
            bool urp = sh != null;
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh);
            m.name = name;
            if (urp)
            {
                m.SetColor("_BaseColor", color);
                if (tex != null) m.SetTexture("_BaseMap", tex);
                m.SetFloat("_Smoothness", smoothness);
            }
            else
            {
                m.SetColor("_Color", color);
                if (tex != null) m.SetTexture("_MainTex", tex);
                m.SetFloat("_Glossiness", smoothness);
            }
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return SaveMat(m, name);
        }

        static Material Unlit(string name, Texture tex, Color color)
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.name = name;
            m.mainTexture = tex;
            m.color = color;
            return SaveMat(m, name);
        }

        static void CreateMaterials(Kit k)
        {
            k.tableTop = Lit("MesaTampo", Color.white, 0.1f, 0.42f, k.table);
            k.tableBody = Lit("MesaCorpo", new Color32(14, 20, 30, 255), 0.2f, 0.3f);
            k.floor = Lit("Chao", new Color32(4, 8, 14, 255), 0f, 0.1f);
            k.gold = Lit("OuroHextech", new Color32(200, 160, 95, 255), 0.85f, 0.62f);
            k.stone = Lit("PedraAzulada", new Color32(52, 62, 78, 255), 0.15f, 0.28f);
            k.cardBody = Lit("CartaCorpo", new Color32(70, 52, 26, 255), 0.7f, 0.45f);
            k.cardBackMat = Lit("CartaVerso", Color.white, 0.2f, 0.4f, k.cardBack);
            k.nexusBlue = Lit("CristalAzul", new Color32(40, 170, 220, 255), 0.1f, 0.9f, null, new Color(0.05f, 0.8f, 1.1f));
            k.nexusRed = Lit("CristalVermelho", new Color32(220, 50, 60, 255), 0.1f, 0.9f, null, new Color(1.2f, 0.12f, 0.15f));
            k.turretCrystal = Lit("CristalTorre", new Color32(230, 70, 60, 255), 0.1f, 0.9f, null, new Color(1.3f, 0.25f, 0.15f));
            k.gateEnergy = Lit("EnergiaPortao", new Color32(20, 210, 200, 255), 0f, 0.8f, null, new Color(0.05f, 1.3f, 1.2f));
            k.projectile = Lit("Projetil", Color.white, 0f, 0.9f, null, new Color(2f, 2f, 2f));
            k.trail = Unlit("Rastro", null, Color.white);
            k.slotPlayer = Unlit("EspacoJogador", k.slot, new Color(0.04f, 0.78f, 0.72f, 0.32f));
            k.slotEnemy = Unlit("EspacoInimigo", k.slot, new Color(0.85f, 0.25f, 0.25f, 0.3f));
            k.ringMat = Unlit("AnelAlvo", k.ring, new Color(0.9f, 0.25f, 0.2f, 1f));
        }

        static Mesh CreateCrystalMesh()
        {
            const int sides = 6;
            var top = new Vector3(0f, 1f, 0f);
            var bottom = new Vector3(0f, -1f, 0f);
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                ring[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0.1f, Mathf.Sin(a) * 0.5f);
            }
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < sides; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % sides];
                int v = verts.Count;
                verts.Add(top); verts.Add(b); verts.Add(a);
                tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
                v = verts.Count;
                verts.Add(bottom); verts.Add(a); verts.Add(b);
                tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
            }
            var mesh = new Mesh { name = "Cristal" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = Gen + "/Cristal.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ------------------------------------------------------------------ animações

        static AnimationClip Clip(string name, string path, float[] times, Vector3[] positions, bool loop)
        {
            var clip = new AnimationClip { name = name };
            var cx = new AnimationCurve();
            var cy = new AnimationCurve();
            var cz = new AnimationCurve();
            for (int i = 0; i < times.Length; i++)
            {
                cx.AddKey(times[i], positions[i].x);
                cy.AddKey(times[i], positions[i].y);
                cz.AddKey(times[i], positions[i].z);
            }
            clip.SetCurve(path, typeof(Transform), "localPosition.x", cx);
            clip.SetCurve(path, typeof(Transform), "localPosition.y", cy);
            clip.SetCurve(path, typeof(Transform), "localPosition.z", cz);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string asset = AnimDir + "/" + name + ".anim";
            AssetDatabase.DeleteAsset(asset);
            AssetDatabase.CreateAsset(clip, asset);
            return clip;
        }

        static AnimatorController Controller(string name)
        {
            string path = AnimDir + "/" + name + ".controller";
            AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static void TriggerState(AnimatorController c, AnimatorState idle, string trigger, AnimationClip clip)
        {
            c.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
            var sm = c.layers[0].stateMachine;
            var st = sm.AddState(trigger);
            st.motion = clip;
            var t = sm.AddAnyStateTransition(st);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.hasExitTime = false;
            t.duration = 0.02f;
            t.canTransitionToSelf = true;
            var back = st.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0.04f;
        }

        static void CreateAnimations(Kit k)
        {
            var z = Vector3.zero;
            var idle = Clip("Carta_Repouso", "Body", new[] { 0f, 1f }, new[] { z, z }, true);
            var attack = Clip("Carta_Ataque", "Body", new[] { 0f, 0.1f, 0.18f, 0.38f },
                new[] { z, new Vector3(0f, 0.25f, -0.15f), new Vector3(0f, 0.4f, 0.55f), z }, false);
            var attackDown = Clip("Carta_AtaqueInimigo", "Body", new[] { 0f, 0.1f, 0.18f, 0.38f },
                new[] { z, new Vector3(0f, 0.25f, 0.15f), new Vector3(0f, 0.4f, -0.55f), z }, false);
            var hit = Clip("Carta_Impacto", "Body", new[] { 0f, 0.05f, 0.1f, 0.16f, 0.22f, 0.3f },
                new[] { z, new Vector3(0.09f, 0.05f, 0f), new Vector3(-0.08f, 0.03f, 0f), new Vector3(0.05f, 0.01f, 0f), new Vector3(-0.02f, 0f, 0f), z }, false);

            k.cardController = Controller("CartaVisual");
            var sm = k.cardController.layers[0].stateMachine;
            var idleState = sm.AddState("Repouso");
            idleState.motion = idle;
            sm.defaultState = idleState;
            TriggerState(k.cardController, idleState, "Attack", attack);
            TriggerState(k.cardController, idleState, "AttackDown", attackDown);
            TriggerState(k.cardController, idleState, "Hit", hit);

            var closed = Clip("Portao_Fechado", "Bars", new[] { 0f, 1f }, new[] { z, z }, true);
            var open = Clip("Portao_Abrindo", "Bars", new[] { 0f, 0.25f, 1.1f },
                new[] { z, new Vector3(0f, 0.12f, 0f), new Vector3(0f, -2.5f, 0f) }, false);
            k.gateController = Controller("Portao");
            sm = k.gateController.layers[0].stateMachine;
            var closedState = sm.AddState("Fechado");
            closedState.motion = closed;
            sm.defaultState = closedState;
            k.gateController.AddParameter("Open", AnimatorControllerParameterType.Trigger);
            var openState = sm.AddState("Aberto");
            openState.motion = open;
            var tr = closedState.AddTransition(openState);
            tr.AddCondition(AnimatorConditionMode.If, 0f, "Open");
            tr.hasExitTime = false;
            tr.duration = 0f;

            var bob = Clip("Nexus_Flutuar", "Shake/Crystal", new[] { 0f, 1.5f, 3f },
                new[] { new Vector3(0f, 1.35f, 0f), new Vector3(0f, 1.55f, 0f), new Vector3(0f, 1.35f, 0f) }, true);
            k.nexusController = Controller("Nexus");
            sm = k.nexusController.layers[0].stateMachine;
            var bobState = sm.AddState("Flutuando");
            bobState.motion = bob;
            sm.defaultState = bobState;
        }

        // ------------------------------------------------------------------ helpers de cena

        static GameObject Node(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject CrystalObj(Kit k, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = k.crystal;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 2f, 1f);
            return go;
        }

        static Light PointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = Node("Luz", parent, pos);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat)
        {
            var go = Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size.x, size.y, 1f), mat, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------------ helpers de UI

        static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            return (RectTransform)go.transform;
        }

        static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            return Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, bool sliced = false)
        {
            var rt = UI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced) img.type = Image.Type.Sliced;
            return img;
        }

        static Text Txt(Transform parent, string name, string value, Font font, int size, Color color, TextAnchor align)
        {
            var rt = UI(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = value;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        static void Shadowed(Graphic g, float distance = 2f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.85f);
            s.effectDistance = new Vector2(distance, -distance);
        }

        static Button Btn(Kit k, Transform parent, string name, string label, bool primary, UnityAction action, int fontSize = 26)
        {
            var img = Img(parent, name, primary ? k.buttonPrimary : k.button, Color.white, true);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.0f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.7f);
            colors.colorMultiplier = 1.2f;
            b.colors = colors;
            var t = Txt(img.transform, "Rotulo", label, k.title, fontSize, UIStyle.GoldLight, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            return b;
        }

        static Canvas ScreenCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        static void EventSystemObject()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null)
            {
                var module = go.AddComponent(t);
                var assign = t.GetMethod("AssignDefaultActions");
                if (assign != null) assign.Invoke(module, null);
                return;
            }
#endif
            go.AddComponent<StandaloneInputModule>();
        }

        static CanvasGroup Fader(Transform canvas)
        {
            var img = Img(canvas, "Fader", k_white, Color.black);
            Stretch(img.rectTransform);
            img.raycastTarget = true;
            return img.gameObject.AddComponent<CanvasGroup>();
        }

        static Sprite k_white;

        static Image Bar(Kit k, Transform parent, string name, float x, float y, float w, float h, Color fill, out Text label)
        {
            var bg = Img(parent, name, k.pill, new Color(0f, 0f, 0f, 0.6f), true);
            TopLeft(bg.rectTransform, x, y, w, h);
            var f = Img(bg.transform, "Preenchimento", k.pill, fill);
            Stretch(f.rectTransform, 3f);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillOrigin = (int)Image.OriginHorizontal.Left;
            f.fillAmount = 1f;
            label = Txt(bg.transform, "Valor", "", k.heavy, Mathf.RoundToInt(h * 0.72f), Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            Shadowed(label, 1.5f);
            return f;
        }

        // ------------------------------------------------------------------ prefabs

        static GameObject SavePrefab(GameObject go, string name)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void CreatePrefabs(Kit k)
        {
            k.cardPrefab = SavePrefab(BuildCard(k), "Carta");
            k.projectilePrefab = SavePrefab(BuildProjectile(k), "Projetil");
            k.popupPrefab = SavePrefab(BuildPopup(k), "NumeroFlutuante");
            k.slotPrefab = SavePrefab(BuildSlot(k), "EspacoDeMesa");
            k.deckPrefab = SavePrefab(BuildDeck(k), "PilhaDoBaralho");
            k.nexusPrefab = SavePrefab(BuildNexus(k), "Nexus");
            k.turretPrefab = SavePrefab(BuildTurret(k), "Torre");
            k.audioPrefab = SavePrefab(BuildAudio(k), "AudioManager");
        }

        static GameObject BuildCard(Kit k)
        {
            var root = new GameObject("Carta");
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            PhysicsCompat.SetDamping(rb, 0.4f, 2f);
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.1f, 0.06f, 1.6f);
            box.center = new Vector3(0f, 0.03f, 0f);

            var visual = Node("Visual", root.transform, Vector3.zero);
            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = k.cardController;
            animator.applyRootMotion = false;
            var body = Node("Body", visual.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "Lamina", body.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.1f, 0.04f, 1.6f), k.cardBody, false);
            var back = Prim(PrimitiveType.Quad, "Verso", body.transform, new Vector3(0f, -0.001f, 0f), new Vector3(1.08f, 1.58f, 1f), k.cardBackMat, false);
            back.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var aim = Node("Mira", root.transform, new Vector3(0f, 0.45f, 0f));

            var canvasRt = UI("Face", body.transform);
            var canvas = canvasRt.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasRt.sizeDelta = new Vector2(440f, 640f);
            canvasRt.localScale = Vector3.one * 0.0025f;
            canvasRt.localPosition = new Vector3(0f, 0.0415f, 0f);
            canvasRt.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var glow = Img(canvasRt, "Brilho", k.glow, Color.white);
            Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 840f));
            glow.enabled = false;

            var art = UI("Arte", canvasRt).gameObject.AddComponent<RawImage>();
            art.raycastTarget = false;
            TopLeft(art.rectTransform, 20f, 20f, 400f, 376f);

            var frame = Img(canvasRt, "Moldura", k.frame, Color.white);
            Stretch(frame.rectTransform);

            var bar = Img(canvasRt, "FaixaRegiao", k.white, Color.white);
            TopLeft(bar.rectTransform, 70f, 441f, 300f, 4f);

            var name = Txt(canvasRt, "Nome", "Nome", k.title, 34, UIStyle.GoldLight, TextAnchor.MiddleCenter);
            TopLeft(name.rectTransform, 30f, 384f, 380f, 52f);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 18;
            name.resizeTextMaxSize = 34;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            Shadowed(name);

            var region = Txt(canvasRt, "Regiao", "REGIÃO", k.heavy, 22, Color.white, TextAnchor.MiddleCenter);
            TopLeft(region.rectTransform, 20f, 452f, 400f, 30f);

            var rules = Txt(canvasRt, "Regras", "", k.body, 25, new Color(0.93f, 0.9f, 0.82f), TextAnchor.UpperCenter);
            TopLeft(rules.rectTransform, 92f, 488f, 256f, 120f);
            rules.lineSpacing = 0.9f;

            var status = Txt(canvasRt, "Estado", "", k.heavy, 28, UIStyle.Teal, TextAnchor.MiddleCenter);
            TopLeft(status.rectTransform, 20f, 338f, 400f, 40f);
            Shadowed(status, 3f);

            var gem = Img(canvasRt, "Custo", k.gem, Color.white);
            TopLeft(gem.rectTransform, -10f, -10f, 116f, 116f);
            var cost = Txt(gem.transform, "Valor", "0", k.heavy, 58, Color.white, TextAnchor.MiddleCenter);
            Stretch(cost.rectTransform);
            Shadowed(cost, 3f);

            var atk = Img(canvasRt, "Ataque", k.atk, Color.white);
            Place(atk.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-8f, -8f), new Vector2(108f, 108f));
            var atkText = Txt(atk.transform, "Valor", "0", k.heavy, 54, Color.white, TextAnchor.MiddleCenter);
            Stretch(atkText.rectTransform);
            Shadowed(atkText, 3f);

            var hp = Img(canvasRt, "Vida", k.hp, Color.white);
            Place(hp.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(8f, -8f), new Vector2(108f, 108f));
            var hpText = Txt(hp.transform, "Valor", "0", k.heavy, 54, Color.white, TextAnchor.MiddleCenter);
            Stretch(hpText.rectTransform);
            hpText.rectTransform.offsetMax = new Vector2(0f, 6f);
            Shadowed(hpText, 3f);

            var view = root.AddComponent<CardView>();
            view.body = rb;
            view.hitBox = box;
            view.visual = visual.transform;
            view.animator = animator;
            view.aim = aim.transform;
            view.art = art;
            view.glow = glow;
            view.regionBar = bar;
            view.nameText = name;
            view.regionText = region;
            view.rulesText = rules;
            view.costText = cost;
            view.atkText = atkText;
            view.hpText = hpText;
            view.statusText = status;
            view.atkBadge = atk.gameObject;
            view.hpBadge = hp.gameObject;
            return root;
        }

        static GameObject BuildProjectile(Kit k)
        {
            var go = Prim(PrimitiveType.Sphere, "Projetil", null, Vector3.zero, Vector3.one * 0.24f, k.projectile, true);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.22f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.sharedMaterial = k.trail;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            var light = PointLight(go.transform, Vector3.zero, Color.white, 3f, 3.5f);
            var p = go.AddComponent<Projectile>();
            p.body = rb;
            p.sphere = go.GetComponent<SphereCollider>();
            p.core = go.GetComponent<Renderer>();
            p.trail = trail;
            p.glow = light;
            return go;
        }

        static GameObject BuildPopup(Kit k)
        {
            var rt = UI("NumeroFlutuante", null);
            var canvas = rt.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            rt.sizeDelta = new Vector2(400f, 120f);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            var t = Txt(rt, "Texto", "-3", k.heavy, 80, UIStyle.Crimson, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(3f, -3f);
            var p = rt.gameObject.AddComponent<DamagePopup>();
            p.label = t;
            p.group = group;
            return rt.gameObject;
        }

        static GameObject BuildSlot(Kit k)
        {
            var root = new GameObject("EspacoDeMesa");
            var quad = Quad("Marca", root.transform, new Vector3(0f, 0.004f, 0f), new Vector2(1.3f, 1.8f), k.slotPlayer);
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.3f, 0.05f, 1.8f);
            var s = root.AddComponent<SlotMarker>();
            s.quad = quad.GetComponent<Renderer>();
            return root;
        }

        static GameObject BuildDeck(Kit k)
        {
            var root = new GameObject("PilhaDoBaralho");
            var stack = Prim(PrimitiveType.Cube, "Pilha", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.1f, 0.3f, 1.6f), k.cardBody, false);
            var top = Quad("Topo", root.transform, new Vector3(0f, 0.302f, 0f), new Vector2(1.08f, 1.58f), k.cardBackMat);
            top.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            var d = root.AddComponent<DeckPileView>();
            d.stack = stack.transform;
            d.top = top.transform;
            return root;
        }

        static Image WorldHpBar(Kit k, Transform parent, float height, Color fill, out Text label, out GameObject barRoot)
        {
            var rt = UI("BarraDeVida", parent);
            rt.localPosition = new Vector3(0f, height, 0f);
            rt.localScale = Vector3.one * 0.008f;
            rt.sizeDelta = new Vector2(240f, 44f);
            var c = rt.gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 10;
            var f = Bar(k, rt, "Barra", 0f, 0f, 240f, 44f, fill, out label);
            barRoot = rt.gameObject;
            return f;
        }

        static GameObject BuildNexus(Kit k)
        {
            var root = new GameObject("Nexus");
            var shake = Node("Shake", root.transform, Vector3.zero);
            Prim(PrimitiveType.Cylinder, "Base", shake.transform, new Vector3(0f, 0.12f, 0f), new Vector3(1.9f, 0.12f, 1.9f), k.stone, true);
            var ring = Prim(PrimitiveType.Cylinder, "Anel", shake.transform, new Vector3(0f, 0.28f, 0f), new Vector3(1.45f, 0.05f, 1.45f), k.gold, true);
            var pieces = new System.Collections.Generic.List<Transform> { ring.transform };
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f + 0.5f;
                var ob = Prim(PrimitiveType.Cube, "Obelisco" + i, shake.transform, new Vector3(Mathf.Cos(a) * 0.78f, 0.75f, Mathf.Sin(a) * 0.78f), new Vector3(0.14f, 1f, 0.14f), k.gold, true);
                ob.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 8f);
                pieces.Add(ob.transform);
            }
            var crystal = CrystalObj(k, "Crystal", shake.transform, new Vector3(0f, 1.35f, 0f), new Vector3(0.75f, 0.7f, 0.75f), k.nexusBlue);
            crystal.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 35f, 0f);
            pieces.Add(crystal.transform);
            var aim = Node("Mira", root.transform, new Vector3(0f, 1.0f, 0f));
            var light = PointLight(root.transform, new Vector3(0f, 1.5f, 0f), new Color(0.2f, 0.85f, 1f), 3f, 6f);
            var ringQuad = Quad("AnelDeAlvo", root.transform, new Vector3(0f, 0.02f, 0f), new Vector2(2.8f, 2.8f), k.ringMat);
            var cap = root.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1.1f, 0f);
            cap.radius = 0.85f;
            cap.height = 2.4f;
            var anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = k.nexusController;
            Text hpText;
            GameObject barRoot;
            var fill = WorldHpBar(k, root.transform, 2.75f, UIStyle.Teal, out hpText, out barRoot);
            var sv = root.AddComponent<StructureView>();
            sv.side = Side.Player;
            sv.kind = TargetKind.Nexus;
            sv.aim = aim.transform;
            sv.hitCollider = cap;
            sv.glowLight = light;
            sv.targetRing = ringQuad.GetComponent<Renderer>();
            sv.shakeRoot = shake.transform;
            sv.hpFill = fill;
            sv.hpText = hpText;
            sv.hpBar = barRoot;
            sv.animator = anim;
            sv.breakables = pieces.ToArray();
            return root;
        }

        static GameObject BuildTurret(Kit k)
        {
            var root = new GameObject("Torre");
            var shake = Node("Shake", root.transform, Vector3.zero);
            Prim(PrimitiveType.Cylinder, "Base", shake.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.4f, 0.15f, 1.4f), k.stone, true);
            var column = Prim(PrimitiveType.Cylinder, "Coluna", shake.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.7f, 0.8f, 0.7f), k.stone, true);
            var band = Prim(PrimitiveType.Cylinder, "Cinta", shake.transform, new Vector3(0f, 1.72f, 0f), new Vector3(0.82f, 0.06f, 0.82f), k.gold, true);
            var capTop = Prim(PrimitiveType.Cube, "Capitel", shake.transform, new Vector3(0f, 1.95f, 0f), new Vector3(0.9f, 0.22f, 0.9f), k.gold, true);
            capTop.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var crystal = CrystalObj(k, "Cristal", shake.transform, new Vector3(0f, 2.55f, 0f), new Vector3(0.45f, 0.42f, 0.45f), k.turretCrystal);
            crystal.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, -60f, 0f);
            var aim = Node("Mira", root.transform, new Vector3(0f, 1.6f, 0f));
            var muzzle = Node("Boca", root.transform, new Vector3(0f, 3.25f, 0f));
            var light = PointLight(root.transform, new Vector3(0f, 2.6f, 0f), new Color(1f, 0.35f, 0.25f), 2.5f, 5f);
            var ringQuad = Quad("AnelDeAlvo", root.transform, new Vector3(0f, 0.02f, 0f), new Vector2(2.2f, 2.2f), k.ringMat);
            var cap = root.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1.55f, 0f);
            cap.radius = 0.7f;
            cap.height = 3.3f;
            Text hpText;
            GameObject barRoot;
            var fill = WorldHpBar(k, root.transform, 3.6f, new Color(1f, 0.45f, 0.3f), out hpText, out barRoot);
            var sv = root.AddComponent<StructureView>();
            sv.side = Side.Enemy;
            sv.kind = TargetKind.Turret;
            sv.aim = aim.transform;
            sv.muzzle = muzzle.transform;
            sv.hitCollider = cap;
            sv.glowLight = light;
            sv.targetRing = ringQuad.GetComponent<Renderer>();
            sv.shakeRoot = shake.transform;
            sv.hpFill = fill;
            sv.hpText = hpText;
            sv.hpBar = barRoot;
            sv.breakables = new[] { column.transform, band.transform, capTop.transform, crystal.transform };
            return root;
        }

        static GameObject BuildAudio(Kit k)
        {
            var go = new GameObject("AudioManager");
            var a = go.AddComponent<AudioManager>();
            a.menuMusic = k.menuMusic;
            a.battleMusic = k.battleMusic;
            a.effects = k.sfx;
            return go;
        }

        static GameObject Instance(GameObject prefab, string name, Transform parent, Vector3 pos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        // ------------------------------------------------------------------ ambiente comum

        static void Environment(Kit k, out Camera cam, out CameraRig rig, Vector3 camPos, Vector3 look, float fov)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.17f, 0.2f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.fogColor = new Color(0.015f, 0.035f, 0.06f);

            var rigGo = new GameObject("CameraRig");
            rigGo.transform.position = camPos;
            rigGo.transform.rotation = Quaternion.LookRotation(look - camPos, Vector3.up);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rigGo.transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.03f, 0.055f);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            rig = rigGo.AddComponent<CameraRig>();
            rig.cam = cam;

            var sun = new GameObject("Luz Principal");
            sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.93f, 0.82f);
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.75f;

            var fill = new GameObject("Luz de Preenchimento");
            fill.transform.rotation = Quaternion.Euler(28f, 150f, 0f);
            var f = fill.AddComponent<Light>();
            f.type = LightType.Directional;
            f.color = new Color(0.45f, 0.6f, 1f);
            f.intensity = 0.28f;
            f.shadows = LightShadows.None;
        }

        static void Table(Kit k, Vector3 center, Vector2 size)
        {
            var root = new GameObject("Mesa");
            var body = Prim(PrimitiveType.Cube, "Tampo (colisor)", root.transform, center + new Vector3(0f, -0.2f, 0f), new Vector3(size.x, 0.4f, size.y), k.tableBody, true);
            body.AddComponent<TableSurface>();
            Quad("Superficie", root.transform, center + new Vector3(0f, 0.002f, 0f), size, k.tableTop).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            float hx = size.x * 0.5f, hz = size.y * 0.5f;
            Prim(PrimitiveType.Cube, "Borda N", root.transform, center + new Vector3(0f, 0.03f, hz), new Vector3(size.x + 0.3f, 0.1f, 0.15f), k.gold, true);
            Prim(PrimitiveType.Cube, "Borda S", root.transform, center + new Vector3(0f, 0.03f, -hz), new Vector3(size.x + 0.3f, 0.1f, 0.15f), k.gold, true);
            Prim(PrimitiveType.Cube, "Borda L", root.transform, center + new Vector3(hx, 0.03f, 0f), new Vector3(0.15f, 0.1f, size.y + 0.3f), k.gold, true);
            Prim(PrimitiveType.Cube, "Borda O", root.transform, center + new Vector3(-hx, 0.03f, 0f), new Vector3(0.15f, 0.1f, size.y + 0.3f), k.gold, true);
            Prim(PrimitiveType.Plane, "Chao", root.transform, new Vector3(0f, -1.2f, 0f), new Vector3(10f, 1f, 10f), k.floor, true);
        }

        // ------------------------------------------------------------------ cena: menu

        static void BuildMenuScene(Kit k)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            k_white = k.white;
            Camera cam;
            CameraRig rig;
            Environment(k, out cam, out rig, new Vector3(-0.5f, 3.4f, -6.4f), new Vector3(0.6f, 1.5f, 1.4f), 42f);
            Table(k, new Vector3(1.5f, 0f, 1.5f), new Vector2(15f, 10f));
            var nexus = Instance(k.nexusPrefab, "Nexus (vitrine)", null, new Vector3(2.8f, 0f, 1.4f));
            nexus.GetComponent<StructureView>().hpBar.SetActive(false);
            var ring = new GameObject("Vitrine de Cartas");
            ring.transform.position = new Vector3(2.8f, 0f, 1.4f);
            var show = ring.AddComponent<MenuShowcase>();
            show.radius = 2.8f;
            show.cards = new CardView[7];
            for (int i = 0; i < show.cards.Length; i++)
                show.cards[i] = Instance(k.cardPrefab, "Carta Vitrine " + (i + 1), ring.transform, Vector3.zero).GetComponent<CardView>();
            Instance(k.audioPrefab, "AudioManager", null, Vector3.zero);

            var canvas = ScreenCanvas("Canvas Menu");
            var ct = canvas.transform;
            var vig = Img(ct, "Vinheta", k.vignette, Color.white);
            Stretch(vig.rectTransform);
            var shade = Img(ct, "Sombra Esquerda", k.white, new Color(0.004f, 0.02f, 0.04f, 0.72f));
            Place(shade.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(820f, 2000f));

            var menu = new GameObject("Menu").AddComponent<MainMenu>();
            menu.transform.SetParent(ct, false);

            var col = UI("Coluna", ct);
            Place(col, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(620f, 860f));
            var title = Txt(col, "Titulo", "NEXUS", k.title, 150, UIStyle.GoldLight, TextAnchor.UpperLeft);
            TopLeft(title.rectTransform, 0f, 40f, 640f, 170f);
            Shadowed(title, 4f);
            var sub = Txt(col, "Subtitulo", "DUELO DE CAMPEÕES", k.title, 34, UIStyle.Gold, TextAnchor.UpperLeft);
            TopLeft(sub.rectTransform, 6f, 205f, 620f, 44f);
            var orn = Img(col, "Ornamento", k.ornament, Color.white);
            TopLeft(orn.rectTransform, -60f, 258f, 560f, 28f);
            var tag = Txt(col, "Chamada", "Um jogo de cartas de mesa em Runeterra. Combine regiões, forme duplas lendárias e derrube o Nexus inimigo.",
                k.body, 24, new Color(0.85f, 0.82f, 0.74f), TextAnchor.UpperLeft);
            TopLeft(tag.rectTransform, 6f, 300f, 520f, 90f);

            float y = 420f;
            var play = Btn(k, col, "Botao Jogar", "JOGAR", true, menu.Play, 32);
            TopLeft((RectTransform)play.transform, 0f, y, 440f, 82f);
            y += 100f;
            var how = Btn(k, col, "Botao Como Jogar", "COMO JOGAR", false, menu.ShowHowTo);
            TopLeft((RectTransform)how.transform, 0f, y, 440f, 64f);
            y += 78f;
            var diff = Btn(k, col, "Botao Dificuldade", "DIFICULDADE: NORMAL", false, menu.ToggleDifficulty, 22);
            TopLeft((RectTransform)diff.transform, 0f, y, 440f, 64f);
            menu.difficultyLabel = diff.GetComponentInChildren<Text>();
            y += 78f;
            var cred = Btn(k, col, "Botao Creditos", "CRÉDITOS", false, menu.ShowCredits);
            TopLeft((RectTransform)cred.transform, 0f, y, 440f, 64f);
            y += 78f;
            var quit = Btn(k, col, "Botao Sair", "SAIR", false, menu.Quit);
            TopLeft((RectTransform)quit.transform, 0f, y, 440f, 64f);
            menu.bestText = Txt(col, "Recorde", "", k.heavy, 22, UIStyle.Teal, TextAnchor.UpperLeft);
            TopLeft(menu.bestText.rectTransform, 4f, y + 86f, 440f, 30f);

            var foot = Txt(ct, "Aviso Legal", "Projeto acadêmico e gratuito, sem fins lucrativos. Não é endossado pela Riot Games. League of Legends e seus campeões são marcas da Riot Games, Inc.",
                k.body, 17, new Color(0.6f, 0.58f, 0.52f), TextAnchor.LowerRight);
            Place(foot.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 22f), new Vector2(900f, 60f));

            menu.howToPanel = Modal(k, ct, "Painel Como Jogar", "COMO JOGAR", HowToText(), menu.ClosePanels);
            menu.creditsPanel = Modal(k, ct, "Painel Creditos", "CRÉDITOS", CreditsText(), menu.ClosePanels);

            var flow = canvas.gameObject.AddComponent<SceneFlow>();
            flow.fader = Fader(ct);
            EventSystemObject();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        static GameObject Modal(Kit k, Transform canvas, string name, string title, string body, UnityAction close)
        {
            var dim = Img(canvas, name, k.white, new Color(0f, 0.01f, 0.03f, 0.82f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = Img(dim.transform, "Painel", k.panelGold, Color.white, true);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 720f));
            var t = Txt(panel.transform, "Titulo", title, k.title, 44, UIStyle.Gold, TextAnchor.UpperCenter);
            TopLeft(t.rectTransform, 40f, 34f, 960f, 60f);
            var orn = Img(panel.transform, "Ornamento", k.ornament, Color.white);
            TopLeft(orn.rectTransform, 270f, 96f, 500f, 24f);
            var b = Txt(panel.transform, "Texto", body, k.body, 23, new Color(0.9f, 0.87f, 0.8f), TextAnchor.UpperLeft);
            TopLeft(b.rectTransform, 64f, 134f, 912f, 480f);
            b.lineSpacing = 1.05f;
            var btn = Btn(k, panel.transform, "Fechar", "FECHAR", true, close, 24);
            Place((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(260f, 64f));
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        static string HowToText()
        {
            return "<b>Objetivo:</b> destruir o Nexus inimigo antes que ele destrua o seu.\n" +
                   "<b>Mana:</b> cresce 1 por turno (máx. 10). Cada carta custa o valor da gema azul.\n" +
                   "<b>Desafio 1 — Cerco à Torre:</b> um Portão físico protege o Nexus inimigo e bloqueia projéteis. A Torre atira no seu campeão mais forte todo turno. Derrube a Torre para abrir o Portão.\n" +
                   "<b>Desafio 2 — Nexus Exposto:</b> o Nexus convoca uma Tropa Guardiã por turno.\n" +
                   "<b>Guardião</b> precisa ser derrotado antes de outros alvos. <b>Ímpeto</b> ataca no turno em que entra.\n" +
                   "<b>Sinergias:</b> 2+ campeões da mesma região dão bônus. <b>Combos:</b> duplas (Xayah+Rakan, Jinx+Vi, Yasuo+Malphite, Garen+Lux, Ashe+Braum, Darius+Draven) disparam efeitos.\n\n" +
                   "<b>Controles:</b> clique numa carta da mão e depois num espaço livre (ou 1–8 no teclado) · clique num campeão PRONTO e depois no alvo · Espaço encerra o turno · botão direito/Esc cancela · H ajuda · M silencia.";
        }

        static string CreditsText()
        {
            return "<b>Design, código e áudio:</b> projeto acadêmico em Unity 6 (C#).\n" +
                   "<b>Música e efeitos:</b> sintetizados proceduralmente para este projeto.\n" +
                   "<b>Fontes:</b> Cinzel e Barlow Semi Condensed (SIL Open Font License).\n" +
                   "<b>Artes dos campeões:</b> carregadas do Data Dragon, o CDN público da Riot Games para desenvolvedores.\n\n" +
                   "NEXUS foi criado sob a política “Legal Jibber Jabber” da Riot Games usando recursos de propriedade da Riot Games. " +
                   "A Riot Games não endossa nem patrocina este projeto. Uso não comercial.";
        }

        // ------------------------------------------------------------------ cena: arena

        static void BuildArenaScene(Kit k)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            k_white = k.white;
            Camera cam;
            CameraRig rig;
            Environment(k, out cam, out rig, CamPos, CamLook, 40f);
            Table(k, new Vector3(0f, 0f, 1.4f), new Vector2(15f, 10f));
            Instance(k.audioPrefab, "AudioManager", null, Vector3.zero);

            var board = new GameObject("Tabuleiro").transform;
            var playerRow = Node("Fileira do Jogador", board, new Vector3(0f, 0f, PlayerRowZ)).transform;
            var enemyRow = Node("Fileira do Inimigo", board, new Vector3(0f, 0f, EnemyRowZ)).transform;
            var pSlots = new SlotMarker[Match.BoardSize];
            var eSlots = new SlotMarker[Match.BoardSize];
            for (int i = 0; i < Match.BoardSize; i++)
            {
                float x = (i - 2) * SlotSpacing;
                pSlots[i] = Instance(k.slotPrefab, "Espaco J" + (i + 1), playerRow, new Vector3(x, 0f, 0f)).GetComponent<SlotMarker>();
                pSlots[i].side = Side.Player;
                pSlots[i].index = i;
                eSlots[i] = Instance(k.slotPrefab, "Espaco I" + (i + 1), enemyRow, new Vector3(x, 0f, 0f)).GetComponent<SlotMarker>();
                eSlots[i].side = Side.Enemy;
                eSlots[i].index = i;
                eSlots[i].quad.sharedMaterial = k.slotEnemy;
            }

            var pDeck = Instance(k.deckPrefab, "Baralho do Jogador", board, new Vector3(5.4f, 0f, -0.4f)).GetComponent<DeckPileView>();
            var eDeck = Instance(k.deckPrefab, "Baralho do Inimigo", board, new Vector3(5.4f, 0f, 4.0f)).GetComponent<DeckPileView>();

            var pNexus = Instance(k.nexusPrefab, "Nexus do Jogador", board, new Vector3(-5.4f, 0f, -0.4f)).GetComponent<StructureView>();
            var eNexusGo = Instance(k.nexusPrefab, "Nexus Inimigo", board, new Vector3(0f, 0f, 4.5f));
            var eNexus = eNexusGo.GetComponent<StructureView>();
            eNexus.side = Side.Enemy;
            eNexus.transform.Find("Shake/Crystal").GetComponent<Renderer>().sharedMaterial = k.nexusRed;
            eNexus.glowLight.color = new Color(1f, 0.3f, 0.3f);
            eNexus.hpFill.color = UIStyle.Crimson;
            var turret = Instance(k.turretPrefab, "Torre Inimiga", board, new Vector3(-3.4f, 0f, 4.0f)).GetComponent<StructureView>();
            var gate = BuildGate(k, board, new Vector3(0f, 0f, 3.55f), eNexus);

            var game = new GameObject("Jogo");
            var layout = game.AddComponent<BoardLayout>();
            layout.cam = cam;
            layout.playerSlots = pSlots;
            layout.enemySlots = eSlots;
            layout.playerDeck = pDeck;
            layout.enemyDeck = eDeck;
            var director = game.AddComponent<BattleDirector>();
            var input = game.AddComponent<PlayerController>();

            var hud = BuildHud(k, director);
            director.layout = layout;
            director.cameraRig = rig;
            director.playerNexus = pNexus;
            director.enemyNexus = eNexus;
            director.enemyTurret = turret;
            director.gate = gate;
            director.hud = hud;
            director.cardPrefab = k.cardPrefab.GetComponent<CardView>();
            director.projectilePrefab = k.projectilePrefab.GetComponent<Projectile>();
            director.popupPrefab = k.popupPrefab.GetComponent<DamagePopup>();
            input.director = director;
            input.hud = hud;
            input.cam = cam;

            EventSystemObject();
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
        }

        static GateController BuildGate(Kit k, Transform parent, Vector3 pos, StructureView protects)
        {
            var root = Node("Portao do Nexus", parent, pos);
            foreach (float x in new[] { -1.95f, 1.95f })
            {
                Prim(PrimitiveType.Cube, x < 0 ? "Pilar E" : "Pilar D", root.transform, new Vector3(x, 1.3f, 0f), new Vector3(0.36f, 2.6f, 0.5f), k.stone, true);
                Prim(PrimitiveType.Cube, "Capitel", root.transform, new Vector3(x, 2.68f, 0f), new Vector3(0.52f, 0.16f, 0.62f), k.gold, true);
            }
            var bars = Node("Bars", root.transform, Vector3.zero);
            var rb = bars.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var box = bars.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.15f, 0f);
            box.size = new Vector3(3.6f, 2.3f, 0.3f);
            bars.AddComponent<GateBarrier>().protectedStructure = protects;
            for (int i = 0; i < 7; i++)
            {
                float x = -1.5f + i * 0.5f;
                Prim(PrimitiveType.Cylinder, "Barra " + (i + 1), bars.transform, new Vector3(x, 1.12f, 0f), new Vector3(0.08f, 1.1f, 0.08f), k.gateEnergy, false);
            }
            Prim(PrimitiveType.Cube, "Travessa Baixa", bars.transform, new Vector3(0f, 0.55f, 0f), new Vector3(3.5f, 0.08f, 0.1f), k.gold, false);
            Prim(PrimitiveType.Cube, "Travessa Alta", bars.transform, new Vector3(0f, 1.75f, 0f), new Vector3(3.5f, 0.08f, 0.1f), k.gold, false);
            Prim(PrimitiveType.Cube, "Coroa", bars.transform, new Vector3(0f, 2.24f, 0f), new Vector3(3.5f, 0.12f, 0.14f), k.gold, false);
            var light = PointLight(root.transform, new Vector3(0f, 1.2f, -0.4f), new Color(0.1f, 1f, 0.9f), 2.2f, 4.5f);
            var anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = k.gateController;
            var g = root.AddComponent<GateController>();
            g.animator = anim;
            g.barrierCollider = box;
            g.gateLight = light;
            return g;
        }

        static HUDController BuildHud(Kit k, BattleDirector director)
        {
            var canvas = ScreenCanvas("Canvas HUD");
            var ct = canvas.transform;
            var hud = canvas.gameObject.AddComponent<HUDController>();
            var vig = Img(ct, "Vinheta", k.vignette, Color.white);
            Stretch(vig.rectTransform);

            // Pontuação
            var sp = Img(ct, "Painel Pontos", k.panel, Color.white, true);
            TopLeft(sp.rectTransform, 28f, 24f, 340f, 118f);
            var spLabel = Txt(sp.transform, "Rotulo", "PONTOS", k.heavy, 20, UIStyle.Gold, TextAnchor.UpperLeft);
            TopLeft(spLabel.rectTransform, 24f, 16f, 140f, 26f);
            hud.bestText = Txt(sp.transform, "Recorde", "RECORDE 0000", k.body, 18, UIStyle.Muted, TextAnchor.UpperRight);
            TopLeft(hud.bestText.rectTransform, 150f, 17f, 166f, 26f);
            hud.scoreText = Txt(sp.transform, "Valor", "0000", k.title, 54, UIStyle.GoldLight, TextAnchor.UpperLeft);
            TopLeft(hud.scoreText.rectTransform, 22f, 40f, 300f, 70f);
            hud.scoreDeltaText = Txt(ct, "Ganho", "", k.heavy, 26, UIStyle.Teal, TextAnchor.UpperLeft);
            TopLeft(hud.scoreDeltaText.rectTransform, 34f, 150f, 520f, 36f);
            Shadowed(hud.scoreDeltaText);
            hud.scoreDeltaGroup = hud.scoreDeltaText.gameObject.AddComponent<CanvasGroup>();

            // Fase
            var ph = Img(ct, "Painel Desafio", k.panel, Color.white, true);
            Place(ph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(760f, 100f));
            hud.phaseTitle = Txt(ph.transform, "Titulo", "DESAFIO 1", k.title, 30, UIStyle.Gold, TextAnchor.UpperCenter);
            TopLeft(hud.phaseTitle.rectTransform, 20f, 14f, 720f, 40f);
            hud.phaseDesc = Txt(ph.transform, "Descricao", "", k.body, 21, new Color(0.88f, 0.85f, 0.78f), TextAnchor.UpperCenter);
            TopLeft(hud.phaseDesc.rectTransform, 20f, 56f, 720f, 30f);
            hud.phaseAccent = Img(ph.transform, "Destaque", k.white, UIStyle.Gold);
            Place(hud.phaseAccent.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 4f));
            hud.turnText = Txt(ct, "Turno", "", k.heavy, 24, UIStyle.Teal, TextAnchor.UpperCenter);
            Place(hud.turnText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(700f, 34f));
            Shadowed(hud.turnText);

            // Inimigo
            var ep = Img(ct, "Painel Inimigo", k.panel, Color.white, true);
            Place(ep.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(420f, 172f));
            var en = Txt(ep.transform, "Rotulo Nexus", "NEXUS INIMIGO", k.heavy, 20, UIStyle.Crimson, TextAnchor.UpperLeft);
            TopLeft(en.rectTransform, 24f, 16f, 300f, 26f);
            hud.enemyNexusFill = Bar(k, ep.transform, "Barra Nexus", 24f, 44f, 372f, 28f, UIStyle.Crimson, out hud.enemyNexusText);
            var turretRow = UI("Linha Torre", ep.transform);
            Stretch(turretRow);
            hud.turretGroup = turretRow.gameObject.AddComponent<CanvasGroup>();
            var tl = Txt(turretRow, "Rotulo Torre", "TORRE", k.heavy, 20, new Color(1f, 0.55f, 0.4f), TextAnchor.UpperLeft);
            TopLeft(tl.rectTransform, 24f, 84f, 300f, 26f);
            hud.turretFill = Bar(k, turretRow, "Barra Torre", 24f, 112f, 372f, 22f, new Color(1f, 0.45f, 0.3f), out hud.turretText);
            hud.enemyHandText = Txt(ep.transform, "Mao Inimiga", "", k.body, 18, UIStyle.Muted, TextAnchor.UpperLeft);
            TopLeft(hud.enemyHandText.rectTransform, 24f, 140f, 372f, 24f);

            var help = Btn(k, ct, "Botao Ajuda", "AJUDA  ·  H", false, hud.ToggleHelp, 20);
            Place((RectTransform)help.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -208f), new Vector2(200f, 50f));

            // Jogador
            var pp = Img(ct, "Painel Jogador", k.panel, Color.white, true);
            Place(pp.rectTransform, Vector2.zero, Vector2.zero, new Vector2(28f, 28f), new Vector2(440f, 168f));
            var pn = Txt(pp.transform, "Rotulo Nexus", "SEU NEXUS", k.heavy, 20, UIStyle.Teal, TextAnchor.UpperLeft);
            TopLeft(pn.rectTransform, 24f, 16f, 200f, 26f);
            hud.playerDeckText = Txt(pp.transform, "Baralho", "BARALHO 0", k.body, 18, UIStyle.Muted, TextAnchor.UpperRight);
            TopLeft(hud.playerDeckText.rectTransform, 216f, 17f, 200f, 26f);
            hud.playerNexusFill = Bar(k, pp.transform, "Barra Nexus", 24f, 44f, 392f, 28f, UIStyle.Teal, out hud.playerNexusText);
            var ml = Txt(pp.transform, "Rotulo Mana", "MANA", k.heavy, 20, new Color(0.45f, 0.8f, 1f), TextAnchor.UpperLeft);
            TopLeft(ml.rectTransform, 24f, 86f, 120f, 26f);
            hud.manaText = Txt(pp.transform, "Mana Valor", "0/0", k.heavy, 20, new Color(0.45f, 0.8f, 1f), TextAnchor.UpperRight);
            TopLeft(hud.manaText.rectTransform, 296f, 86f, 120f, 26f);
            hud.manaGems = new Image[Match.ManaCap];
            for (int i = 0; i < Match.ManaCap; i++)
            {
                var g = Img(pp.transform, "Gema " + (i + 1), k.diamond, new Color(1f, 1f, 1f, 0.08f));
                TopLeft(g.rectTransform, 20f + i * 39.5f, 116f, 36f, 36f);
                hud.manaGems[i] = g;
            }

            // Encerrar turno
            hud.endTurnGlow = Img(ct, "Brilho Encerrar", k.glow, new Color(0f, 0f, 0f, 0f));
            Place(hud.endTurnGlow.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-196f, 86f), new Vector2(210f, 470f));
            hud.endTurnGlow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hud.endTurnButton = Btn(k, ct, "Botao Encerrar Turno", "ENCERRAR TURNO", true, director.RequestEndTurn, 26);
            Place((RectTransform)hud.endTurnButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 44f), new Vector2(320f, 84f));
            hud.endTurnLabel = hud.endTurnButton.GetComponentInChildren<Text>();
            var key = Txt(ct, "Atalho", "ESPAÇO", k.heavy, 17, UIStyle.Muted, TextAnchor.LowerCenter);
            Place(key.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-196f, 136f), new Vector2(320f, 26f));

            // Sinergias
            var syn = UI("Sinergias", ct);
            Place(syn, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 70f), new Vector2(420f, 320f));
            var st = Txt(syn, "Titulo", "SINERGIAS", k.title, 24, UIStyle.Gold, TextAnchor.UpperLeft);
            TopLeft(st.rectTransform, 4f, 0f, 300f, 34f);
            Shadowed(st);
            var so = Img(syn, "Linha", k.ornament, Color.white);
            TopLeft(so.rectTransform, -120f, 34f, 420f, 18f);
            hud.synergyEmpty = Txt(syn, "Vazio", "Nenhuma ativa. Junte 2 campeões da mesma região ou uma dupla de combo.", k.body, 19, UIStyle.Muted, TextAnchor.UpperLeft);
            TopLeft(hud.synergyEmpty.rectTransform, 4f, 60f, 360f, 60f);
            Shadowed(hud.synergyEmpty, 1.5f);
            int chips = 6;
            hud.synergyChips = new GameObject[chips];
            hud.synergyTexts = new Text[chips];
            hud.synergyDots = new Image[chips];
            for (int i = 0; i < chips; i++)
            {
                var chip = Img(syn, "Chip " + (i + 1), k.pill, new Color(0.02f, 0.06f, 0.11f, 0.85f), true);
                TopLeft(chip.rectTransform, 0f, 60f + i * 42f, 410f, 36f);
                var dot = Img(chip.transform, "Ponto", k.diamond, Color.white);
                Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(22f, 22f));
                var txt = Txt(chip.transform, "Texto", "", k.body, 18, UIStyle.GoldLight, TextAnchor.MiddleLeft);
                Stretch(txt.rectTransform);
                txt.rectTransform.offsetMin = new Vector2(40f, 0f);
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                hud.synergyChips[i] = chip.gameObject;
                hud.synergyTexts[i] = txt;
                hud.synergyDots[i] = dot;
                chip.gameObject.SetActive(false);
            }

            // Painel de informação
            var info = Img(ct, "Painel Info", k.panel, Color.white, true);
            Place(info.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 20f), new Vector2(420f, 260f));
            hud.infoGroup = info.gameObject.AddComponent<CanvasGroup>();
            hud.infoGroup.blocksRaycasts = false;
            hud.infoTitle = Txt(info.transform, "Titulo", "", k.title, 26, UIStyle.Gold, TextAnchor.UpperLeft);
            TopLeft(hud.infoTitle.rectTransform, 24f, 18f, 372f, 36f);
            hud.infoBody = Txt(info.transform, "Texto", "", k.body, 19, new Color(0.9f, 0.87f, 0.8f), TextAnchor.UpperLeft);
            TopLeft(hud.infoBody.rectTransform, 24f, 62f, 372f, 190f);

            // Avisos e faixa
            hud.toastText = Txt(ct, "Aviso", "", k.heavy, 30, UIStyle.GoldLight, TextAnchor.MiddleCenter);
            Place(hud.toastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1100f, 50f));
            var toastOutline = hud.toastText.gameObject.AddComponent<Outline>();
            toastOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            toastOutline.effectDistance = new Vector2(2f, -2f);
            hud.toastGroup = hud.toastText.gameObject.AddComponent<CanvasGroup>();
            hud.toastGroup.blocksRaycasts = false;

            var banner = Img(ct, "Faixa", k.white, new Color(0.004f, 0.02f, 0.04f, 0.78f));
            Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(2400f, 150f));
            hud.bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            hud.bannerGroup.blocksRaycasts = false;
            var bt = Img(banner.transform, "Linha Topo", k.ornament, Color.white);
            Place(bt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 22f));
            var bb = Img(banner.transform, "Linha Base", k.ornament, Color.white);
            Place(bb.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 22f));
            hud.bannerTitle = Txt(banner.transform, "Titulo", "", k.title, 52, UIStyle.Gold, TextAnchor.MiddleCenter);
            Place(hud.bannerTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1500f, 70f));
            hud.bannerSub = Txt(banner.transform, "Subtitulo", "", k.body, 24, new Color(0.9f, 0.87f, 0.8f), TextAnchor.MiddleCenter);
            Place(hud.bannerSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(1400f, 36f));

            // Ajuda
            hud.helpPanel = Modal(k, ct, "Painel Ajuda", "COMO JOGAR", HowToText(), hud.CloseHelp);

            // Resultado
            hud.result = BuildResult(k, ct);

            var flow = canvas.gameObject.AddComponent<SceneFlow>();
            flow.fader = Fader(ct);
            return hud;
        }

        static ResultPanel BuildResult(Kit k, Transform ct)
        {
            var dim = Img(ct, "Painel Resultado", k.white, new Color(0f, 0.01f, 0.03f, 0.78f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var r = dim.gameObject.AddComponent<ResultPanel>();
            r.group = dim.gameObject.AddComponent<CanvasGroup>();
            var panel = Img(dim.transform, "Painel", k.panelGold, Color.white, true);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 600f));
            r.accent = Img(panel.transform, "Destaque", k.white, UIStyle.Gold);
            Place(r.accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 6f));
            r.title = Txt(panel.transform, "Titulo", "VITÓRIA", k.title, 96, UIStyle.Gold, TextAnchor.UpperCenter);
            TopLeft(r.title.rectTransform, 20f, 34f, 780f, 116f);
            Shadowed(r.title, 4f);
            r.subtitle = Txt(panel.transform, "Subtitulo", "", k.body, 26, new Color(0.9f, 0.87f, 0.8f), TextAnchor.UpperCenter);
            TopLeft(r.subtitle.rectTransform, 20f, 150f, 780f, 36f);
            var ol = Txt(panel.transform, "Rotulo Pontos", "PONTUAÇÃO", k.heavy, 22, UIStyle.Gold, TextAnchor.UpperCenter);
            TopLeft(ol.rectTransform, 20f, 214f, 780f, 30f);
            r.scoreText = Txt(panel.transform, "Pontos", "0000", k.title, 72, UIStyle.GoldLight, TextAnchor.UpperCenter);
            TopLeft(r.scoreText.rectTransform, 20f, 244f, 780f, 90f);
            r.recordText = Txt(panel.transform, "Recorde", "", k.heavy, 22, UIStyle.Teal, TextAnchor.UpperCenter);
            TopLeft(r.recordText.rectTransform, 20f, 334f, 780f, 30f);
            r.detailText = Txt(panel.transform, "Detalhes", "", k.body, 21, UIStyle.Muted, TextAnchor.UpperCenter);
            TopLeft(r.detailText.rectTransform, 40f, 378f, 740f, 70f);
            r.retryButton = Btn(k, panel.transform, "Botao Jogar Novamente", "JOGAR NOVAMENTE", true, r.Retry, 24);
            Place((RectTransform)r.retryButton.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 40f), new Vector2(320f, 72f));
            r.menuButton = Btn(k, panel.transform, "Botao Menu", "MENU PRINCIPAL", false, r.GoToMenu, 24);
            Place((RectTransform)r.menuButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(12f, 40f), new Vector2(320f, 72f));
            return r;
        }
    }
}
