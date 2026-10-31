using System;

namespace Kael
{
    /// <summary>Puntaje global de la partida. Se reinicia al empezar el nivel.</summary>
    public static class ScoreManager
    {
        public static int Score { get; private set; }
        public static event Action<int> OnScoreChanged;

        public static void Reset()
        {
            Score = 0;
            OnScoreChanged?.Invoke(Score);
        }

        public static void Add(int points)
        {
            if (points == 0) return;
            Score += points;
            OnScoreChanged?.Invoke(Score);
        }
    }
}
