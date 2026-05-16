namespace TripleMatchFrenzy.Core
{
    /// <summary>High-level states the game can occupy at any given moment.</summary>
    public enum GameState
    {
        /// <summary>Initial asset loading phase before the board is ready.</summary>
        Loading,

        /// <summary>Board is visible and waiting for the player to tap a tile.</summary>
        Idle,

        /// <summary>A tile or tray animation is in progress; input is blocked.</summary>
        Animating,

        /// <summary>The board is being evaluated for matches or end conditions.</summary>
        Checking,

        /// <summary>All tiles have been cleared — the player has won.</summary>
        Won,

        /// <summary>The tray is full with no valid moves — the player has lost.</summary>
        Lost
    }
}
