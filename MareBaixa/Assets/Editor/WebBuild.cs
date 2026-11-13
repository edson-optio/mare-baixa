using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MareBaixa.Editor
{
    // Build para navegador (itch.io). Gera Entrega/WebGL/ com index.html; o zip dessa pasta vai para o itch
    // marcado como "This file will be played in the browser" (viewport 1280x720).
    // Requer o módulo "Web Build Support" instalado no Unity Hub para o 6000.3.24f1.
    // Uso: Unity -batchmode -quit -projectPath MareBaixa -executeMethod MareBaixa.Editor.WebBuild.Build
    public static class WebBuild
    {
        const string OutputPath = "../Entrega/WebGL";

        [MenuItem("Maré Baixa/3. Gerar versão web (itch.io)")]
        public static void Build()
        {
            if (!File.Exists("Assets/Scenes/Menu.unity")) ProjectBuilder.CreateProject();

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new System.Exception("Falha no build: módulo WebGL não instalado (Unity Hub → Installs → Add modules → Web Build Support).");

            // O itch.io não envia o cabeçalho Content-Encoding; o fallback descompacta no navegador.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
            PlayerSettings.runInBackground = true;

            if (Directory.Exists(OutputPath)) Directory.Delete(OutputPath, true);
            Directory.CreateDirectory(OutputPath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Falha no build: " + report.summary.result);
            Debug.Log("MARE_WEB_READY: " + Path.GetFullPath(OutputPath));
        }

        public static void BuildAll()
        {
            ProjectBuilder.CreateProject();
            Build();
        }
    }
}
