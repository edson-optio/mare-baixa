using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MareBaixa.Editor
{
    // Monta o projeto inteiro a partir da arte e dos arquivos de sala: importa sprites, cria tiles,
    // prefabs, o Kit e as cenas (Menu, Vila, Run). As salas são montadas em tempo de execução.
    // Os prefabs dos inimigos novos e dos chefes ficam nos arquivos parciais de cada frente
    // (ProjectBuilder.Mundo.cs e ProjectBuilder.Chefes.cs).
    public static partial class ProjectBuilder
    {
        public const int GroundLayer = 6;
        const string ScenesPath = "Assets/Scenes/";
        const string PrefabsPath = "Assets/Prefabs/";
        const string KitPath = "Assets/Kit.asset";
        const string AppPath = "../Entrega/MareBaixa.app";

        static readonly string[] SceneOrder = { GameRules.MenuScene, GameRules.VillageScene, GameRules.RunScene };

        [InitializeOnLoadMethod]
        static void FirstOpen()
        {
            if (Application.isBatchMode || File.Exists(ScenesPath + "Menu.unity")) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenesPath + "Menu.unity")) CreateProject();
            };
        }

        [MenuItem("Maré Baixa/1. Montar projeto")]
        public static void CreateProject()
        {
            Directory.CreateDirectory(ScenesPath);
            Directory.CreateDirectory(PrefabsPath);
            ConfigureSettings();
            ArtImporter.ImportAll();
            AssetDatabase.Refresh();

            Kit kit = CreateKit();
            CreateMenuScene(kit);
            CreateGameScene(GameRules.VillageScene, "vila", kit, typeof(VilaManager));
            CreateGameScene(GameRules.RunScene, "recife", kit, typeof(RunManager));
            AssetDatabase.DeleteAsset(ScenesPath + "RecifeRaso.unity");   // cena antiga da v0.1

            EditorBuildSettings.scenes = SceneOrder
                .Select(name => new EditorBuildSettingsScene(ScenesPath + name + ".unity", true)).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenesPath + GameRules.MenuScene + ".unity");
            Debug.Log("MARE_PROJECT_READY");
        }

        // ------------------------------------------------------------------ Kit e prefabs

        static Kit CreateKit()
        {
            var kit = AssetDatabase.LoadAssetAtPath<Kit>(KitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<Kit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }

            // Índice = Zone { Vila, Recife, Cidade, Fossa, Bolha }. A bolha usa os blocos do recife.
            kit.zones = new[]
            {
                ZoneArtOf("vila", "vila", GameRules.DeepSea),
                ZoneArtOf("recife", "recife", GameRules.DeepSea),
                ZoneArtOf("cidade", "cidade", new Color32(28, 44, 40, 255)),
                ZoneArtOf("fossa", "fossa", GameRules.AbyssBlue),
                ZoneArtOf("recife", "bolha", GameRules.DeepSea),
            };

            kit.shellSprites = new[]
            {
                null, S("concha-comum"), S("concha-tampinha"), S("concha-espiral"), S("concha-lata"), S("concha-lampada"),
            };
            kit.weaponSprites = new[] { null, S("arma-canudo"), S("arma-garfo"), S("arma-escova") };
            kit.benchSprite = S("banco-descanso");
            kit.shardSprites = new[]
            {
                null, S("caco-areia"), S("caco-turquesa"), S("caco-creme"), S("caco-lata"), S("caco-lampada"),
            };
            kit.sparkFrames = ArtImporter.Frames("brilho", 3);
            kit.seaweedFrames = ArtImporter.Frames("alga-decor", 2);
            kit.coral1 = S("coral-1");
            kit.coral2 = S("coral-2");
            kit.sign = S("placa");
            kit.bottleHouse = S("casa-garrafa");
            kit.car = S("carro");
            kit.trafficLight = S("semaforo");
            kit.bioCoral = S("coral-bio");
            kit.trash = S("lixo");
            kit.cutscenePanels = ArtImporter.Cutscenes();
            kit.white = S("agua");
            kit.waterSurface = ArtImporter.Frames("agua-superficie", 2);
            kit.pearlIcon = S("perola-1");
            kit.algaIcon = S("alga");
            kit.bubbleIcon = S("bolha-1");
            kit.conceptArt = ArtImporter.Concept("casco-conceito");
            // Retratos do Gerador-Imagens quando existirem; senão, o próprio sprite do morador.
            kit.portraits = new[]
            {
                ArtImporter.Portrait("vovo-buzia") ?? S("vovo-buzia"),
                ArtImporter.Portrait("ostracia") ?? S("ostracia"),
                ArtImporter.Portrait("dona-lagosta") ?? S("dona-lagosta"),
                ArtImporter.Portrait("bolota") ?? S("bolota"),
            };
            kit.cascoPortrait = ArtImporter.Portrait("casco") ?? S("casco-parado-1");

            kit.village = Room("vila");
            kit.reefEntrance = Room("recife-entrada");
            kit.reefShellRoom = Room("recife-concha");
            kit.reefRooms = Bank("recife-0*.txt");
            kit.bubbleRoom = Room("bolha");
            kit.cityEntrance = Room("cidade-entrada");
            kit.cityBossRoom = Room("cidade-colosso");
            kit.cityRooms = Bank("cidade-0*.txt");
            kit.abyssEntrance = Room("fossa-entrada");
            kit.abyssBossRoom = Room("fossa-polvo");
            kit.abyssRooms = Bank("fossa-0*.txt");

            kit.casco = CreateCasco();
            kit.pearl = Item<Pearl>("Perola", S("perola-1"), .3f, pearl => pearl.frames = ArtImporter.Frames("perola", 2));
            kit.alga = Item<AlgaPickup>("Alga", S("alga"), .35f, null);
            kit.bubble = Item<AirBubble>("BolhaDeAr", S("bolha-1"), .4f, bubble => bubble.frames = ArtImporter.Frames("bolha", 2));
            kit.chest = CreateChest();
            kit.shellPickup = CreateShellPickup();
            kit.jellyfish = CreateJellyfish();
            kit.pufferfish = CreatePufferfish();
            kit.spike = CreateSpike();
            kit.exit = CreateExit();
            kit.descent = CreateDescent();
            kit.buzia = CreateNpc("VovoBuzia", NpcKind.Buzia, "Vovó Búzia", "vovo-buzia");
            kit.ostracia = CreateNpc("Ostracia", NpcKind.Ostracia, "Ostrácia", "ostracia");
            kit.lagosta = CreateNpc("DonaLagosta", NpcKind.Lagosta, "Dona Lagosta", "dona-lagosta");
            kit.bolota = CreateNpc("Bolota", NpcKind.Bolota, "Bolota", "bolota");
            kit.moray = CreateMoray();
            kit.crab = CreateCrab();
            kit.angler = CreateAngler();
            kit.tentacle = CreateTentacle();
            kit.valve = CreateValve();
            kit.current = CreateCurrent();
            kit.healFont = CreateHealFont();
            kit.colosso = CreateColosso();
            kit.polvo = CreatePolvo();

            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            return kit;
        }

        static Sprite S(string name) => ArtImporter.Sprite(name);
        static TextAsset Room(string name) => AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Salas/" + name + ".txt");

        // Toda sala que casar com o padrão entra sozinha no banco de sorteio da zona.
        static TextAsset[] Bank(string pattern) => Directory.GetFiles("Assets/Salas", pattern).OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<TextAsset>(p.Replace('\\', '/'))).ToArray();

        static ZoneArt ZoneArtOf(string tiles, string scenery, Color background) => new ZoneArt
        {
            top = ArtImporter.TileAsset(tiles + "-topo"),
            fill = ArtImporter.TileAsset(tiles + "-meio"),
            platform = ArtImporter.TileAsset(tiles + "-plataforma"),
            background = ArtImporter.Scenery("fundo-" + scenery),
            silhouettes = ArtImporter.Scenery("silhuetas-" + scenery),
            backgroundColor = background,
        };

        static GameObject Sprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        static Rigidbody2D Kinematic(GameObject go)
        {
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            return body;
        }

        static CircleCollider2D TriggerCircle(GameObject go, float radius)
        {
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = radius;
            return collider;
        }

        static BoxCollider2D TriggerBox(GameObject go, Vector2 size, Vector2 offset)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            collider.offset = offset;
            return collider;
        }

        static T Save<T>(GameObject go, string name) where T : Object
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsPath + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab is T asGameObject ? asGameObject : prefab.GetComponent(typeof(T)) as T;
        }

        static Casco CreateCasco()
        {
            var go = new GameObject("Casco");
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = GameRules.GravityScale;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(.72f, .58f);
            collider.offset = new Vector2(0f, .31f);
            collider.edgeRadius = .02f;
            collider.sharedMaterial = NoFriction();   // não gruda ao pular encostado na parede

            // Visual fica no meio do corpo para rolar e girar em volta do centro, não dos pés.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            visual.localPosition = new Vector3(0f, .5f, 0f);
            var bodyRenderer = Sprite("Corpo", S("casco-parado-1"), 20).GetComponent<SpriteRenderer>();
            bodyRenderer.transform.SetParent(visual, false);
            bodyRenderer.transform.localPosition = new Vector3(0f, -.5f, 0f);
            var shellRenderer = Sprite("Concha", S("concha-comum"), 21).GetComponent<SpriteRenderer>();
            shellRenderer.transform.SetParent(visual, false);
            shellRenderer.transform.localPosition = new Vector3(0f, -.5f, 0f);

            var casco = go.AddComponent<Casco>();
            casco.visual = visual;
            casco.bodyRenderer = bodyRenderer;
            casco.shellRenderer = shellRenderer;
            casco.idleFrames = ArtImporter.Frames("casco-parado", 2);
            casco.walkFrames = ArtImporter.Frames("casco-anda", 4);
            casco.jumpFrame = S("casco-pulo");
            casco.fallFrame = S("casco-queda");
            casco.attackFrames = ArtImporter.Frames("casco-ataque", 2);
            return Save<Casco>(go, "Casco");
        }

        static PhysicsMaterial2D NoFriction()
        {
            const string path = PrefabsPath + "SemAtrito.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material != null) return material;
            material = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Item<T>(string name, Sprite sprite, float radius, System.Action<T> setup) where T : Component
        {
            var go = Sprite(name, sprite, 10);
            Kinematic(go);
            TriggerCircle(go, radius);
            setup?.Invoke(go.AddComponent<T>());
            return Save<GameObject>(go, name);
        }

        static GameObject CreateChest()
        {
            var go = Sprite("Bau", S("bau-fechado"), 10);
            TriggerBox(go, new Vector2(.9f, .7f), new Vector2(0f, .35f));
            var chest = go.AddComponent<Chest>();
            chest.closedSprite = S("bau-fechado");
            chest.openSprite = S("bau-aberto");
            return Save<GameObject>(go, "Bau");
        }

        static ShellPickup CreateShellPickup()
        {
            var go = new GameObject("ConchaNoChao");
            var art = Sprite("Arte", S("concha-comum"), 10);
            art.transform.SetParent(go.transform, false);
            go.AddComponent<ShellPickup>().art = art.GetComponent<SpriteRenderer>();
            return Save<ShellPickup>(go, "ConchaNoChao");
        }

        static GameObject CreateJellyfish()
        {
            var calm = ArtImporter.Frames("agua-viva", 2);
            var go = Sprite("AguaViva", calm[0], 15);
            Kinematic(go);
            TriggerCircle(go, .4f);
            var jelly = go.AddComponent<AguaViva>();
            jelly.calmFrames = calm;
            jelly.chargedFrames = ArtImporter.Frames("agua-viva-carregada", 2);
            return Save<GameObject>(go, "AguaViva");
        }

        static GameObject CreatePufferfish()
        {
            var swim = ArtImporter.Frames("baiacu", 2);
            var go = Sprite("Baiacu", swim[0], 15);
            Kinematic(go);
            TriggerCircle(go, .38f);
            var puffer = go.AddComponent<Baiacu>();
            puffer.swimFrames = swim;
            puffer.inflated = S("baiacu-inflado");
            return Save<GameObject>(go, "Baiacu");
        }

        static Spike CreateSpike()
        {
            var go = Sprite("Espinho", S("espinho"), 15);
            Kinematic(go);
            TriggerCircle(go, .15f);
            go.AddComponent<Spike>();
            return Save<Spike>(go, "Espinho");
        }

        static GameObject CreateExit()
        {
            var go = Sprite("Saida", S("saida"), 10);
            TriggerBox(go, new Vector2(1f, 2f), new Vector2(0f, 1f));
            var exit = go.AddComponent<RoomExit>();
            exit.openSprite = S("saida");
            exit.closedSprite = S("saida-fechada");   // arena com o chefe vivo
            return Save<GameObject>(go, "Saida");
        }

        static GameObject CreateDescent()
        {
            var go = Sprite("Descida", S("saida"), 10);
            go.AddComponent<Descent>();
            return Save<GameObject>(go, "Descida");
        }

        static GameObject CreateNpc(string name, NpcKind kind, string displayName, string sprite)
        {
            var go = Sprite(name, S(sprite), 12);
            var npc = go.AddComponent<Npc>();
            npc.kind = kind;
            npc.displayName = displayName;
            return Save<GameObject>(go, name);
        }

        // ------------------------------------------------------------------ Cenas

        static Camera CreateCamera(Color background)
        {
            // Câmera de fundo pinta as barras pretas quando a janela não é 16:9.
            var bars = new GameObject("Barras", typeof(Camera)).GetComponent<Camera>();
            bars.clearFlags = CameraClearFlags.SolidColor;
            bars.backgroundColor = Color.black;
            bars.cullingMask = 0;
            bars.depth = -100;

            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(FixedViewport), typeof(CameraFollow));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(10f, GameRules.CameraSize, -10f);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = GameRules.CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            return camera;
        }

        // Fundo fixo na câmera (640x360 = a tela inteira) e a camada de silhuetas em parallax.
        static void CreateBackground(Camera camera, string zone)
        {
            var far = Sprite("Fundo", ArtImporter.Scenery("fundo-" + zone), -100);
            far.transform.SetParent(camera.transform, false);
            far.transform.localPosition = new Vector3(0f, 0f, 20f);

            Sprite silhouettes = ArtImporter.Scenery("silhuetas-" + zone);
            var layer = Sprite("Silhuetas (parallax)", silhouettes, -90);
            layer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 10f);
            var renderer = layer.GetComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(silhouettes.bounds.size.x * 3f, silhouettes.bounds.size.y);
            var parallax = layer.AddComponent<Parallax>();
            parallax.parallaxEffect = .7f;
            parallax.verticalEffect = 1f;   // as silhuetas ficam sempre na parte de baixo da tela

            var zoneBackground = camera.gameObject.AddComponent<ZoneBackground>();
            zoneBackground.far = far.GetComponent<SpriteRenderer>();
            zoneBackground.silhouettes = renderer;
        }

        static void CreateMenuScene(Kit kit)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(GameRules.DeepSea);
            var menu = new GameObject("Menu").AddComponent<MenuScreen>();
            menu.keyArt = ArtImporter.Scenery("menu-chave");
            menu.conceptArt = kit.conceptArt;
            new GameObject("Verificação automática").AddComponent<GameVerification>();
            EditorSceneManager.SaveScene(scene, ScenesPath + GameRules.MenuScene + ".unity");
        }

        static void CreateGameScene(string sceneName, string zone, Kit kit, System.Type director)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateCamera(GameRules.DeepSea);
            CreateBackground(camera, zone);
            var manager = (Director)new GameObject("Gerenciador").AddComponent(director);
            manager.kit = kit;
            EditorSceneManager.SaveScene(scene, ScenesPath + sceneName + ".unity");
        }

        // ------------------------------------------------------------------ Configuração e build

        static void ConfigureSettings()
        {
            PlayerSettings.companyName = "CC7140 Desenvolvimento de Jogos";
            PlayerSettings.productName = "Maré Baixa";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "br.cc7140.marebaixa");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            SetProperty("ProjectSettings/ProjectSettings.asset", "activeInputHandler", 2); // Teclado legado + gamepads pelo Input System
            SetGroundLayer();
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 0;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            // As pinças acham inimigos e baús por consulta (OverlapBox) e eles são triggers. No Unity 6.3,
            // com isto desligado a consulta ignora triggers mesmo com ContactFilter2D.useTriggers = true.
            Physics2D.queriesHitTriggers = true;
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        static void SetProperty(string asset, string property, int value)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(asset);
            if (assets.Length == 0) return;
            var settings = new SerializedObject(assets[0]);
            var field = settings.FindProperty(property);
            if (field == null) return;
            field.intValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Camada "Chao" (6): chão e plataformas, usada para saber se Casco está pisando em algo.
        static void SetGroundLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tagManager.FindProperty("layers").GetArrayElementAtIndex(GroundLayer).stringValue = "Chao";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BuildAll()
        {
            CreateProject();
            BuildMac();
        }

        [MenuItem("Maré Baixa/2. Gerar aplicativo macOS")]
        public static void BuildMac()
        {
            if (!File.Exists(ScenesPath + "Menu.unity")) CreateProject();
            Directory.CreateDirectory("../Entrega");
            var options = new BuildPlayerOptions
            {
                scenes = SceneOrder.Select(name => ScenesPath + name + ".unity").ToArray(),
                locationPathName = AppPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Falha no build: " + report.summary.result);
            Debug.Log("MARE_BUILD_READY: " + Path.GetFullPath(AppPath));
        }
    }
}
