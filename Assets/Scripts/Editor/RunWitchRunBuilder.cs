using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RunWitchRunBuilder
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/Gameplay/Gameplay.unity";
    private const string CreditsScenePath = "Assets/Scenes/Credits/Credits.unity";
    private const string PlayerDataPath = "Assets/Data/PlayerData/PlayerData.asset";
    private const string ForestBiomePath = "Assets/Data/Biomes/ForestBiome.asset";
    private const string MixerPath = "Assets/Audio/Mixers/MainMixer.mixer";
    private const string UiActionsPath = "Assets/Data/UIActions.asset";

    private const float GroundTopY = -2.5f;
    private const float PlayerStartX = -5f;

    private static readonly Color TextLight = new Color(0.95f, 0.91f, 1f);
    private static readonly Color TextDim = new Color(0.72f, 0.66f, 0.9f);
    private static readonly Color Gold = new Color(1f, 0.88f, 0.4f);
    private static readonly Color Cyan = new Color(0.49f, 0.94f, 1f);
    private static readonly Color Danger = new Color(1f, 0.36f, 0.48f);
    private static readonly Color Dim = new Color(0.02f, 0.01f, 0.06f, 0.7f);

    private static Sprite buttonSprite;
    private static Sprite panelSprite;
    private static Sprite pauseIconSprite;
    private static Font uiFont;

    static RunWitchRunBuilder()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    private static void AutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }
        if (File.Exists(GameplayScenePath) || !File.Exists("Assets/Art/Characters/witch_run_0.png"))
        {
            return;
        }
        BuildAll();
    }

    [MenuItem("Run Witch Run/Rebuild Project (scenes, prefabs, data)")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureSpriteImports();

            EnsureFolders();
            LoadUiAssets();

            EnsurePlayerData();
            EnsureForestBiome();
            EnsureMaterial("ParticleSpark", "Assets/Art/Particles/particle_spark.png");
            EnsureMaterial("ParticleDot", "Assets/Art/Particles/particle_dot.png");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPlayerPrefab();
            BuildObstaclePrefabs();
            BuildGemPrefab();
            BuildBroomPrefab();
            BuildInvincibilityPrefab();
            BuildExtraLifePrefab();

            BuildMainMenuScene();
            BuildGameplayScene();
            BuildCreditsScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene(CreditsScenePath, true)
            };

            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.productName = "Run Witch Run";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MenuScenePath);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
            }
            Debug.Log("[Run Witch Run] Proyecto armado. Abri la escena MainMenu y toca Play.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Run Witch Run] Fallo el armado del proyecto: " + e);
        }
    }

    private static void EnsureSpriteImports()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && RunWitchRunTextures.NeedsConfigure(importer))
            {
                RunWitchRunTextures.Configure(importer, path);
                importer.SaveAndReimport();
            }
        }
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/Data/PlayerData", "Assets/Data/Biomes", "Assets/Prefabs/Player", "Assets/Prefabs/Obstacles",
            "Assets/Prefabs/Collectibles", "Assets/Prefabs/PowerUps", "Assets/Art/Materials",
            "Assets/Scenes/MainMenu", "Assets/Scenes/Gameplay", "Assets/Scenes/Credits"
        };
        foreach (string f in folders)
        {
            Directory.CreateDirectory(f);
        }
        AssetDatabase.Refresh();
    }

    private static void LoadUiAssets()
    {
        buttonSprite = LoadSprite("Assets/Art/UI/ui_button.png");
        panelSprite = LoadSprite("Assets/Art/UI/ui_panel.png");
        pauseIconSprite = LoadSprite("Assets/Art/UI/ui_pause_icon.png");

        uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/PixelFont.ttf");
        if (uiFont == null)
        {
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/PixelFont.otf");
        }
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    // Despues de cada NewScene Unity descarga de memoria los assets que nadie referencia,
    // asi que los assets se vuelven a cargar por path en vez de guardar la referencia.
    private static PlayerData LoadPlayerData()
    {
        return AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
    }

    private static Material LoadMaterial(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + name + ".mat");
    }

    private static GameObject LoadPrefab(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static PlayerData EnsurePlayerData()
    {
        var data = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<PlayerData>();
            AssetDatabase.CreateAsset(data, PlayerDataPath);
            AssetDatabase.SaveAssets();
        }
        return data;
    }

    private static BiomeData EnsureForestBiome()
    {
        var biome = AssetDatabase.LoadAssetAtPath<BiomeData>(ForestBiomePath);
        if (biome == null)
        {
            biome = ScriptableObject.CreateInstance<BiomeData>();
            AssetDatabase.CreateAsset(biome, ForestBiomePath);
        }

        if (biome.layers == null || biome.layers.Length == 0)
        {
            const string env = "Assets/Art/Environment/";
            biome.layers = new[]
            {
                new BiomeData.Layer { layerName = "TreesFar", sprite = LoadSprite(env + "bg_trees_far.png"), speedFactor = 0.08f, ambientSpeed = 0f },
                new BiomeData.Layer { layerName = "TreesMid", sprite = LoadSprite(env + "bg_trees_mid.png"), speedFactor = 0.2f, ambientSpeed = 0f },
                new BiomeData.Layer { layerName = "FogBack", sprite = LoadSprite(env + "bg_fog_back.png"), speedFactor = 0.35f, ambientSpeed = 0.15f },
                new BiomeData.Layer { layerName = "TreesNear", sprite = LoadSprite(env + "bg_trees_near.png"), speedFactor = 0.45f, ambientSpeed = 0f },
                new BiomeData.Layer { layerName = "Ground", sprite = LoadSprite(env + "ground.png"), speedFactor = 1f, ambientSpeed = 0f },
                new BiomeData.Layer { layerName = "GroundPlants", sprite = LoadSprite(env + "ground_plants.png"), speedFactor = 1f, ambientSpeed = 0f },
                new BiomeData.Layer { layerName = "FogFront", sprite = LoadSprite(env + "bg_fog_front.png"), speedFactor = 1.3f, ambientSpeed = 0.3f }
            };
            EditorUtility.SetDirty(biome);
        }
        return biome;
    }

    private static Material EnsureMaterial(string name, string texturePath)
    {
        string path = "Assets/Art/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            Debug.LogError("[Run Witch Run] No se pudo cargar el sprite: " + path);
        }
        return s;
    }

    private static SerializedProperty Find(SerializedObject so, string field)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError("[Run Witch Run] El campo '" + field + "' no existe en " + so.targetObject.GetType().Name);
        }
        return p;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = Find(so, field);
        if (p != null)
        {
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = Find(so, field);
        if (p != null)
        {
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = Find(so, field);
        if (p != null)
        {
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = Find(so, field);
        if (p != null)
        {
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetRefArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = Find(so, field);
        if (p != null)
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private class ParticleConfig
    {
        public float lifeMin = 0.4f, lifeMax = 0.8f;
        public float speedMin = 1f, speedMax = 2f;
        public float sizeMin = 0.1f, sizeMax = 0.2f;
        public float gravity;
        public float rate;
        public Color colorA = Color.white, colorB = Color.white;
        public ParticleSystemShapeType shape = ParticleSystemShapeType.Circle;
        public float radius = 0.1f;
        public float angle = 25f;
        public Vector3 scale = Vector3.one;
        public Vector3 rotation = Vector3.zero;
        public int maxParticles = 200;
        public bool prewarm;
        public bool noise;
        public int order = 12;
    }

    private static ParticleSystem MakeParticles(Transform parent, string name, Vector3 localPos, Material mat, ParticleConfig c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(c.rotation);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = c.prewarm;
        main.startLifetime = new ParticleSystem.MinMaxCurve(c.lifeMin, c.lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(c.speedMin, c.speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(c.sizeMin, c.sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(c.colorA, c.colorB);
        main.gravityModifier = c.gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = c.maxParticles;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = c.rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = c.shape;
        shape.radius = c.radius;
        shape.angle = c.angle;
        shape.scale = c.scale;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        if (c.noise)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
        }

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = c.order;
        return ps;
    }

    private static ParticleWorldScroll AddWorldScroll(ParticleSystem ps, float factor, float idleSpeed = 0f)
    {
        var scroll = ps.gameObject.AddComponent<ParticleWorldScroll>();
        SetFloat(scroll, "factor", factor);
        SetFloat(scroll, "idleSpeed", idleSpeed);
        return scroll;
    }

    private static ParticleSystem MakeAmbientMagic(Transform parent, Material mat)
    {
        var cfg = new ParticleConfig
        {
            lifeMin = 4f, lifeMax = 8f, speedMin = 0.05f, speedMax = 0.3f, sizeMin = 0.07f, sizeMax = 0.14f,
            gravity = -0.01f, rate = 10f, colorA = Cyan, colorB = new Color(0.75f, 0.45f, 1f),
            shape = ParticleSystemShapeType.Box, scale = new Vector3(18f, 6f, 1f), maxParticles = 120,
            prewarm = true, noise = true, order = 20
        };
        ParticleSystem ps = MakeParticles(parent, "AmbientMagic", new Vector3(0f, -0.5f, 0f), mat, cfg);
        AddWorldScroll(ps, 0.15f, 3f);
        return ps;
    }

    private static GameObject BuildPlayerPrefab()
    {
        PlayerData data = LoadPlayerData();
        Material sparkMat = LoadMaterial("ParticleSpark");
        Material dotMat = LoadMaterial("ParticleDot");

        var root = new GameObject("Player");
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = data.gravityScale;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = root.AddComponent<BoxCollider2D>();
        box.size = data.hitboxSize;
        box.offset = data.hitboxOffset;
        var controller = root.AddComponent<PlayerController>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/Art/Characters/witch_run_0.png");
        sr.sortingOrder = 10;

        var broom = new GameObject("BroomVisual");
        broom.transform.SetParent(root.transform, false);
        broom.transform.localPosition = new Vector3(0.15f, 0.2f, 0f);
        var broomSr = broom.AddComponent<SpriteRenderer>();
        broomSr.sprite = LoadSprite("Assets/Art/PowerUps/magic_broom.png");
        broomSr.sortingOrder = 9;

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform, false);

        var animator = visual.AddComponent<PlayerAnimator>();
        SetRef(animator, "player", controller);
        SetRef(animator, "spriteRenderer", sr);
        SetRefArray(animator, "runFrames", new Object[]
        {
            LoadSprite("Assets/Art/Characters/witch_run_0.png"), LoadSprite("Assets/Art/Characters/witch_run_1.png"),
            LoadSprite("Assets/Art/Characters/witch_run_2.png"), LoadSprite("Assets/Art/Characters/witch_run_3.png")
        });
        SetRef(animator, "jumpSprite", LoadSprite("Assets/Art/Characters/witch_jump.png"));
        SetRef(animator, "deadSprite", LoadSprite("Assets/Art/Characters/witch_dead.png"));
        SetRef(animator, "broomTransform", broom.transform);

        Color dust = new Color(0.7f, 0.65f, 0.85f, 0.8f);
        ParticleSystem runDust = MakeParticles(root.transform, "RunDust", new Vector3(-0.3f, 0.05f, 0f), sparkMat, new ParticleConfig
        {
            lifeMin = 0.25f, lifeMax = 0.45f, speedMin = 0.3f, speedMax = 0.9f, sizeMin = 0.08f, sizeMax = 0.16f,
            gravity = -0.1f, colorA = dust, colorB = dust, shape = ParticleSystemShapeType.Cone, angle = 20f,
            radius = 0.15f, rotation = new Vector3(-90f, 0f, 0f), order = 8
        });
        ParticleSystem jumpBurst = MakeParticles(root.transform, "JumpBurst", new Vector3(0f, 0.05f, 0f), sparkMat, new ParticleConfig
        {
            lifeMin = 0.3f, lifeMax = 0.55f, speedMin = 1.5f, speedMax = 3.5f, sizeMin = 0.1f, sizeMax = 0.2f,
            gravity = 0.4f, colorA = Cyan, colorB = new Color(0.8f, 0.5f, 1f), shape = ParticleSystemShapeType.Cone,
            angle = 40f, radius = 0.3f, rotation = new Vector3(90f, 0f, 0f), order = 12
        });
        ParticleSystem landBurst = MakeParticles(root.transform, "LandBurst", new Vector3(0f, 0.05f, 0f), sparkMat, new ParticleConfig
        {
            lifeMin = 0.3f, lifeMax = 0.6f, speedMin = 1f, speedMax = 3f, sizeMin = 0.1f, sizeMax = 0.2f,
            gravity = 0.3f, colorA = dust, colorB = new Color(0.6f, 0.9f, 1f), shape = ParticleSystemShapeType.Cone,
            angle = 70f, radius = 0.35f, rotation = new Vector3(-90f, 0f, 0f), order = 12
        });
        ParticleSystem aura = MakeParticles(root.transform, "MagicAura", new Vector3(0f, 0.9f, 0f), sparkMat, new ParticleConfig
        {
            lifeMin = 0.8f, lifeMax = 1.4f, speedMin = 0.1f, speedMax = 0.4f, sizeMin = 0.07f, sizeMax = 0.14f,
            gravity = -0.05f, rate = 6f, colorA = Gold, colorB = Cyan, shape = ParticleSystemShapeType.Circle,
            radius = 0.9f, maxParticles = 150, order = 11
        });
        ParticleSystem trail = MakeParticles(root.transform, "BroomTrail", new Vector3(-0.9f, 0.25f, 0f), dotMat, new ParticleConfig
        {
            lifeMin = 0.45f, lifeMax = 0.7f, speedMin = 0f, speedMax = 0.3f, sizeMin = 0.16f, sizeMax = 0.32f,
            gravity = 0f, rate = 0f, colorA = new Color(0.5f, 0.95f, 1f), colorB = new Color(0.75f, 0.4f, 1f),
            shape = ParticleSystemShapeType.Box, scale = new Vector3(0.1f, 0.3f, 0.1f), maxParticles = 400, order = 8
        });
        AddWorldScroll(runDust, 1f);
        AddWorldScroll(jumpBurst, 1f);
        AddWorldScroll(landBurst, 1f);
        AddWorldScroll(trail, 1f);
        AddWorldScroll(aura, 0.3f);

        SetRef(controller, "data", data);
        SetRef(controller, "groundCheck", groundCheck.transform);
        SetRef(controller, "broomVisual", broom);
        SetRef(controller, "runDust", runDust);
        SetRef(controller, "jumpBurst", jumpBurst);
        SetRef(controller, "landBurst", landBurst);
        SetRef(controller, "magicAura", aura);
        SetRef(controller, "broomTrail", trail);

        return SavePrefab(root, "Assets/Prefabs/Player/Player.prefab");
    }

    private struct ObstacleDef
    {
        public string name, sprite;
        public float hitW, hitH, weight, unlock;
        public ObstacleDef(string name, string sprite, float hitW, float hitH, float weight, float unlock)
        {
            this.name = name; this.sprite = sprite; this.hitW = hitW; this.hitH = hitH; this.weight = weight; this.unlock = unlock;
        }
    }

    private static ObstacleDef[] GetObstacleDefs()
    {
        return new ObstacleDef[]
        {
            new ObstacleDef("Tombstone", "obstacle_tombstone", 0.7f, 1.25f, 1f, 0f),
            new ObstacleDef("Pumpkin", "obstacle_pumpkin", 0.95f, 0.85f, 1f, 0f),
            new ObstacleDef("Rock", "obstacle_rock", 1.0f, 0.55f, 1f, 0f),
            new ObstacleDef("Bush", "obstacle_bush", 1.15f, 0.75f, 1f, 10f),
            new ObstacleDef("Cauldron", "obstacle_cauldron", 1.1f, 0.85f, 0.9f, 25f),
            new ObstacleDef("SmallTree", "obstacle_tree", 0.5f, 1.65f, 0.8f, 40f)
        };
    }

    private static string ObstaclePrefabPath(string name)
    {
        return "Assets/Prefabs/Obstacles/Obstacle_" + name + ".prefab";
    }

    private static List<SpawnEntry> LoadObstacleEntries()
    {
        var entries = new List<SpawnEntry>();
        foreach (ObstacleDef d in GetObstacleDefs())
        {
            entries.Add(new SpawnEntry
            {
                name = d.name, prefab = LoadPrefab(ObstaclePrefabPath(d.name)), width = d.hitW, height = d.hitH,
                weight = d.weight, unlockAfterSeconds = d.unlock
            });
        }
        return entries;
    }

    private static List<SpawnEntry> BuildObstaclePrefabs()
    {
        ObstacleDef[] defs = GetObstacleDefs();

        var entries = new List<SpawnEntry>();
        foreach (ObstacleDef d in defs)
        {
            var go = new GameObject("Obstacle_" + d.name);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(d.hitW, d.hitH);
            col.offset = new Vector2(0f, d.hitH * 0.5f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Assets/Art/Obstacles/" + d.sprite + ".png");
            sr.sortingOrder = 5;

            var obstacle = go.AddComponent<Obstacle>();
            SetRef(obstacle, "visualRoot", visual.transform);
            go.AddComponent<ScrollingObject>();
            GameObject prefab = SavePrefab(go, ObstaclePrefabPath(d.name));

            entries.Add(new SpawnEntry
            {
                name = d.name, prefab = prefab, width = d.hitW, height = d.hitH, weight = d.weight, unlockAfterSeconds = d.unlock
            });
        }
        return entries;
    }

    private static GameObject BuildGemPrefab()
    {
        Material sparkMat = LoadMaterial("ParticleSpark");

        var root = new GameObject("MoonGem");
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;
        var collectible = root.AddComponent<Collectible>();
        root.AddComponent<ScrollingObject>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/Art/Collectibles/moon_gem.png");
        sr.sortingOrder = 6;

        ParticleSystem burst = MakeParticles(root.transform, "CollectBurst", Vector3.zero, sparkMat, new ParticleConfig
        {
            lifeMin = 0.4f, lifeMax = 0.8f, speedMin = 1.5f, speedMax = 3.5f, sizeMin = 0.12f, sizeMax = 0.25f,
            colorA = Cyan, colorB = Color.white, shape = ParticleSystemShapeType.Circle, radius = 0.1f, order = 13
        });
        AddWorldScroll(burst, 1f);

        SetRef(collectible, "visualRoot", visual.transform);
        SetRef(collectible, "collectBurst", burst);
        return SavePrefab(root, "Assets/Prefabs/Collectibles/MoonGem.prefab");
    }

    private static GameObject BuildBroomPrefab()
    {
        Material sparkMat = LoadMaterial("ParticleSpark");

        var root = new GameObject("MagicBroomPickup");
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.8f, 0.9f);
        var powerUp = root.AddComponent<PowerUp>();
        root.AddComponent<ScrollingObject>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/Art/PowerUps/magic_broom.png");
        sr.sortingOrder = 6;

        ParticleSystem sparkles = MakeParticles(visual.transform, "Sparkles", Vector3.zero, sparkMat, new ParticleConfig
        {
            lifeMin = 0.6f, lifeMax = 1.1f, speedMin = 0.1f, speedMax = 0.5f, sizeMin = 0.08f, sizeMax = 0.16f,
            gravity = -0.05f, rate = 14f, colorA = Gold, colorB = new Color(0.8f, 0.5f, 1f),
            shape = ParticleSystemShapeType.Box, scale = new Vector3(1.8f, 0.6f, 0.1f), order = 7
        });
        AddWorldScroll(sparkles, 1f);

        ParticleSystem burst = MakeParticles(root.transform, "CollectBurst", Vector3.zero, sparkMat, new ParticleConfig
        {
            lifeMin = 0.5f, lifeMax = 1f, speedMin = 2f, speedMax = 5f, sizeMin = 0.15f, sizeMax = 0.3f,
            colorA = Gold, colorB = new Color(0.8f, 0.5f, 1f), shape = ParticleSystemShapeType.Circle, radius = 0.2f, order = 13
        });
        AddWorldScroll(burst, 1f);

        SetRef(powerUp, "visualRoot", visual.transform);
        SetRef(powerUp, "collectBurst", burst);
        SetInt(powerUp, "burstCount", 30);
        return SavePrefab(root, "Assets/Prefabs/PowerUps/MagicBroom.prefab");
    }

    private static GameObject BuildInvincibilityPrefab()
    {
        Material sparkMat = LoadMaterial("ParticleSpark");

        var root = new GameObject("InvincibilityPickup");
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);
        var powerUp = root.AddComponent<PowerUp>();
        SetInt(powerUp, "type", (int)PowerUpType.Invincibility);
        root.AddComponent<ScrollingObject>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/Art/PowerUps/invincibility.png");
        sr.sortingOrder = 6;

        ParticleSystem burst = MakeParticles(root.transform, "CollectBurst", Vector3.zero, sparkMat, new ParticleConfig
        {
            lifeMin = 0.5f, lifeMax = 1f, speedMin = 2f, speedMax = 5f, sizeMin = 0.15f, sizeMax = 0.3f,
            colorA = new Color(0.65f, 1f, 0.9f), colorB = Color.white, shape = ParticleSystemShapeType.Circle, radius = 0.2f, order = 13
        });
        AddWorldScroll(burst, 1f);

        SetRef(powerUp, "visualRoot", visual.transform);
        SetRef(powerUp, "collectBurst", burst);
        SetInt(powerUp, "burstCount", 24);
        return SavePrefab(root, "Assets/Prefabs/PowerUps/Invincibility.prefab");
    }

    private static GameObject BuildExtraLifePrefab()
    {
        Material sparkMat = LoadMaterial("ParticleSpark");

        var root = new GameObject("ExtraLifePickup");
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);
        var powerUp = root.AddComponent<PowerUp>();
        SetInt(powerUp, "type", (int)PowerUpType.ExtraLife);
        root.AddComponent<ScrollingObject>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/Art/PowerUps/extra_life.png");
        sr.sortingOrder = 6;

        ParticleSystem burst = MakeParticles(root.transform, "CollectBurst", Vector3.zero, sparkMat, new ParticleConfig
        {
            lifeMin = 0.5f, lifeMax = 1f, speedMin = 2f, speedMax = 5f, sizeMin = 0.15f, sizeMax = 0.3f,
            colorA = new Color(1f, 0.4f, 0.55f), colorB = Color.white, shape = ParticleSystemShapeType.Circle, radius = 0.2f, order = 13
        });
        AddWorldScroll(burst, 1f);

        SetRef(powerUp, "visualRoot", visual.transform);
        SetRef(powerUp, "collectBurst", burst);
        SetInt(powerUp, "burstCount", 24);
        return SavePrefab(root, "Assets/Prefabs/PowerUps/ExtraLife.prefab");
    }

    private static GameObject SavePrefab(GameObject go, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static Camera BuildCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.09f);
        go.AddComponent<AudioListener>();
        return cam;
    }

    private static SpriteRenderer MakeSprite(Transform parent, string name, string spritePath, Vector3 pos, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        sr.sortingOrder = order;
        return sr;
    }

    private static Transform MakeLayer(Transform parent, string name, string spritePath, Vector3 pos, int order, float factor, float ambient)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        Sprite sprite = LoadSprite(spritePath);
        float width = sprite.bounds.size.x;
        for (int i = 0; i < 3; i++)
        {
            var tile = new GameObject("Tile_" + i);
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3(i * width, 0f, 0f);
            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }
        var layer = root.AddComponent<ParallaxLayer>();
        SetFloat(layer, "speedFactor", factor);
        SetFloat(layer, "ambientSpeed", ambient);
        return root.transform;
    }

    private static Transform BuildEnvironment(bool withGroundCollider)
    {
        var root = new GameObject("Environment").transform;
        const string env = "Assets/Art/Environment/";

        MakeSprite(root, "Sky", env + "bg_sky.png", Vector3.zero, -100);
        MakeSprite(root, "Moon", env + "bg_moon.png", new Vector3(5.2f, 2.7f, 0f), -95);
        Transform treesFar = MakeLayer(root, "TreesFar", env + "bg_trees_far.png", Vector3.zero, -90, 0.08f, 0f);
        Transform treesMid = MakeLayer(root, "TreesMid", env + "bg_trees_mid.png", Vector3.zero, -80, 0.2f, 0f);
        Transform fogBack = MakeLayer(root, "FogBack", env + "bg_fog_back.png", Vector3.zero, -75, 0.35f, 0.15f);
        Transform treesNear = MakeLayer(root, "TreesNear", env + "bg_trees_near.png", Vector3.zero, -70, 0.45f, 0f);
        Transform ground = MakeLayer(root, "Ground", env + "ground.png", new Vector3(0f, GroundTopY - 1f, 0f), -10, 1f, 0f);
        Transform groundPlants = MakeLayer(root, "GroundPlants", env + "ground_plants.png", new Vector3(0f, GroundTopY - 0.05f, 0f), -9, 1f, 0f);
        Transform fogFront = MakeLayer(root, "FogFront", env + "bg_fog_front.png", Vector3.zero, 30, 1.3f, 0.3f);

        var rig = root.gameObject.AddComponent<ParallaxRig>();
        SetRefArray(rig, "layerRoots", new Object[] { treesFar, treesMid, fogBack, treesNear, ground, groundPlants, fogFront });
        SetRef(rig, "biome", AssetDatabase.LoadAssetAtPath<BiomeData>(ForestBiomePath));

        if (withGroundCollider)
        {
            var groundCollider = new GameObject("GroundCollider");
            groundCollider.transform.SetParent(root, false);
            groundCollider.transform.position = new Vector3(0f, GroundTopY - 1f, 0f);
            var col = groundCollider.AddComponent<BoxCollider2D>();
            col.size = new Vector2(60f, 2f);
            groundCollider.AddComponent<GroundSurface>();
        }
        return root;
    }

    private static AudioManager BuildAudioManager()
    {
        var go = new GameObject("AudioManager");
        var audio = go.AddComponent<AudioManager>();

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            Debug.LogError("[Run Witch Run] No se pudo cargar el AudioMixer en " + MixerPath +
                           ". Crealo con Create > Audio Mixer (grupos Music y SFX, exponer MusicVolume y SFXVolume) y asignalo al AudioManager.");
        }
        else
        {
            AudioMixerGroup[] music = mixer.FindMatchingGroups("Master/Music");
            AudioMixerGroup[] sfx = mixer.FindMatchingGroups("Master/SFX");
            if (music.Length == 0 || sfx.Length == 0)
            {
                Debug.LogError("[Run Witch Run] El AudioMixer no tiene los grupos Master/Music y Master/SFX. Revisa el README (seccion AudioMixer).");
            }
            SetRef(audio, "mixer", mixer);
            if (music.Length > 0) { SetRef(audio, "musicGroup", music[0]); }
            if (sfx.Length > 0) { SetRef(audio, "sfxGroup", sfx[0]); }
        }

        SetRef(audio, "menuMusic", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/menu_music.wav"));
        SetRef(audio, "gameplayMusic", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/gameplay_music.wav"));
        SetRef(audio, "jumpClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump.wav"));
        SetRef(audio, "landClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_land.wav"));
        SetRef(audio, "gemClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_gem.wav"));
        SetRef(audio, "powerUpClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_powerup.wav"));
        SetRef(audio, "gameOverClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_gameover.wav"));
        SetRef(audio, "buttonClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_button.wav"));
        return audio;
    }

    private static void SaveScene(string path)
    {
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
    }

    private static void BuildMainMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LoadUiAssets();
        Material sparkMat = LoadMaterial("ParticleSpark");
        BuildCamera();
        BuildEnvironment(false);
        BuildAudioManager();
        MakeAmbientMagic(null, sparkMat);

        var witch = new GameObject("MenuWitch");
        witch.transform.position = new Vector3(-5.5f, GroundTopY, 0f);
        var sr = witch.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/Art/Characters/witch_run_0.png");
        sr.sortingOrder = 10;
        var anim = witch.AddComponent<SpriteFrameAnimator>();
        SetRefArray(anim, "frames", new Object[]
        {
            LoadSprite("Assets/Art/Characters/witch_run_0.png"), LoadSprite("Assets/Art/Characters/witch_run_1.png"),
            LoadSprite("Assets/Art/Characters/witch_run_2.png"), LoadSprite("Assets/Art/Characters/witch_run_3.png")
        });

        NewEventSystem();
        Canvas canvas = NewCanvas();
        var ui = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();

        Text title = NewText(canvas.transform, "Title", "RUN WITCH RUN", 150, TextLight, TextAnchor.MiddleCenter,
            Anchor.Center, new Vector2(0f, 330f), new Vector2(1600f, 200f));
        title.GetComponent<Outline>().effectColor = new Color(0.45f, 0.2f, 0.85f);
        title.GetComponent<Outline>().effectDistance = new Vector2(8f, -8f);
        NewText(canvas.transform, "Subtitle", "An endless night in the enchanted forest", 40, Cyan, TextAnchor.MiddleCenter,
            Anchor.Center, new Vector2(0f, 220f), new Vector2(1400f, 60f));

        RectTransform buttons = NewUI("Buttons", canvas.transform);
        Place(buttons, Anchor.Center, Vector2.zero, new Vector2(700f, 700f));
        Button play = NewButton(buttons, "PlayButton", "PLAY", new Vector2(0f, 100f), new Vector2(520f, 100f));
        Button settings = NewButton(buttons, "SettingsButton", "SETTINGS", new Vector2(0f, -20f), new Vector2(520f, 100f));
        Button credits = NewButton(buttons, "CreditsButton", "CREDITS", new Vector2(0f, -140f), new Vector2(520f, 100f));
        Button quit = NewButton(buttons, "QuitButton", "QUIT", new Vector2(0f, -260f), new Vector2(520f, 100f));

        Text best = NewText(canvas.transform, "BestScore", "BEST SCORE: 000000", 40, Gold, TextAnchor.MiddleCenter,
            Anchor.BottomCenter, new Vector2(0f, 40f), new Vector2(900f, 60f));

        SettingsMenu settingsMenu = BuildSettingsPanel(canvas.transform);

        SetRef(ui, "playButton", play);
        SetRef(ui, "settingsButton", settings);
        SetRef(ui, "creditsButton", credits);
        SetRef(ui, "quitButton", quit);
        SetRef(ui, "settingsMenu", settingsMenu);
        SetRef(ui, "buttonsRoot", buttons.gameObject);
        SetRef(ui, "bestScoreText", best);

        settingsMenu.gameObject.SetActive(false);
        SaveScene(MenuScenePath);
    }

    private static void BuildCreditsScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LoadUiAssets();
        Material sparkMat = LoadMaterial("ParticleSpark");
        BuildCamera();
        BuildEnvironment(false);
        BuildAudioManager();
        MakeAmbientMagic(null, sparkMat);

        NewEventSystem();
        Canvas canvas = NewCanvas();
        var ui = new GameObject("CreditsUI").AddComponent<CreditsUI>();

        RectTransform panel = NewPanel(canvas.transform, "CreditsPanel", new Vector2(1200f, 940f));

        NewText(panel, "Title", "RUN WITCH RUN", 96, TextLight, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -50f), new Vector2(1100f, 120f));
        NewText(panel, "CreatedByLabel", "Created by:", 38, TextDim, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -200f), new Vector2(1000f, 50f));
        NewText(panel, "CreatedBy", "Teo Vinaraz", 58, Gold, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -250f), new Vector2(1000f, 80f));
        NewText(panel, "MusicLabel", "Music by:", 38, TextDim, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -370f), new Vector2(1000f, 50f));
        NewText(panel, "Music", "ChatGPT / OpenAI", 52, TextLight, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -420f), new Vector2(1000f, 70f));
        NewText(panel, "ArtLabel", "Art & Visual Assets by:", 38, TextDim, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -530f), new Vector2(1000f, 50f));
        NewText(panel, "Art", "ChatGPT / OpenAI", 52, TextLight, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -580f), new Vector2(1000f, 70f));
        NewText(panel, "Note", "Pixel Art generated specifically for this project.", 36, Cyan, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -700f), new Vector2(1100f, 60f));

        Button back = NewButton(panel, "BackButton", "BACK", new Vector2(0f, 60f), new Vector2(420f, 90f), Anchor.BottomCenter);
        SetRef(ui, "backButton", back);
        SaveScene(CreditsScenePath);
    }

    private static void BuildGameplayScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LoadUiAssets();
        Material sparkMat = LoadMaterial("ParticleSpark");
        GameObject playerPrefab = LoadPrefab("Assets/Prefabs/Player/Player.prefab");
        GameObject gemPrefab = LoadPrefab("Assets/Prefabs/Collectibles/MoonGem.prefab");
        GameObject broomPrefab = LoadPrefab("Assets/Prefabs/PowerUps/MagicBroom.prefab");
        GameObject invinciblePrefab = LoadPrefab("Assets/Prefabs/PowerUps/Invincibility.prefab");
        GameObject extraLifePrefab = LoadPrefab("Assets/Prefabs/PowerUps/ExtraLife.prefab");
        List<SpawnEntry> entries = LoadObstacleEntries();
        BuildCamera();
        BuildEnvironment(true);
        BuildAudioManager();
        MakeAmbientMagic(null, sparkMat);

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(PlayerStartX, GroundTopY, 0f);
        var controller = player.GetComponent<PlayerController>();

        var gmObject = new GameObject("GameManager");
        var gm = gmObject.AddComponent<GameManager>();
        SetRef(gm, "player", controller);

        var spawnerObject = new GameObject("EndlessSpawner");
        var spawner = spawnerObject.AddComponent<EndlessSpawner>();
        SetRef(spawner, "player", controller);
        SetRef(spawner, "gemPrefab", gemPrefab);
        SetFloat(spawner, "groundY", GroundTopY);
        var so = new SerializedObject(spawner);
        SerializedProperty list = so.FindProperty("obstacles");
        list.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("name").stringValue = entries[i].name;
            e.FindPropertyRelative("prefab").objectReferenceValue = entries[i].prefab;
            e.FindPropertyRelative("width").floatValue = entries[i].width;
            e.FindPropertyRelative("height").floatValue = entries[i].height;
            e.FindPropertyRelative("weight").floatValue = entries[i].weight;
            e.FindPropertyRelative("unlockAfterSeconds").floatValue = entries[i].unlockAfterSeconds;
        }

        var powerUpSlots = new[]
        {
            new { name = "MagicBroom", prefab = broomPrefab, first = 130f, min = 160f, max = 260f, height = 1.1f },
            new { name = "Invincibility", prefab = invinciblePrefab, first = 200f, min = 220f, max = 340f, height = 1.1f },
            new { name = "ExtraLife", prefab = extraLifePrefab, first = 260f, min = 300f, max = 420f, height = 1.1f }
        };
        SerializedProperty powerUpList = so.FindProperty("powerUps");
        powerUpList.arraySize = powerUpSlots.Length;
        for (int i = 0; i < powerUpSlots.Length; i++)
        {
            SerializedProperty p = powerUpList.GetArrayElementAtIndex(i);
            p.FindPropertyRelative("name").stringValue = powerUpSlots[i].name;
            p.FindPropertyRelative("prefab").objectReferenceValue = powerUpSlots[i].prefab;
            p.FindPropertyRelative("firstDistance").floatValue = powerUpSlots[i].first;
            p.FindPropertyRelative("minDistance").floatValue = powerUpSlots[i].min;
            p.FindPropertyRelative("maxDistance").floatValue = powerUpSlots[i].max;
            p.FindPropertyRelative("height").floatValue = powerUpSlots[i].height;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        NewEventSystem();
        Canvas canvas = NewCanvas();
        var pauseManager = new GameObject("PauseManager").AddComponent<PauseManager>();
        var uiManager = new GameObject("UIManager").AddComponent<UIManager>();

        Text score = NewText(canvas.transform, "ScoreText", "SCORE: 000000", 64, TextLight, TextAnchor.MiddleLeft,
            Anchor.TopLeft, new Vector2(40f, -30f), new Vector2(900f, 80f));
        Text best = NewText(canvas.transform, "BestText", "BEST: 000000", 34, TextDim, TextAnchor.MiddleLeft,
            Anchor.TopLeft, new Vector2(44f, -110f), new Vector2(700f, 50f));
        Button pause = NewButton(canvas.transform, "PauseButton", string.Empty, new Vector2(-40f, -30f), new Vector2(100f, 100f), Anchor.TopRight);
        RectTransform icon = NewUI("Icon", pause.transform);
        Place(icon, Anchor.Center, Vector2.zero, new Vector2(52f, 52f));
        var iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = pauseIconSprite;
        iconImage.raycastTarget = false;

        Text popup = NewText(canvas.transform, "PopupText", string.Empty, 56, Gold, TextAnchor.MiddleCenter,
            Anchor.TopCenter, new Vector2(0f, -170f), new Vector2(1200f, 80f));

        RectTransform broomIndicator = NewUI("BroomIndicator", canvas.transform);
        Place(broomIndicator, Anchor.TopCenter, new Vector2(0f, -260f), new Vector2(520f, 70f));
        NewText(broomIndicator, "Label", "MAGIC BROOM", 32, Gold, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, 0f), new Vector2(520f, 40f));
        RectTransform barBg = NewUI("BarBackground", broomIndicator);
        Place(barBg, Anchor.BottomCenter, Vector2.zero, new Vector2(520f, 20f));
        barBg.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.03f, 0.15f, 0.9f);
        RectTransform fill = NewUI("Fill", barBg);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);
        fill.gameObject.AddComponent<Image>().color = Cyan;

        RectTransform invincibleIndicator = NewUI("InvincibleIndicator", canvas.transform);
        Place(invincibleIndicator, Anchor.TopCenter, new Vector2(0f, -340f), new Vector2(520f, 70f));
        NewText(invincibleIndicator, "Label", "INVINCIBLE", 32, new Color(0.65f, 1f, 0.9f), TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, 0f), new Vector2(520f, 40f));
        RectTransform invincibleBarBg = NewUI("BarBackground", invincibleIndicator);
        Place(invincibleBarBg, Anchor.BottomCenter, Vector2.zero, new Vector2(520f, 20f));
        invincibleBarBg.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.03f, 0.15f, 0.9f);
        RectTransform invincibleFill = NewUI("Fill", invincibleBarBg);
        invincibleFill.anchorMin = Vector2.zero;
        invincibleFill.anchorMax = Vector2.one;
        invincibleFill.pivot = new Vector2(0f, 0.5f);
        invincibleFill.offsetMin = new Vector2(3f, 3f);
        invincibleFill.offsetMax = new Vector2(-3f, -3f);
        invincibleFill.gameObject.AddComponent<Image>().color = new Color(0.65f, 1f, 0.9f);

        Text lives = NewText(canvas.transform, "LivesDisplay", "LIVES: 0", 30, new Color(1f, 0.4f, 0.55f), TextAnchor.MiddleRight,
            Anchor.TopRight, new Vector2(-40f, -140f), new Vector2(400f, 44f));

        NewText(canvas.transform, "Hints", "SPACE / CLICK: JUMP     P / ESC: PAUSE", 28, new Color(0.72f, 0.66f, 0.9f, 0.75f),
            TextAnchor.MiddleCenter, Anchor.BottomCenter, new Vector2(0f, 24f), new Vector2(1200f, 40f));

        RectTransform pauseOverlay = NewOverlay(canvas.transform, "PausePanel");
        RectTransform pausePanel = NewPanel(pauseOverlay, "Panel", new Vector2(720f, 760f));
        NewText(pausePanel, "Title", "PAUSED", 96, TextLight, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -60f), new Vector2(680f, 120f));
        Button resume = NewButton(pausePanel, "ResumeButton", "RESUME", new Vector2(0f, 60f), new Vector2(520f, 100f));
        Button pauseSettings = NewButton(pausePanel, "SettingsButton", "SETTINGS", new Vector2(0f, -70f), new Vector2(520f, 100f));
        Button pauseMenu = NewButton(pausePanel, "MainMenuButton", "MAIN MENU", new Vector2(0f, -200f), new Vector2(520f, 100f));

        RectTransform overOverlay = NewOverlay(canvas.transform, "GameOverPanel");
        RectTransform overPanel = NewPanel(overOverlay, "Panel", new Vector2(860f, 860f));
        Text overTitle = NewText(overPanel, "Title", "GAME OVER", 110, Danger, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -60f), new Vector2(800f, 140f));
        overTitle.GetComponent<Outline>().effectDistance = new Vector2(6f, -6f);
        Text finalScore = NewText(overPanel, "FinalScore", "SCORE: 000000", 70, TextLight, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -240f), new Vector2(800f, 90f));
        Text bestScore = NewText(overPanel, "BestScore", "BEST: 000000", 44, TextDim, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -330f), new Vector2(800f, 60f));
        Text newRecord = NewText(overPanel, "NewRecord", "NEW RECORD!", 52, Gold, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -400f), new Vector2(800f, 70f));
        Button retry = NewButton(overPanel, "RetryButton", "RETRY", new Vector2(0f, 190f), new Vector2(520f, 100f), Anchor.BottomCenter);
        Button overMenu = NewButton(overPanel, "MainMenuButton", "MAIN MENU", new Vector2(0f, 60f), new Vector2(520f, 100f), Anchor.BottomCenter);
        NewText(overPanel, "RetryHint", "(press R to retry)", 28, TextDim, TextAnchor.MiddleCenter, Anchor.BottomCenter, new Vector2(0f, 300f), new Vector2(600f, 40f));

        SettingsMenu settingsMenu = BuildSettingsPanel(canvas.transform);

        SetRef(uiManager, "scoreText", score);
        SetRef(uiManager, "highScoreText", best);
        SetRef(uiManager, "pauseButton", pause);
        SetRef(uiManager, "popupText", popup);
        SetRef(uiManager, "broomIndicator", broomIndicator.gameObject);
        SetRef(uiManager, "broomFill", fill);
        SetRef(uiManager, "invincibleIndicator", invincibleIndicator.gameObject);
        SetRef(uiManager, "invincibleFill", invincibleFill);
        SetRef(uiManager, "livesDisplay", lives.gameObject);
        SetRef(uiManager, "livesText", lives);
        SetRef(uiManager, "pausePanel", pauseOverlay.gameObject);
        SetRef(uiManager, "gameOverPanel", overOverlay.gameObject);
        SetRef(uiManager, "settingsMenu", settingsMenu);
        SetRef(uiManager, "pauseManager", pauseManager);
        SetRef(uiManager, "resumeButton", resume);
        SetRef(uiManager, "pauseSettingsButton", pauseSettings);
        SetRef(uiManager, "pauseMainMenuButton", pauseMenu);
        SetRef(uiManager, "finalScoreText", finalScore);
        SetRef(uiManager, "bestScoreText", bestScore);
        SetRef(uiManager, "newRecordLabel", newRecord.gameObject);
        SetRef(uiManager, "retryButton", retry);
        SetRef(uiManager, "gameOverMenuButton", overMenu);
        SetRef(pauseManager, "uiManager", uiManager);

        pauseOverlay.gameObject.SetActive(false);
        overOverlay.gameObject.SetActive(false);
        broomIndicator.gameObject.SetActive(false);
        invincibleIndicator.gameObject.SetActive(false);
        lives.gameObject.SetActive(false);
        settingsMenu.gameObject.SetActive(false);

        SaveScene(GameplayScenePath);
    }

    private static class Anchor
    {
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    }

    private static Canvas NewCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void NewEventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var uiActions = EnsureUiActionsAsset();
        module.point = EnsureUiActionReference(uiActions, "Point");
        module.leftClick = EnsureUiActionReference(uiActions, "LeftClick");
        module.scrollWheel = EnsureUiActionReference(uiActions, "ScrollWheel");
        AssetDatabase.SaveAssets();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static UnityEngine.InputSystem.InputActionAsset EnsureUiActionsAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(UiActionsPath);
        if (asset == null)
        {
            string json = "{"
                + "\"name\":\"UIActions\","
                + "\"maps\":[{"
                + "\"name\":\"UI\",\"id\":\"" + System.Guid.NewGuid() + "\","
                + "\"actions\":["
                + "{\"name\":\"Point\",\"type\":\"PassThrough\",\"id\":\"" + System.Guid.NewGuid() + "\",\"expectedControlType\":\"Vector2\"},"
                + "{\"name\":\"LeftClick\",\"type\":\"PassThrough\",\"id\":\"" + System.Guid.NewGuid() + "\",\"expectedControlType\":\"Button\"},"
                + "{\"name\":\"ScrollWheel\",\"type\":\"PassThrough\",\"id\":\"" + System.Guid.NewGuid() + "\",\"expectedControlType\":\"Vector2\"}"
                + "],"
                + "\"bindings\":["
                + "{\"name\":\"\",\"id\":\"" + System.Guid.NewGuid() + "\",\"path\":\"<Pointer>/position\",\"action\":\"Point\"},"
                + "{\"name\":\"\",\"id\":\"" + System.Guid.NewGuid() + "\",\"path\":\"<Pointer>/press\",\"action\":\"LeftClick\"},"
                + "{\"name\":\"\",\"id\":\"" + System.Guid.NewGuid() + "\",\"path\":\"<Mouse>/scroll\",\"action\":\"ScrollWheel\"}"
                + "]"
                + "}]"
                + "}";
            asset = UnityEngine.InputSystem.InputActionAsset.FromJson(json);
            AssetDatabase.CreateAsset(asset, UiActionsPath);
        }
        return asset;
    }

    private static UnityEngine.InputSystem.InputActionReference EnsureUiActionReference(UnityEngine.InputSystem.InputActionAsset asset, string actionName)
    {
        UnityEngine.InputSystem.InputActionMap map = asset.FindActionMap("UI");
        UnityEngine.InputSystem.InputAction action = map.FindAction(actionName);

        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(UiActionsPath))
        {
            if (sub is UnityEngine.InputSystem.InputActionReference existing && existing.action != null && existing.action.id == action.id)
            {
                return existing;
            }
        }

        var reference = UnityEngine.InputSystem.InputActionReference.Create(action);
        reference.name = actionName;
        AssetDatabase.AddObjectToAsset(reference, asset);
        return reference;
    }
