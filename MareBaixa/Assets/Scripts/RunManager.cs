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
    // bancos de descanso, morte, cutscenes e vitória.
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
        public bool Resting { get; private set; }
        public bool HasCheckpoint => Progress.HasCheckpoint;

        LightSource cascoLight;

        public override bool InputLocked =>
            base.InputLocked || Transitioning || Ending || Resting || Hud.PanelOpen || CutscenePlayer.Playing;
        public override int PearlsShown => Progress.Data.pearls + PearlsCollected;
        public override bool ExitOpen => Room == null || Room.Boss == null || Room.Boss.Defeated;

        void Start()
        {
            bool resume = TryRestorePlan();
            if (!resume)
            {
                Progress.Data.checkpoint = null;
                BuildRunOrder();
                Progress.Data.runs++;
            }
            Progress.Save();
            Hud.SetFade(1f);
            StartCoroutine(EnterRoom(resume ? Progress.Data.checkpoint.roomIndex : 0, resume));
        }

        bool TryRestorePlan()
        {
            var saved = Progress.Data.checkpoint;
            if (saved == null || !saved.Valid) return false;
            var bank = new Dictionary<string, (TextAsset room, Zone zone)>();
            void Add(TextAsset room, Zone zone) { if (room != null) bank[room.name] = (room, zone); }
            Add(kit.reefEntrance, Zone.Recife); Add(kit.reefShellRoom, Zone.Recife);
            Add(kit.cityEntrance, Zone.Cidade); Add(kit.cityBossRoom, Zone.Cidade);
            Add(kit.abyssEntrance, Zone.Fossa); Add(kit.abyssBossRoom, Zone.Fossa);
            Add(kit.bubbleRoom, Zone.Bolha);
            foreach (var room in kit.reefRooms) Add(room, Zone.Recife);
            foreach (var room in kit.cityRooms) Add(room, Zone.Cidade);
            foreach (var room in kit.abyssRooms) Add(room, Zone.Fossa);
            // Valida tudo antes de preencher: um save antigo com sala removida não deixa meia run.
            for (int i = 0; i < saved.rooms.Length; i++)
                if (string.IsNullOrEmpty(saved.rooms[i]) || !bank.TryGetValue(saved.rooms[i], out var item)
                    || item.zone != saved.zones[i]) return false;
            for (int i = 0; i < saved.rooms.Length; i++)
                AddStep(bank[saved.rooms[i]].room, saved.zones[i], ZoneName(saved.zones[i]));
            return true;
        }

        static string ZoneName(Zone zone) => zone switch
        {
            Zone.Recife => GameRules.ReefName,
            Zone.Cidade => GameRules.CityName,
            Zone.Fossa => GameRules.AbyssName,
            _ => GameRules.BubbleName,
        };

        // Cada zona: entrada sempre igual, salas sorteadas do banco em ordem de dificuldade (@nivel) e
        // a sala fixa do fim (concha garantida no Recife, chefe na Cidade e na Fossa). GDD p. 4 e 5.
        void BuildRunOrder()
        {
            var reef = Pick(kit.reefRooms, GameRules.ReefRandomRooms);
            reef.Insert(Random.Range(0, reef.Count + 1), kit.reefShellRoom);
            AddZone(Zone.Recife, GameRules.ReefName, kit.reefEntrance, reef, null, true);
            AddStep(kit.bubbleRoom, Zone.Bolha, GameRules.BubbleName);
            AddZone(Zone.Cidade, GameRules.CityName, kit.cityEntrance, Pick(kit.cityRooms, GameRules.CityRandomRooms), kit.cityBossRoom, true);
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

        void AddZone(Zone zone, string name, TextAsset entrance, List<TextAsset> middle, TextAsset last, bool midwayRest = false)
        {
            AddStep(entrance, zone, name);
            for (int i = 0; i < middle.Count; i++)
            {
                AddStep(middle[i], zone, name);
                if (midwayRest && i == 1) AddStep(kit.bubbleRoom, Zone.Bolha, GameRules.BubbleName);
            }
            if (last != null) AddStep(kit.bubbleRoom, Zone.Bolha, GameRules.BubbleName);
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
            int number = 0, total = 0;
            for (int i = 0; i < plan.Count; i++)
                if (plan[i].zone == plan[index].zone) { total++; if (i <= index) number++; }
            return (number, total);
        }

        IEnumerator EnterRoom(int index, bool restore = false)
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
            if (restore && Casco != null)
            {
                Casco.gameObject.SetActive(false);
                Destroy(Casco.gameObject);
                Casco = null;
                cascoLight = null;
            }
            if (Casco == null) SpawnRunCasco();
            else Casco.TeleportTo(Room.StartPoint);
            if (restore) RestoreCasco();
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
            Zone.Bolha => "descanse no banco para salvar a jornada",
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

        void RestoreCasco()
        {
            var saved = Progress.Data.checkpoint;
            // O banco atual determina uma posição segura mesmo se o desenho da sala mudar.
            var bench = Room.GetComponentInChildren<BancoDescanso>();
            Vector2 feet = bench != null ? bench.Feet : Room.StartPoint;
            Casco.TeleportTo(feet);
            Casco.Equip(saved.shell, -1);
            Casco.EquipWeapon(saved.weapon);
            Casco.SetAlgae(saved.algae);
            Casco.NakedGuards = Mathf.Clamp(saved.nakedGuards, 0, Progress.Data.nakedGuardLevel);
            Casco.MakeInvulnerable(1.5f);
        }

        public void RestAt(BancoDescanso bench)
        {
            if (bench == null || Casco == null || Casco.Dead || InputLocked || CurrentZone != Zone.Bolha) return;
            StartCoroutine(Rest(bench));
        }

        IEnumerator Rest(BancoDescanso bench)
        {
            Resting = true;
            Vector2 feet = bench.Feet;
            Casco.RestOnBench(feet, true);
            Casco.Equip(Casco.Shell == ShellType.Nenhuma ? ShellType.Comum : Casco.Shell, -1);
            Casco.SetAlgae(GameRules.MaxAlgae);
            Casco.NakedGuards = Progress.Data.nakedGuardLevel;
            Casco.RefillAir();
            var saved = new RunCheckpoint
            {
                rooms = new string[plan.Count], zones = new Zone[plan.Count], roomIndex = RoomIndex,
                x = feet.x, y = feet.y, shell = Casco.Shell, weapon = Casco.Weapon,
                algae = Casco.Algae, nakedGuards = Casco.NakedGuards,
            };
            for (int i = 0; i < plan.Count; i++) { saved.rooms[i] = plan[i].room.name; saved.zones[i] = plan[i].zone; }
            Progress.Data.pearls += PearlsCollected;
            PearlsCollected = 0;
            Progress.Data.checkpoint = saved;
            Progress.Save();
            Sound.Play(Sfx.Equip);
            Effects.Spark(Casco.Center);
            Hud.Toast("Banco salvo · concha, ar e algas recuperados · pérolas guardadas", 4f);
            yield return new WaitForSeconds(.95f);
            Casco.RestOnBench(feet, false);
            Resting = false;
        }

        void Update()
        {
            if (cascoLight != null && Casco != null) cascoLight.radius = Casco.LightRadius;
            if (Controls.Pressed(GameAction.Pause) && !Ending && !Transitioning && !Resting && !Hud.PanelOpen && !CutscenePlayer.Playing)
            {
                if (Paused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            Paused = true;
            Time.timeScale = 0f;
            Hud.ShowPause(Resume, GiveUp, ReturnToMenu);
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
            Progress.Data.checkpoint = null;
            Casco.Die("Você desistiu e nadou de volta para a vila.");
        }

        public void ReturnToMenu()
        {
            if (Ending || Transitioning || Resting) return;
            Resume();
            Ending = true;
            StartCoroutine(LeaveToMenu());
        }

        IEnumerator LeaveToMenu()
        {
            yield return Hud.Fade(0f, 1f, .3f);
            SceneManager.LoadScene(GameRules.MenuScene);
        }

        public override void AddPearls(int amount)
        {
            base.AddPearls(amount);
            PearlsCollected += amount;
        }

        public override void OnExitReached()
        {
            if (InputLocked || Casco == null || Casco.Dead || !ExitOpen) return;
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

        // Com banco: recomeça no abrigo com o equipamento salvo. Só perde as pérolas ainda não
        // guardadas. Sem banco (ou desistindo), mantém a antiga volta à vila com metade das pérolas.
        IEnumerator Death(string reason)
        {
            Ending = true;
            if (HasCheckpoint)
            {
                int lost = PearlsCollected;
                PearlsCollected = 0;
                Progress.Save();
                yield return new WaitForSecondsRealtime(1f);
                Hud.ShowMessage("De volta ao último banco",
                    reason + "\n\nEquipamento e pérolas guardados no banco estão a salvo.\n" +
                    "Pérolas perdidas desde o descanso: " + lost + ".",
                    Controls.FormatHints("E / {padConfirm}: continuar do banco"));
                yield return WaitForConfirm(3f);
                Hud.HideMessage();
                yield return EnterRoom(Progress.Data.checkpoint.roomIndex, true);
                Ending = false;
                Hud.Toast("Você voltou ao banco · a jornada continua");
                yield break;
            }
            int total = Progress.Data.pearls + PearlsCollected;
            int kept = total / 2;
            Progress.Data.pearls = kept;
            PearlsCollected = 0;   // o HUD passa a mostrar só o que voltou para a vila
            Progress.Save();
            yield return new WaitForSecondsRealtime(1f);
            Hud.ShowMessage("Casco foi levado pela maré",
                reason + "\n\nVocê volta à vila com " + kept + " de " + total + " pérolas.",
                Controls.FormatHints("E / {padConfirm}: voltar à vila"));
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
            Progress.Data.checkpoint = null;
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
