using UnityEngine;

namespace MareBaixa.Editor
{
    // Prefabs dos chefes (frente E): Ermitão Colosso e Polvo Abissal, com os dois olhos e a tinta.
    public static partial class ProjectBuilder
    {
        static GameObject CreateColosso()
        {
            var go = new GameObject("ErmitaoColosso");
            Kinematic(go);
            // Hitbox menor que a arte (3 × 2.5): Casco pula 2.5 blocos e precisa conseguir passar por cima do pneu.
            TriggerBox(go, new Vector2(2.2f, 1.7f), new Vector2(0f, .85f));

            var walk = ArtImporter.Frames("colosso-anda", 2);
            var art = Sprite("Arte", walk[0], 15);
            art.transform.SetParent(go.transform, false);

            var colosso = go.AddComponent<ErmitaoColosso>();
            colosso.art = art.GetComponent<SpriteRenderer>();
            colosso.walkFrames = walk;
            colosso.rollFrames = ArtImporter.Frames("colosso-rola", 2);
            colosso.dizzySprite = S("colosso-tonto");
            colosso.warnSprite = S("colosso-aviso");
            return Save<GameObject>(go, "ErmitaoColosso");
        }

        static GameObject CreatePolvo()
        {
            var go = new GameObject("PolvoAbissal");
            Kinematic(go);

            // Corpo grande no fundo, atrás dos blocos e sem colisão.
            var body = Sprite("Corpo", S("polvo-corpo"), -5);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var polvo = go.AddComponent<PolvoAbissal>();
            polvo.body = body.GetComponent<SpriteRenderer>();
            polvo.leftEye = CreateEye(go.transform, "OlhoEsquerdo", -3f, polvo);
            polvo.rightEye = CreateEye(go.transform, "OlhoDireito", 3f, polvo);
            polvo.inkPrefab = CreateInk();
            return Save<GameObject>(go, "PolvoAbissal");
        }

        static PolvoOlho CreateEye(Transform parent, string name, float x, PolvoAbissal polvo)
        {
            var closed = S("polvo-olho-fechado");
            var go = Sprite(name, closed, 16);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 1f, 0f);
            TriggerCircle(go, .5f);
            var light = go.AddComponent<LightSource>();
            light.radius = .7f;
            var eye = go.AddComponent<PolvoOlho>();
            eye.boss = polvo;
            eye.openSprite = S("polvo-olho-aberto");
            eye.closedSprite = closed;
            return eye;
        }

        static JatoDeTinta CreateInk()
        {
            var go = Sprite("JatoDeTinta", S("polvo-tinta"), 15);
            Kinematic(go);
            TriggerCircle(go, .25f);
            go.AddComponent<JatoDeTinta>();
            return Save<JatoDeTinta>(go, "JatoDeTinta");
        }
    }
}
