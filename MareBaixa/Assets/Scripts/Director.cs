using UnityEngine;

namespace MareBaixa
{
    // Base dos gerenciadores de cena jogável (Vila e Recife). Os objetos do mundo falam com a
    // cena por aqui, sem precisar saber se estão na vila ou numa run.
    public abstract class Director : MonoBehaviour
    {
        public static Director Instance { get; private set; }

        public Kit kit;
        public GameAudio Sound { get; private set; }
        public Hud Hud { get; private set; }
        public Casco Casco { get; protected set; }
        public CameraFollow CameraRig { get; private set; }
        public Room Room { get; protected set; }
        public bool Paused { get; protected set; }

        // Bloqueia o controle de Casco (diálogos, loja, telas de morte e transições).
        public virtual bool InputLocked => Paused;

        // Arenas: a saída só abre depois do chefe (RoomExit consulta isto).
        public virtual bool ExitOpen => true;

        // Pérolas que aparecem no HUD: as guardadas na vila mais as pegas nesta run.
        public virtual int PearlsShown => Progress.Data.pearls;

        protected virtual void Awake()
        {
            Instance = this;
            Kit.Current = kit;
            Time.timeScale = 1f;
            Controls.ClearSimulation();
            Sound = new GameObject("Áudio", typeof(AudioSource), typeof(AudioLowPassFilter)).AddComponent<GameAudio>();
            Hud = new GameObject("HUD").AddComponent<Hud>();
            CameraRig = Camera.main.GetComponent<CameraFollow>();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected Casco SpawnCasco(Vector2 position, ShellType shell)
        {
            Casco = Instantiate(kit.casco, position, Quaternion.identity);
            Casco.name = "Casco";
            Casco.Equip(shell, Shells.Get(shell).Resistance);
            CameraRig.target = Casco.transform;
            return Casco;
        }

        protected Room BuildRoom(TextAsset file, Zone zone)
        {
            if (Room != null) Destroy(Room.gameObject);
            Room = Room.Build(file, zone, kit);
            CameraRig.SetBounds(Room.Width, Room.Height);
            ZoneBackground.Show(zone);
            return Room;
        }

        public virtual void AddPearls(int amount)
        {
            Sound.Play(Sfx.Pearl);
        }

        public virtual void OnExitReached() { }
        public virtual void OnCascoDied(string reason) { }
        public virtual void OnTalk(Npc npc) { }
        public virtual void OnBossDefeated(Boss boss) { }
        public virtual void OnShellEquipped(ShellType type) { }
    }
}
