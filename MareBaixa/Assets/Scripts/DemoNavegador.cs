using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Modo -demo: leva Casco até um ponto da sala como um jogador faria (anda, pula, desce de plataforma),
    // apertando os controles simulados. O grafo é o mesmo do ferramentas/validar_salas.py: células onde Casco
    // fica em pé, ligadas por andar/cair, descer da plataforma e pulos de até 2 blocos de altura.
    // Controle em malha fechada: replaneja a cada pouso, então correnteza, água e empurrões não o perdem.
    public class DemoNavegador
    {
        const int SubidaMax = 2;
        const int AlcanceSubindo = 3;
        const int AlcanceNivel = 4;

        enum Tipo { Andar, Descer, Pular }

        struct Passo
        {
            public Vector2Int Destino;
            public Tipo Tipo;
            public float Custo;
        }

        readonly Room room;
        readonly Vector2 origem;
        readonly Dictionary<Vector2Int, List<Passo>> grafo = new Dictionary<Vector2Int, List<Passo>>();

        // Pulo em andamento
        bool noAr;
        Passo pulo;
        float puloDesde;
        Vector2Int ultimaCelula = new Vector2Int(int.MinValue, 0);
        List<Passo> rota = new List<Passo>();
        Vector2Int rotaAlvo = new Vector2Int(int.MinValue, 0);
        float progressoEm;
        float melhorDistancia = float.MaxValue;
        float esperaAte;
        Vector2 ultimoAlvoMundo = new Vector2(float.MaxValue, 0f);
        Vector2Int alvoCache;

        public Vector2 Saida { get; }

        public DemoNavegador(Room room)
        {
            this.room = room;
            origem = room.transform.position;
            for (int x = 0; x < room.Width; x++)
                for (int y = 0; y < room.Height; y++)
                    if (EmPe(x, y)) grafo[new Vector2Int(x, y)] = Vizinhos(x, y);
            Saida = Mundo(Procurar('S') ?? Procurar('D') ?? Vector2Int.zero);
        }

        // ------------------------------------------------------------ API

        public bool Ir(Casco casco, Vector2 alvoMundo)
        {
            Controls.Simulated = true;
            Controls.SimulatedDown = false;
            if (casco == null || casco.Dead) { Soltar(); return false; }
            Vector2 pes = casco.transform.position;

            // No ar: termina o pulo mirando a célula de destino.
            if (noAr)
            {
                if (casco.Grounded && Time.time - puloDesde > .12f) { noAr = false; Controls.Hold(GameAction.Jump, false); }
                else
                {
                    ConduzirPulo(casco, pes);
                    if (Time.time - puloDesde > 2.5f) noAr = false;
                    return false;
                }
            }

            Vector2Int? atual = CelulaEmPe(pes);
            if (atual == null)
            {
                // Caindo ou nadando: mira a coluna da próxima célula conhecida.
                Controls.Hold(GameAction.Jump, false);
                Controls.SimulatedMove = rota.Count > 0 ? Mathf.Clamp((Mundo(rota[0].Destino).x - pes.x) * 2f, -1f, 1f) : 0f;
                return false;
            }

            if (atual.Value != ultimaCelula || Vector2.Distance(alvoMundo, ultimoAlvoMundo) > .5f)
            {
                alvoCache = MaisPerto(alvoMundo, atual.Value);
                ultimoAlvoMundo = alvoMundo;
            }
            Vector2Int alvo = alvoCache;
            if (atual.Value != ultimaCelula || alvo != rotaAlvo || rota.Count == 0)
            {
                rota = Caminho(atual.Value, alvo);
                ultimaCelula = atual.Value;
                rotaAlvo = alvo;
            }

            // Detecta travamento (sem chegar mais perto do alvo por 2 s): dá um pulo para destravar.
            float distancia = Vector2.Distance(pes, Mundo(alvo));
            if (distancia < melhorDistancia - .3f) { melhorDistancia = distancia; progressoEm = Time.time; }
            if (Time.time - progressoEm > 2f && Time.time > esperaAte)
            {
                progressoEm = Time.time;
                melhorDistancia = distancia;
                ultimaCelula = new Vector2Int(int.MinValue, 0);
                Controls.Press(GameAction.Jump);
                Controls.Hold(GameAction.Jump, true);
                esperaAte = Time.time + .5f;
            }

            if (atual.Value == alvo)
            {
                float dx = Mundo(alvo).x - pes.x;
                if (Mathf.Abs(dx) < .2f) { Soltar(); return true; }
                Controls.SimulatedMove = Mathf.Clamp(dx * 3f, -1f, 1f);
                return false;
            }
            if (rota.Count == 0) { Soltar(); return true; }   // não há caminho: fica no mais perto

            // Junta os passos de andar e anda até o primeiro passo que não é andar.
            Passo proximo = rota[0];
            float centro = atual.Value.x + .5f + origem.x;
            switch (proximo.Tipo)
            {
                case Tipo.Andar:
                    Controls.Hold(GameAction.Jump, false);
                    Controls.SimulatedMove = Mathf.Sign(proximo.Destino.x - atual.Value.x);
                    break;
                case Tipo.Descer:
                    Controls.SimulatedMove = 0f;
                    if (Mathf.Abs(pes.x - centro) > .25f) { Controls.SimulatedMove = Mathf.Clamp((centro - pes.x) * 3f, -1f, 1f); break; }
                    Controls.SimulatedDown = true;
                    Controls.Press(GameAction.Jump);
                    break;
                case Tipo.Pular:
                    int dir = proximo.Destino.x == atual.Value.x ? 0 : (int)Mathf.Sign(proximo.Destino.x - atual.Value.x);
                    // Toma impulso perto do centro da célula (um pouco para trás num pulo longo).
                    float largada = centro - dir * (Mathf.Abs(proximo.Destino.x - atual.Value.x) >= 3 ? .2f : 0f);
                    if (Mathf.Abs(pes.x - largada) > .3f)
                    {
                        Controls.SimulatedMove = Mathf.Clamp((largada - pes.x) * 3f, -1f, 1f);
                        break;
                    }
                    pulo = proximo;
                    noAr = true;
                    puloDesde = Time.time;
                    Controls.Press(GameAction.Jump);
                    Controls.Hold(GameAction.Jump, true);
                    Controls.SimulatedMove = 0f;
                    break;
            }
            return false;
        }

        public bool Alcancavel(Vector2 alvoMundo)
        {
            var casco = Director.Instance != null ? Director.Instance.Casco : null;
            if (casco == null) return false;
            Vector2Int? atual = CelulaEmPe(casco.transform.position);
            if (atual == null) return false;
            Vector2Int alvo = MaisPerto(alvoMundo, atual.Value);
            return Vector2.Distance(Mundo(alvo), alvoMundo) < 1.6f && (alvo == atual.Value || Caminho(atual.Value, alvo).Count > 0);
        }

        public void Soltar()
        {
            Controls.SimulatedMove = 0f;
            Controls.SimulatedDown = false;
            Controls.Hold(GameAction.Jump, false);
            noAr = false;
        }

        // ------------------------------------------------------------ Execução do pulo

        void ConduzirPulo(Casco casco, Vector2 pes)
        {
            float alvoX = Mundo(pulo.Destino).x;
            float alvoY = pulo.Destino.y + origem.y;
            float dx = alvoX - pes.x;
            float vy = casco.Body.linearVelocity.y;
            int dir = Mathf.Abs(dx) < .05f ? 0 : (int)Mathf.Sign(dx);
            // Subindo para um degrau mais alto: só vai para o lado quando os pés passarem da borda,
            // senão bate na quina (o validador assume esse mesmo arco: sobe, anda, desce).
            bool bloqueado = false;
            if (dir != 0 && pes.y < alvoY + .05f)
            {
                int cx = Mathf.FloorToInt(pes.x - origem.x + dir * .45f);
                int cy = Mathf.FloorToInt(pes.y - origem.y + .05f);
                bloqueado = room.At(cx, cy) == '#' || room.At(cx, cy + 1) == '#';
            }
            Controls.SimulatedMove = bloqueado ? 0f : Mathf.Clamp(dx * 2.2f, -1f, 1f);
            // Segura o pulo enquanto ainda precisa de altura; num pulo para baixo, solta cedo.
            bool precisaAltura = pes.y < alvoY + .6f || Mathf.Abs(dx) > 2.5f;
            Controls.Hold(GameAction.Jump, vy > 0f && precisaAltura);
        }

        // ------------------------------------------------------------ Grafo

        Vector2Int? CelulaEmPe(Vector2 pes)
        {
            int x = Mathf.FloorToInt(pes.x - origem.x);
            int y = Mathf.FloorToInt(pes.y - origem.y + .15f);
            var c = new Vector2Int(x, y);
            if (grafo.ContainsKey(c)) return c;
            // Na quina, os pés podem estar meio bloco para fora da célula com apoio.
            foreach (int lado in new[] { -1, 1 })
            {
                var v = new Vector2Int(Mathf.FloorToInt(pes.x - origem.x + lado * .36f), y);
                if (grafo.ContainsKey(v)) return v;
            }
            return null;
        }

        Vector2 Mundo(Vector2Int c) => new Vector2(c.x + .5f + origem.x, c.y + origem.y);

        Vector2Int? Procurar(char ch)
        {
            for (int y = 0; y < room.Height; y++)
                for (int x = 0; x < room.Width; x++)
                    if (room.At(x, y) == ch)
                    {
                        // O S fica em pé; se não estiver numa célula do grafo, desce até achar uma.
                        for (int yy = y; yy >= 0; yy--)
                            if (grafo.ContainsKey(new Vector2Int(x, yy))) return new Vector2Int(x, yy);
                        return new Vector2Int(x, y);
                    }
            return null;
        }

        // Célula alcançável mais perto do alvo (no mundo).
        Vector2Int MaisPerto(Vector2 alvoMundo, Vector2Int de)
        {
            var alcance = Dijkstra(de, null, out _);
            Vector2Int melhor = de;
            float melhorD = float.MaxValue;
            foreach (var c in alcance.Keys)
            {
                float d = Vector2.SqrMagnitude(Mundo(c) + new Vector2(0f, .4f) - alvoMundo);
                if (d < melhorD) { melhorD = d; melhor = c; }
            }
            return melhor;
        }

        List<Passo> Caminho(Vector2Int de, Vector2Int para)
        {
            Dijkstra(de, para, out var anterior);
            var lista = new List<Passo>();
            if (!anterior.ContainsKey(para)) return lista;
            var c = para;
            while (c != de)
            {
                var (pai, passo) = anterior[c];
                lista.Add(passo);
                c = pai;
            }
            lista.Reverse();
            return lista;
        }

        Dictionary<Vector2Int, float> Dijkstra(Vector2Int de, Vector2Int? para,
            out Dictionary<Vector2Int, (Vector2Int, Passo)> anterior)
        {
            var custo = new Dictionary<Vector2Int, float> { [de] = 0f };
            anterior = new Dictionary<Vector2Int, (Vector2Int, Passo)>();
            var fila = new SortedSet<(float, int, int)> { (0f, de.x, de.y) };
            while (fila.Count > 0)
            {
                var (t, x, y) = fila.Min;
                fila.Remove(fila.Min);
                var atual = new Vector2Int(x, y);
                if (t > custo[atual]) continue;
                if (para.HasValue && atual == para.Value) break;
                if (!grafo.TryGetValue(atual, out var passos)) continue;
                foreach (var p in passos)
                {
                    float nt = t + p.Custo;
                    if (custo.TryGetValue(p.Destino, out float velho) && velho <= nt) continue;
                    if (custo.ContainsKey(p.Destino)) fila.Remove((velho, p.Destino.x, p.Destino.y));
                    custo[p.Destino] = nt;
                    anterior[p.Destino] = (atual, p);
                    fila.Add((nt, p.Destino.x, p.Destino.y));
                }
            }
            return custo;
        }

        char At(int x, int y) => x < 0 || x >= room.Width || y < 0 ? '#' : room.At(x, y);
        bool Livre(int x, int y) => At(x, y) != '#';
        bool VazioParaFicar(int x, int y) { char c = At(x, y); return c != '#' && c != '='; }
        bool Apoio(int x, int y) { char c = At(x, y - 1); return c == '#' || c == '='; }
        bool EmPe(int x, int y) => y > 0 && y < room.Height && VazioParaFicar(x, y) && Apoio(x, y);

        Vector2Int? Cair(int x, int y)
        {
            for (; y > 0; y--)
            {
                if (!VazioParaFicar(x, y)) return null;
                if (Apoio(x, y)) return new Vector2Int(x, y);
            }
            return null;
        }

        static float Queda(int blocos) => Mathf.Sqrt(2f * Mathf.Max(blocos, 0) / (9.81f * 4f));

        List<Passo> Vizinhos(int x, int y)
        {
            var lista = new List<Passo>();
            foreach (int dx in new[] { -1, 1 })
                if (VazioParaFicar(x + dx, y) && Cair(x + dx, y) is Vector2Int destino)
                    lista.Add(new Passo { Destino = destino, Tipo = Tipo.Andar, Custo = 1f / 6.5f + Queda(y - destino.y) });
            if (At(x, y - 1) == '=' && Cair(x, y - 2) is Vector2Int baixo)
                lista.Add(new Passo { Destino = baixo, Tipo = Tipo.Descer, Custo = .3f + Queda(y - baixo.y) });
            for (int subida = -room.Height; subida <= SubidaMax; subida++)
            {
                int alcance = subida > 0 ? AlcanceSubindo : AlcanceNivel;
                for (int dx = -alcance; dx <= alcance; dx++)
                {
                    if (subida <= 0 && dx == 0) continue;
                    int ax = x + dx, ay = y + subida;
                    if (!EmPe(ax, ay) || !TrajetoLivre(x, y, subida, dx)) continue;
                    // Andar/cair já cobre o vizinho do lado; pulo só quando é preciso.
                    if (subida <= 0 && Mathf.Abs(dx) == 1 && Cair(x + dx, y) == new Vector2Int(ax, ay)) continue;
                    lista.Add(new Passo { Destino = new Vector2Int(ax, ay), Tipo = Tipo.Pular,
                        Custo = Mathf.Max(.45f, Mathf.Abs(dx) / 6.5f) + Queda(-subida) + .15f });
                }
            }
            return lista;
        }

        // Mesmo arco do validador: sobe na coluna de saída, anda na altura de travessia e desce na chegada.
        bool TrajetoLivre(int x, int y, int subida, int dx)
        {
            int topo;
            if (subida >= SubidaMax) topo = y + subida;
            else if (subida > 0 || Mathf.Abs(dx) > 1) topo = y + Mathf.Max(subida, 0) + 1;
            else topo = y + Mathf.Max(subida, 0);
            for (int linha = y + 1; linha <= topo; linha++)
                if (!Livre(x, linha)) return false;
            int passo = dx > 0 ? 1 : -1;
            if (dx != 0)
            {
                for (int coluna = x; coluna != x + dx + passo; coluna += passo)
                    if (!Livre(coluna, topo)) return false;
            }
            else if (!Livre(x, topo)) return false;
            int alvoY = y + subida;
            for (int linha = topo; linha >= alvoY; linha--)
            {
                char c = At(x + dx, linha);
                if (c == '#' || (linha < topo && linha >= alvoY && c == '=')) return false;
            }
            return true;
        }
    }
}
