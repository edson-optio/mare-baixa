using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MareBaixa
{
    // Executado somente com -selfTest. Joga sozinho o loop principal no app compilado (menu, vila,
    // loja, run, combate, conchas, maré e ar, morte, salas sorteadas e fim da zona) e salva um
    // relatório JSON com capturas de tela de cada sala. Usa outro save para não mexer no do jogador.
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
            if (!finished && Time.realtimeSinceStartup > 300f)
            {
                report.failures.Add("Tempo esgotado: o teste não terminou em 5 minutos");
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
            Check(Hud.PanelOpen && Hud.Dialog != null, "Primeira visita abre a história com a Vovó Búzia");
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
            yield return WaitScene(GameRules.ReefScene);
            Check(SceneManager.GetActiveScene().name == GameRules.ReefScene, "A descida leva ao Recife Raso");
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
            Check(run != null && run.Rooms.Count == 2 + GameRules.ReefRandomRooms && run.Rooms[0] == kit.reefEntrance
                  && ((IList<TextAsset>)run.Rooms).Contains(kit.reefShellRoom),
                "Run sorteada: entrada fixa + " + GameRules.ReefRandomRooms + " salas do banco + sala de concha garantida");
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
            yield return Press(GameAction.Interact);
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

            casco.SetShellHp(2);
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

        // ------------------------------------------------------------------ Todas as salas

        IEnumerator TestAllRooms()
        {
            var descent = FindAnyObjectByType<Descent>();
            Casco.TeleportTo(descent.transform.position);
            yield return new WaitForSeconds(.3f);
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.ReefScene);

            var run = Director.Instance as RunManager;
            int count = run.Rooms.Count;
            for (int i = 0; i < count; i++)
            {
                var casco = Casco;
                casco.MakeInvulnerable(120f);
                Check(run.RoomIndex == i && casco.Grounded && !casco.HeadUnderwater,
                    "Sala " + (i + 1) + " (" + run.Room.RoomName + ") monta e Casco começa em pé, fora d'água" + Pos(casco));
                yield return Capture("sala-" + (i + 1) + "-" + run.Rooms[i].name);
                var exit = FindAnyObjectByType<RoomExit>();
                Check(exit != null, "Sala " + (i + 1) + " tem saída");
                if (exit == null) yield break;
                casco.TeleportTo((Vector2)exit.transform.position + new Vector2(0f, .1f));
                float start = Time.realtimeSinceStartup;
                while (run.RoomIndex == i && !run.Ending && Time.realtimeSinceStartup - start < 4f) yield return null;
                yield return new WaitForSeconds(.8f);
            }
            Check(run.Ending && Hud.MessageVisible, "Depois da última sala, o Recife Raso é concluído");
            yield return Capture("10-recife-concluido");
            int expected = Progress.Data.pearls;
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.VillageScene);
            Check(SceneManager.GetActiveScene().name == GameRules.VillageScene && Progress.Data.pearls == expected,
                "Concluir a zona guarda todas as pérolas e volta à vila");
        }

        static string Pos(Casco casco) => casco == null ? "" : " [pos " + casco.transform.position.x.ToString("F2") + ", " + casco.transform.position.y.ToString("F2") + "]";
    }
}
