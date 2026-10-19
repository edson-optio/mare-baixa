using System;
using UnityEngine;

namespace MareBaixa
{
    // Progresso permanente entre runs (pérolas guardadas e melhorias da vila), salvo em PlayerPrefs.
    [Serializable]
    public class SaveData
    {
        public int pearls;
        public int airLevel;          // Ostrácia: +2 s de ar por nível
        public int nakedGuardLevel;   // Ostrácia: aguenta 1 golpe sem concha
        public int algaeLevel;        // Ostrácia: começa a run com algas
        public bool tampinhaUnlocked; // Dona Lagosta
        public bool espiralUnlocked;  // Dona Lagosta
        public bool lataUnlocked;     // Dona Lagosta, depois de achar a Lata numa run
        public bool lampadaUnlocked;  // Dona Lagosta, depois de achar a Lâmpada numa run
        public bool lataSeen;
        public bool lampadaSeen;
        public ShellType startShell = ShellType.Comum;
        public bool introSeen;
        public bool firstShellCutsceneSeen;
        public bool abyssCutsceneSeen;
        public bool colossoDefeated;
        public int victories;
        public int runs;
        public int bestRoom;          // maior número de salas vencidas numa run (todas as zonas)
        public int bestZone;          // 1 Recife, 2 Cidade, 3 Fossa
    }

    public static class Progress
    {
        // O teste automático usa outra chave para não apagar o progresso de quem joga.
        public static string Key = "mare-baixa-progresso";

        static SaveData data;
        public static SaveData Data => data ??= Load();

        static SaveData Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new SaveData();
            try { return JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
            catch (ArgumentException) { return new SaveData(); }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            data = new SaveData();
            Save();
        }

        public static float MaxAir => GameRules.BaseAir + Data.airLevel * GameRules.AirPerUpgrade;

        public static bool IsUnlocked(ShellType type) => type switch
        {
            ShellType.Comum => true,
            ShellType.Tampinha => Data.tampinhaUnlocked,
            ShellType.Espiral => Data.espiralUnlocked,
            ShellType.Lata => Data.lataUnlocked,
            ShellType.Lampada => Data.lampadaUnlocked,
            _ => false,
        };
    }
}
