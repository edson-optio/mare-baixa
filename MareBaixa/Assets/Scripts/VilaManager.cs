using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MareBaixa
{
    // Vila da Praia (hub): sem maré e sem dano. Os moradores contam a história, Ostrácia vende
    // melhorias permanentes e Dona Lagosta forja conchas iniciais (GDD p. 3 e 7).
    public class VilaManager : Director
    {
        static readonly string[] IntroLines =
        {
            "Casco! Ontem à noite, sob a lua cheia, um tentáculo saiu do mar e levou a Pérola Lunar.",
            "Sem a Pérola, a maré não para de subir. Hoje de manhã a água já cobria metade da praia.",
            "Lá embaixo fica Coralina, a cidade dos humanos que afundou. O Polvo Abissal mora no fundo dela.",
            "Alguém precisa descer. E você é pequeno o bastante para passar por qualquer fresta.",
            "A concha é sua casa e sua armadura. Sem ela, um golpe basta. Troque sempre que achar uma melhor.",
            "Junte pérolas. Ostrácia troca por melhorias e Dona Lagosta forja conchas. A descida fica à direita.",
        };

        static readonly string[] BuziaTips =
        {
            "A maré sobe sem parar em cada sala. Os tesouros ficam lá embaixo... e a saída, quase sempre, lá em cima.",
            "Com a cabeça debaixo d'água, o ar acaba em poucos segundos. Procure as bolhas.",
            "Água-viva só dá choque de tempos em tempos. Belisque entre um choque e outro.",
            "Baiacu bravo incha quando apanha. Bata uma vez e se afaste antes que ele exploda!",
            "Rolar dentro da concha (K) te deixa passar por qualquer golpe. Use na hora certa.",
            "Alga cura a concha. Aperte Q para comer quando ela estiver rachada.",
        };

        static readonly string[] BolotaLines =
        {
            "E-eu ia junto, viu? Mas alguém precisa ficar cuidando da vila... né?",
            "Se você vir um baiacu bravo lá embaixo, ele NÃO é meu parente. Eu juro.",
            "Volta inteiro, tá? E com uma concha nova pra me mostrar!",
            "A água chegou no meu pé hoje. Eu nem tenho pé. Foi assustador mesmo assim.",
        };

        bool leaving;
        int buziaTip;

        public override bool InputLocked => base.InputLocked || Hud.PanelOpen || leaving;

        void Start()
        {
            BuildRoom(kit.village, Zone.Vila);
            ShellType shell = Progress.IsUnlocked(Progress.Data.startShell) ? Progress.Data.startShell : ShellType.Comum;
            SpawnCasco(Room.StartPoint, shell);
            Casco.CanTakeDamage = false;
            Casco.MaxAir = Progress.MaxAir;
            CameraRig.SnapToTarget();
            Hud.SetTitle("Vila da Praia");
            buziaTip = Progress.Data.runs % BuziaTips.Length;
            StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            Hud.SetFade(1f);
            yield return Hud.Fade(1f, 0f, .4f);
            // Primeira visita: Vovó Búzia conta a história (substitui a cutscene 1 por enquanto).
            if (!Progress.Data.introSeen)
                Hud.ShowDialog("Vovó Búzia", Portrait(NpcKind.Buzia), IntroLines, () =>
                {
                    Progress.Data.introSeen = true;
                    Progress.Save();
                });
        }

        Sprite Portrait(NpcKind kind)
        {
            int index = (int)kind;
            return kit.portraits != null && index < kit.portraits.Length ? kit.portraits[index] : null;
        }

        public override void OnTalk(Npc npc)
        {
            switch (npc.kind)
            {
                case NpcKind.Buzia:
                    Hud.ShowDialog(npc.displayName, Portrait(npc.kind), new[] { BuziaTips[buziaTip] }, null);
                    buziaTip = (buziaTip + 1) % BuziaTips.Length;
                    break;
                case NpcKind.Bolota:
                    Hud.ShowDialog(npc.displayName, Portrait(npc.kind),
                        new[] { BolotaLines[UnityEngine.Random.Range(0, BolotaLines.Length)] }, null);
                    break;
                case NpcKind.Ostracia:
                    Hud.ShowShop("Ostrácia · pérolas por melhorias", OstraciaShop(), null);
                    break;
                case NpcKind.Lagosta:
                    Hud.ShowShop("Forja da Dona Lagosta", LagostaShop(), null);
                    break;
            }
        }

        // ---------------------------------------------------------------- Lojas

        List<ShopEntry> OstraciaShop()
        {
            var data = Progress.Data;
            return new List<ShopEntry>
            {
                Upgrade("Pulmão de Peixe: +2 s de ar", new[] { 3, 5, 8 }, () => data.airLevel, () => data.airLevel++),
                Upgrade("Casca Grossa: aguenta 1 golpe sem concha", new[] { 6 }, () => data.nakedGuardLevel, () => data.nakedGuardLevel++),
                Upgrade("Alga na Bolsa: +1 alga no início", new[] { 4, 7 }, () => data.algaeLevel, () => data.algaeLevel++),
            };
        }

        ShopEntry Upgrade(string label, int[] prices, Func<int> level, Action raise)
        {
            return new ShopEntry
            {
                label = label,
                detail = () => level() >= prices.Length
                    ? "Esgotado"
                    : prices[level()] + " pérolas · nível " + level() + "/" + prices.Length,
                onSelect = () =>
                {
                    if (level() >= prices.Length || !Spend(prices[level()])) return false;
                    raise();
                    Progress.Save();
                    Casco.MaxAir = Progress.MaxAir;
                    return true;
                },
            };
        }

        List<ShopEntry> LagostaShop()
        {
            return new List<ShopEntry>
            {
                ShellChoice(ShellType.Comum, 0),
                ShellChoice(ShellType.Tampinha, 6),
                ShellChoice(ShellType.Espiral, 8),
            };
        }

        // Primeiro a ferreira forja a concha (custa pérolas); depois ela vira opção de concha inicial.
        ShopEntry ShellChoice(ShellType type, int price)
        {
            var data = Progress.Data;
            return new ShopEntry
            {
                label = Shells.Get(type).Name + " · " + Shells.Get(type).Ability,
                detail = () => !Progress.IsUnlocked(type) ? "Forjar: " + price + " pérolas"
                    : data.startShell == type ? "Concha inicial ✓" : "Começar com ela",
                onSelect = () =>
                {
                    if (!Progress.IsUnlocked(type))
                    {
                        if (!Spend(price)) return false;
                        if (type == ShellType.Tampinha) data.tampinhaUnlocked = true;
                        if (type == ShellType.Espiral) data.espiralUnlocked = true;
                    }
                    data.startShell = type;
                    Progress.Save();
                    Casco.Equip(type, -1);
                    return true;
                },
            };
        }

        static bool Spend(int price)
        {
            if (Progress.Data.pearls < price) return false;
            Progress.Data.pearls -= price;
            return true;
        }

        // A descida para o Recife Raso.
        public override void OnExitReached()
        {
            if (leaving) return;
            leaving = true;
            Sound.Play(Sfx.Door);
            StartCoroutine(Dive());
        }

        IEnumerator Dive()
        {
            yield return Hud.Fade(0f, 1f, .35f);
            SceneManager.LoadScene(GameRules.ReefScene);
        }
    }
}
