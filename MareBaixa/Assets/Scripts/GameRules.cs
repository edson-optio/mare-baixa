using UnityEngine;

namespace MareBaixa
{
    // Números do jogo num lugar só. Os valores de conchas, maré e ar seguem a página 7 do GDD
    // e são o ponto de partida para ajustar nos testes.
    public static class GameRules
    {
        public const string MenuScene = "Menu";
        public const string VillageScene = "Vila";
        public const string RunScene = "Run";

        // Câmera: blocos de 32 px desenhados com 64 px de tela em 720p (escala 2x, 3x em 1080p).
        public const float CameraSize = 5.625f;
        public const float TargetAspect = 16f / 9f;

        // ---------------------------------------------------------------- Casco
        public const float MoveSpeed = 6.5f;
        public const float NakedSpeedFactor = 1.15f;   // sem concha é mais rápido (GDD p. 3)
        public const float Acceleration = 70f;
        public const float JumpSpeed = 14f;
        public const float GravityScale = 4f;
        public const float MaxFallSpeed = 18f;
        public const float CoyoteTime = .1f;
        public const float JumpBufferTime = .12f;

        public const float AttackCooldown = .32f;
        public const float AttackActiveTime = .12f;
        public const int AttackDamage = 1;
        public const float DodgeTime = .3f;
        public const float DodgeSpeed = 11f;
        public const float DodgeCooldown = .55f;
        public const float SpinCooldown = 1.1f;
        public const float SpinActiveTime = .3f;
        public const float SpinRadius = 1.4f;
        public const int SpinDamage = 2;
        public const float ChargeTime = .35f;           // investida da Lata
        public const float ChargeSpeed = 13f;
        public const float ChargeCooldown = 1.2f;
        public const int ChargeDamage = 2;
        public const float ChargeStun = 1.2f;
        public const float FlashCooldown = 4f;          // clarão da Lâmpada
        public const float FlashRadius = 4.5f;
        public const float FlashStun = 2f;
        public const float LightRadius = 2.5f;          // luz de Casco no escuro
        public const float LampLightRadius = 4.5f;
        public const float InvulnerableTime = 1f;

        // Na água, Casco anda e pula 30% mais devagar e ataca mais devagar (GDD p. 7).
        public const float WaterMoveFactor = .7f;
        public const float WaterGravityFactor = .45f;
        public const float WaterJumpFactor = .75f;
        public const float WaterMaxFallSpeed = 6f;
        public const float WaterAttackFactor = 1.3f;

        // ---------------------------------------------------------------- Ar, algas e pérolas
        public const float BaseAir = 8f;
        public const float AirPerUpgrade = 2f;
        public const float AirRefillPerSecond = 4f;
        public const int MaxAlgae = 2;

        // ---------------------------------------------------------------- Maré (blocos por segundo)
        public const float ReefTideSpeed = .16f;      // lenta no Recife, média na Cidade, rápida na Fossa (GDD p. 5)
        public const float CityTideSpeed = .25f;
        public const float AbyssTideSpeed = .36f;
        public const float AbyssDarkness = .9f;
        public const float TideStartBelowFloor = .5f;

        // ---------------------------------------------------------------- Estrutura da run
        public const string ReefName = "Recife Raso";
        public const string CityName = "Cidade Afundada";
        public const string AbyssName = "Fossa Abissal";
        public const string BubbleName = "Bolha de Ar";
        public const int ReefRandomRooms = 4;          // 4 a 5 salas sorteadas + sala de concha (GDD p. 4)
        public const int CityRandomRooms = 5;          // 5 a 6 salas + Ermitão Colosso
        public const int AbyssRandomRooms = 3;         // 3 a 4 salas escuras + arena do Polvo

        // ---------------------------------------------------------------- Cores (paleta do LEIA-ME)
        public static readonly Color Ink = new Color32(16, 27, 45, 255);
        public static readonly Color DeepSea = new Color32(18, 61, 73, 255);
        public static readonly Color Orange = new Color32(244, 119, 36, 255);
        public static readonly Color Sand = new Color32(230, 189, 133, 255);
        public static readonly Color Cream = new Color32(255, 240, 206, 255);
        public static readonly Color Turquoise = new Color32(66, 181, 172, 255);
        public static readonly Color Violet = new Color32(112, 80, 173, 255);
        public static readonly Color Pearl = new Color32(244, 236, 255, 255);
        public static readonly Color Water = new Color32(46, 150, 170, 105);
        public static readonly Color WaterSurface = new Color32(190, 245, 240, 170);
        public static readonly Color Danger = new Color32(232, 88, 72, 255);
        public static readonly Color CityGreen = new Color32(112, 140, 108, 255);
        public static readonly Color AbyssBlue = new Color32(20, 24, 58, 255);
        public static readonly Color Biolum = new Color32(170, 120, 255, 255);
    }
}
