using UnityEngine;
using UnityEngine.Tilemaps;

namespace MareBaixa
{
    // Tudo o que as salas montadas em tempo de execução precisam: blocos, prefabs, sprites e
    // os arquivos de texto das salas. O ProjectBuilder preenche o asset Assets/Kit.asset.
    [CreateAssetMenu(menuName = "Maré Baixa/Kit")]
    public class Kit : ScriptableObject
    {
        public static Kit Current;

        [Header("Blocos")]
        public TileBase reefTop;
        public TileBase reefFill;
        public TileBase reefPlatform;
        public TileBase villageTop;
        public TileBase villageFill;
        public TileBase villagePlatform;

        [Header("Prefabs")]
        public Casco casco;
        public GameObject pearl;
        public GameObject alga;
        public GameObject bubble;
        public GameObject chest;
        public ShellPickup shellPickup;
        public GameObject jellyfish;
        public GameObject pufferfish;
        public Spike spike;
        public GameObject exit;
        public GameObject descent;
        public GameObject buzia;
        public GameObject ostracia;
        public GameObject lagosta;
        public GameObject bolota;

        [Header("Sprites")]
        public Sprite[] shellSprites;   // índice = ShellType (0 = sem concha)
        public Sprite[] shardSprites;   // cacos da concha quebrando, mesmo índice
        public Sprite[] sparkFrames;
        public Sprite[] seaweedFrames;
        public Sprite coral1;
        public Sprite coral2;
        public Sprite sign;
        public Sprite bottleHouse;
        public Sprite white;
        public Sprite[] waterSurface;
        public Sprite pearlIcon;
        public Sprite algaIcon;
        public Sprite bubbleIcon;
        public Sprite conceptArt;
        public Sprite[] portraits;      // retratos do diálogo, índice = NpcKind
        public Sprite cascoPortrait;

        [Header("Salas (texto)")]
        public TextAsset village;
        public TextAsset reefEntrance;
        public TextAsset reefShellRoom;
        public TextAsset[] reefRooms;

        public Sprite ShellSprite(ShellType type) => shellSprites[(int)type];
    }
}
