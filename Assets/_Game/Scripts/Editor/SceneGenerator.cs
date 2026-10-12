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

        private static Material Lit(string name, float r, float g, float b)
        {
            return Mat(name, new Color(r, g, b), "Mobile/Diffuse");
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
            RenderSettings.ambientLight = new Color(0.65f, 0.68f, 0.75f);
            AddSun(new Color(1f, 0.96f, 0.88f), 1.0f);

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

        private static void GenerateNeighborhood()
        {
            var scene = NewEmptyScene();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.65f, 0.72f);
            AddSun(new Color(1f, 0.95f, 0.85f), 1.1f);

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
            MakeBuilding(houses.transform, "Clinic", new Vector3(0, 0, 28), new Vector3(12, 5, 8), Lit("WallWhite", 0.95f, 0.95f, 0.95f), Lit("RoofClinic", 0.85f, 0.25f, 0.25f), -1);
            zones.Add(new Zone(0, 28, 9, 7));
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
