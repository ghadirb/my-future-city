using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyFutureCity.EditorTools
{
    /// <summary>
    /// Builds both scenes from scratch with primitives. Safe to run any number of times:
    /// every run recreates the scene files and reuses (never duplicates) material assets.
    /// Menu: Tools > My Future City > Generate Scenes
    /// </summary>
    public static class SceneGenerator
    {
        public const string MenuScenePath = "Assets/_Game/Scenes/MainMenu/MainMenu.unity";
        public const string GameScenePath = "Assets/_Game/Scenes/Neighborhood01/Neighborhood01.unity";
        private const string MaterialDir = "Assets/_Game/Art/Materials/Generated";

        // ---------- entry points ----------

        [MenuItem("Tools/My Future City/Generate Scenes")]
        public static void GenerateAll()
        {
            EnsureFolder("Assets/_Game/Scenes/MainMenu");
            EnsureFolder("Assets/_Game/Scenes/Neighborhood01");
            EnsureFolder(MaterialDir);

            GenerateMainMenu();
            GenerateNeighborhood();
            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MyFutureCity] Scenes generated.");
        }

        public static void RegisterBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        // ---------- helpers ----------

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static Material Mat(string name, Color color, string shaderName)
        {
            string path = MaterialDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader sh = Shader.Find(shaderName);
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Creates the base palette for the low-poly world.  These objects deliberately use
        /// an unlit shader: the target scene has no real-time shadows, and Mobile/Diffuse
        /// produced radically different (often nearly black) colours on the Android build.
        /// Unlit colours make landmarks and interaction targets readable on every device.
        /// </summary>
        private static Material Lit(string name, float r, float g, float b)
        {
            return Mat(name, new Color(r, g, b), "Unlit/Color");
        }

        private static Material Glow(string name, float r, float g, float b)
        {
            return Mat(name, new Color(r, g, b), "Unlit/Color");
        }

        private static GameObject Empty(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return go;
        }

        private static UnityEngine.SceneManagement.Scene NewEmptyScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void Save(UnityEngine.SceneManagement.Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new System.Exception("Could not save scene: " + path);
        }

        private static void AddSun(Color color, float intensity)
        {
            var go = new GameObject("Sun");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.shadows = LightShadows.None;   // cheap by default for low-end phones
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Camera AddCamera(Vector3 pos, Color sky)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 160f;
            go.AddComponent<AudioListener>();
            go.transform.position = pos;
            return cam;
        }

        // ---------- main menu ----------

        private static void GenerateMainMenu()
        {
            var scene = NewEmptyScene();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.22f);
            AddSun(new Color(1f, 0.96f, 0.88f), 0.65f);

            var cam = AddCamera(new Vector3(0f, 9f, -22f), new Color(0.45f, 0.75f, 0.95f));
            cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

            var spin = Empty("Backdrop", null, Vector3.zero);
            spin.AddComponent<MenuBackdropSpin>();
            Prim(PrimitiveType.Cylinder, "Ground", spin.transform, new Vector3(0, -0.5f, 0), new Vector3(30, 0.5f, 30), Lit("Grass", 0.45f, 0.75f, 0.35f), false);
            Color[] cols = { new Color(0.95f, 0.8f, 0.35f), new Color(0.45f, 0.65f, 0.9f), new Color(0.95f, 0.6f, 0.7f), new Color(0.5f, 0.85f, 0.7f), new Color(0.9f, 0.55f, 0.25f) };
            var rnd = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                float r = 6f + (i % 3) * 3.5f;
                float h = 2f + (float)rnd.NextDouble() * 4f;
                var m = Lit("Menu_" + (i % cols.Length), cols[i % cols.Length].r, cols[i % cols.Length].g, cols[i % cols.Length].b);
                Prim(PrimitiveType.Cube, "Building" + i, spin.transform, new Vector3(Mathf.Cos(a) * r, h / 2f, Mathf.Sin(a) * r), new Vector3(3f, h, 3f), m, false);
            }

            var controller = new GameObject("MainMenuController");
            controller.AddComponent<MainMenuController>();
            Save(scene, MenuScenePath);
        }

        // ---------- neighbourhood ----------

        private struct Zone
        {
            public float x, z, hx, hz;
            public Zone(float x, float z, float hx, float hz) { this.x = x; this.z = z; this.hx = hx; this.hz = hz; }
        }

        private class Human
        {
            public GameObject root;
            public Transform leftArm, rightArm, leftLeg, rightLeg;
            public Renderer shirt;
            public GameObject hairShort, hairLong;
        }

        private static Human BuildHuman(Transform parent, Material shirtMat, Material hairMat, bool hat)
        {
            var skin = Lit("Skin", 0.95f, 0.8f, 0.65f);
            var pants = Lit("Pants", 0.2f, 0.2f, 0.3f);
            var dark = Lit("Eye", 0.05f, 0.05f, 0.05f);

            var h = new Human();
            h.root = Empty("Visual", parent, Vector3.zero);
            var t = h.root.transform;
            h.shirt = Prim(PrimitiveType.Cube, "Shirt", t, new Vector3(0, 1.15f, 0), new Vector3(0.6f, 0.7f, 0.35f), shirtMat, false).GetComponent<Renderer>();
            Prim(PrimitiveType.Sphere, "Head", t, new Vector3(0, 1.78f, 0), new Vector3(0.42f, 0.42f, 0.42f), skin, false);
            Prim(PrimitiveType.Sphere, "EyeL", t, new Vector3(-0.1f, 1.82f, 0.18f), new Vector3(0.07f, 0.07f, 0.07f), dark, false);
            Prim(PrimitiveType.Sphere, "EyeR", t, new Vector3(0.1f, 1.82f, 0.18f), new Vector3(0.07f, 0.07f, 0.07f), dark, false);
            h.hairShort = Prim(PrimitiveType.Sphere, "HairShort", t, new Vector3(0, 1.92f, -0.02f), new Vector3(0.44f, 0.26f, 0.44f), hairMat, false);
            h.hairLong = Prim(PrimitiveType.Cube, "HairLong", t, new Vector3(0, 1.62f, -0.17f), new Vector3(0.44f, 0.62f, 0.14f), hairMat, false);
            h.hairLong.SetActive(false);
            if (hat) Prim(PrimitiveType.Sphere, "HardHat", t, new Vector3(0, 1.97f, 0), new Vector3(0.5f, 0.28f, 0.5f), Lit("HardHat", 1f, 0.85f, 0.1f), false);

            h.leftLeg = Empty("LegL", t, new Vector3(-0.15f, 0.8f, 0)).transform;
            h.rightLeg = Empty("LegR", t, new Vector3(0.15f, 0.8f, 0)).transform;
            Prim(PrimitiveType.Cube, "Leg", h.leftLeg, new Vector3(0, -0.4f, 0), new Vector3(0.22f, 0.8f, 0.24f), pants, false);
            Prim(PrimitiveType.Cube, "Leg", h.rightLeg, new Vector3(0, -0.4f, 0), new Vector3(0.22f, 0.8f, 0.24f), pants, false);
            h.leftArm = Empty("ArmL", t, new Vector3(-0.42f, 1.45f, 0)).transform;
            h.rightArm = Empty("ArmR", t, new Vector3(0.42f, 1.45f, 0)).transform;
            Prim(PrimitiveType.Cube, "Arm", h.leftArm, new Vector3(0, -0.3f, 0), new Vector3(0.16f, 0.62f, 0.18f), skin, false);
            Prim(PrimitiveType.Cube, "Arm", h.rightArm, new Vector3(0, -0.3f, 0), new Vector3(0.16f, 0.62f, 0.18f), skin, false);
            return h;
        }

        private static GameObject MakeBuilding(Transform parent, string name, Vector3 center, Vector3 size, Material wall, Material roof, int doorSideZ)
        {
            var window = Lit("Window", 0.15f, 0.17f, 0.22f);
            var door = Lit("Door", 0.35f, 0.22f, 0.15f);
            var root = Empty(name, parent, center);
            Prim(PrimitiveType.Cube, "Body", root.transform, new Vector3(0, size.y / 2f, 0), size, wall, true);
            Prim(PrimitiveType.Cube, "Roof", root.transform, new Vector3(0, size.y + 0.25f, 0), new Vector3(size.x + 0.8f, 0.5f, size.z + 0.8f), roof, false);
            float fz = doorSideZ * (size.z / 2f + 0.03f);
            Prim(PrimitiveType.Cube, "Door", root.transform, new Vector3(0, 1.1f, fz), new Vector3(1.4f, 2.2f, 0.1f), door, false);
            int cols = Mathf.Max(2, Mathf.FloorToInt(size.x / 3.5f));
            for (int i = 0; i < cols; i++)
            {
                float x = (i - (cols - 1) / 2f) * (size.x / cols) * 0.95f;
                if (Mathf.Abs(x) < 1.2f) continue;      // keep the door clear
                Prim(PrimitiveType.Cube, "Window" + i, root.transform, new Vector3(x, size.y * 0.62f, fz), new Vector3(1.1f, 1.1f, 0.1f), window, false);
            }
            return root;
        }

        private static void AddPickupMarker(Transform parent, List<Zone> zones, Vector3 position, string name, string missionId, string targetId, string displayNameFa, int order, Material material, Material beacon)
        {
            var root = Empty(name + order, parent, position);
            var visual = Empty("Visual", root.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "Item", visual.transform, Vector3.zero, new Vector3(0.62f, 0.62f, 0.24f), material, false);
            Prim(PrimitiveType.Cylinder, "Beacon", visual.transform, new Vector3(0, 2f, 0), new Vector3(0.09f, 1.35f, 0.09f), beacon, false);
            var item = root.AddComponent<PickupInteractable>();
            item.missionId = missionId;
            item.targetId = targetId;
            item.displayNameFa = displayNameFa;
            item.order = order;
            item.spin = visual.transform;
            item.radius = 2.4f;
            zones.Add(new Zone(position.x, position.z, 2, 2));
        }

        private static void AddTaskSpot(Transform parent, List<Zone> zones, Vector3 position, string name, string missionId, string targetId, string promptFa, int order, Material pendingMaterial, Material completedMaterial, Material beacon)
        {
            var root = Empty(name + order, parent, position);
            var pending = Prim(PrimitiveType.Cube, "Pending", root.transform, new Vector3(0, 0.45f, 0), new Vector3(0.85f, 0.85f, 0.22f), pendingMaterial, false);
            Prim(PrimitiveType.Cylinder, "Beacon", pending.transform, new Vector3(0, 1.9f, 0), new Vector3(0.09f, 1.3f, 0.09f), beacon, false);
            var completed = Prim(PrimitiveType.Cube, "Completed", root.transform, new Vector3(0, 0.45f, 0), new Vector3(0.85f, 0.85f, 0.22f), completedMaterial, false);
            var task = root.AddComponent<RepairInteractable>();
            task.missionId = missionId;
            task.targetId = targetId;
            task.order = order;
            task.promptFa = promptFa;
            task.radius = 2.4f;
            task.brokenVisual = pending;
            task.fixedVisual = completed;
            zones.Add(new Zone(position.x, position.z, 2, 2));
        }

        private static void GenerateNeighborhood()
        {
            var scene = NewEmptyScene();
            RenderSettings.ambientMode = AmbientMode.Flat;
            // World materials are deliberately unlit; retaining a mild sky/sun setup leaves
            // future lit accents possible without allowing device lighting to darken the city.
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.22f);
            AddSun(new Color(1f, 0.95f, 0.85f), 0.65f);

            var grass = Lit("Grass", 0.45f, 0.75f, 0.35f);
            var road = Lit("Road", 0.35f, 0.36f, 0.4f);
            var stone = Lit("Stone", 0.8f, 0.78f, 0.7f);
            var water = Lit("Water", 0.3f, 0.6f, 0.9f);
            var trunk = Lit("Trunk", 0.45f, 0.3f, 0.2f);
            var leaves = Lit("Leaves", 0.2f, 0.6f, 0.25f);
            var roofMat = Lit("Roof", 0.7f, 0.3f, 0.25f);

            var zones = new List<Zone>();

            // ----- environment (static, batched) -----
            var env = Empty("Environment", null, Vector3.zero);
            Prim(PrimitiveType.Plane, "Ground", env.transform, Vector3.zero, new Vector3(8f, 1f, 8f), grass, true);
            Prim(PrimitiveType.Cube, "RoadEW", env.transform, new Vector3(0, 0.03f, 0), new Vector3(80f, 0.06f, 8f), road, false);
            Prim(PrimitiveType.Cube, "RoadNS", env.transform, new Vector3(0, 0.035f, 0), new Vector3(8f, 0.06f, 80f), road, false);
            Prim(PrimitiveType.Cylinder, "Plaza", env.transform, new Vector3(0, 0.05f, 0), new Vector3(18f, 0.05f, 18f), stone, false);
            Prim(PrimitiveType.Cylinder, "FountainBasin", env.transform, new Vector3(0, 0.3f, 0), new Vector3(4.4f, 0.3f, 4.4f), stone, true);
            Prim(PrimitiveType.Cylinder, "FountainWater", env.transform, new Vector3(0, 0.62f, 0), new Vector3(3.8f, 0.02f, 3.8f), water, false);
            Prim(PrimitiveType.Cylinder, "FountainPillar", env.transform, new Vector3(0, 1.0f, 0), new Vector3(0.6f, 1.0f, 0.6f), stone, true);
            zones.Add(new Zone(0, 0, 12, 12));

            // Boundary walls (invisible)
            var walls = Empty("Boundary", null, Vector3.zero);
            float[,] wallDefs = { { 0, 41, 84, 2 }, { 0, -41, 84, 2 }, { 41, 0, 2, 84 }, { -41, 0, 2, 84 } };
            for (int i = 0; i < 4; i++)
            {
                var w = Empty("Wall" + i, walls.transform, new Vector3(wallDefs[i, 0], 5f, wallDefs[i, 1]));
                w.AddComponent<BoxCollider>().size = new Vector3(wallDefs[i, 2], 10f, wallDefs[i, 3]);
            }

            // Ordinary buildings
            var houses = Empty("Buildings", null, Vector3.zero);
            MakeBuilding(houses.transform, "Bakery", new Vector3(-24, 0, -16), new Vector3(8, 5, 7), Lit("WallYellow", 0.95f, 0.8f, 0.35f), roofMat, +1);
            zones.Add(new Zone(-24, -16, 7, 6));
            MakeBuilding(houses.transform, "HouseMint", new Vector3(-24, 0, -30), new Vector3(8, 5, 7), Lit("WallMint", 0.5f, 0.85f, 0.7f), roofMat, +1);
            zones.Add(new Zone(-24, -30, 7, 6));
            MakeBuilding(houses.transform, "HousePink", new Vector3(24, 0, -30), new Vector3(8, 5, 7), Lit("WallPink", 0.95f, 0.6f, 0.7f), roofMat, +1);
            zones.Add(new Zone(24, -30, 7, 6));
            MakeBuilding(houses.transform, "Workshop", new Vector3(24, 0, 15), new Vector3(10, 5, 8), Lit("WallOrange", 0.9f, 0.55f, 0.25f), roofMat, -1);
            zones.Add(new Zone(24, 15, 8, 7));
            var home = MakeBuilding(houses.transform, "PlayerHome", new Vector3(30, 0, -16), new Vector3(8, 5, 7), Lit("WallBlue", 0.45f, 0.65f, 0.9f), roofMat, +1);
            zones.Add(new Zone(30, -16, 7, 6));

            var homePoint = Empty("HomePoint", home.transform.parent, new Vector3(30, 0, -11.2f));
            var homeI = homePoint.AddComponent<HomeInteractable>();
            homeI.promptFa = "خانهٔ من";
            homeI.radius = 3f;
            Prim(PrimitiveType.Cylinder, "Mat", homePoint.transform, new Vector3(0, 0.03f, 0), new Vector3(1.6f, 0.03f, 1.6f), Lit("DoorMat", 0.8f, 0.3f, 0.3f), false);

            // ----- school (dynamic: look changes with restoration) -----
            var landmarks = Empty("Landmarks", null, Vector3.zero);
            var schoolRoot = MakeBuilding(landmarks.transform, "School", new Vector3(-26, 0, 16), new Vector3(16, 7, 10), Lit("SchoolAbandoned", 0.5f, 0.48f, 0.45f), Lit("RoofSchool", 0.45f, 0.3f, 0.3f), -1);
            zones.Add(new Zone(-26, 16, 11, 8));
            var bodyR = schoolRoot.transform.Find("Body").GetComponent<Renderer>();
            var visual = schoolRoot.AddComponent<BuildingVisual>();
            visual.buildingId = "school";
            visual.bodyRenderers = new[] { bodyR };
            visual.abandonedMaterial = Lit("SchoolAbandoned", 0.5f, 0.48f, 0.45f);
            visual.renovatingMaterial = Lit("SchoolRenovating", 0.75f, 0.7f, 0.55f);
            visual.restoredMaterial = Lit("SchoolRestored", 0.98f, 0.85f, 0.45f);

            var boards = Lit("Boards", 0.4f, 0.3f, 0.2f);
            var abandoned = Empty("AbandonedProps", schoolRoot.transform, Vector3.zero);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                var b = Prim(PrimitiveType.Cube, "Board" + i, abandoned.transform, new Vector3(i * 3f, 4.4f, -5.1f), new Vector3(1.8f, 0.2f, 0.1f), boards, false);
                b.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 20f : -20f);
            }
            Prim(PrimitiveType.Cube, "Debris1", abandoned.transform, new Vector3(-5, 0.25f, -7), new Vector3(1.2f, 0.5f, 0.8f), Lit("Debris", 0.4f, 0.4f, 0.4f), false);
            Prim(PrimitiveType.Cube, "Debris2", abandoned.transform, new Vector3(4, 0.2f, -7.5f), new Vector3(0.9f, 0.4f, 1.1f), Lit("Debris", 0.4f, 0.4f, 0.4f), false);

            var scaffold = Empty("Scaffolding", schoolRoot.transform, Vector3.zero);
            var wood = Lit("Scaffold", 0.7f, 0.55f, 0.3f);
            for (int i = -3; i <= 3; i += 2)
                Prim(PrimitiveType.Cube, "Pole" + i, scaffold.transform, new Vector3(i * 2.4f, 2.5f, -6f), new Vector3(0.2f, 5f, 0.2f), wood, false);
            Prim(PrimitiveType.Cube, "Bar1", scaffold.transform, new Vector3(0, 2f, -6f), new Vector3(15f, 0.2f, 0.2f), wood, false);
            Prim(PrimitiveType.Cube, "Bar2", scaffold.transform, new Vector3(0, 4f, -6f), new Vector3(15f, 0.2f, 0.2f), wood, false);

            var restored = Empty("RestoredProps", schoolRoot.transform, Vector3.zero);
            var glow = Glow("WindowGlow", 1f, 0.95f, 0.6f);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                Prim(PrimitiveType.Cube, "Glow" + i, restored.transform, new Vector3(i * 3f, 4.3f, -5.06f), new Vector3(1.3f, 1.3f, 0.08f), glow, false);
            }
            var flowerMat = Lit("Flower", 0.95f, 0.4f, 0.6f);
            for (int i = -3; i <= 3; i++)
                Prim(PrimitiveType.Sphere, "Flower" + i, restored.transform, new Vector3(i * 2f, 0.3f, -6.4f), new Vector3(0.6f, 0.6f, 0.6f), flowerMat, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cylinder, "TreeTrunk", restored.transform, new Vector3(s * 10f, 1.0f, -7f), new Vector3(0.4f, 1f, 0.4f), trunk, false);
                Prim(PrimitiveType.Sphere, "TreeTop", restored.transform, new Vector3(s * 10f, 3.0f, -7f), new Vector3(3f, 3f, 3f), leaves, false);
            }
            Prim(PrimitiveType.Cylinder, "FlagPole", restored.transform, new Vector3(0, 9.2f, 0), new Vector3(0.12f, 2f, 0.12f), Lit("Pole", 0.8f, 0.8f, 0.8f), false);
            Prim(PrimitiveType.Cube, "Flag", restored.transform, new Vector3(0.7f, 10.4f, 0), new Vector3(1.4f, 0.8f, 0.05f), Lit("Flag", 0.2f, 0.6f, 0.9f), false);

            visual.abandonedObjects = new[] { abandoned };
            visual.renovatingObjects = new[] { scaffold };
            visual.restoredObjects = new[] { restored };

            // ----- clinic (dynamic: repaired by the doctor job) -----
            var clinicRoot = MakeBuilding(landmarks.transform, "Clinic", new Vector3(0, 0, 28), new Vector3(12, 5, 8), Lit("ClinicAbandoned", 0.62f, 0.6f, 0.58f), Lit("RoofClinic", 0.85f, 0.25f, 0.25f), -1);
            zones.Add(new Zone(0, 28, 9, 7));
            var clinicBody = clinicRoot.transform.Find("Body").GetComponent<Renderer>();
            var clinicVisual = clinicRoot.AddComponent<BuildingVisual>();
            clinicVisual.buildingId = "clinic";
            clinicVisual.bodyRenderers = new[] { clinicBody };
            clinicVisual.abandonedMaterial = Lit("ClinicAbandoned", 0.62f, 0.6f, 0.58f);
            clinicVisual.renovatingMaterial = Lit("ClinicRenovating", 0.78f, 0.78f, 0.62f);
            clinicVisual.restoredMaterial = Lit("ClinicRestored", 0.88f, 0.96f, 0.98f);
            var clinicClosed = Empty("ClinicClosedProps", clinicRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "ClosedSign", clinicClosed.transform, new Vector3(0, 3.1f, -4.08f), new Vector3(3.6f, 0.8f, 0.12f), Lit("ClosedSign", 0.55f, 0.18f, 0.15f), false);
            Prim(PrimitiveType.Cube, "Crate", clinicClosed.transform, new Vector3(-4.4f, 0.45f, -4.6f), new Vector3(1.2f, 0.9f, 1.1f), boards, false);
            var clinicWork = Empty("ClinicWorkProps", clinicRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "WorkSign", clinicWork.transform, new Vector3(0, 3.1f, -4.08f), new Vector3(3.6f, 0.8f, 0.12f), Lit("WorkSign", 0.95f, 0.7f, 0.18f), false);
            Prim(PrimitiveType.Cylinder, "WorkPole", clinicWork.transform, new Vector3(-4.4f, 1.0f, -4.6f), new Vector3(0.12f, 1f, 0.12f), wood, false);
            var clinicOpen = Empty("ClinicOpenProps", clinicRoot.transform, Vector3.zero);
            var clinicBlue = Lit("ClinicBlue", 0.2f, 0.62f, 0.9f);
            Prim(PrimitiveType.Cube, "OpenSign", clinicOpen.transform, new Vector3(0, 3.1f, -4.08f), new Vector3(3.6f, 0.8f, 0.12f), clinicBlue, false);
            Prim(PrimitiveType.Cube, "CrossVertical", clinicOpen.transform, new Vector3(0, 3.1f, -4.17f), new Vector3(0.35f, 0.62f, 0.08f), Lit("CrossWhite", 1f, 1f, 1f), false);
            Prim(PrimitiveType.Cube, "CrossHorizontal", clinicOpen.transform, new Vector3(0, 3.1f, -4.18f), new Vector3(0.75f, 0.25f, 0.08f), Lit("CrossWhite2", 1f, 1f, 1f), false);
            for (int i = -1; i <= 1; i += 2)
                Prim(PrimitiveType.Sphere, "ClinicFlower" + i, clinicOpen.transform, new Vector3(i * 4.4f, 0.32f, -4.7f), new Vector3(0.55f, 0.55f, 0.55f), flowerMat, false);
            clinicVisual.abandonedObjects = new[] { clinicClosed };
            clinicVisual.renovatingObjects = new[] { clinicWork };
            clinicVisual.restoredObjects = new[] { clinicOpen };

            // ----- recycling station (dynamic: restored by the environment job) -----
            var recycleRoot = MakeBuilding(landmarks.transform, "RecyclingStation", new Vector3(27, 0, 30), new Vector3(10, 5, 7), Lit("RecycleAbandoned", 0.5f, 0.52f, 0.48f), Lit("RoofRecycle", 0.28f, 0.6f, 0.42f), -1);
            zones.Add(new Zone(27, 30, 8, 6));
            var recycleBody = recycleRoot.transform.Find("Body").GetComponent<Renderer>();
            var recycleVisual = recycleRoot.AddComponent<BuildingVisual>();
            recycleVisual.buildingId = "recycling";
            recycleVisual.bodyRenderers = new[] { recycleBody };
            recycleVisual.abandonedMaterial = Lit("RecycleAbandoned", 0.5f, 0.52f, 0.48f);
            recycleVisual.renovatingMaterial = Lit("RecycleRenovating", 0.72f, 0.72f, 0.52f);
            recycleVisual.restoredMaterial = Lit("RecycleRestored", 0.48f, 0.86f, 0.65f);
            var recycleClosed = Empty("RecycleClosedProps", recycleRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "WastePile", recycleClosed.transform, new Vector3(-3.5f, 0.38f, -4.1f), new Vector3(1.4f, 0.75f, 1.0f), Lit("WastePile", 0.35f, 0.32f, 0.28f), false);
            Prim(PrimitiveType.Cube, "ClosedRecycleSign", recycleClosed.transform, new Vector3(0, 3.1f, -3.58f), new Vector3(3f, 0.7f, 0.12f), Lit("ClosedRecycleSign", 0.5f, 0.2f, 0.15f), false);
            var recycleWork = Empty("RecycleWorkProps", recycleRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "WorkRecycleSign", recycleWork.transform, new Vector3(0, 3.1f, -3.58f), new Vector3(3f, 0.7f, 0.12f), Lit("WorkRecycleSign", 0.95f, 0.72f, 0.18f), false);
            Prim(PrimitiveType.Cylinder, "WorkRecyclePole", recycleWork.transform, new Vector3(3.5f, 1.1f, -4f), new Vector3(0.12f, 1.1f, 0.12f), wood, false);
            var recycleOpen = Empty("RecycleOpenProps", recycleRoot.transform, Vector3.zero);
            var recycleGreen = Lit("RecycleGreen", 0.22f, 0.78f, 0.42f);
            Prim(PrimitiveType.Cube, "RecycleSign", recycleOpen.transform, new Vector3(0, 3.1f, -3.58f), new Vector3(3f, 0.7f, 0.12f), recycleGreen, false);
            for (int i = -1; i <= 1; i++)
                Prim(PrimitiveType.Cylinder, "Bin" + i, recycleOpen.transform, new Vector3(i * 2.2f, 0.55f, -4.0f), new Vector3(0.45f, 0.55f, 0.45f), i == 0 ? Lit("BinBlue", 0.25f, 0.55f, 0.95f) : recycleGreen, false);
            recycleVisual.abandonedObjects = new[] { recycleClosed };
            recycleVisual.renovatingObjects = new[] { recycleWork };
            recycleVisual.restoredObjects = new[] { recycleOpen };

            // ----- community farm (dynamic: restored by the farmer job) -----
            var farmRoot = MakeBuilding(landmarks.transform, "CommunityFarm", new Vector3(7, 0, -32), new Vector3(11, 4.5f, 7), Lit("FarmAbandoned", 0.54f, 0.48f, 0.4f), Lit("RoofFarm", 0.55f, 0.35f, 0.2f), +1);
            zones.Add(new Zone(7, -32, 8, 6));
            var farmBody = farmRoot.transform.Find("Body").GetComponent<Renderer>();
            var farmVisual = farmRoot.AddComponent<BuildingVisual>();
            farmVisual.buildingId = "farm";
            farmVisual.bodyRenderers = new[] { farmBody };
            farmVisual.abandonedMaterial = Lit("FarmAbandoned", 0.54f, 0.48f, 0.4f);
            farmVisual.renovatingMaterial = Lit("FarmRenovating", 0.7f, 0.62f, 0.4f);
            farmVisual.restoredMaterial = Lit("FarmRestored", 0.75f, 0.9f, 0.55f);
            var farmClosed = Empty("FarmClosedProps", farmRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "DryPatch", farmClosed.transform, new Vector3(-3.7f, 0.13f, -4f), new Vector3(3f, 0.2f, 1.7f), Lit("DryPatch", 0.45f, 0.3f, 0.15f), false);
            Prim(PrimitiveType.Cube, "FarmClosedSign", farmClosed.transform, new Vector3(0, 2.7f, -3.58f), new Vector3(3.2f, 0.65f, 0.12f), Lit("FarmClosedSign", 0.5f, 0.2f, 0.15f), false);
            var farmWork = Empty("FarmWorkProps", farmRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "FarmWorkSign", farmWork.transform, new Vector3(0, 2.7f, -3.58f), new Vector3(3.2f, 0.65f, 0.12f), Lit("FarmWorkSign", 0.95f, 0.72f, 0.18f), false);
            Prim(PrimitiveType.Cylinder, "FarmWorkPole", farmWork.transform, new Vector3(3.7f, 1f, -4f), new Vector3(0.12f, 1f, 0.12f), wood, false);
            var farmOpen = Empty("FarmOpenProps", farmRoot.transform, Vector3.zero);
            var cropMat = Lit("Crop", 0.22f, 0.78f, 0.32f);
            for (int row = -1; row <= 1; row++)
                for (int crop = -1; crop <= 1; crop++)
                    Prim(PrimitiveType.Sphere, "Crop" + row + "_" + crop, farmOpen.transform, new Vector3(row * 2.4f, 0.36f, -4f + crop * 0.7f), new Vector3(0.5f, 0.7f, 0.5f), cropMat, false);
            Prim(PrimitiveType.Cube, "FarmOpenSign", farmOpen.transform, new Vector3(0, 2.7f, -3.58f), new Vector3(3.2f, 0.65f, 0.12f), Lit("FarmOpenSign", 0.25f, 0.72f, 0.35f), false);
            farmVisual.abandonedObjects = new[] { farmClosed };
            farmVisual.renovatingObjects = new[] { farmWork };
            farmVisual.restoredObjects = new[] { farmOpen };

            // ----- fire station (dynamic: restored by the firefighter job) -----
            var fireRoot = MakeBuilding(landmarks.transform, "FireStation", new Vector3(-28, 0, 0), new Vector3(11, 5, 7), Lit("FireAbandoned", 0.5f, 0.45f, 0.42f), Lit("RoofFire", 0.72f, 0.2f, 0.18f), +1);
            zones.Add(new Zone(-28, 0, 8, 6));
            var fireBody = fireRoot.transform.Find("Body").GetComponent<Renderer>();
            var fireVisual = fireRoot.AddComponent<BuildingVisual>();
            fireVisual.buildingId = "fire_station";
            fireVisual.bodyRenderers = new[] { fireBody };
            fireVisual.abandonedMaterial = Lit("FireAbandoned", 0.5f, 0.45f, 0.42f);
            fireVisual.renovatingMaterial = Lit("FireRenovating", 0.76f, 0.65f, 0.38f);
            fireVisual.restoredMaterial = Lit("FireRestored", 0.9f, 0.35f, 0.28f);
            var fireClosed = Empty("FireClosedProps", fireRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "FireClosedSign", fireClosed.transform, new Vector3(0, 3.1f, -3.58f), new Vector3(3.2f, 0.7f, 0.12f), Lit("FireClosedSign", 0.45f, 0.18f, 0.15f), false);
            Prim(PrimitiveType.Cube, "FireDebris", fireClosed.transform, new Vector3(-3.8f, 0.35f, -4f), new Vector3(1.5f, 0.7f, 0.9f), Lit("FireDebris", 0.35f, 0.32f, 0.3f), false);
            var fireWork = Empty("FireWorkProps", fireRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "FireWorkSign", fireWork.transform, new Vector3(0, 3.1f, -3.58f), new Vector3(3.2f, 0.7f, 0.12f), Lit("FireWorkSign", 0.95f, 0.72f, 0.18f), false);
            Prim(PrimitiveType.Cylinder, "FireWorkPole", fireWork.transform, new Vector3(3.8f, 1.0f, -4f), new Vector3(0.12f, 1f, 0.12f), wood, false);
            var fireOpen = Empty("FireOpenProps", fireRoot.transform, Vector3.zero);
            var fireRed = Lit("FireRed", 0.9f, 0.18f, 0.15f);
            Prim(PrimitiveType.Cube, "FireOpenSign", fireOpen.transform, new Vector3(0, 3.1f, -3.58f), new Vector3(3.2f, 0.7f, 0.12f), fireRed, false);
            Prim(PrimitiveType.Cylinder, "FireLight", fireOpen.transform, new Vector3(0, 4.25f, -3.75f), new Vector3(0.26f, 0.18f, 0.26f), Glow("FireLightGlow", 1f, 0.32f, 0.2f), false);
            Prim(PrimitiveType.Cube, "FireDoor", fireOpen.transform, new Vector3(-3.2f, 1.25f, -3.62f), new Vector3(1.7f, 2.5f, 0.1f), Lit("FireDoorBlue", 0.2f, 0.5f, 0.8f), false);
            fireVisual.abandonedObjects = new[] { fireClosed };
            fireVisual.renovatingObjects = new[] { fireWork };
            fireVisual.restoredObjects = new[] { fireOpen };

            // Repair points around the school
            var brokenMat = Lit("Broken", 0.9f, 0.25f, 0.2f);
            var fixedMat = Lit("Fixed", 0.3f, 0.85f, 0.4f);
            var beacon = Glow("Beacon", 1f, 0.9f, 0.2f);
            Vector3[] repairPos = { new Vector3(-31, 0, 9.3f), new Vector3(-21, 0, 9.3f), new Vector3(-16.3f, 0, 14f) };
            var repairs = Empty("RepairPoints", null, Vector3.zero);
            for (int i = 0; i < repairPos.Length; i++)
            {
                var rp = Empty("RepairPoint" + i, repairs.transform, repairPos[i]);
                var br = Prim(PrimitiveType.Cube, "Broken", rp.transform, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.8f, 0.8f), brokenMat, false);
                Prim(PrimitiveType.Cylinder, "Beacon", br.transform, new Vector3(0, 2.2f, 0), new Vector3(0.12f, 1.6f, 0.12f), beacon, false);
                var fx = Prim(PrimitiveType.Cube, "Fixed", rp.transform, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.6f, 0.6f), fixedMat, false);
                var ri = rp.AddComponent<RepairInteractable>();
                ri.missionId = "eng_03_repair_school";
                ri.targetId = "school_repair";
                ri.order = i;
                ri.promptFa = "تعمیر";
                ri.radius = 2.4f;
                ri.brokenVisual = br;
                ri.fixedVisual = fx;
                zones.Add(new Zone(repairPos[i].x, repairPos[i].z, 2, 2));
            }

            // ----- NPC -----
            var npcRoot = Empty("NPC_Behrooz", null, new Vector3(24, 0, 8.5f));
            npcRoot.transform.rotation = Quaternion.Euler(0, 180f, 0);
            var npcHuman = BuildHuman(npcRoot.transform, Lit("NpcShirt", 0.95f, 0.5f, 0.1f), Lit("NpcHair", 0.25f, 0.2f, 0.18f), true);
            var npc = npcRoot.AddComponent<NpcInteractable>();
            npc.npcId = "npc_engineer";
            npc.displayNameFa = "استاد بهروز";
            npc.radius = 3f;
            Prim(PrimitiveType.Sphere, "Marker", npcRoot.transform, new Vector3(0, 2.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), beacon, false);
            var npcCol = npcRoot.AddComponent<CapsuleCollider>();
            npcCol.height = 1.9f; npcCol.radius = 0.4f; npcCol.center = new Vector3(0, 0.95f, 0);
            var npcAnim = npcRoot.AddComponent<ProceduralWalkAnimator>();
            npcAnim.leftArm = npcHuman.leftArm; npcAnim.rightArm = npcHuman.rightArm; npcAnim.leftLeg = npcHuman.leftLeg; npcAnim.rightLeg = npcHuman.rightLeg;
            zones.Add(new Zone(24, 8.5f, 3, 3));

            var teacherRoot = Empty("NPC_Noori", null, new Vector3(-13, 0, 27));
            teacherRoot.transform.rotation = Quaternion.Euler(0, 210f, 0);
            var teacherHuman = BuildHuman(teacherRoot.transform, Lit("TeacherShirt", 0.75f, 0.35f, 0.75f), Lit("TeacherHair", 0.18f, 0.1f, 0.06f), false);
            var teacher = teacherRoot.AddComponent<NpcInteractable>();
            teacher.npcId = "npc_teacher";
            teacher.displayNameFa = "خانم نوری";
            teacher.radius = 3f;
            Prim(PrimitiveType.Sphere, "Marker", teacherRoot.transform, new Vector3(0, 2.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), beacon, false);
            var teacherCol = teacherRoot.AddComponent<CapsuleCollider>();
            teacherCol.height = 1.9f; teacherCol.radius = 0.4f; teacherCol.center = new Vector3(0, 0.95f, 0);
            var teacherAnim = teacherRoot.AddComponent<ProceduralWalkAnimator>();
            teacherAnim.leftArm = teacherHuman.leftArm; teacherAnim.rightArm = teacherHuman.rightArm; teacherAnim.leftLeg = teacherHuman.leftLeg; teacherAnim.rightLeg = teacherHuman.rightLeg;
            zones.Add(new Zone(-13, 27, 3, 3));

            var doctorRoot = Empty("NPC_DrFarhad", null, new Vector3(14, 0, 31));
            doctorRoot.transform.rotation = Quaternion.Euler(0, 230f, 0);
            var doctorHuman = BuildHuman(doctorRoot.transform, Lit("DoctorCoat", 0.9f, 0.95f, 0.98f), Lit("DoctorHair", 0.12f, 0.08f, 0.05f), true);
            var doctor = doctorRoot.AddComponent<NpcInteractable>();
            doctor.npcId = "npc_doctor";
            doctor.displayNameFa = "دکتر فرهاد";
            doctor.radius = 3f;
            Prim(PrimitiveType.Sphere, "Marker", doctorRoot.transform, new Vector3(0, 2.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), beacon, false);
            var doctorCol = doctorRoot.AddComponent<CapsuleCollider>();
            doctorCol.height = 1.9f; doctorCol.radius = 0.4f; doctorCol.center = new Vector3(0, 0.95f, 0);
            var doctorAnim = doctorRoot.AddComponent<ProceduralWalkAnimator>();
            doctorAnim.leftArm = doctorHuman.leftArm; doctorAnim.rightArm = doctorHuman.rightArm; doctorAnim.leftLeg = doctorHuman.leftLeg; doctorAnim.rightLeg = doctorHuman.rightLeg;
            zones.Add(new Zone(14, 31, 3, 3));

            var rangerRoot = Empty("NPC_Sahar", null, new Vector3(15, 0, 21));
            rangerRoot.transform.rotation = Quaternion.Euler(0, 210f, 0);
            var rangerHuman = BuildHuman(rangerRoot.transform, Lit("RangerShirt", 0.25f, 0.7f, 0.42f), Lit("RangerHair", 0.25f, 0.13f, 0.08f), false);
            var ranger = rangerRoot.AddComponent<NpcInteractable>();
            ranger.npcId = "npc_ranger";
            ranger.displayNameFa = "سحر، محیط‌بان";
            ranger.radius = 3f;
            Prim(PrimitiveType.Sphere, "Marker", rangerRoot.transform, new Vector3(0, 2.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), beacon, false);
            var rangerCol = rangerRoot.AddComponent<CapsuleCollider>();
            rangerCol.height = 1.9f; rangerCol.radius = 0.4f; rangerCol.center = new Vector3(0, 0.95f, 0);
            var rangerAnim = rangerRoot.AddComponent<ProceduralWalkAnimator>();
            rangerAnim.leftArm = rangerHuman.leftArm; rangerAnim.rightArm = rangerHuman.rightArm; rangerAnim.leftLeg = rangerHuman.leftLeg; rangerAnim.rightLeg = rangerHuman.rightLeg;
            zones.Add(new Zone(15, 21, 3, 3));

            var farmerRoot = Empty("NPC_Parvin", null, new Vector3(-6, 0, -30));
            farmerRoot.transform.rotation = Quaternion.Euler(0, 70f, 0);
            var farmerHuman = BuildHuman(farmerRoot.transform, Lit("FarmerShirt", 0.82f, 0.45f, 0.18f), Lit("FarmerHair", 0.18f, 0.1f, 0.06f), true);
            var farmer = farmerRoot.AddComponent<NpcInteractable>();
            farmer.npcId = "npc_farmer";
            farmer.displayNameFa = "عمو پروین، کشاورز";
            farmer.radius = 3f;
            Prim(PrimitiveType.Sphere, "Marker", farmerRoot.transform, new Vector3(0, 2.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), beacon, false);
            var farmerCol = farmerRoot.AddComponent<CapsuleCollider>();
            farmerCol.height = 1.9f; farmerCol.radius = 0.4f; farmerCol.center = new Vector3(0, 0.95f, 0);
            var farmerAnim = farmerRoot.AddComponent<ProceduralWalkAnimator>();
            farmerAnim.leftArm = farmerHuman.leftArm; farmerAnim.rightArm = farmerHuman.rightArm; farmerAnim.leftLeg = farmerHuman.leftLeg; farmerAnim.rightLeg = farmerHuman.rightLeg;
            zones.Add(new Zone(-6, -30, 3, 3));

            var firefighterRoot = Empty("NPC_Amin", null, new Vector3(-19, 0, -7));
            firefighterRoot.transform.rotation = Quaternion.Euler(0, 45f, 0);
            var firefighterHuman = BuildHuman(firefighterRoot.transform, Lit("FirefighterShirt", 0.9f, 0.22f, 0.16f), Lit("FirefighterHair", 0.12f, 0.08f, 0.05f), true);
            var firefighter = firefighterRoot.AddComponent<NpcInteractable>();
            firefighter.npcId = "npc_firefighter";
            firefighter.displayNameFa = "امین، آتش‌نشان";
            firefighter.radius = 3f;
            Prim(PrimitiveType.Sphere, "Marker", firefighterRoot.transform, new Vector3(0, 2.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), beacon, false);
            var firefighterCol = firefighterRoot.AddComponent<CapsuleCollider>();
            firefighterCol.height = 1.9f; firefighterCol.radius = 0.4f; firefighterCol.center = new Vector3(0, 0.95f, 0);
            var firefighterAnim = firefighterRoot.AddComponent<ProceduralWalkAnimator>();
            firefighterAnim.leftArm = firefighterHuman.leftArm; firefighterAnim.rightArm = firefighterHuman.rightArm; firefighterAnim.leftLeg = firefighterHuman.leftLeg; firefighterAnim.rightLeg = firefighterHuman.rightLeg;
            zones.Add(new Zone(-19, -7, 3, 3));

            // ----- pickups -----
            var pickups = Empty("Pickups", null, Vector3.zero);
            var toolRed = Lit("ToolboxRed", 0.85f, 0.15f, 0.15f);
            var toolRoot = Empty("Toolbox", pickups.transform, new Vector3(12, 0.55f, -12));
            var toolVis = Empty("Visual", toolRoot.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "Box", toolVis.transform, Vector3.zero, new Vector3(0.7f, 0.4f, 0.4f), toolRed, false);
            Prim(PrimitiveType.Cube, "Handle", toolVis.transform, new Vector3(0, 0.3f, 0), new Vector3(0.35f, 0.08f, 0.08f), Lit("Metal", 0.6f, 0.6f, 0.65f), false);
            Prim(PrimitiveType.Cylinder, "Beacon", toolVis.transform, new Vector3(0, 2.2f, 0), new Vector3(0.1f, 1.6f, 0.1f), beacon, false);
            var toolP = toolRoot.AddComponent<PickupInteractable>();
            toolP.missionId = "eng_01_toolbox"; toolP.targetId = "toolbox"; toolP.displayNameFa = "جعبه\u200Cابزار"; toolP.order = 0; toolP.spin = toolVis.transform; toolP.radius = 2.4f;
            zones.Add(new Zone(12, -12, 2, 2));

            Vector3[] plankPos = { new Vector3(-14, 0.4f, -20), new Vector3(14, 0.4f, -26), new Vector3(-10, 0.4f, 24) };
            var plankMat = Lit("Plank", 0.8f, 0.65f, 0.4f);
            for (int i = 0; i < plankPos.Length; i++)
            {
                var pr = Empty("Plank" + i, pickups.transform, plankPos[i]);
                var pv = Empty("Visual", pr.transform, Vector3.zero);
                Prim(PrimitiveType.Cube, "Board1", pv.transform, new Vector3(0, 0, 0), new Vector3(1.0f, 0.12f, 0.28f), plankMat, false);
                Prim(PrimitiveType.Cube, "Board2", pv.transform, new Vector3(0, 0.14f, 0.05f), new Vector3(1.0f, 0.12f, 0.28f), plankMat, false);
                Prim(PrimitiveType.Cylinder, "Beacon", pv.transform, new Vector3(0, 2.2f, 0), new Vector3(0.1f, 1.6f, 0.1f), beacon, false);
                var pp = pr.AddComponent<PickupInteractable>();
                pp.missionId = "eng_02_planks"; pp.targetId = "plank"; pp.displayNameFa = "تخته"; pp.order = i; pp.spin = pv.transform; pp.radius = 2.4f;
                zones.Add(new Zone(plankPos[i].x, plankPos[i].z, 2, 2));
            }

            // ----- engineer stages 4 and 5: paint and finish the restored school -----
            // These are deliberately separate world interactions, not UI-only counters, so
            // the final two stages complete the same exploration -> repair -> city-change loop.
            Vector3[] paintPos = { new Vector3(-6, 0.45f, 20), new Vector3(13, 0.45f, 18) };
            var paintMat = Lit("Paint", 0.25f, 0.55f, 0.95f);
            for (int i = 0; i < paintPos.Length; i++)
            {
                var pr = Empty("Paint" + i, pickups.transform, paintPos[i]);
                var pv = Empty("Visual", pr.transform, Vector3.zero);
                Prim(PrimitiveType.Cylinder, "Can", pv.transform, Vector3.zero, new Vector3(0.34f, 0.38f, 0.34f), paintMat, false);
                Prim(PrimitiveType.Cylinder, "Beacon", pv.transform, new Vector3(0, 2.0f, 0), new Vector3(0.1f, 1.4f, 0.1f), beacon, false);
                var pp = pr.AddComponent<PickupInteractable>();
                pp.missionId = "eng_04_paint_school"; pp.targetId = "paint"; pp.displayNameFa = "قوطی رنگ"; pp.order = i; pp.spin = pv.transform; pp.radius = 2.4f;
                zones.Add(new Zone(paintPos[i].x, paintPos[i].z, 2, 2));
            }

            Vector3[] paintSpotPos = { new Vector3(-32.5f, 0, 20.5f), new Vector3(-19.5f, 0, 20.5f) };
            for (int i = 0; i < paintSpotPos.Length; i++)
            {
                var rp = Empty("PaintSpot" + i, repairs.transform, paintSpotPos[i]);
                var br = Prim(PrimitiveType.Cube, "FadedWall", rp.transform, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.9f, 0.18f), Lit("FadedWall", 0.55f, 0.5f, 0.4f), false);
                Prim(PrimitiveType.Cylinder, "Beacon", br.transform, new Vector3(0, 1.9f, 0), new Vector3(0.1f, 1.3f, 0.1f), beacon, false);
                var fx = Prim(PrimitiveType.Cube, "PaintedWall", rp.transform, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.9f, 0.18f), Lit("PaintedWall", 0.95f, 0.8f, 0.35f), false);
                var ri = rp.AddComponent<RepairInteractable>();
                ri.missionId = "eng_04_paint_school"; ri.targetId = "school_paint"; ri.order = i; ri.promptFa = "رنگ‌آمیزی"; ri.radius = 2.4f; ri.brokenVisual = br; ri.fixedVisual = fx;
                zones.Add(new Zone(paintSpotPos[i].x, paintSpotPos[i].z, 2, 2));
            }

            Vector3[] cleanupPos = { new Vector3(-36, 0.25f, 10), new Vector3(-16, 0.25f, 23), new Vector3(-27, 0.25f, 27) };
            var cleanupMat = Lit("CleanupBag", 0.3f, 0.75f, 0.45f);
            for (int i = 0; i < cleanupPos.Length; i++)
            {
                var pr = Empty("Cleanup" + i, pickups.transform, cleanupPos[i]);
                var pv = Empty("Visual", pr.transform, Vector3.zero);
                Prim(PrimitiveType.Sphere, "Bag", pv.transform, Vector3.zero, new Vector3(0.5f, 0.45f, 0.5f), cleanupMat, false);
                Prim(PrimitiveType.Cylinder, "Beacon", pv.transform, new Vector3(0, 1.9f, 0), new Vector3(0.1f, 1.3f, 0.1f), beacon, false);
                var pp = pr.AddComponent<PickupInteractable>();
                pp.missionId = "eng_05_open_school"; pp.targetId = "school_cleanup"; pp.displayNameFa = "کیسهٔ زباله"; pp.order = i; pp.spin = pv.transform; pp.radius = 2.4f;
                zones.Add(new Zone(cleanupPos[i].x, cleanupPos[i].z, 2, 2));
            }

            // ----- teacher job: unlocked only after the school becomes active -----
            var bookMat = Lit("LibraryBook", 0.3f, 0.5f, 0.95f);
            Vector3[] bookPos = { new Vector3(-5, 0.42f, 31), new Vector3(17, 0.42f, 30) };
            for (int i = 0; i < bookPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, bookPos[i], "LibraryBook", "teach_01_books", "library_book", "کتاب", i, bookMat, beacon);

            var deskPending = Lit("DeskPending", 0.75f, 0.55f, 0.3f);
            var deskReady = Lit("DeskReady", 0.3f, 0.8f, 0.55f);
            Vector3[] deskPos = { new Vector3(-34, 0, 28), new Vector3(-28, 0, 28), new Vector3(-22, 0, 28) };
            for (int i = 0; i < deskPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, deskPos[i], "ClassroomDesk", "teach_02_classroom", "classroom_setup", "مرتب کردن", i, deskPending, deskReady, beacon);

            var seedMat = Lit("GardenSeed", 0.95f, 0.7f, 0.2f);
            Vector3[] seedPos = { new Vector3(-7, 0.42f, 13), new Vector3(6, 0.42f, 23), new Vector3(17, 0.42f, 8) };
            for (int i = 0; i < seedPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, seedPos[i], "GardenSeed", "teach_03_garden", "garden_seed", "بستهٔ بذر", i, seedMat, beacon);

            var soilMat = Lit("GardenSoil", 0.5f, 0.35f, 0.18f);
            var plantMat = Lit("GardenPlant", 0.25f, 0.8f, 0.35f);
            Vector3[] gardenPos = { new Vector3(-38, 0, 13), new Vector3(-38, 0, 19) };
            for (int i = 0; i < gardenPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, gardenPos[i], "GardenPlot", "teach_03_garden", "school_garden", "کاشت بذر", i, soilMat, plantMat, beacon);

            var cardMat = Lit("LessonCard", 0.95f, 0.55f, 0.25f);
            var boardMat = Lit("LessonReady", 0.25f, 0.65f, 0.95f);
            Vector3[] boardPos = { new Vector3(-33, 0, 7), new Vector3(-27, 0, 7), new Vector3(-21, 0, 7) };
            for (int i = 0; i < boardPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, boardPos[i], "LessonCard", "teach_04_lesson_board", "lesson_board", "قرار دادن کارت", i, cardMat, boardMat, beacon);

            var invitationMat = Lit("Invitation", 0.95f, 0.4f, 0.6f);
            Vector3[] invitationPos = { new Vector3(-4, 0.42f, -24), new Vector3(13, 0.42f, -8), new Vector3(30, 0.42f, 4) };
            for (int i = 0; i < invitationPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, invitationPos[i], "Invitation", "teach_05_open_class", "class_invitation", "دعوت‌نامه", i, invitationMat, beacon);

            // ----- doctor job: unlocks after the first class, then reopens the clinic -----
            var kitMat = Lit("MedicalKit", 0.96f, 0.95f, 0.95f);
            Vector3[] kitPos = { new Vector3(31, 0.42f, 28), new Vector3(7, 0.42f, -30) };
            for (int i = 0; i < kitPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, kitPos[i], "MedicalKit", "med_01_first_aid", "medical_kit", "کیف کمک‌های اولیه", i, kitMat, beacon);

            var bandageMat = Lit("Bandage", 0.96f, 0.82f, 0.72f);
            Vector3[] bandagePos = { new Vector3(-8, 0.42f, -30), new Vector3(31, 0.42f, 8), new Vector3(-13, 0.42f, -7) };
            for (int i = 0; i < bandagePos.Length; i++)
                AddPickupMarker(pickups.transform, zones, bandagePos[i], "Bandage", "med_02_supplies", "bandage", "باند پانسمان", i, bandageMat, beacon);

            var supplyPending = Lit("SupplyPending", 0.8f, 0.55f, 0.25f);
            var supplyReady = Lit("SupplyReady", 0.3f, 0.75f, 0.9f);
            Vector3[] supplyPos = { new Vector3(-8.5f, 0, 36), new Vector3(8.5f, 0, 36) };
            for (int i = 0; i < supplyPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, supplyPos[i], "ClinicSupply", "med_02_supplies", "clinic_supplies", "چیدن تجهیزات", i, supplyPending, supplyReady, beacon);

            var repairPending = Lit("ClinicRepairPending", 0.78f, 0.28f, 0.2f);
            var repairReady = Lit("ClinicRepairReady", 0.3f, 0.82f, 0.5f);
            Vector3[] clinicRepairPos = { new Vector3(-8.5f, 0, 22), new Vector3(0, 0, 21.5f), new Vector3(8.5f, 0, 22) };
            for (int i = 0; i < clinicRepairPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, clinicRepairPos[i], "ClinicRepair", "med_03_repair_clinic", "clinic_repair", "تعمیر کلینیک", i, repairPending, repairReady, beacon);

            var roomPending = Lit("RoomPending", 0.72f, 0.55f, 0.42f);
            var roomReady = Lit("RoomReady", 0.42f, 0.8f, 0.7f);
            Vector3[] roomPos = { new Vector3(-7, 0, 18), new Vector3(7, 0, 18) };
            for (int i = 0; i < roomPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, roomPos[i], "WaitingRoom", "med_04_waiting_room", "waiting_room", "آماده‌سازی سالن", i, roomPending, roomReady, beacon);

            var healthCardMat = Lit("HealthCard", 0.3f, 0.85f, 0.8f);
            Vector3[] healthCardPos = { new Vector3(-31, 0.42f, -5), new Vector3(17, 0.42f, -23), new Vector3(34, 0.42f, -28) };
            for (int i = 0; i < healthCardPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, healthCardPos[i], "HealthCard", "med_05_open_clinic", "health_card", "کارت سلامت", i, healthCardMat, beacon);

            // ----- environment job: unlocks after the clinic becomes active -----
            var gloveMat = Lit("WorkGlove", 0.95f, 0.72f, 0.2f);
            Vector3[] glovePos = { new Vector3(-34, 0.42f, 31), new Vector3(4, 0.42f, -14) };
            for (int i = 0; i < glovePos.Length; i++)
                AddPickupMarker(pickups.transform, zones, glovePos[i], "WorkGlove", "env_01_gloves", "work_glove", "دستکش کار", i, gloveMat, beacon);

            var litterMat = Lit("LitterBag", 0.32f, 0.38f, 0.42f);
            Vector3[] litterPos = { new Vector3(-31, 0.42f, 2), new Vector3(4, 0.42f, 10), new Vector3(34, 0.42f, -5) };
            for (int i = 0; i < litterPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, litterPos[i], "LitterBag", "env_02_cleanup", "litter_bag", "کیسهٔ زباله", i, litterMat, beacon);

            var cleanupPending = Lit("CleanupPending", 0.7f, 0.35f, 0.2f);
            var cleanupReady = Lit("CleanupReady", 0.28f, 0.82f, 0.45f);
            Vector3[] cleanupStationPos = { new Vector3(18, 0, 37), new Vector3(36, 0, 37), new Vector3(37, 0, 24) };
            for (int i = 0; i < cleanupStationPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, cleanupStationPos[i], "CleanupStation", "env_02_cleanup", "cleanup_station", "تحویل زباله", i, cleanupPending, cleanupReady, beacon);

            var sortPending = Lit("SortPending", 0.85f, 0.55f, 0.25f);
            var sortReady = Lit("SortReady", 0.25f, 0.62f, 0.92f);
            Vector3[] sortPos = { new Vector3(18, 0, 25), new Vector3(18, 0, 30), new Vector3(18, 0, 35) };
            for (int i = 0; i < sortPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, sortPos[i], "SortingBin", "env_03_sorting", "sorting_bin", "تفکیک زباله", i, sortPending, sortReady, beacon);

            var plantPending = Lit("PlantPending", 0.48f, 0.32f, 0.16f);
            var plantReady = Lit("PlantReady", 0.22f, 0.78f, 0.35f);
            Vector3[] plantPos = { new Vector3(10, 0, 16), new Vector3(20, 0, 16) };
            for (int i = 0; i < plantPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, plantPos[i], "GreenPlot", "env_04_green_space", "green_plot", "کاشت نهال", i, plantPending, plantReady, beacon);

            var recycleCardMat = Lit("RecycleCard", 0.25f, 0.78f, 0.65f);
            Vector3[] recycleCardPos = { new Vector3(-17, 0.42f, -28), new Vector3(8, 0.42f, 36), new Vector3(34, 0.42f, 12) };
            for (int i = 0; i < recycleCardPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, recycleCardPos[i], "RecycleCard", "env_05_open_station", "recycle_card", "کارت بازیافت", i, recycleCardMat, beacon);

            // ----- farmer job: unlocks after the recycling station becomes active -----
            var seedPacketMat = Lit("SeedPacket", 0.92f, 0.72f, 0.2f);
            Vector3[] farmSeedPos = { new Vector3(-14, 0.42f, -13), new Vector3(31, 0.42f, -15) };
            for (int i = 0; i < farmSeedPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, farmSeedPos[i], "FarmSeed", "farm_01_seeds", "farm_seed", "بستهٔ بذر", i, seedPacketMat, beacon);

            var soilPending = Lit("FarmSoil", 0.45f, 0.28f, 0.13f);
            var soilReady = Lit("FarmSoilReady", 0.35f, 0.7f, 0.25f);
            Vector3[] farmSoilPos = { new Vector3(0, 0, -25), new Vector3(7, 0, -24.5f), new Vector3(14, 0, -25) };
            for (int i = 0; i < farmSoilPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, farmSoilPos[i], "FarmSoil", "farm_02_soil", "farm_soil", "آماده‌سازی خاک", i, soilPending, soilReady, beacon);

            var waterMat = Lit("WaterBottle", 0.25f, 0.62f, 0.95f);
            Vector3[] waterPos = { new Vector3(-33, 0.42f, -25), new Vector3(-3, 0.42f, 4), new Vector3(27, 0.42f, -4) };
            for (int i = 0; i < waterPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, waterPos[i], "WaterBottle", "farm_03_watering", "water_bottle", "بطری آب", i, waterMat, beacon);

            var waterPending = Lit("WaterPlotPending", 0.65f, 0.48f, 0.2f);
            var waterReady = Lit("WaterPlotReady", 0.2f, 0.75f, 0.4f);
            Vector3[] waterPlotPos = { new Vector3(0, 0, -38), new Vector3(14, 0, -38) };
            for (int i = 0; i < waterPlotPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, waterPlotPos[i], "WaterPlot", "farm_03_watering", "water_plot", "آبیاری", i, waterPending, waterReady, beacon);

            var harvestPending = Lit("HarvestPending", 0.88f, 0.65f, 0.2f);
            var harvestReady = Lit("HarvestReady", 0.82f, 0.28f, 0.2f);
            Vector3[] harvestPos = { new Vector3(-3, 0, -37.5f), new Vector3(7, 0, -37.5f), new Vector3(17, 0, -37.5f) };
            for (int i = 0; i < harvestPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, harvestPos[i], "HarvestBasket", "farm_04_harvest", "harvest_basket", "برداشت محصول", i, harvestPending, harvestReady, beacon);

            var marketCardMat = Lit("MarketCard", 0.95f, 0.45f, 0.25f);
            Vector3[] marketCardPos = { new Vector3(-36, 0.42f, 24), new Vector3(17, 0.42f, -35), new Vector3(35, 0.42f, 18) };
            for (int i = 0; i < marketCardPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, marketCardPos[i], "MarketCard", "farm_05_open_farm", "market_card", "کارت بازار", i, marketCardMat, beacon);

            // ----- firefighter job: unlocks after the community farm becomes active -----
            var helmetMat = Lit("FireHelmet", 0.95f, 0.75f, 0.12f);
            Vector3[] helmetPos = { new Vector3(-35, 0.42f, 12), new Vector3(22, 0.42f, -22) };
            for (int i = 0; i < helmetPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, helmetPos[i], "FireHelmet", "fire_01_helmets", "fire_helmet", "کلاه ایمنی", i, helmetMat, beacon);

            var hoseMat = Lit("Hose", 0.2f, 0.55f, 0.9f);
            Vector3[] hosePos = { new Vector3(-10, 0.42f, -6), new Vector3(12, 0.42f, 7), new Vector3(32, 0.42f, -35) };
            for (int i = 0; i < hosePos.Length; i++)
                AddPickupMarker(pickups.transform, zones, hosePos[i], "FireHose", "fire_02_hoses", "fire_hose", "شلنگ آب", i, hoseMat, beacon);

            var hosePending = Lit("HoseRackPending", 0.7f, 0.4f, 0.2f);
            var hoseReady = Lit("HoseRackReady", 0.25f, 0.65f, 0.92f);
            Vector3[] hoseRackPos = { new Vector3(-37, 0, -6), new Vector3(-37, 0, 6) };
            for (int i = 0; i < hoseRackPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, hoseRackPos[i], "HoseRack", "fire_02_hoses", "hose_rack", "نصب شلنگ", i, hosePending, hoseReady, beacon);

            var safetyPending = Lit("SafetyPending", 0.8f, 0.3f, 0.2f);
            var safetyReady = Lit("SafetyReady", 0.3f, 0.82f, 0.45f);
            Vector3[] safetyPos = { new Vector3(-20, 0, 7), new Vector3(-28, 0, 9), new Vector3(-36, 0, 7) };
            for (int i = 0; i < safetyPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, safetyPos[i], "SafetyCheck", "fire_03_safety", "safety_check", "بررسی ایمنی", i, safetyPending, safetyReady, beacon);

            var alarmPending = Lit("AlarmPending", 0.7f, 0.45f, 0.18f);
            var alarmReady = Lit("AlarmReady", 0.92f, 0.22f, 0.18f);
            Vector3[] alarmPos = { new Vector3(-20, 0, -14), new Vector3(-35, 0, -14) };
            for (int i = 0; i < alarmPos.Length; i++)
                AddTaskSpot(repairs.transform, zones, alarmPos[i], "Alarm", "fire_04_alarms", "fire_alarm", "آزمون آژیر", i, alarmPending, alarmReady, beacon);

            var safetyCardMat = Lit("SafetyCard", 0.95f, 0.45f, 0.25f);
            Vector3[] safetyCardPos = { new Vector3(-9, 0.42f, 35), new Vector3(19, 0.42f, -8), new Vector3(34, 0.42f, 33) };
            for (int i = 0; i < safetyCardPos.Length; i++)
                AddPickupMarker(pickups.transform, zones, safetyCardPos[i], "SafetyCard", "fire_05_open_station", "safety_card", "کارت ایمنی", i, safetyCardMat, beacon);

            // ----- trees (scenery) -----
            var rnd = new System.Random(12345);
            int placed = 0, attempts = 0;
            while (placed < 26 && attempts < 600)
            {
                attempts++;
                float x = (float)(rnd.NextDouble() * 70 - 35);
                float z = (float)(rnd.NextDouble() * 70 - 35);
                if (Mathf.Abs(x) < 6f || Mathf.Abs(z) < 6f) continue;
                bool bad = false;
                foreach (var zn in zones)
                    if (Mathf.Abs(x - zn.x) < zn.hx + 2.5f && Mathf.Abs(z - zn.z) < zn.hz + 2.5f) { bad = true; break; }
                if (bad) continue;
                float s = 0.8f + (float)rnd.NextDouble() * 0.6f;
                var tr = Empty("Tree" + placed, env.transform, new Vector3(x, 0, z));
                Prim(PrimitiveType.Cylinder, "Trunk", tr.transform, new Vector3(0, s, 0), new Vector3(0.4f * s, s, 0.4f * s), trunk, true);
                Prim(PrimitiveType.Sphere, "Leaves", tr.transform, new Vector3(0, 2.4f * s + 0.8f, 0), new Vector3(2.4f * s, 2.4f * s, 2.4f * s), leaves, false);
                placed++;
            }

            // Static batching for pure scenery
            foreach (var t in env.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            foreach (var t in houses.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            // ----- player + camera + controller -----
            Vector3 spawn = new Vector3(0, 0.1f, -24f);
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = spawn;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.85f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.93f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 45f;
            var pc = player.AddComponent<PlayerController>();
            pc.respawnPosition = spawn;
            var human = BuildHuman(player.transform, Lit("PlayerShirt", 0.2f, 0.45f, 0.85f), Lit("PlayerHair", 0.15f, 0.1f, 0.08f), false);
            var anim = player.AddComponent<ProceduralWalkAnimator>();
            anim.source = pc;
            anim.leftArm = human.leftArm; anim.rightArm = human.rightArm; anim.leftLeg = human.leftLeg; anim.rightLeg = human.rightLeg;
            var look = player.AddComponent<CharacterAppearance>();
            look.shirt = human.shirt; look.hairShort = human.hairShort; look.hairLong = human.hairLong;
            var interactor = player.AddComponent<Interactor>();

            var cam = AddCamera(spawn + new Vector3(0, 5f, -7f), new Color(0.55f, 0.78f, 0.95f));
            var tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;
            pc.cameraTransform = cam.transform;

            var ctrl = new GameObject("GameSceneController");
            var gsc = ctrl.AddComponent<GameSceneController>();
            gsc.player = pc;
            gsc.interactor = interactor;
            gsc.appearance = look;

            Save(scene, GameScenePath);
        }
    }
}
