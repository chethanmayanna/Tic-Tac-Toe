namespace TicTacToe.Api.Models;

public class Game
{
    public Guid Id { get; set; }

    // 9 cells: 0 to 8
    public Player?[] Board { get; set; } = new Player?[9];

    public Player CurrentPlayer { get; set; } = Player.X;

    public GameMode Mode { get; set; }

    public GameStatus Status { get; set; } = GameStatus.InProgress;

    public Player? Winner { get; set; }

    public List<int> WinningCells { get; set; } = new();

    public List<Move> Moves { get; set; } = new();

    // Prevent scoreboard from being updated more than once
    public bool ScoreboardUpdated { get; set; }
}