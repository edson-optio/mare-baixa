using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MareBaixa.Editor
{
    // Importa toda a arte como pixel art (32 Pixels Per Unit, Point, sem compressão, Full Rect)
    // e cria os Tiles dos blocos de chão de cada zona.
    public static class ArtImporter
    {
        public const string GeneratedPath = "Assets/Art/Gerados/";
        public const string SceneryPath = "Assets/Art/Cenarios/";
        public const string ConceptPath = "Assets/Art/Conceito/";
        public const string PortraitsPath = "Assets/Art/Retratos/";
        public const string BlocksPath = "Assets/Art/Blocos/";
        public const string TilesPath = "Assets/Tiles/";
        public const int PixelsPerUnit = 32;

        // Personagens e objetos apoiados no chão têm o pivô nos pés; o resto, no centro.
        static readonly string[] FeetPivot =
        {
            "casco-", "concha-", "vovo-buzia", "ostracia", "dona-lagosta", "bolota", "saida", "placa",
            "coral-", "alga-decor-", "casa-garrafa", "bau-",
        };

        static readonly string[] TileNames =
        {
            "recife-topo", "recife-meio", "recife-plataforma", "vila-topo", "vila-meio", "vila-plataforma",
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
            foreach (string path in Pngs(SceneryPath))
                Import(path, new Vector2(.5f, .5f), Path.GetFileName(path).StartsWith("silhuetas"));
            foreach (string path in Pngs(ConceptPath).Concat(Pngs(PortraitsPath)).Concat(Pngs(BlocksPath)))
                Import(path, new Vector2(.5f, .5f), false);
            CreateTiles();
        }

        static string[] Pngs(string folder) => Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.png").Select(p => p.Replace('\\', '/')).ToArray()
            : new string[0];

        static void Import(string path, Vector2 pivot, bool repeat)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
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
                tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlocksPath + name + ".png");
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
            AssetDatabase.SaveAssets();
        }

        public static Tile TileAsset(string name) => AssetDatabase.LoadAssetAtPath<Tile>(TilesPath + name + ".asset");
        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedPath + name + ".png");
        public static Sprite Scenery(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SceneryPath + name + ".png");
        public static Sprite Concept(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ConceptPath + name + ".png");
        public static Sprite Portrait(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PortraitsPath + name + ".png");

        public static Sprite[] Frames(string prefix, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = Sprite(prefix + "-" + (i + 1));
            return frames;
        }
    }
}
