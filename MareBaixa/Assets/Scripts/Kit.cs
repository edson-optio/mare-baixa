using UnityEngine;
using UnityEngine.Tilemaps;

namespace MareBaixa
{
    // Blocos e fundo de uma zona. O Kit guarda um por Zone (índice = (int)Zone).
    [System.Serializable]
    public class ZoneArt
    {
        public TileBase top;
        public TileBase fill;
        public TileBase platform;
        public Sprite background;
        public Sprite silhouettes;
        public Color backgroundColor = Color.black;
    }

    // Tudo o que as salas montadas em tempo de execução precisam: blocos, prefabs, sprites e
    // os arquivos de texto das salas. O ProjectBuilder preenche o asset Assets/Kit.asset.
    [CreateAssetMenu(menuName = "Maré Baixa/Kit")]
    public class Kit : ScriptableObject
    {
        public static Kit Current;

        [Header("Zonas (índice = Zone)")]
        public ZoneArt[] zones;

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
        public GameObject moray;
        public GameObject crab;
        public GameObject angler;
        public TentaculoSombrio tentacle;
        public GameObject valve;
        public GameObject current;
        public GameObject healFont;
        public GameObject colosso;
        public GameObject polvo;

        [Header("Sprites")]
        public Sprite[] shellSprites;   // índice = ShellType (0 = sem concha)
        public Sprite[] shardSprites;   // cacos da concha quebrando, mesmo índice
        public Sprite[] sparkFrames;
        public Sprite[] seaweedFrames;
        public Sprite coral1;
        public Sprite coral2;
        public Sprite sign;
        public Sprite bottleHouse;
        public Sprite car;
        public Sprite trafficLight;
        public Sprite bioCoral;
        public Sprite trash;
        public Sprite white;
        public Sprite[] waterSurface;
        public Sprite pearlIcon;
        public Sprite algaIcon;
        public Sprite bubbleIcon;
        public Sprite conceptArt;
        public Sprite[] portraits;      // retratos do diálogo, índice = NpcKind
        public Sprite cascoPortrait;
        public Sprite[] cutscenePanels;  // Art/Cutscenes, procurados pelo nome (cena1-1...)

        [Header("Salas (texto)")]
        public TextAsset village;
        public TextAsset reefEntrance;
        public TextAsset reefShellRoom;
        public TextAsset[] reefRooms;
        public TextAsset bubbleRoom;
        public TextAsset cityEntrance;
        public TextAsset cityBossRoom;
        public TextAsset[] cityRooms;
        public TextAsset abyssEntrance;
        public TextAsset abyssBossRoom;
        public TextAsset[] abyssRooms;

        public Sprite ShellSprite(ShellType type) => shellSprites[(int)type];
        public ZoneArt Art(Zone zone) => zones[(int)zone];

        // Quadro de cutscene pelo nome; nulo se a arte ainda não existir.
        public Sprite CutscenePanel(string name)
        {
            if (cutscenePanels == null) return null;
            foreach (var panel in cutscenePanels)
                if (panel != null && panel.name == name) return panel;
            return null;
        }
    }
}
