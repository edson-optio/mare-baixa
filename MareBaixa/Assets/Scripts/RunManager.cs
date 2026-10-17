using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MareBaixa
{
    // Uma run no Recife Raso: sorteia as salas, troca de sala na saída, cuida da pausa, da morte
    // (volta à vila com metade das pérolas) e do fim da zona (GDD p. 4 e 7).
    public class RunManager : Director
    {
        readonly List<TextAsset> rooms = new List<TextAsset>();

        public IReadOnlyList<TextAsset> Rooms => rooms;
        public int RoomIndex { get; private set; }
        public int PearlsCollected { get; private set; }
        public bool Ending { get; private set; }
        public bool Transitioning { get; private set; }

        public override bool InputLocked => base.InputLocked || Transitioning || Ending || Hud.PanelOpen;
        public override int PearlsShown => Progress.Data.pearls + PearlsCollected;

        void Start()
        {
            BuildRunOrder();
            Progress.Data.runs++;
            Progress.Save();
            Hud.SetFade(1f);
            StartCoroutine(EnterRoom(0));
        }

        // Entrada sempre igual, depois as salas sorteadas do banco com a sala de concha
        // garantida numa posição aleatória (GDD p. 4 e 5).
        void BuildRunOrder()
        {
            var bank = new List<TextAsset>(kit.reefRooms);
            for (int i = bank.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bank[i], bank[j]) = (bank[j], bank[i]);
            }
            var picked = bank.GetRange(0, Mathf.Min(GameRules.ReefRandomRooms, bank.Count));
            picked.Insert(Random.Range(0, picked.Count + 1), kit.reefShellRoom);
            rooms.Add(kit.reefEntrance);
            rooms.AddRange(picked);
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
            BuildRoom(rooms[index], Zone.Recife);
            if (Casco == null) SpawnRunCasco();
            else Casco.TeleportTo(Room.StartPoint);
            Casco.SetAir(Casco.MaxAir);
            CameraRig.SnapToTarget();
            Hud.SetTitle(GameRules.ReefName + " · " + Room.RoomName + " · Sala " + (index + 1) + "/" + rooms.Count);

            Progress.Data.bestRoom = Mathf.Max(Progress.Data.bestRoom, index + 1);
            yield return Hud.Fade(1f, 0f, .3f);
            Transitioning = false;
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
            if (Controls.Pressed(GameAction.Pause) && !Ending && !Transitioning && !Hud.PanelOpen)
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
            if (Transitioning || Ending || Casco == null || Casco.Dead) return;
            if (RoomIndex + 1 < rooms.Count) StartCoroutine(EnterRoom(RoomIndex + 1));
            else StartCoroutine(ZoneComplete());
        }

        public override void OnCascoDied(string reason)
        {
            if (!Ending) StartCoroutine(Death(reason));
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

        // Fim do Recife: no protótipo é o fim da run, com todas as pérolas guardadas.
        IEnumerator ZoneComplete()
        {
            Ending = true;
            Sound.Play(Sfx.Buy);
            int collected = PearlsCollected;
            Progress.Data.pearls += collected;
            PearlsCollected = 0;
            Progress.Save();
            Hud.ShowMessage("Recife Raso concluído!",
                "Casco chegou à bolha de ar no fim do recife com " + collected + " pérolas.\n\n" +
                "Daqui para baixo começa a Cidade Afundada, que ainda está sendo construída.",
                "E: voltar à vila");
            yield return WaitForConfirm(8f);
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
    }
}
