using UnityEngine;

namespace MareBaixa.Editor
{
    // Prefabs da frente B (inimigos e objetos da Cidade Afundada e da Fossa Abissal).
    public static partial class ProjectBuilder
    {
        static GameObject CreateMoray()
        {
            var go = Sprite("Moreia", S("moreia-escondida"), 15);
            Kinematic(go);
            TriggerBox(go, new Vector2(.7f, .5f), new Vector2(0f, .4f));
            var body = Sprite("Corpo", S("moreia-corpo"), 14).GetComponent<SpriteRenderer>();
            body.transform.SetParent(go.transform, false);
            body.drawMode = SpriteDrawMode.Tiled;
            body.size = new Vector2(0f, 1f);
            body.enabled = false;
            var moray = go.AddComponent<Moreia>();
            moray.hidden = S("moreia-escondida");
            moray.strike = S("moreia-bote");
            moray.retract = S("moreia-volta");
            moray.body = body;
            return Save<GameObject>(go, "Moreia");
        }

        static GameObject CreateCrab()
        {
            var walk = ArtImporter.Frames("siri", 2);
            var go = Sprite("Siri", walk[0], 15);
            Kinematic(go);
            TriggerBox(go, new Vector2(.8f, .5f), new Vector2(0f, .25f));
            // A concha roubada vai nas costas; o sprite é trocado pelo tipo roubado.
            var carried = Sprite("Concha", S("concha-comum"), 16).GetComponent<SpriteRenderer>();
            carried.transform.SetParent(go.transform, false);
            carried.transform.localPosition = new Vector3(0f, .3f, 0f);
            carried.transform.localScale = new Vector3(.8f, .8f, 1f);
            carried.enabled = false;
            var crab = go.AddComponent<Siri>();
            crab.walkFrames = walk;
            crab.claws = S("siri-garra");
            crab.fleeing = S("siri-fugindo");
            crab.carried = carried;
            return Save<GameObject>(go, "Siri");
        }

        static GameObject CreateAngler()
        {
            var swim = ArtImporter.Frames("pescador", 2);
            var go = Sprite("PeixePescador", swim[0], 15);
            Kinematic(go);
            TriggerCircle(go, .45f);
            // A isca fica acima da escuridão (sorting 60): no escuro é só ela que aparece.
            var lure = Sprite("Isca", S("pescador-isca"), 61);
            lure.transform.SetParent(go.transform, false);
            lure.transform.localPosition = new Vector3(.62f, .14f, 0f);
            var light = lure.AddComponent<LightSource>();
            light.radius = 1.2f;
            var angler = go.AddComponent<PeixePescador>();
            angler.swimFrames = swim;
            angler.mouthOpen = S("pescador-boca");
            angler.lure = light;
            angler.lureOffset = .62f;
            angler.lureHeight = .14f;
            // A luz da isca cobre o corpo inteiro: no escuro dá para ver que é um peixe.
            angler.lureRadius = 1.7f;
            return Save<GameObject>(go, "PeixePescador");
        }

        static TentaculoSombrio CreateTentacle()
        {
            var frames = ArtImporter.Frames("tentaculo", 2);
            var go = Sprite("TentaculoSombrio", frames[0], 15);
            Kinematic(go);
            TriggerBox(go, new Vector2(.6f, 2.4f), new Vector2(0f, 1.2f));
            var tentacle = go.AddComponent<TentaculoSombrio>();
            tentacle.frames = frames;
            tentacle.warning = S("tentaculo-aviso");
            return Save<TentaculoSombrio>(go, "TentaculoSombrio");
        }

        static GameObject CreateValve()
        {
            var frames = ArtImporter.Frames("valvula", 2);
            var go = Sprite("Valvula", frames[0], 10);
            TriggerBox(go, new Vector2(.9f, .9f), new Vector2(0f, .45f));
            go.AddComponent<Valvula>().frames = frames;
            return Save<GameObject>(go, "Valvula");
        }

        static GameObject CreateCurrent()
        {
            var frames = ArtImporter.Frames("correnteza", 2);
            var go = Sprite("Correnteza", frames[0], 4);
            go.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, .5f);
            Kinematic(go);
            TriggerBox(go, Vector2.one, Vector2.zero);
            go.AddComponent<Correnteza>().frames = frames;
            return Save<GameObject>(go, "Correnteza");
        }

        static GameObject CreateHealFont()
        {
            var go = Sprite("FonteDeCura", S("fonte-1"), 10);
            var font = go.AddComponent<FonteDeCura>();
            font.full = S("fonte-1");
            font.dry = S("fonte-seca");
            return Save<GameObject>(go, "FonteDeCura");
        }
    }
}
