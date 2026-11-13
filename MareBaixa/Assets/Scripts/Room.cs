using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MareBaixa
{
    public enum Zone { Vila, Recife, Cidade, Fossa, Bolha }

    // Monta uma sala a partir de um arquivo de texto (Assets/Salas/*.txt) em tempo de execução,
    // para a run poder sortear a ordem das salas (GDD p. 5). Cada caractere é um bloco de 32 px:
    //
    //   #  pedra (o bloco de topo é escolhido sozinho)   =  plataforma que dá para atravessar por baixo
    //   C  início de Casco    S  saída da sala           D  descida para o Recife (vila)
    //   j  Água-viva Errante  b  Baiacu Nervoso  m  Moreia de Cano  s  Siri Saqueador
    //   f  Peixe-pescador  w  Tentáculo Sombrio (do chão, ou da parede se não houver chão embaixo)
    //   B  chefe da arena (Ermitão Colosso na Cidade, Polvo Abissal na Fossa)
    //   v  válvula   <  >  correnteza   H  fonte de cura (bolha de ar)
    //   o  pérola   T  baú   a  alga   O  bolha de ar
    //   R  banco de descanso (só em abrigos)   u  espada de canudo   F  lança de garfo   E  martelo de escova
    //   c  Concha Comum   t  Tampinha   e  Espiral   l  Lata   i  Lâmpada
    //   r  concha nova sorteada (Tampinha/Espiral no Recife, Lata/Lâmpada mais fundo)
    //   k  K  corais   g  algas   p  placa   h  casa de garrafa   x  carro   q  semáforo
    //   n  coral bioluminescente (luz fraca)   z  lixo                (decoração, sem colisão)
    //   V  Vovó Búzia   Y  Ostrácia   L  Dona Lagosta   U  Bolota
    //   1..9  dica de tutorial (texto na linha "@dicaN" do arquivo)
    //
    // Linhas que começam com @ são dados da sala: "@nome Entrada do Recife", "@mare 0.2",
    // "@nivel 2" (dificuldade das salas sorteadas, 1 a 3) e "@escuro 0.8" (opacidade da escuridão).
    public class Room : MonoBehaviour
    {
        public string RoomName { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Vector2 StartPoint { get; private set; }
        public float FloorY { get; private set; }
        public float TideSpeed { get; private set; } = -1f;
        public int Level { get; private set; } = 1;
        public float Darkness { get; private set; } = -1f;
        public Zone Zone { get; private set; }
        public Boss Boss { get; private set; }
        public Collider2D Platforms { get; private set; }
        public Mare Mare { get; private set; }
        public List<string> Rows { get; } = new List<string>();

        readonly Dictionary<string, string> data = new Dictionary<string, string>();

        public static Room Build(TextAsset file, Zone zone, Kit kit)
        {
            var room = new GameObject("Sala " + file.name).AddComponent<Room>();
            room.Zone = zone;
            room.Parse(file.text);
            room.Paint(zone, kit);
            room.Populate(kit);
            if (room.TideSpeed <= 0f) room.TideSpeed = DefaultTide(zone);
            if (room.TideSpeed > 0f)
                room.Mare = Mare.Create(room.transform, kit, room.Width, room.Height, room.FloorY, room.TideSpeed);
            if (room.Darkness < 0f) room.Darkness = zone == Zone.Fossa ? GameRules.AbyssDarkness : 0f;
            if (room.Darkness > 0f)
                Escuridao.Create(room.Darkness).transform.SetParent(room.transform, false);
            return room;
        }

        void Parse(string text)
        {
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                if (raw.StartsWith("@"))
                {
                    int space = raw.IndexOf(' ');
                    if (space > 1) data[raw.Substring(1, space - 1).Trim()] = raw.Substring(space + 1).Trim();
                    continue;
                }
                if (raw.Trim().Length == 0) continue;
                Rows.Add(raw);
            }
            Height = Rows.Count;
            foreach (string row in Rows) Width = Mathf.Max(Width, row.Length);
            RoomName = data.TryGetValue("nome", out string name) ? name : "Sala";
            // Valor torto num @ não pode derrubar a montagem da sala (a run ficaria presa na transição).
            TideSpeed = Number("mare", TideSpeed);
            Level = Mathf.RoundToInt(Number("nivel", Level));
            Darkness = Number("escuro", Darkness);
        }

        float Number(string key, float fallback)
        {
            if (!data.TryGetValue(key, out string text)) return fallback;
            string first = text.Split(' ')[0].Replace(',', '.');
            if (float.TryParse(first, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                return value;
            Debug.LogWarning("Sala " + RoomName + ": @" + key + " inválido (" + text + ")");
            return fallback;
        }

        // A vila e a bolha de ar não têm maré; o resto sobe na velocidade da zona (GDD p. 5).
        static float DefaultTide(Zone zone) => zone switch
        {
            Zone.Recife => GameRules.ReefTideSpeed,
            Zone.Cidade => GameRules.CityTideSpeed,
            Zone.Fossa => GameRules.AbyssTideSpeed,
            _ => 0f,
        };

        // Lê só o @nivel de um arquivo, para o RunManager ordenar as salas sem montá-las.
        public static int LevelOf(TextAsset file)
        {
            foreach (string raw in file.text.Replace("\r", "").Split('\n'))
                if (raw.StartsWith("@nivel ") && int.TryParse(raw.Substring(7).Trim(), out int level)) return level;
            return 1;
        }

        // Caractere na coluna x, linha y (y = 0 é a linha de baixo).
        public char At(int x, int y)
        {
            if (y < 0 || y >= Height) return ' ';
            string row = Rows[Height - 1 - y];
            return x >= 0 && x < row.Length ? row[x] : ' ';
        }

        void Paint(Zone zone, Kit kit)
        {
            ZoneArt art = kit.Art(zone);
            TileBase top = art.top;
            TileBase fill = art.fill;
            TileBase platform = art.platform;

            var grid = new GameObject("Grid", typeof(Grid)).transform;
            grid.SetParent(transform, false);
            Tilemap ground = CreateTilemap("Chao", grid, 0);
            Tilemap platforms = CreateTilemap("Plataformas", grid, 1);

            FloorY = float.MaxValue;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = At(x, y);
                    if (c == '#')
                    {
                        // O teto da sala (linha de cima) não ganha acabamento de superfície.
                        bool exposed = y + 1 < Height && At(x, y + 1) != '#';
                        ground.SetTile(new Vector3Int(x, y, 0), exposed ? top : fill);
                        if (exposed) FloorY = Mathf.Min(FloorY, y + 1);
                    }
                    else if (c == '=')
                        platforms.SetTile(new Vector3Int(x, y, 0), platform);
                }
            }
            if (FloorY == float.MaxValue) FloorY = 0f;

            AddCollision(ground.gameObject, false);
            Platforms = AddCollision(platforms.gameObject, true);
        }

        static Tilemap CreateTilemap(string name, Transform grid, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        // Tilemap Collider 2D + Composite Collider 2D + Rigidbody 2D estático, como no lab05.
        // As plataformas ganham um Platform Effector 2D de mão única.
        static Collider2D AddCollision(GameObject tilemap, bool oneWay)
        {
            tilemap.layer = LayerMask.NameToLayer("Chao");
            var body = tilemap.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = tilemap.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = tilemap.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (oneWay)
            {
                composite.usedByEffector = true;
                var effector = tilemap.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 160f;
            }
            tilemapCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            return composite;
        }

        void Populate(Kit kit)
        {
            var objects = new GameObject("Objetos").transform;
            objects.SetParent(transform, false);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = At(x, y);
                    var feet = new Vector2(x + .5f, y);
                    var center = new Vector2(x + .5f, y + .5f);
                    switch (c)
                    {
                        case 'C': StartPoint = feet; break;
                        case 'S': Spawn(kit.exit, feet, objects); break;
                        case 'D': Spawn(kit.descent, feet, objects); break;
                        case 'j': Spawn(kit.jellyfish, center, objects); break;
                        case 'b': Spawn(kit.pufferfish, center, objects); break;
                        case 'o': Spawn(kit.pearl, center, objects); break;
                        case 'T': Spawn(kit.chest, feet, objects); break;
                        case 'a': Spawn(kit.alga, center, objects); break;
                        case 'O': Spawn(kit.bubble, center, objects); break;
                        case 'c': ShellPickup.Spawn(ShellType.Comum, -1, feet, objects); break;
                        case 't': ShellPickup.Spawn(ShellType.Tampinha, -1, feet, objects); break;
                        case 'e': ShellPickup.Spawn(ShellType.Espiral, -1, feet, objects); break;
                        case 'l': ShellPickup.Spawn(ShellType.Lata, -1, feet, objects); break;
                        case 'i': ShellPickup.Spawn(ShellType.Lampada, -1, feet, objects); break;
                        case 'r': ShellPickup.Spawn(Shells.RandomFor(Zone), -1, feet, objects); break;
                        case 'm': Spawn(kit.moray, feet, objects); break;
                        case 's': Spawn(kit.crab, feet, objects); break;
                        case 'f': Spawn(kit.angler, center, objects); break;
                        case 'w': SpawnTentacle(kit, x, y, objects); break;
                        case 'v': Spawn(kit.valve, feet, objects); break;
                        case '<': SpawnCurrent(kit, center, -1f, objects); break;
                        case '>': SpawnCurrent(kit, center, 1f, objects); break;
                        case 'H': Spawn(kit.healFont, feet, objects); break;
                        case 'R': BancoDescanso.Spawn(feet, objects); break;
                        case 'u': WeaponPickup.Spawn(WeaponType.Canudo, feet, objects); break;
                        case 'F': WeaponPickup.Spawn(WeaponType.Garfo, feet, objects); break;
                        case 'E': WeaponPickup.Spawn(WeaponType.Escova, feet, objects); break;
                        case 'B':
                            var boss = Spawn(Zone == Zone.Fossa ? kit.polvo : kit.colosso, feet, objects);
                            if (boss != null) Boss = boss.GetComponent<Boss>();
                            break;
                        case 'x': Decor(kit.car, feet, objects); break;
                        case 'q': Decor(kit.trafficLight, feet, objects); break;
                        case 'z': Decor(kit.trash, feet, objects); break;
                        case 'n': Decor(kit.bioCoral, feet, objects).gameObject.AddComponent<LightSource>().radius = 1.6f; break;
                        case 'k': Decor(kit.coral1, feet, objects); break;
                        case 'K': Decor(kit.coral2, feet, objects); break;
                        case 'p': Decor(kit.sign, feet, objects); break;
                        case 'h': Decor(kit.bottleHouse, feet, objects); break;
                        case 'g':
                            Decor(kit.seaweedFrames[0], feet, objects).gameObject.AddComponent<SpriteAnimator>()
                                .Play(kit.seaweedFrames, 2.5f);
                            break;
                        case 'V': Spawn(kit.buzia, feet, objects); break;
                        case 'Y': Spawn(kit.ostracia, feet, objects); break;
                        case 'L': Spawn(kit.lagosta, feet, objects); break;
                        case 'U': Spawn(kit.bolota, feet, objects); break;
                        default:
                            if (c >= '1' && c <= '9' && data.TryGetValue("dica" + c, out string text))
                            {
                                var hint = new GameObject("Dica " + c).AddComponent<Hint>();
                                hint.transform.SetParent(objects, false);
                                hint.transform.position = center;
                                hint.text = text;
                            }
                            break;
                    }
                }
            }
        }

        // Prefab que ainda não existe (frente em construção) só avisa, para a sala montar mesmo assim.
        static GameObject Spawn(GameObject prefab, Vector2 position, Transform parent)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Sala: prefab faltando em " + position);
                return null;
            }
            return Instantiate(prefab, position, Quaternion.identity, parent);
        }

        static void SpawnCurrent(Kit kit, Vector2 center, float direction, Transform parent)
        {
            var current = Spawn(kit.current, center, parent);
            var flow = current != null ? current.GetComponent<Correnteza>() : null;
            if (flow != null) flow.direction = direction;
        }

        // O tentáculo sai do chão; sem chão embaixo e com parede ao lado, sai da parede.
        void SpawnTentacle(Kit kit, int x, int y, Transform parent)
        {
            if (kit.tentacle == null) return;
            Vector2 up = Vector2.up;
            Vector2 feet = new Vector2(x + .5f, y);
            if (At(x, y - 1) != '#')
            {
                if (At(x - 1, y) == '#') { up = Vector2.right; feet = new Vector2(x, y + .5f); }
                else if (At(x + 1, y) == '#') { up = Vector2.left; feet = new Vector2(x + 1f, y + .5f); }
            }
            TentaculoSombrio.Spawn(feet, up, parent, false);
        }

        static SpriteRenderer Decor(Sprite sprite, Vector2 position, Transform parent)
        {
            var renderer = new GameObject(sprite != null ? sprite.name : "Decoração", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.transform.position = position;
            renderer.sprite = sprite;
            renderer.sortingOrder = 3;
            return renderer;
        }
    }
}
