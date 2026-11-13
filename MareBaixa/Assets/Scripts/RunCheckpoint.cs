using System;

namespace MareBaixa
{
    // Salva o roteiro sorteado inteiro, para continuar exatamente a mesma jornada depois de fechar.
    // Só há bancos nas salas de abrigo, sem tesouros: reconstruir o abrigo não duplica recompensas.
    [Serializable]
    public class RunCheckpoint
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public string[] rooms;
        public Zone[] zones;
        public int roomIndex;
        public float x, y;
        public ShellType shell;
        public WeaponType weapon;
        public int algae, nakedGuards;

        public bool Valid => version == CurrentVersion && rooms != null && zones != null
            && rooms.Length == zones.Length && rooms.Length > 0 && rooms.Length <= 100
            && roomIndex >= 0 && roomIndex < rooms.Length && zones[roomIndex] == Zone.Bolha
            && (int)shell >= (int)ShellType.Comum && (int)shell <= (int)ShellType.Lampada
            && Weapons.Valid(weapon) && !float.IsNaN(x) && !float.IsInfinity(x)
            && !float.IsNaN(y) && !float.IsInfinity(y);
    }
}
