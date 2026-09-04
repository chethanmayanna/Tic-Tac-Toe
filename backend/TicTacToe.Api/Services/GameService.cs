using TicTacToe.Api.Models;
using System.Linq;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public class GameService
{
    private readonly ComputerPlayerService _computerPlayerService;

    private readonly Dictionary<Guid, Game> _games = new();

    private readonly Scoreboard _scoreboard = new();

    private readonly object _lock = new();

    public GameService(ComputerPlayerService computerPlayerService)
    {
        _computerPlayerService = computerPlayerService;
    }

    // ============================================================
    // CREATE GAME
    // ============================================================

    public Game CreateGame(GameMode mode)
    {
        lock (_lock)
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Mode = mode,
                CurrentPlayer = Player.X,
                Status = GameStatus.InProgress,
                Winner = null,
                WinningCells = new List<int>(),
                Moves = new List<Move>(),
                Board = new Player?[9]
            };

            _games[game.Id] = game;

            return game;
        }
    }

    // ============================================================
    // GET GAME
    // ============================================================

    public Game GetGame(Guid gameId)
    {
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out var game))
            {
                throw new KeyNotFoundException("Game not found.");
            }

            return game;
        }
    }

    // ============================================================
    // MAKE MOVE
    // ============================================================

    public Game MakeMove(
        Guid gameId,
        Player player,
        int row,
        int column)
    {
        lock (_lock)
        {
            var game = GetGame(gameId);

            ValidateMove(game, player, row, column);

            int cellIndex = row * 3 + column;

            // Place player's mark
            game.Board[cellIndex] = player;

            // Add move history
            game.Moves.Add(new Move
            {
                MoveNumber = game.Moves.Count + 1,
                Player = player,
                Row = row,
                Column = column
            });

            // Check whether game has ended
            EvaluateGame(game);

            // If game ended, do not allow another move
            if (game.Status != GameStatus.InProgress)
            {
                UpdateScoreboard(game);
                return game;
            }

            // ====================================================
            // COMPUTER MODE
            // Human = X
            // Computer = O
            // ====================================================

            if (game.Mode == GameMode.Computer &&
                player == Player.X)
            {
                MakeComputerMove(game);

                return game;
            }

            // ====================================================
            // TWO PLAYER MODE
            // ====================================================

            game.CurrentPlayer = GetOpponent(player);

            return game;
        }
    }

    // ============================================================
    // VALIDATE MOVE
    // ============================================================

    private void ValidateMove(
        Game game,
        Player player,
        int row,
        int column)
    {
        // Game already completed
        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Game is already completed.");
        }

        // Invalid row
        if (row < 0 || row > 2)
        {
            throw new ArgumentException(
                "Row must be between 0 and 2.");
        }

        // Invalid column
        if (column < 0 || column > 2)
        {
            throw new ArgumentException(
                "Column must be between 0 and 2.");
        }

        // Wrong player's turn
        if (player != game.CurrentPlayer)
        {
            throw new InvalidOperationException(
                $"It is {game.CurrentPlayer}'s turn.");
        }

        // Cell already occupied
        int cellIndex = row * 3 + column;

        if (game.Board[cellIndex] != null)
        {
            throw new InvalidOperationException(
                "Cell is already occupied.");
        }

        // In computer mode only X can be sent by frontend
        if (game.Mode == GameMode.Computer &&
            player != Player.X)
        {
            throw new InvalidOperationException(
                "In computer mode, the human player is X.");
        }
    }

    // ============================================================
    // COMPUTER MOVE
    // ============================================================

    private void MakeComputerMove(Game game)
    {
        // Computer is O
        int cellIndex = _computerPlayerService
            .GetBestMove(game.Board);

        if (cellIndex == -1)
        {
            return;
        }

        int row = cellIndex / 3;
        int column = cellIndex % 3;

        // Place O
        game.Board[cellIndex] = Player.O;

        // Add computer move to history
        game.Moves.Add(new Move
        {
            MoveNumber = game.Moves.Count + 1,
            Player = Player.O,
            Row = row,
            Column = column
        });

        // Check whether computer won/draw
        EvaluateGame(game);

        if (game.Status != GameStatus.InProgress)
        {
            UpdateScoreboard(game);
            return;
        }

        // Give control back to human X
        game.CurrentPlayer = Player.X;
    }

    // ============================================================
    // EVALUATE GAME
    // ============================================================

    private void EvaluateGame(Game game)
    {
        int[][] winningCombinations =
        {
            // Rows
            new[] { 0, 1, 2 },
            new[] { 3, 4, 5 },
            new[] { 6, 7, 8 },

            // Columns
            new[] { 0, 3, 6 },
            new[] { 1, 4, 7 },
            new[] { 2, 5, 8 },

            // Diagonals
            new[] { 0, 4, 8 },
            new[] { 2, 4, 6 }
        };

        foreach (var combination in winningCombinations)
        {
            var a = game.Board[combination[0]];
            var b = game.Board[combination[1]];
            var c = game.Board[combination[2]];

            if (a != null && a == b && b == c)
            {
                game.Status = GameStatus.Won;
                game.Winner = a;

                game.WinningCells = new List<int>
                {
                    combination[0],
                    combination[1],
                    combination[2]
                };

                return;
            }
        }

        // No winner + all cells filled = draw
        if (game.Board.All(cell => cell != null))
        {
            game.Status = GameStatus.Draw;
            game.Winner = null;
            game.WinningCells.Clear();

            return;
        }

        game.Status = GameStatus.InProgress;
        game.Winner = null;
        game.WinningCells.Clear();
    }

    // ============================================================
    // UPDATE SCOREBOARD
    // ============================================================

    private void UpdateScoreboard(Game game)
    {
        // Very important:
        // Scoreboard should only be updated once per game.

        if (game.ScoreboardUpdated)
        {
            return;
        }

        if (game.Status == GameStatus.Won)
        {
            if (game.Winner == Player.X)
            {
                _scoreboard.XWins++;
            }
            else if (game.Winner == Player.O)
            {
                _scoreboard.OWins++;
            }
        }
        else if (game.Status == GameStatus.Draw)
        {
            _scoreboard.Draws++;
        }

        game.ScoreboardUpdated = true;
    }

    // ============================================================
    // GET SCOREBOARD
    // ============================================================

    public Scoreboard GetScoreboard()
    {
        lock (_lock)
        {
            return new Scoreboard
            {
                XWins = _scoreboard.XWins,
                OWins = _scoreboard.OWins,
                Draws = _scoreboard.Draws
            };
        }
    }

    // ============================================================
    // RESET SCOREBOARD
    // ============================================================

    public Scoreboard ResetScoreboard()
    {
        lock (_lock)
        {
            _scoreboard.XWins = 0;
            _scoreboard.OWins = 0;
            _scoreboard.Draws = 0;

            return GetScoreboard();
        }
    }

    // ============================================================
    // UNDO
    // ============================================================

    public Game Undo(Guid gameId)
    {
        lock (_lock)
        {
            var game = GetGame(gameId);

            // We are choosing the assignment's simpler Option A:
            // Undo is disabled after game completion.
            if (game.Status != GameStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "Undo is not available after the game is completed.");
            }

            if (game.Moves.Count == 0)
            {
                throw new InvalidOperationException(
                    "There are no moves to undo.");
            }

            if (game.Mode == GameMode.Computer)
            {
                // Computer mode:
                // remove O + previous X

                if (game.Moves.Count >= 2)
                {
                    RemoveLastMove(game);
                    RemoveLastMove(game);
                }
                else
                {
                    RemoveLastMove(game);
                }

                game.CurrentPlayer = Player.X;
            }
            else
            {
                // Two player:
                // remove only latest move

                var lastMove = game.Moves.Last();

                int index = lastMove.Row * 3 + lastMove.Column;

                game.Board[index] = null;

                game.Moves.RemoveAt(game.Moves.Count - 1);

                game.CurrentPlayer = lastMove.Player;
            }

            // Recalculate state after undo
            EvaluateGame(game);

            return game;
        }
    }

    // ============================================================
    // REMOVE LAST MOVE
    // ============================================================

    private void RemoveLastMove(Game game)
    {
        var lastMove = game.Moves.Last();

        int cellIndex = lastMove.Row * 3 + lastMove.Column;

        game.Board[cellIndex] = null;

        game.Moves.RemoveAt(game.Moves.Count - 1);
    }

    // ============================================================
    // RESET GAME
    // ============================================================

    public Game ResetGame(Guid gameId)
    {
        lock (_lock)
        {
            var oldGame = GetGame(gameId);

            var newGame = new Game
            {
                Id = Guid.NewGuid(),
                Mode = oldGame.Mode,
                CurrentPlayer = Player.X,
                Status = GameStatus.InProgress,
                Winner = null,
                WinningCells = new List<int>(),
                Moves = new List<Move>(),
                Board = new Player?[9]
            };

            // Preserve scoreboard.
            _games[newGame.Id] = newGame;

            return newGame;
        }
    }

    // ============================================================
    // HELPER
    // ============================================================

    private static Player GetOpponent(Player player)
    {
        return player == Player.X
            ? Player.O
            : Player.X;
    }
}