#endif

    private static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text NewText(Transform parent, string name, string content, int size, Color color, TextAnchor align,
        Vector2 anchor, Vector2 pos, Vector2 box)
    {
        RectTransform rt = NewUI(name, parent);
        Place(rt, anchor, pos, box);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.04f, 0.02f, 0.1f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    private static Button NewButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        return NewButton(parent, name, label, pos, size, Anchor.Center);
    }

    private static Button NewButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        RectTransform rt = NewUI(name, parent);
        Place(rt, anchor, pos, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.75f, 1f);
        colors.pressedColor = new Color(0.65f, 0.55f, 0.9f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            Text text = NewText(rt, "Label", label, 46, TextLight, TextAnchor.MiddleCenter, Anchor.Center, Vector2.zero, size);
            Stretch(text.rectTransform);
        }
        rt.gameObject.AddComponent<UIButtonSound>();
        return button;
    }

    private static RectTransform NewOverlay(Transform parent, string name)
    {
        RectTransform rt = NewUI(name, parent);
        Stretch(rt);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = Dim;
        return rt;
    }

    private static RectTransform NewPanel(Transform parent, string name, Vector2 size)
    {
        RectTransform rt = NewUI(name, parent);
        Place(rt, Anchor.Center, Vector2.zero, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        return rt;
    }

    private static Slider NewSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        RectTransform root = NewUI(name, parent);
        Place(root, Anchor.Center, pos, size);
        var slider = root.gameObject.AddComponent<Slider>();

        RectTransform bg = NewUI("Background", root);
        bg.anchorMin = new Vector2(0f, 0.3f);
        bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        var bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.04f, 0.2f, 1f);

        RectTransform fillArea = NewUI("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.3f);
        fillArea.anchorMax = new Vector2(1f, 0.7f);
        fillArea.offsetMin = new Vector2(10f, 0f);
        fillArea.offsetMax = new Vector2(-10f, 0f);
        RectTransform fill = NewUI("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.sizeDelta = new Vector2(10f, 0f);
        fill.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.45f, 0.9f);

        RectTransform handleArea = NewUI("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);
        RectTransform handle = NewUI("Handle", handleArea);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(34f, 0f);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Gold;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        return slider;
    }

    private static SettingsMenu BuildSettingsPanel(Transform canvas)
    {
        RectTransform overlay = NewOverlay(canvas, "SettingsPanel");
        RectTransform panel = NewPanel(overlay, "Panel", new Vector2(1000f, 800f));
        NewText(panel, "Title", "SETTINGS", 90, TextLight, TextAnchor.MiddleCenter, Anchor.TopCenter, new Vector2(0f, -50f), new Vector2(900f, 120f));

        NewText(panel, "MusicLabel", "MUSIC VOLUME", 42, TextLight, TextAnchor.MiddleLeft, Anchor.Center, new Vector2(-120f, 170f), new Vector2(600f, 60f));
        Text musicValue = NewText(panel, "MusicValue", "80%", 42, Gold, TextAnchor.MiddleRight, Anchor.Center, new Vector2(320f, 170f), new Vector2(200f, 60f));
        Slider music = NewSlider(panel, "MusicSlider", new Vector2(0f, 90f), new Vector2(820f, 70f));

        NewText(panel, "SfxLabel", "SFX VOLUME", 42, TextLight, TextAnchor.MiddleLeft, Anchor.Center, new Vector2(-120f, -30f), new Vector2(600f, 60f));
        Text sfxValue = NewText(panel, "SfxValue", "90%", 42, Gold, TextAnchor.MiddleRight, Anchor.Center, new Vector2(320f, -30f), new Vector2(200f, 60f));
        Slider sfx = NewSlider(panel, "SfxSlider", new Vector2(0f, -110f), new Vector2(820f, 70f));

        Button back = NewButton(panel, "BackButton", "BACK", new Vector2(0f, 60f), new Vector2(420f, 90f), Anchor.BottomCenter);

        var menu = overlay.gameObject.AddComponent<SettingsMenu>();
        SetRef(menu, "musicSlider", music);
        SetRef(menu, "sfxSlider", sfx);
        SetRef(menu, "musicValueText", musicValue);
        SetRef(menu, "sfxValueText", sfxValue);
        SetRef(menu, "backButton", back);
        return menu;
    }
}
