using UnityEngine;

namespace MareBaixa
{
    public enum CutsceneId { PerolaRoubada, PrimeiraConcha, FundoDoMar, MareBaixa }

    // Um quadro da cena: a imagem (nome no Kit), a legenda e quanto tempo fica na tela.
    public readonly struct CutsceneFrame
    {
        public readonly string image;
        public readonly string text;
        public readonly float seconds;

        public CutsceneFrame(string image, string text, float seconds)
        {
            this.image = image;
            this.text = text;
            this.seconds = seconds;
        }
    }

    public readonly struct Cutscene
    {
        public readonly string title;
        public readonly Color tint;   // cor do quadro enquanto a arte não chega
        public readonly CutsceneFrame[] frames;

        public Cutscene(string title, Color tint, params CutsceneFrame[] frames)
        {
            this.title = title;
            this.tint = tint;
            this.frames = frames;
        }
    }

    // Roteiro das quatro cenas de corte (GDD p. 9), em quadros no estilo história em quadrinhos.
    public static class Cutscenes
    {
        static readonly Cutscene[] Table =
        {
            new Cutscene("A Pérola Roubada", new Color32(26, 38, 82, 255),
                new CutsceneFrame("cena1-1", "Noite de lua cheia em Coralina. Lá no alto, a Pérola Lunar segura a maré no lugar.", 5f),
                new CutsceneFrame("cena1-2", "Do fundo escuro, um tentáculo enorme sobe devagarinho... devagarinho...", 4.5f),
                new CutsceneFrame("cena1-3", "...e leva a pérola embora! Ninguém viu. Quer dizer: Bolota viu, e desmaiou.", 5f),
                new CutsceneFrame("cena1-4", "De manhã, a água já cobria metade da praia. E não parava de subir.", 4.5f),
                new CutsceneFrame("cena1-5", "Vovó Búzia olhou bem para Casco: — Alguém precisa descer.", 4.5f)),

            new Cutscene("A Primeira Concha", GameRules.DeepSea,
                new CutsceneFrame("cena2-1", "Uma concha nova! Casco entra nela todo orgulhoso.", 4f),
                new CutsceneFrame("cena2-2", "Ficou... um pouquinho grande. Tropeço, cambalhota e areia no olho.", 4.5f),
                new CutsceneFrame("cena2-3", "Cada concha muda tudo: o jeito de andar, de pular e de lutar. Bora testar!", 4.5f)),

            new Cutscene("O Fundo do Mar", new Color32(10, 12, 30, 255),
                new CutsceneFrame("cena3-1", "A Fossa Abissal. Aqui embaixo, até a luz tem medo de entrar.", 4.5f),
                new CutsceneFrame("cena3-2", "Lá longe, no escuro, dois olhos enormes se abrem.", 4f),
                new CutsceneFrame("cena3-3", "— A praia vai ser minha, caranguejinho.", 4.5f)),

            new Cutscene("Maré Baixa", new Color32(186, 120, 70, 255),
                new CutsceneFrame("cena4-1", "Casco ergue a Pérola Lunar. Ela brilha como a lua cheia.", 4.5f),
                new CutsceneFrame("cena4-2", "A água recua e revela a cidade antiga, com todo o lixo que os humanos deixaram.", 5f),
                new CutsceneFrame("cena4-3", "Na Vila da Praia é festa: a maré baixou!", 4f),
                new CutsceneFrame("cena4-4", "Bolota chega tremendo, com um presente: — Achei essa concha pra você. Acho que serve!", 5f),
                new CutsceneFrame("cena4-5", "Serviu. Mais ou menos.", 4f)),
        };

        public static Cutscene Get(CutsceneId id) => Table[(int)id];
    }
}
