using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace MareBaixa
{
    // Modo -demoOutput <pasta> (junto com -demo): grava o jogo em tempo real num MP4 1080p 60 fps com o som.
    // O relógio do áudio (AudioSettings.dspTime) é o relógio mestre: o som vem da mistura final (DemoSom, no
    // AudioListener de cada cena) e cada quadro de vídeo é posto no instante desse relógio; se o jogo engasgar,
    // o último quadro se repete, então imagem e som nunca saem de sincronia.
    // Os quadros vão crus (leitura assíncrona da GPU) para o ffmpeg do Homebrew, que codifica no hardware do Mac;
    // o som vai para um WAV; no fim, os dois viram gameplay.mp4.
    // -demoSeconds N: sai sozinho depois de N s (teste do gravador).
    public class DemoGravador : MonoBehaviour
    {
        public const int Fps = 60;

        public static bool Gravando => current != null;
        static DemoGravador current;

        // Som recebido da thread de áudio, gravado no WAV pela thread principal.
        static readonly object trava = new object();
        static readonly List<float> somPendente = new List<float>();
        static int canaisSom = 2;
        static bool recebendo;
        static double dspInicio;
        static int taxaSom = 48000;
        static long quadrosDeSom;   // amostras por canal já recebidas

        string pasta;
        float limite = -1f;
        Process ffmpeg;
        Stream entrada;
        FileStream wav;
        long amostrasGravadas;
        int taxa;
        int largura, altura;
        RenderTexture tela;
        byte[] ultimoQuadro;
        long quadrosEscritos;
        double inicio = -1;
        bool fechado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Ligar()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-demoOutput");
            if (i < 0 || i + 1 >= args.Length || current != null) return;
            var go = new GameObject("Gravador da demo");
            DontDestroyOnLoad(go);
            current = go.AddComponent<DemoGravador>();
            current.pasta = args[i + 1];
            int j = Array.IndexOf(args, "-demoSeconds");
            if (j >= 0 && j + 1 < args.Length && float.TryParse(args[j + 1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float segundos))
                current.limite = segundos;
        }

        public static void ReceberSom(float[] data, int channels)
        {
            if (!recebendo) return;
            lock (trava)
            {
                canaisSom = channels;
                // Troca de cena sem AudioListener: completa com silêncio até o relógio do áudio.
                long esperado = (long)((AudioSettings.dspTime - dspInicio) * taxaSom);
                long falta = esperado - quadrosDeSom;
                if (falta > 4096)
                {
                    somPendente.AddRange(new float[falta * channels]);
                    quadrosDeSom += falta;
                }
                somPendente.AddRange(data);
                quadrosDeSom += data.Length / channels;
            }
        }

        static string Ffmpeg()
        {
            foreach (string p in new[] { "/opt/homebrew/bin/ffmpeg", "/usr/local/bin/ffmpeg" })
                if (File.Exists(p)) return p;
            return "ffmpeg";
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            Directory.CreateDirectory(pasta);
            for (int i = 0; i < 3; i++) yield return null;   // a janela assenta no tamanho pedido
            largura = Screen.width & ~1;
            altura = Screen.height & ~1;
            tela = new RenderTexture(largura, altura, 0, RenderTextureFormat.ARGB32);
            taxa = AudioSettings.outputSampleRate;

            wav = new FileStream(Path.Combine(pasta, "audio.wav"), FileMode.Create, FileAccess.Write);
            wav.Write(new byte[44], 0, 44);   // o cabeçalho é escrito no fim

            var info = new ProcessStartInfo(Ffmpeg(),
                $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {largura}x{altura} -r {Fps} -i - " +
                $"-c:v h264_videotoolbox -b:v 14M -pix_fmt yuv420p \"{Path.Combine(pasta, "video.mp4")}\"")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            ffmpeg = Process.Start(info);
            ffmpeg.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.LogWarning("[DEMO] ffmpeg: " + e.Data); };
            ffmpeg.BeginErrorReadLine();
            entrada = ffmpeg.StandardInput.BaseStream;

            SceneManager.sceneLoaded += (_, __) => PrenderNoOuvinte();
            PrenderNoOuvinte();
            inicio = AudioSettings.dspTime;
            lock (trava)
            {
                somPendente.Clear();
                dspInicio = inicio;
                taxaSom = taxa;
                quadrosDeSom = 0;
            }
            recebendo = true;
            Debug.Log($"[DEMO] gravando {largura}x{altura} @ {Fps} fps, som {taxa} Hz em {pasta}");

            var fim = new WaitForEndOfFrame();
            while (!fechado)
            {
                yield return fim;
                Capturar();
                GravarSom();
                if (limite > 0f && quadrosEscritos >= limite * Fps) Application.Quit();
            }
        }

        static void PrenderNoOuvinte()
        {
            foreach (var ouvinte in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (ouvinte.GetComponent<DemoSom>() == null)
                {
                    ouvinte.gameObject.AddComponent<DemoSom>();
                    Debug.Log("[DEMO] gravando o som de " + ouvinte.name);
                }
        }

        void Capturar()
        {
            if (fechado) return;
            ScreenCapture.CaptureScreenshotIntoRenderTexture(tela);
            // O quadro pertence a este instante do relógio do áudio, mesmo que a leitura chegue depois.
            long indice = (long)Math.Round((AudioSettings.dspTime - inicio) * Fps);
            AsyncGPUReadback.Request(tela, 0, TextureFormat.RGBA32, pedido =>
            {
                if (fechado || pedido.hasError) return;
                var dados = pedido.GetData<byte>();
                if (ultimoQuadro == null || ultimoQuadro.Length != dados.Length) ultimoQuadro = new byte[dados.Length];
                dados.CopyTo(ultimoQuadro);
                EscreverAte(indice);
            });
        }

        // Escreve o último quadro até chegar ao índice pedido (repete se o jogo engasgou; descarta se adiantou).
        void EscreverAte(long indice)
        {
            try
            {
                while (quadrosEscritos <= indice)
                {
                    entrada.Write(ultimoQuadro, 0, ultimoQuadro.Length);
                    quadrosEscritos++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[DEMO] falha ao gravar quadro: " + e.Message);
                fechado = true;
            }
        }

        void GravarSom()
        {
            float[] pedaco;
            int canais;
            lock (trava)
            {
                if (somPendente.Count == 0) return;
                pedaco = somPendente.ToArray();
                canais = canaisSom;
                somPendente.Clear();
            }
            if (canais != 2)
            {
                // Mistura para estéreo (o WAV é sempre estéreo).
                var estereo = new float[pedaco.Length / canais * 2];
                for (int i = 0, o = 0; i + canais <= pedaco.Length; i += canais, o += 2)
                {
                    estereo[o] = pedaco[i];
                    estereo[o + 1] = canais > 1 ? pedaco[i + 1] : pedaco[i];
                }
                pedaco = estereo;
            }
            var bytes = new byte[pedaco.Length * 4];
            Buffer.BlockCopy(pedaco, 0, bytes, 0, bytes.Length);
            wav.Write(bytes, 0, bytes.Length);
            amostrasGravadas += pedaco.Length;
        }

        void OnApplicationQuit() => Fechar();
        void OnDestroy() => Fechar();

        // Fecha o vídeo e o som e junta os dois em gameplay.mp4. Segura o fim do app até terminar.
        void Fechar()
        {
            if (current != this) return;
            AsyncGPUReadback.WaitAllRequests();
            current = null;
            recebendo = false;
            fechado = true;
            if (wav != null)
            {
                GravarSom();
                EscreverCabecalho();
                wav.Close();
            }
            try
            {
                entrada?.Flush();
                entrada?.Close();
                ffmpeg?.WaitForExit();
            }
            catch (Exception e) { Debug.LogError("[DEMO] ffmpeg (vídeo): " + e.Message); }
            Debug.Log($"[DEMO] {quadrosEscritos} quadros ({quadrosEscritos / (float)Fps:0.0} s), " +
                      $"som {amostrasGravadas / 2f / Mathf.Max(1, taxa):0.0} s");

            string video = Path.Combine(pasta, "video.mp4");
            string audio = Path.Combine(pasta, "audio.wav");
            string final = Path.Combine(pasta, "gameplay.mp4");
            try
            {
                var juntar = Process.Start(new ProcessStartInfo(Ffmpeg(),
                    $"-y -loglevel error -i \"{video}\" -i \"{audio}\" -c:v copy -c:a aac -b:a 192k -shortest " +
                    $"-movflags +faststart \"{final}\"")
                { UseShellExecute = false, CreateNoWindow = true });
                juntar.WaitForExit();
                Debug.Log("[DEMO] vídeo final: " + final + " (código " + juntar.ExitCode + ")");
            }
            catch (Exception e) { Debug.LogError("[DEMO] ffmpeg (juntar): " + e.Message); }
        }

        // WAV estéreo IEEE float 32 bits.
        void EscreverCabecalho()
        {
            long dados = amostrasGravadas * 4;
            wav.Seek(0, SeekOrigin.Begin);
            var w = new BinaryWriter(wav, System.Text.Encoding.ASCII, true);
            w.Write("RIFF".ToCharArray()); w.Write((int)(36 + dados)); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)3); w.Write((short)2);
            w.Write(taxa); w.Write(taxa * 2 * 4); w.Write((short)8); w.Write((short)32);
            w.Write("data".ToCharArray()); w.Write((int)dados);
            w.Flush();
        }
    }
}
