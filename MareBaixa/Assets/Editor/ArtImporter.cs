using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MareBaixa.Editor
{
    // Importa toda a arte como pixel art (32 Pixels Per Unit, Point, sem compressão, Full Rect)
    // e cria os Tiles dos blocos de chão de cada zona. Arte que ainda não chegou (o Gerador-Imagens
    // entrega pelo mesmo nome) vira um placeholder colorido em Art/Provisorios, para o jogo montar já.
    public static class ArtImporter
    {
        public const string GeneratedPath = "Assets/Art/Gerados/";
        // v0.3: os mesmos sprites com o dobro de resolução (64 px por bloco). O tamanho no mundo não muda;
        // quando a versão 64 existe, ela substitui a de Gerados.
        public const string HighResPath = "Assets/Art/Sprites64/";
        public const int HighResPixelsPerUnit = 64;
        public const string SceneryPath = "Assets/Art/Cenarios/";
        public const string ConceptPath = "Assets/Art/Conceito/";
        public const string PortraitsPath = "Assets/Art/Retratos/";
        public const string BlocksPath = "Assets/Art/Blocos/";
        public const string CutscenesPath = "Assets/Art/Cutscenes/";
        public const string PlaceholderPath = "Assets/Art/Provisorios/";
        public const string TilesPath = "Assets/Tiles/";
        public const int PixelsPerUnit = 32;

        // Personagens e objetos apoiados no chão têm o pivô nos pés; o resto, no centro.
        static readonly string[] FeetPivot =
        {
            "casco-", "concha-", "vovo-buzia", "ostracia", "dona-lagosta", "bolota", "saida", "placa",
            "coral-", "alga-decor-", "casa-garrafa", "bau-", "siri-", "tentaculo-", "valvula-", "fonte-",
            "colosso-", "carro", "semaforo", "lixo", "moreia-",
        };

        // Tamanho em pixels dos placeholders (o mesmo combinado na tarefa de arte); o resto é 32x32.
        static readonly (string prefix, int width, int height)[] PlaceholderSizes =
        {
            ("moreia-", 64, 32), ("pescador-", 48, 32), ("tentaculo-aviso", 32, 8), ("tentaculo-", 32, 64),
            ("saida-fechada", 32, 64), ("colosso-", 96, 80), ("polvo-corpo", 192, 128), ("polvo-tinta", 16, 16),
            ("caco-", 8, 8), ("carro", 64, 32), ("semaforo", 16, 48), ("lixo", 32, 16),
            ("fundo-", 640, 360), ("silhuetas-", 640, 360), ("cena", 640, 360),
        };

        static readonly string[] TileNames =
        {
            "recife-topo", "recife-meio", "recife-plataforma", "vila-topo", "vila-meio", "vila-plataforma",
            "cidade-topo", "cidade-meio", "cidade-plataforma", "fossa-topo", "fossa-meio", "fossa-plataforma",
        };

        public static void ImportAll()
        {
            Directory.CreateDirectory(TilesPath);
            AssetDatabase.Refresh();
            foreach (string path in Pngs(GeneratedPath))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                bool feet = FeetPivot.Any(prefix => name.StartsWith(prefix));
                Import(path, feet ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f), false);
            }
            foreach (string path in Pngs(HighResPath))
                Import(path, PivotOf(Path.GetFileNameWithoutExtension(path)), false, HighResPixelsPerUnit);
            foreach (string path in Pngs(SceneryPath))
                Import(path, new Vector2(.5f, .5f), Path.GetFileName(path).StartsWith("silhuetas"));
            foreach (string path in Pngs(ConceptPath).Concat(Pngs(PortraitsPath)).Concat(Pngs(BlocksPath)).Concat(Pngs(CutscenesPath)))
                Import(path, new Vector2(.5f, .5f), false);
            foreach (string path in Pngs(PlaceholderPath))
                Import(path, PivotOf(Path.GetFileNameWithoutExtension(path)), Path.GetFileName(path).StartsWith("silhuetas"));
            CreateTiles();
        }

        static string[] Pngs(string folder) => Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.png").Select(p => p.Replace('\\', '/')).ToArray()
            : new string[0];

        static void Import(string path, Vector2 pivot, bool repeat, int pixelsPerUnit = PixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            // Camadas do parallax e a superfície da água são desenhadas repetidas (Draw Mode = Tiled).
            importer.wrapMode = repeat || Path.GetFileName(path).StartsWith("agua-superficie") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // Um Tile por bloco. Todos colidem como bloco cheio; as plataformas viram de mão única
        // pelo Platform Effector 2D do Tilemap delas (ver Room.cs).
        static void CreateTiles()
        {
            foreach (string name in TileNames)
            {
                string tilePath = TilesPath + name + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = Load(BlocksPath, name);
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
            AssetDatabase.SaveAssets();
        }

        public static Tile TileAsset(string name) => AssetDatabase.LoadAssetAtPath<Tile>(TilesPath + name + ".asset");
        public static Sprite Sprite(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(HighResPath + name + ".png") ?? Load(GeneratedPath, name);
        public static Sprite Scenery(string name) => Load(SceneryPath, name);

        // Todos os quadros de cutscene que existirem (cena1-1, cena1-2...). Os que faltam, o jogo desenha.
        public static Sprite[] Cutscenes() => Pngs(CutscenesPath)
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s => s != null).ToArray();

        static Vector2 PivotOf(string name) => FeetPivot.Any(prefix => name.StartsWith(prefix)) ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f);

        // A arte de verdade quando existe; senão, um placeholder com o mesmo nome.
        static Sprite Load(string folder, string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + name + ".png");
            if (sprite != null) return sprite;
            string path = PlaceholderPath + name + ".png";
            if (!File.Exists(path)) WritePlaceholder(name, path);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Import(path, PivotOf(name), name.StartsWith("silhuetas"));
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
        }

        // Retângulo arredondado de cor tirada do nome, com contorno escuro, para dar para jogar e ver.
        static void WritePlaceholder(string name, string path)
        {
            int width = 32, height = 32;
            foreach (var (prefix, w, h) in PlaceholderSizes)
                if (name.StartsWith(prefix)) { width = w; height = h; break; }
            Directory.CreateDirectory(PlaceholderPath);
            float hue = (Mathf.Abs(name.GetHashCode()) % 1000) / 1000f;
            bool scenery = width >= 640;
            Color fill = Color.HSVToRGB(hue, scenery ? .45f : .55f, scenery ? .25f : .85f);
            Color outline = new Color32(16, 27, 45, 255);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            int inset = scenery ? 0 : Mathf.Max(1, Mathf.Min(width, height) / 10);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = x >= inset && y >= 0 && x < width - inset && y < height - inset;
                    bool corner = !scenery && (x - inset < 2 || width - inset - 1 - x < 2) && (height - inset - 1 - y < 2);
                    bool edge = !scenery && (x == inset || x == width - inset - 1 || y == 0 || y == height - inset - 1);
                    Color color = !inside || corner ? Color.clear : edge ? outline : fill;
                    if (scenery && name.StartsWith("silhuetas")) color = y < height / 4 ? fill * 1.4f : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        public static Sprite Concept(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ConceptPath + name + ".png");
        public static Sprite Portrait(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PortraitsPath + name + ".png");

        // Quadros prefixo-1, prefixo-2...: pelo menos "count", e mais se a arte trouxer mais quadros (a versão
        // 64 tem animações mais longas). Uma animação nunca mistura as duas resoluções.
        public static Sprite[] Frames(string prefix, int count)
        {
            string folder = File.Exists(HighResPath + prefix + "-1.png") ? HighResPath : GeneratedPath;
            int total = count;
            while (File.Exists(folder + prefix + "-" + (total + 1) + ".png")) total++;
            var frames = new Sprite[total];
            for (int i = 0; i < total; i++)
            {
                string name = prefix + "-" + (i + 1);
                frames[i] = folder == HighResPath && File.Exists(folder + name + ".png")
                    ? AssetDatabase.LoadAssetAtPath<Sprite>(folder + name + ".png")
                    : Load(GeneratedPath, name);
            }
            return frames;
        }
    }
}
