using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MareBaixa
{
    // Uma etapa da run: a sala e a zona a que ela pertence.
    public struct RunStep
    {
        public TextAsset room;
        public Zone zone;
        public string zoneName;
    }

    // A run inteira (GDD p. 4): Recife Raso, bolha de ar, Cidade Afundada com o Ermitão Colosso, bolha de
    // ar e Fossa Abissal com o Polvo Abissal. Sorteia as salas, troca de sala na saída, cuida da pausa, da
    // morte (volta à vila com metade das pérolas), das cutscenes e da vitória.
    public class RunManager : Director
    {
        readonly List<RunStep> plan = new List<RunStep>();
        readonly List<TextAsset> rooms = new List<TextAsset>();

        public IReadOnlyList<RunStep> Plan => plan;
        public IReadOnlyList<TextAsset> Rooms => rooms;
        public int RoomIndex { get; private set; }
        public Zone CurrentZone => plan.Count > 0 ? plan[RoomIndex].zone : Zone.Recife;
        public int PearlsCollected { get; private set; }
        public bool Ending { get; private set; }
        public bool Transitioning { get; private set; }

        LightSource cascoLight;

        public override bool InputLocked =>
            base.InputLocked || Transitioning || Ending || Hud.PanelOpen || CutscenePlayer.Playing;
        public override int PearlsShown => Progress.Data.pearls + PearlsCollected;
        public override bool ExitOpen => Room == null || Room.Boss == null || Room.Boss.Defeated;

        void Start()
        {
            BuildRunOrder();
            Progress.Data.runs++;
            Progress.Save();
            Hud.SetFade(1f);
            StartCoroutine(EnterRoom(0));
        }

        // Cada zona: entrada sempre igual, salas sorteadas do banco em ordem de dificuldade (@nivel) e
        // a sala fixa do fim (concha garantida no Recife, chefe na Cidade e na Fossa). GDD p. 4 e 5.
        void BuildRunOrder()
        {
            var reef = Pick(kit.reefRooms, GameRules.ReefRandomRooms);
            reef.Insert(Random.Range(0, reef.Count + 1), kit.reefShellRoom);
            AddZone(Zone.Recife, GameRules.ReefName, kit.reefEntrance, reef, null);
            AddStep(kit.bubbleRoom, Zone.Bolha, GameRules.BubbleName);
            AddZone(Zone.Cidade, GameRules.CityName, kit.cityEntrance, Pick(kit.cityRooms, GameRules.CityRandomRooms), kit.cityBossRoom);
            AddStep(kit.bubbleRoom, Zone.Bolha, GameRules.BubbleName);
            AddZone(Zone.Fossa, GameRules.AbyssName, kit.abyssEntrance, Pick(kit.abyssRooms, GameRules.AbyssRandomRooms), kit.abyssBossRoom);
        }

        static List<TextAsset> Pick(TextAsset[] bank, int count)
        {
            var shuffled = new List<TextAsset>(bank ?? new TextAsset[0]);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            var picked = shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
            // Ordenação estável: salas do mesmo nível mantêm a ordem sorteada.
            var levels = new Dictionary<TextAsset, int>();
            foreach (var room in picked) levels[room] = Room.LevelOf(room);
            var ordered = new List<TextAsset>();
            for (int level = 0; level <= 9; level++)
                foreach (var room in picked)
                    if (Mathf.Clamp(levels[room], 0, 9) == level) ordered.Add(room);
            return ordered;
        }

        void AddZone(Zone zone, string name, TextAsset entrance, List<TextAsset> middle, TextAsset last)
        {
            AddStep(entrance, zone, name);
            foreach (var room in middle) AddStep(room, zone, name);
            AddStep(last, zone, name);
        }

        void AddStep(TextAsset room, Zone zone, string name)
        {
            if (room == null) return;   // zona ainda sem salas: a run pula
            plan.Add(new RunStep { room = room, zone = zone, zoneName = name });
            rooms.Add(room);
        }

        // Número da sala dentro da zona atual e total da zona, para o título.
        (int, int) ZoneProgress(int index)
        {
            int first = index, last = index;
            while (first > 0 && plan[first - 1].zone == plan[index].zone) first--;
            while (last + 1 < plan.Count && plan[last + 1].zone == plan[index].zone) last++;
            return (index - first + 1, last - first + 1);
        }

        IEnumerator EnterRoom(int index)
        {
            Transitioning = true;
            if (index > 0)
            {
                Sound.Play(Sfx.Door);
                yield return Hud.Fade(0f, 1f, .25f);
            }

            RoomIndex = index;
            RunStep step = plan[index];
            BuildRoom(step.room, step.zone);
            if (Casco == null) SpawnRunCasco();
            else Casco.TeleportTo(Room.StartPoint);
            Casco.SetAir(Casco.MaxAir);
            UpdateCascoLight();
            CameraRig.SnapToTarget();
            var (number, total) = ZoneProgress(index);
            Hud.SetTitle(step.zone == Zone.Bolha
                ? GameRules.BubbleName
                : step.zoneName + " · " + Room.RoomName + " · Sala " + number + "/" + total);
            Sound.PlayMusic(step.zone);

            Progress.Data.bestRoom = Mathf.Max(Progress.Data.bestRoom, index + 1);
            int zoneNumber = step.zone == Zone.Fossa ? 3 : step.zone == Zone.Cidade ? 2 : 1;
            Progress.Data.bestZone = Mathf.Max(Progress.Data.bestZone, zoneNumber);
            Progress.Save();

            // Cutscene 3: a primeira vez na Fossa Abissal (GDD p. 9).
            bool firstAbyss = step.zone == Zone.Fossa && !Progress.Data.abyssCutsceneSeen;
            if (firstAbyss)
            {
                Progress.Data.abyssCutsceneSeen = true;
                Progress.Save();
                yield return CutscenePlayer.Play(CutsceneId.FundoDoMar);
            }
            yield return Hud.Fade(1f, 0f, .3f);
            Transitioning = false;
            if (index == 0 || plan[index - 1].zone != step.zone)
                Hud.ShowZoneBanner(step.zoneName, ZoneSubtitle(step.zone), Hud.ZoneColor(step.zone));
        }

        static string ZoneSubtitle(Zone zone) => zone switch
        {
            Zone.Recife => "a maré começa a subir",
            Zone.Bolha => "aqui a água não sobe · respire",
            Zone.Cidade => "a maré sobe mais rápido aqui",
            Zone.Fossa => "quase não há luz · a água sobe depressa",
            _ => "",
        };

        // No escuro, Casco carrega a própria luz (maior com a Lâmpada).
        void UpdateCascoLight()
        {
            if (cascoLight == null) cascoLight = Casco.gameObject.AddComponent<LightSource>();
            cascoLight.radius = Casco.LightRadius;
        }

        void SpawnRunCasco()
        {
            var data = Progress.Data;
            ShellType start = Progress.IsUnlocked(data.startShell) ? data.startShell : ShellType.Comum;
            SpawnCasco(Room.StartPoint, start);
            Casco.MaxAir = Progress.MaxAir;
            Casco.NakedGuards = data.nakedGuardLevel;
            Casco.SetAlgae(data.algaeLevel);
        }

        void Update()
        {
            if (cascoLight != null && Casco != null) cascoLight.radius = Casco.LightRadius;
            if (Controls.Pressed(GameAction.Pause) && !Ending && !Transitioning && !Hud.PanelOpen && !CutscenePlayer.Playing)
            {
                if (Paused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            Paused = true;
            Time.timeScale = 0f;
            Hud.ShowPause(Resume, GiveUp);
        }

        public void Resume()
        {
            Paused = false;
            Time.timeScale = 1f;
            Hud.HidePause();
        }

        // Desistir conta como morte, para não virar atalho que guarda todas as pérolas.
        void GiveUp()
        {
            Resume();
            Casco.Die("Você desistiu e nadou de volta para a vila.");
        }

        public override void AddPearls(int amount)
        {
            base.AddPearls(amount);
            PearlsCollected += amount;
        }

        public override void OnExitReached()
        {
            if (Transitioning || Ending || Casco == null || Casco.Dead || !ExitOpen) return;
            if (RoomIndex + 1 < plan.Count) StartCoroutine(EnterRoom(RoomIndex + 1));
            else StartCoroutine(Victory());
        }

        public override void OnCascoDied(string reason)
        {
            if (!Ending) StartCoroutine(Death(reason));
        }

        // Concha nova vestida: a primeira vez toca a cutscene 2 (GDD p. 9). Achar Lata ou Lâmpada libera a
        // forja delas com a Dona Lagosta.
        public override void OnShellEquipped(ShellType type)
        {
            if (type == ShellType.Lata) Progress.Data.lataSeen = true;
            if (type == ShellType.Lampada) Progress.Data.lampadaSeen = true;
            UpdateCascoLight();
            bool special = type != ShellType.Comum && type != ShellType.Nenhuma;
            if (special && !Progress.Data.firstShellCutsceneSeen)
            {
                Progress.Data.firstShellCutsceneSeen = true;
                StartCoroutine(CutscenePlayer.Play(CutsceneId.PrimeiraConcha));
            }
            Progress.Save();
        }

        // Vencer o Colosso só abre a saída; vencer o Polvo é a vitória da campanha (GDD p. 4).
        public override void OnBossDefeated(Boss boss)
        {
            if (CurrentZone == Zone.Cidade) Progress.Data.colossoDefeated = true;
            Progress.Save();
            Sound.Play(Sfx.Victory);
            CameraRig.Shake(.3f, .5f);
            if (CurrentZone == Zone.Fossa) StartCoroutine(VictoryAfterTide());
        }

        // Deixa a água recuar e a Pérola aparecer antes da cutscene.
        IEnumerator VictoryAfterTide()
        {
            yield return new WaitForSeconds(3f);
            if (!Ending && Casco != null && !Casco.Dead) yield return Victory();
        }

        // Morrer: metade das pérolas não gastas volta para a vila (GDD p. 4 e 7).
        IEnumerator Death(string reason)
        {
            Ending = true;
            int total = Progress.Data.pearls + PearlsCollected;
            int kept = total / 2;
            Progress.Data.pearls = kept;
            PearlsCollected = 0;   // o HUD passa a mostrar só o que voltou para a vila
            Progress.Save();
            yield return new WaitForSecondsRealtime(1f);
            Hud.ShowMessage("Casco foi levado pela maré",
                reason + "\n\nVocê volta à vila com " + kept + " de " + total + " pérolas.",
                "E: voltar à vila");
            yield return WaitForConfirm(5f);
            yield return BackToVillage();
        }

        // Vitória: o Polvo caiu e a Pérola Lunar volta para casa (GDD p. 4 e 9). Guarda todas as pérolas.
        IEnumerator Victory()
        {
            Ending = true;
            Sound.Play(Sfx.Victory);
            int collected = PearlsCollected;
            Progress.Data.pearls += collected;
            Progress.Data.victories++;
            PearlsCollected = 0;
            Progress.Save();
            Sound.StopMusic();
            yield return Hud.Fade(0f, 1f, .6f);
            yield return CutscenePlayer.Play(CutsceneId.MareBaixa);
            yield return CreditsScreen.Play();
            Hud.SetFade(0f);
            Hud.ShowMessage("A Pérola Lunar voltou para casa!",
                "A maré baixou e a vila está salva. Casco trouxe " + collected + " pérolas.\n\n" +
                "Vitórias: " + Progress.Data.victories + ". A Fossa continua lá embaixo, se quiser descer de novo.",
                "E: voltar à vila");
            yield return WaitForConfirm(12f);
            yield return BackToVillage();
        }

        // Volta rápida (menos de 5 s na morte, GDD p. 6): sai sozinho ou com E.
        IEnumerator WaitForConfirm(float timeout)
        {
            float shown = Time.unscaledTime;
            while (Time.unscaledTime - shown < timeout)
            {
                bool pressed = Controls.Pressed(GameAction.Interact) || Controls.Pressed(GameAction.Jump)
                               || Input.GetKeyDown(KeyCode.Return);
                if (pressed && Time.unscaledTime - shown > .5f) break;
                yield return null;
            }
        }

        IEnumerator BackToVillage()
        {
            yield return Hud.Fade(0f, 1f, .3f);
            SceneManager.LoadScene(GameRules.VillageScene);
        }

        // ---------------------------------------------------------------- Teste automático

        public void JumpToStep(int index)
        {
            if (!Transitioning && !Ending) StartCoroutine(EnterRoom(Mathf.Clamp(index, 0, plan.Count - 1)));
        }
    }
}
