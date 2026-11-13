using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MareBaixa
{
    public partial class GameVerification
    {
        IEnumerator TestEquipmentAndCheckpoints()
        {
            var legacy = JsonUtility.FromJson<SaveData>("{\"pearls\":17,\"espiralUnlocked\":true,\"startShell\":3}");
            Check(legacy.pearls == 17 && legacy.espiralUnlocked && legacy.startShell == ShellType.Espiral
                && legacy.startWeapon == WeaponType.Pincas && legacy.checkpoint == null,
                "Save antigo mantém pérolas/conchas e começa sem checkpoint ou arma comprada");

            Casco.TeleportTo(FindAnyObjectByType<Descent>().transform.position);
            yield return new WaitForSeconds(.3f);
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.RunScene);
            var run = (RunManager)Director.Instance;
            int firstRest = -1, secondRest = -1;
            for (int i = 0; i < run.Plan.Count; i++)
                if (run.Plan[i].zone == Zone.Bolha)
                {
                    if (firstRest < 0) firstRest = i;
                    else { secondRest = i; break; }
                }
            Check(firstRest > 0 && secondRest > firstRest, "Jornada oferece descansos antes de chegar aos chefes");
            run.JumpToStep(firstRest);
            yield return new WaitForSeconds(1f);
            var casco = Casco;
            casco.CanTakeDamage = false;
            var room = run.Room;
            Check(room.GetComponentsInChildren<Pearl>().Length == 0 && room.GetComponentsInChildren<Enemy>().Length == 0
                && room.GetComponentsInChildren<Chest>().Length == 0, "Abrigo é seguro e não recria tesouros ao restaurar o banco");

            // Coletar/trocar pelo mesmo botão usado no controle e no teclado.
            casco.TeleportTo(new Vector2(10.5f, 2.02f));
            casco.EquipWeapon(WeaponType.Pincas);
            var pickup = WeaponPickup.Spawn(WeaponType.Canudo, casco.Center, room.transform);
            yield return new WaitForSeconds(.2f);
            yield return Press(GameAction.Interact);
            Check(casco.Weapon == WeaponType.Canudo && pickup == null, "Interagir pega e equipa a arma encontrada");
            WeaponPickup.Spawn(WeaponType.Garfo, casco.Center, room.transform);
            yield return new WaitForSeconds(.2f);
            yield return Press(GameAction.Interact);
            bool dropped = false;
            foreach (var item in room.GetComponentsInChildren<WeaponPickup>()) dropped |= item.Type == WeaponType.Canudo;
            Check(casco.Weapon == WeaponType.Garfo && dropped && Hud.WeaponText.Contains("Garfo"),
                "Trocar sucata larga a arma anterior e atualiza o HUD");

            // Alvos imóveis isolam alcance, dano, recuperação e acerto único por ataque.
            casco.TeleportTo(new Vector2(10.5f, 2.02f));
            casco.Body.simulated = false;
            Controls.SimulatedMove = 1f;
            yield return null;
            yield return null;
            Controls.SimulatedMove = 0f;
            foreach (var type in new[] { WeaponType.Pincas, WeaponType.Canudo, WeaponType.Garfo, WeaponType.Escova })
            {
                casco.EquipWeapon(type);
                float distance = type == WeaponType.Garfo ? 2.15f : type == WeaponType.Canudo ? 1.6f : .95f;
                var target = VerificationTarget.Spawn(casco.Center + Vector2.right * distance, room.transform);
                var far = VerificationTarget.Spawn(casco.Center + Vector2.right * (Weapons.Get(type).Reach + .5f), room.transform);
                yield return new WaitForSeconds(.8f);
                yield return Press(GameAction.Attack);
                yield return new WaitForSeconds(.07f);
                yield return Press(GameAction.Attack); // apertar de novo não dribla o intervalo
                yield return Capture("arma-" + type);
                yield return new WaitForSeconds(.25f);
                Check(target.Damage == Weapons.Get(type).Damage && target.Hits == 1 && far.Hits == 0,
                    type + ": alcance e dano próprios, sem acertos repetidos no mesmo golpe");
                if (type == WeaponType.Escova) Check(target.StunTime > 0f, "Escova pesada também atordoa o alvo");
                Destroy(target.gameObject);
                Destroy(far.gameObject);
            }
            casco.Body.simulated = true;

            // Cambalhota real, com física, alvos na frente e atrás e tentativa de cancelar com outro ataque.
            casco.TeleportTo(new Vector2(7.5f, 2.02f));
            casco.Equip(ShellType.Espiral, -1);
            yield return new WaitForSeconds(.5f);
            var ahead = VerificationTarget.Spawn(casco.Center + new Vector2(.85f, .2f), room.transform);
            var behind = VerificationTarget.Spawn(casco.Center + new Vector2(-.65f, .2f), room.transform);
            Vector2 start = casco.transform.position;
            casco.ClearInvulnerability();
            int hp = casco.ShellHp;
            yield return Press(GameAction.Ability);
            Check(casco.Spinning && casco.Invulnerable, "Espiral inicia cambalhota ofensiva com proteção");
            casco.CanTakeDamage = true;
            casco.Hurt(casco.Center + Vector2.right);
            Check(casco.ShellHp == hp, "Cambalhota protege de dano por contato");
            yield return new WaitForSeconds(.13f);
            Check(casco.transform.position.y > start.y + .3f, "Cambalhota dá um salto próprio sem apertar pulo");
            yield return Capture("espiral-cambalhota");
            yield return Press(GameAction.Attack);
            yield return Press(GameAction.Dodge);
            yield return new WaitForSeconds(.65f);
            Check(ahead.Damage == GameRules.SpinDamage && behind.Damage == GameRules.SpinDamage
                && ahead.Hits == 1 && behind.Hits == 1, "Cambalhota dá 3 de dano nos dois lados, uma vez por alvo");
            Check(casco.transform.position.x - start.x > 5f, "Cambalhota avança mais que o rolamento comum");
            yield return Press(GameAction.Ability);
            Check(!casco.Spinning && casco.AbilityCooldown > 0f, "Espiral respeita a recarga indicada no HUD");
            Destroy(ahead.gameObject);
            Destroy(behind.gameObject);

            // Descansar salva equipamento curado e deposita pérolas uma única vez.
            var bench = room.GetComponentInChildren<BancoDescanso>();
            casco.TeleportTo(bench.Feet);
            casco.Equip(ShellType.Espiral, 1);
            casco.EquipWeapon(WeaponType.Garfo);
            casco.SetAlgae(0);
            casco.SetAir(1f);
            int bankBefore = Progress.Data.pearls;
            run.AddPearls(7);
            yield return new WaitForSeconds(.2f);
            yield return Press(GameAction.Interact);
            Check(casco.Resting && run.Resting && Progress.HasCheckpoint, "Deitar no banco salva imediatamente e mostra o descanso");
            yield return Capture("banco-descanso");
            yield return new WaitForSeconds(1.1f);
            Check(casco.ShellHp == 4 && casco.Algae == GameRules.MaxAlgae && casco.Air == casco.MaxAir
                && !casco.Resting && casco.Body.simulated, "Banco recupera concha, algas e ar e devolve os comandos");
            Check(Progress.Data.pearls == bankBefore + 7 && run.PearlsCollected == 0,
                "Banco guarda as pérolas coletadas sem duplicar o saldo");
            string itinerary = Itinerary(run);
            int runsBeforeReload = Progress.Data.runs;
            Progress.Reload();
            Check(Progress.HasCheckpoint && Progress.Data.checkpoint.weapon == WeaponType.Garfo
                && Progress.Data.checkpoint.shell == ShellType.Espiral
                && Progress.Data.checkpoint.roomIndex == firstRest,
                "Checkpoint e equipamento sobrevivem à recarga real do PlayerPrefs");

            // Morre numa sala posterior: volta ao banco, mantendo apenas o que foi guardado.
            run.JumpToStep(firstRest + 1);
            yield return new WaitForSeconds(1f);
            run.AddPearls(5);
            casco.Equip(ShellType.Comum, 1);
            casco.EquipWeapon(WeaponType.Escova);
            casco.Die("Verificação do retorno ao banco.");
            yield return new WaitForSecondsRealtime(1.8f);
            Check(Hud.MessageVisible, "Morte com banco mostra retorno ao checkpoint");
            yield return Capture("morte-com-banco");
            yield return Press(GameAction.Interact);
            for (float t = 0; t < 6f && run.Ending; t += Time.unscaledDeltaTime) yield return null;
            casco = Casco;
            Check(SceneManager.GetActiveScene().name == GameRules.RunScene && run.RoomIndex == firstRest && !casco.Dead,
                "Morte retorna ao banco na mesma jornada, sem voltar à vila");
            Check(casco.Shell == ShellType.Espiral && casco.ShellHp == 4 && casco.Weapon == WeaponType.Garfo
                && casco.Algae == GameRules.MaxAlgae && Progress.Data.pearls == bankBefore + 7 && run.PearlsCollected == 0,
                "Retorno restaura equipamento salvo e preserva pérolas depositadas; perde só as novas");
            Check(Itinerary(run) == itinerary, "Morte mantém a ordem das salas sorteadas");

            // Um segundo banco substitui o primeiro, e Continuar reabre esse mesmo abrigo.
            run.JumpToStep(secondRest);
            yield return new WaitForSeconds(1f);
            bench = run.Room.GetComponentInChildren<BancoDescanso>();
            casco.TeleportTo(bench.Feet);
            casco.Equip(ShellType.Nenhuma, 0);
            run.AddPearls(3);
            yield return new WaitForSeconds(.2f);
            yield return Press(GameAction.Interact);
            yield return new WaitForSeconds(1.1f);
            Check(Progress.Data.checkpoint.roomIndex == secondRest && casco.Shell == ShellType.Comum
                && casco.ShellHp == 3, "Banco mais recente substitui o anterior e dá concha comum se Casco estiver sem proteção");
            yield return Press(GameAction.Interact);
            yield return new WaitForSeconds(1.1f);
            Check(Progress.Data.pearls == bankBefore + 10, "Descansar novamente não duplica as pérolas depositadas");
            run.Pause();
            yield return Capture("pausa-banco");
            ClickPauseButton("Menu (");
            yield return WaitScene(GameRules.MenuScene);
            Progress.Reload();
            var menu = FindAnyObjectByType<MenuScreen>();
            Check(menu != null && menu.ContinueButton != null && Progress.HasCheckpoint, "Menu oferece continuar do banco salvo");
            yield return Capture("menu-continuar-banco");
            menu.ContinueButton.onClick.Invoke();
            yield return WaitScene(GameRules.RunScene);
            run = (RunManager)Director.Instance;
            Check(run.RoomIndex == secondRest && Itinerary(run) == itinerary && Progress.Data.runs == runsBeforeReload
                && Casco.Weapon == WeaponType.Garfo && !Casco.Dead, "Continuar reabre o último banco, equipamento e roteiro sem contar nova run");
            yield return Capture("checkpoint-retomado");

            // Saída deliberada para a vila abandona o checkpoint; morrer sem banco continua coberto em TestRun.
            run.Pause();
            ClickPauseButton("Voltar à vila");
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Press(GameAction.Interact);
            yield return WaitScene(GameRules.VillageScene);
            Check(SceneManager.GetActiveScene().name == GameRules.VillageScene && !Progress.HasCheckpoint,
                "Voltar à vila encerra o checkpoint sem prender o jogador no abrigo");
        }

        static string Itinerary(RunManager run)
        {
            string text = "";
            foreach (var step in run.Plan) text += step.zone + ":" + step.room.name + ";";
            return text;
        }

        static void ClickPauseButton(string prefix)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var label = button.GetComponentInChildren<Text>();
                if (label != null && label.text.StartsWith(prefix)) { button.onClick.Invoke(); return; }
            }
            throw new System.InvalidOperationException("Botão de pausa ausente: " + prefix);
        }
    }
}
