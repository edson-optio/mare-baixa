using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MareBaixa
{
    // Executado somente com -selfTest. Joga sozinho o loop principal no app compilado (menu, vila,
    // loja, run, combate, conchas, maré e ar, morte, a campanha inteira com as três zonas, os chefes e
    // a vitória) e salva um relatório JSON com capturas de tela de cada sala e de cada cutscene.
    // Usa outro save para não mexer no do jogador.
    [DefaultExecutionOrder(-1000)]
    public class GameVerification : MonoBehaviour
    {
        [Serializable] class Report { public bool passed; public List<string> checks = new List<string>(); public List<string> failures = new List<string>(); }
        readonly Report report = new Report();
        static bool running;
        bool finished;
        string output;

        void Awake()
        {
            if (running || Array.IndexOf(Environment.GetCommandLineArgs(), "-selfTest") < 0) { Destroy(gameObject); return; }
            running = true;
            DontDestroyOnLoad(gameObject);
            Progress.Key = "mare-baixa-teste";
            Progress.Reset();
        }

        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Verificacao"));
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-testOutput") output = args[i + 1];
            Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            yield return new WaitForSeconds(1f);

            StartCoroutine(WatchCutscenes());
            yield return Step(TestMenu());
            yield return Step(TestVillage());
            yield return Step(TestRun());
            yield return Step(TestAllRooms());
            Finish();
        }

        // Uma etapa que trava ou lança exceção não derruba as outras verificações.
        IEnumerator Step(IEnumerator routine)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext()) yield break;
                    current = routine.Current;
                }
                catch (Exception error)
                {
                    report.failures.Add("Exceção: " + error.Message);
                    Controls.ClearSimulation();
                    yield break;
                }
                yield return current;
            }
        }

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup > 600f)
            {
                report.failures.Add("Tempo esgotado: o teste não terminou em 10 minutos");
                Finish();
            }
        }

        void Finish()
        {
            finished = true;
            report.passed = report.failures.Count == 0;
            File.WriteAllText(Path.Combine(output, "resultado-testes.json"), JsonUtility.ToJson(report, true));
            Debug.Log("MARE_TESTS: " + (report.passed ? "PASSED" : "FAILED") + " / " + report.checks.Count + " checks, " + report.failures.Count + " falhas");
            Application.logMessageReceived -= OnLog;
            Application.Quit(report.passed ? 0 : 1);
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                report.failures.Add("Erro no log: " + message);
        }

        void Check(bool ok, string description)
        {
            (ok ? report.checks : report.failures).Add(description);
            Debug.Log((ok ? "OK: " : "FALHOU: ") + description);
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
            yield return null;
            yield return null;
        }

        // Cutscenes e créditos: fotografa cada um e pula, como um jogador apressado faria.
        readonly List<string> cutscenesSeen = new List<string>();

        IEnumerator WatchCutscenes()
        {
            var creditsPlaying = typeof(CreditsScreen).GetProperty("Playing");
            var creditsSkip = typeof(CreditsScreen).GetMethod("Skip");
            while (!finished)
            {
                bool credits = creditsPlaying != null && (bool)creditsPlaying.GetValue(null);
                if (CutscenePlayer.Playing || credits)
                {
                    yield return new WaitForSecondsRealtime(1.2f);
                    string name = (credits ? "creditos" : "cutscene") + "-" + (cutscenesSeen.Count + 1);
                    cutscenesSeen.Add(name);
                    yield return Capture(name);
                    if (CutscenePlayer.Playing) CutscenePlayer.SkipCurrent();
                    else creditsSkip?.Invoke(null, null);
                    float start = Time.realtimeSinceStartup;
                    while ((CutscenePlayer.Playing || (creditsPlaying != null && (bool)creditsPlaying.GetValue(null)))
                           && Time.realtimeSinceStartup - start < 3f) yield return null;
                }
                yield return null;
            }
        }

        static IEnumerator WaitCutscene()
        {
            yield return null;
            float start = Time.realtimeSinceStartup;
            while (CutscenePlayer.Playing && Time.realtimeSinceStartup - start < 8f) yield return null;
            yield return null;
        }

        static IEnumerator WaitScene(string name)
        {
            float start = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup - start < 10f) yield return null;
            yield return new WaitForSeconds(.8f);
        }

        static IEnumerator Press(GameAction action)
        {
            Controls.Press(action);
            yield return null;
            yield return null;
        }

        static Casco Casco => Director.Instance != null ? Director.Instance.Casco : null;
        static Hud Hud => Director.Instance.Hud;

        // ------------------------------------------------------------------ Menu

        IEnumerator TestMenu()
        {
            var menu = FindAnyObjectByType<MenuScreen>();
            Check(SceneManager.GetActiveScene().name == GameRules.MenuScene && menu != null, "Jogo abre na tela inicial");
            Check(menu != null && menu.TitleText.ToUpperInvariant().Contains("MARÉ BAIXA"), "Tela inicial mostra o título");
            yield return Capture("01-menu");
            Controls.Simulated = true;
            menu.PlayButton.onClick.Invoke();
            yield return WaitScene(GameRules.VillageScene);
            Check(SceneManager.GetActiveScene().name == GameRules.VillageScene, "Botão Jogar leva à Vila da Praia");
        }

        // ------------------------------------------------------------------ Vila

        IEnumerator TestVillage()
        {
            var casco = Casco;
            Check(Director.Instance is VilaManager && casco != null && casco.Grounded, "Casco nasce na vila, em pé no chão" + Pos(casco));
            Check(FindObjectsByType<Npc>(FindObjectsSortMode.None).Length == 4, "Os quatro moradores estão na vila");
            Check(FindAnyObjectByType<Descent>() != null, "A descida para o Recife existe");
            yield return WaitCutscene();
            yield return new WaitForSeconds(.6f);
            Check(cutscenesSeen.Count >= 1, "Primeira visita toca a cutscene 1 (A Pérola Roubada)");
            Check(Hud.PanelOpen && Hud.Dialog != null, "Depois da cutscene, a Vovó Búzia conta a história");
            yield return Capture("02-vila-historia");
            for (int i = 0; i < 40 && Hud.PanelOpen; i++) yield return Press(GameAction.Interact);
            Check(!Hud.PanelOpen && Progress.Data.introSeen, "História termina apertando E e fica marcada como vista");

            // Andar e pular
            float x0 = casco.transform.position.x, y0 = casco.transform.position.y;
            Controls.SimulatedMove = 1f;
            yield return new WaitForSeconds(.6f);
            Controls.SimulatedMove = 0f;
            Check(casco.transform.position.x - x0 > 2f, "Casco anda para a direita");
            yield return new WaitForSeconds(.4f);
            y0 = casco.transform.position.y;
            float top = y0;
            Controls.Hold(GameAction.Jump, true);
            Controls.Press(GameAction.Jump);
            for (float t = 0; t < 1f; t += Time.deltaTime) { top = Mathf.Max(top, casco.transform.position.y); yield return null; }
            Controls.Hold(GameAction.Jump, false);
            Check(top - y0 > 1.8f && top - y0 < 3.2f, "Pulo alto sobe entre 2 e 3 blocos (" + (top - y0).ToString("F2") + ")");
            yield return Capture("03-vila");

            // Loja da Ostrácia
            Progress.Data.pearls = 20;
            yield return TalkTo(NpcKind.Ostracia);
            Check(Hud.PanelOpen && Hud.Shop != null, "Ostrácia abre a loja de melhorias");
            yield return Capture("04-loja-ostracia");
            Hud.Shop.Select(0);
            Hud.Shop.Confirm();
            yield return null;
            Check(Progress.Data.airLevel == 1 && Progress.Data.pearls == 17, "Comprar Pulmão de Peixe gasta 3 pérolas e dá +2 s de ar");
            yield return Press(GameAction.Pause);
            Check(!Hud.PanelOpen, "Esc fecha a loja");

            // Forja da Dona Lagosta
            yield return TalkTo(NpcKind.Lagosta);
            Hud.Shop.Select(1);
            Hud.Shop.Confirm();
            yield return null;
            Check(Progress.Data.tampinhaUnlocked && Progress.Data.startShell == ShellType.Tampinha && Progress.Data.pearls == 11
                  && casco.Shell == ShellType.Tampinha, "Dona Lagosta forja a Tampinha (6 pérolas) e ela vira a concha inicial");
            yield return Capture("05-forja-lagosta");
            yield return Press(GameAction.Pause);

            // Descer
            var descent = FindAnyObjectByType<Descent>();
            casco.TeleportTo(descent.transform.position);
            yield return new WaitForSeconds(.3f);
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.RunScene);
            Check(SceneManager.GetActiveScene().name == GameRules.RunScene, "A descida leva à run, no Recife Raso");
        }

        IEnumerator TalkTo(NpcKind kind)
        {
            foreach (var npc in FindObjectsByType<Npc>(FindObjectsSortMode.None))
            {
                if (npc.kind != kind) continue;
                Casco.TeleportTo(npc.transform.position + Vector3.left * .8f);
                yield return new WaitForSeconds(.3f);
                yield return Press(GameAction.Interact);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ Run

        IEnumerator TestRun()
        {
            var run = Director.Instance as RunManager;
            var casco = Casco;
            var kit = Kit.Current;
            CheckPlan(run, kit);
            Check(casco.Shell == ShellType.Tampinha && Mathf.Approximately(casco.MaxAir, 10f),
                "Run começa com a concha escolhida e o ar comprado (10 s)");
            yield return new WaitForSeconds(.6f);
            Check(casco.Grounded, "Casco pousa no chão da entrada do Recife" + Pos(casco));
            yield return Capture("06-recife-entrada");

            float level = Mare.Current.Level;
            yield return new WaitForSeconds(1f);
            Check(Mare.Current.Level > level, "A maré sobe sozinha (" + level.ToString("F2") + " → " + Mare.Current.Level.ToString("F2") + ")");

            // Pulo duplo da Tampinha
            casco.MakeInvulnerable(60f);
            float y0 = casco.transform.position.y, top = y0;
            Controls.Hold(GameAction.Jump, true);
            Controls.Press(GameAction.Jump);
            for (float t = 0; t < .3f; t += Time.deltaTime) yield return null;
            Controls.Press(GameAction.Jump);
            for (float t = 0; t < 1f; t += Time.deltaTime) { top = Mathf.Max(top, casco.transform.position.y); yield return null; }
            Controls.Hold(GameAction.Jump, false);
            Check(top - y0 > 3.3f, "Tampinha dá pulo duplo (subiu " + (top - y0).ToString("F2") + " blocos)");
            yield return new WaitForSeconds(1f);

            // Baiacu: um golpe faz inchar e explodir em espinhos
            casco.ClearInvulnerability();
            int hpBefore = casco.ShellHp;
            int pearlsBefore = run.PearlsCollected;
            var puffer = Instantiate(kit.pufferfish, casco.Center + new Vector2(1f, 0f), Quaternion.identity, run.Room.transform);
            yield return new WaitForFixedUpdate();
            yield return Press(GameAction.Attack);
            // Espera o primeiro golpe do baiacu e protege Casco, para a Tampinha (2 de resistência)
            // não quebrar e matar Casco no meio do teste.
            for (float t = 0; t < 1.5f && casco.ShellHp >= hpBefore; t += Time.deltaTime) yield return null;
            casco.MakeInvulnerable(60f);
            bool spikes = FindObjectsByType<Spike>(FindObjectsSortMode.None).Length > 0;
            for (float t = 0; t < 2f && !spikes; t += Time.deltaTime)
            {
                spikes = FindObjectsByType<Spike>(FindObjectsSortMode.None).Length > 0;
                yield return null;
            }
            Check(puffer != null && spikes, "Baiacu apanha, incha e solta espinhos");
            yield return Capture("07-baiacu");
            Check(casco.ShellHp < hpBefore && !casco.Dead, "Golpe tira resistência da concha (" + hpBefore + " → " + casco.ShellHp + ")");
            yield return new WaitForSeconds(.5f);
            for (float t = 0; t < 6f && puffer != null; t += .4f)
            {
                casco.TeleportTo((Vector2)puffer.transform.position + new Vector2(-.9f, -.35f));
                yield return Press(GameAction.Attack);
                yield return new WaitForSeconds(.4f);
            }
            Check(puffer == null, "Baiacu é derrotado a beliscões");
            yield return new WaitForSeconds(1.5f);
            Check(run.PearlsCollected > pearlsBefore, "Inimigo derrotado solta pérolas que Casco recolhe (" + run.PearlsCollected + ")");

            // Água-viva
            var jelly = Instantiate(kit.jellyfish, casco.Center + new Vector2(1f, .2f), Quaternion.identity, run.Room.transform);
            for (float t = 0; t < 8f && jelly != null; t += .4f)
            {
                casco.TeleportTo((Vector2)jelly.transform.position + new Vector2(-.9f, -.35f));
                yield return Press(GameAction.Attack);
                yield return new WaitForSeconds(.4f);
            }
            Check(jelly == null, "Água-viva é derrotada atacando entre os choques");

            // Troca de concha e habilidades
            ShellPickup.Spawn(ShellType.Espiral, -1, casco.transform.position, run.Room.transform);
            yield return null;
            int seenBefore = cutscenesSeen.Count;
            yield return Press(GameAction.Interact);
            yield return WaitCutscene();
            Check(cutscenesSeen.Count > seenBefore, "Primeira concha nova toca a cutscene 2 (A Primeira Concha)");
            bool dropped = false;
            foreach (var pickup in FindObjectsByType<ShellPickup>(FindObjectsSortMode.None))
                dropped |= pickup.type == ShellType.Tampinha;
            Check(casco.Shell == ShellType.Espiral && casco.ShellHp == 4 && dropped,
                "E troca pela Concha Espiral e a Tampinha fica no chão");
            yield return Press(GameAction.Ability);
            Check(casco.Spinning, "L com a Espiral faz o ataque giratório");
            yield return new WaitForSeconds(.5f);
            yield return Press(GameAction.Dodge);
            Check(casco.Dodging && casco.Invulnerable, "K rola dentro da concha e fica invulnerável");
            yield return new WaitForSeconds(.6f);

            yield return TestNewShells(run, casco);

            casco.Equip(ShellType.Espiral, 2);
            casco.AddAlga();
            yield return Press(GameAction.Eat);
            Check(casco.ShellHp == 3 && casco.Algae == 0, "Q come a alga e recupera 1 de resistência");

            // Concha quebrando
            casco.ClearInvulnerability();
            casco.SetShellHp(1);
            casco.Hurt(casco.transform.position + Vector3.right);
            yield return new WaitForSeconds(.1f);
            Check(casco.Shell == ShellType.Nenhuma && !casco.Dead, "Último golpe quebra a concha e Casco fica sem ela");
            yield return Capture("08-sem-concha");
            yield return new WaitForSeconds(1.2f);

            // Maré e ar
            int total = Progress.Data.pearls + run.PearlsCollected;
            Mare.Current.SetLevel(run.Room.Height + 1f);
            yield return new WaitForSeconds(.4f);
            float air = casco.Air;
            yield return new WaitForSeconds(.5f);
            Check(casco.HeadUnderwater && casco.Air < air && Director.Instance.Sound.Muffled,
                "Debaixo d'água o ar diminui e o som fica abafado (cabeça " + casco.HeadUnderwater + ", ar "
                + air.ToString("F2") + " → " + casco.Air.ToString("F2") + ", abafado " + Director.Instance.Sound.Muffled + ")");
            casco.SetAir(.3f);
            yield return new WaitForSeconds(1.8f);
            Check(casco.Dead && Hud.MessageVisible, "Sem ar Casco morre e aparece a tela de derrota");
            yield return Capture("09-morte");
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.VillageScene);
            Check(SceneManager.GetActiveScene().name == GameRules.VillageScene && Progress.Data.pearls == total / 2,
                "Depois de morrer volta à vila com metade das pérolas (" + total + " → " + Progress.Data.pearls + ")");
        }

        void CheckPlan(RunManager run, Kit kit)
        {
            var plan = run.Plan;
            bool shape = plan.Count > 0 && plan[0].room == kit.reefEntrance && ((IList<TextAsset>)run.Rooms).Contains(kit.reefShellRoom);
            int reef = 0, bubbles = 0, city = 0, abyss = 0;
            foreach (var step in plan)
            {
                if (step.zone == Zone.Recife) reef++;
                if (step.zone == Zone.Bolha) bubbles++;
                if (step.zone == Zone.Cidade) city++;
                if (step.zone == Zone.Fossa) abyss++;
            }
            Check(shape && reef == 2 + GameRules.ReefRandomRooms,
                "Recife: entrada fixa + " + GameRules.ReefRandomRooms + " salas do banco + sala de concha garantida (" + reef + " salas)");
            Check(bubbles == 2 || kit.bubbleRoom == null, "Uma bolha de ar entre cada zona (" + bubbles + ")");
            Check(city == 0 || (city == 2 + Mathf.Min(GameRules.CityRandomRooms, kit.cityRooms.Length) && plan[plan.Count - 1].zone != Zone.Cidade),
                "Cidade Afundada: entrada + salas sorteadas + arena do Colosso (" + city + " salas)");
            Check(abyss == 0 || abyss == 2 + Mathf.Min(GameRules.AbyssRandomRooms, kit.abyssRooms.Length),
                "Fossa Abissal: entrada + salas sorteadas + arena do Polvo (" + abyss + " salas)");
            // Dentro de cada zona, as salas sorteadas vêm em ordem de @nivel.
            bool ordered = true;
            for (int i = 2; i < plan.Count - 1; i++)
            {
                bool middle = plan[i].zone == plan[i - 1].zone && plan[i].zone == plan[i + 1].zone
                              && plan[i].room != kit.reefShellRoom && plan[i - 1].room != kit.reefShellRoom
                              && plan[i - 1].room != kit.reefEntrance && plan[i - 1].room != kit.cityEntrance && plan[i - 1].room != kit.abyssEntrance;
                if (middle && Room.LevelOf(plan[i].room) < Room.LevelOf(plan[i - 1].room)) ordered = false;
            }
            Check(ordered, "Salas sorteadas de cada zona sobem de dificuldade (@nivel)");
        }

        // Lata (investida que atordoa), Lâmpada (clarão), o Siri levando a concha, a correnteza e a válvula.
        IEnumerator TestNewShells(RunManager run, Casco casco)
        {
            casco.MakeInvulnerable(60f);
            casco.Equip(ShellType.Lata, -1);
            Check(casco.ShellHp == 6 && Shells.Get(ShellType.Lata).SpeedFactor < 1f, "Lata de Refrigerante: 6 de resistência e mais lenta");
            yield return new WaitForSeconds(.3f);
            // Investe para os dois lados: um deles pode estar encostado numa parede.
            bool charged = false;
            float distance = 0f;
            foreach (float side in new[] { 1f, -1f })
            {
                Controls.SimulatedMove = side;
                yield return null;
                yield return null;
                float x0 = casco.transform.position.x;
                yield return Press(GameAction.Ability);
                charged |= casco.Charging;
                Controls.SimulatedMove = 0f;
                yield return new WaitForSeconds(.4f);
                distance = Mathf.Max(distance, Mathf.Abs(casco.transform.position.x - x0));
                yield return new WaitForSeconds(GameRules.ChargeCooldown);
            }
            Check(charged, "L com a Lata faz a investida");
            Check(distance > 1.5f, "A investida leva Casco para a frente (" + distance.ToString("F2") + ")");
            yield return Capture("08a-lata");

            casco.Equip(ShellType.Lampada, -1);
            Check(casco.LightRadius > GameRules.LightRadius, "Lâmpada Velha aumenta a luz de Casco");
            yield return new WaitForSeconds(.2f);
            yield return Press(GameAction.Ability);
            yield return Capture("08b-lampada");

            ShellType stolenType;
            casco.ClearInvulnerability();
            bool stolen = casco.StealShell(out stolenType, out int stolenHp);
            Check(stolen && stolenType == ShellType.Lampada && casco.Shell == ShellType.Nenhuma && !casco.Dead,
                "Roubar a concha (Siri) deixa Casco sem concha, sem quebrar");
            casco.MakeInvulnerable(60f);

            float level = Mare.Current.Level;
            Mare.Current.SetLevel(level + 3f);
            Mare.Current.Drain(2f, 1f);
            Check(Mathf.Abs(Mare.Current.Level - (level + 1f)) < .05f, "A válvula baixa a água de uma vez (Mare.Drain)");
            yield return new WaitForSeconds(.5f);
            Check(Mathf.Abs(Mare.Current.Level - (level + 1f)) < .05f, "Depois da válvula a água fica parada um tempo");
            Mare.Current.SetLevel(level);

            // Empurra para os dois lados: um deles pode estar encostado numa parede.
            float drift = 0f;
            foreach (float side in new[] { 1f, -1f })
            {
                float before = casco.transform.position.x;
                for (int i = 0; i < 20; i++) { casco.Push(new Vector2(side * 3.5f, 0f)); yield return new WaitForFixedUpdate(); }
                drift = Mathf.Max(drift, Mathf.Abs(casco.transform.position.x - before));
                yield return new WaitForSeconds(.2f);
            }
            Check(drift > .6f && drift < 3f, "Correnteza empurra Casco com deriva constante (" + drift.ToString("F2") + " em 0.4 s)");
            casco.Equip(ShellType.Espiral, -1);
        }

        // ------------------------------------------------------------------ Todas as salas

        IEnumerator TestAllRooms()
        {
            var descent = FindAnyObjectByType<Descent>();
            Casco.TeleportTo(descent.transform.position);
            yield return new WaitForSeconds(.3f);
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.RunScene);

            var run = Director.Instance as RunManager;
            int count = run.Rooms.Count;
            for (int i = 0; i < count; i++)
            {
                yield return WaitCutscene();
                var casco = Casco;
                casco.MakeInvulnerable(120f);
                yield return new WaitForSeconds(.3f);
                Check(run.RoomIndex == i && casco.Grounded && !casco.HeadUnderwater,
                    "Sala " + (i + 1) + " (" + run.Plan[i].zone + " · " + run.Room.RoomName + ") monta e Casco começa em pé, fora d'água" + Pos(casco));
                if (run.Plan[i].zone == Zone.Fossa)
                    Check(Escuridao.Current != null, "Sala da Fossa é escura");
                yield return Capture("sala-" + (i + 1) + "-" + run.Rooms[i].name);
                var boss = run.Room.Boss;
                if (boss != null)
                {
                    Check(!run.ExitOpen, "Arena " + run.Room.RoomName + ": a saída fica fechada com o chefe vivo");
                    // Chega perto para o chefe acordar e caber na captura.
                    casco.TeleportTo(new Vector2(Mathf.Max(2.5f, boss.transform.position.x - 6f), run.Room.StartPoint.y));
                    yield return new WaitForSeconds(2.5f);
                    Check(boss.GetComponentInChildren<SpriteRenderer>() != null && boss.GetComponentInChildren<SpriteRenderer>().isVisible,
                        "Arena " + run.Room.RoomName + ": o chefe aparece na tela");
                    yield return Capture("chefe-" + run.Rooms[i].name);
                    typeof(Boss).GetMethod("DefeatNow")?.Invoke(boss, null);
                    yield return new WaitForSeconds(2.5f);
                    Check(run.ExitOpen, "Arena " + run.Room.RoomName + ": vencer o chefe abre a saída");
                }
                var exit = FindAnyObjectByType<RoomExit>();
                Check(exit != null, "Sala " + (i + 1) + " tem saída");
                if (exit == null) yield break;
                casco.TeleportTo((Vector2)exit.transform.position + new Vector2(0f, .1f));
                float start = Time.realtimeSinceStartup;
                while (run.RoomIndex == i && !run.Ending && Time.realtimeSinceStartup - start < 4f) yield return null;
                yield return new WaitForSeconds(.8f);
            }
            for (float t = 0; t < 15f && !Hud.MessageVisible; t += Time.unscaledDeltaTime) yield return null;
            Check(run.Ending && Hud.MessageVisible && Progress.Data.victories == 1,
                "Depois da arena do Polvo, a campanha termina em vitória (cutscene 4 e créditos)");
            yield return Capture("10-vitoria");
            int expected = Progress.Data.pearls;
            yield return new WaitForSecondsRealtime(.7f);   // a mensagem ignora apertos nos primeiros 0,5 s
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.VillageScene);
            Check(SceneManager.GetActiveScene().name == GameRules.VillageScene && Progress.Data.pearls == expected,
                "Vencer guarda todas as pérolas e volta à vila");
            Check(cutscenesSeen.Count >= 4, "As quatro cutscenes tocaram (" + string.Join(", ", cutscenesSeen) + ")");
        }

        static string Pos(Casco casco) => casco == null ? "" : " [pos " + casco.transform.position.x.ToString("F2") + ", " + casco.transform.position.y.ToString("F2") + "]";
    }
}
