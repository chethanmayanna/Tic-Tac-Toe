using System;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;
using Xunit;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    [Fact]
    public void ValidMoveUpdatesBoardHistoryAndTurn()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        var updated = service.MakeMove(game.Id, Player.X, 0, 1);

        Assert.Equal(Player.X, updated.Board[1]);
        Assert.Equal(Player.O, updated.CurrentPlayer);
        Assert.Single(updated.Moves);
        Assert.Equal((0, 1), (updated.Moves[0].Row, updated.Moves[0].Column));
    }

    [Fact]
    public void InvalidMoveDoesNotChangeState()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Assert.Throws<InvalidOperationException>(() => service.MakeMove(game.Id, Player.O, 0, 0));
        Assert.Throws<ArgumentException>(() => service.MakeMove(game.Id, Player.X, 3, 0));
        service.MakeMove(game.Id, Player.X, 0, 0);
        Assert.Throws<InvalidOperationException>(() => service.MakeMove(game.Id, Player.O, 0, 0));

        var state = service.GetGame(game.Id);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Single(state.Moves);
    }

    [Fact]
    public void RowWinHighlightsCellsAndUpdatesScoreboardOnce()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 1, 0);
        Play(service, game.Id, Player.X, 0, 1);
        Play(service, game.Id, Player.O, 1, 1);
        var won = service.MakeMove(game.Id, Player.X, 0, 2);

        Assert.Equal(GameStatus.Won, won.Status);
        Assert.Equal(Player.X, won.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, won.WinningCells);
        Assert.Equal(1, service.GetScoreboard().XWins);
        Assert.Throws<InvalidOperationException>(() => service.MakeMove(game.Id, Player.O, 2, 0));
        Assert.Equal(1, service.GetScoreboard().XWins);
    }

    [Fact]
    public void ColumnWinIsDetected()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 0, 1);
        Play(service, game.Id, Player.X, 1, 0);
        Play(service, game.Id, Player.O, 1, 1);
        var won = service.MakeMove(game.Id, Player.X, 2, 0);

        Assert.Equal(GameStatus.Won, won.Status);
        Assert.Equal(new[] { 0, 3, 6 }, won.WinningCells);
    }

    [Fact]
    public void DiagonalWinIsDetected()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);

        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 0, 1);
        Play(service, game.Id, Player.X, 1, 1);
        Play(service, game.Id, Player.O, 0, 2);
        var won = service.MakeMove(game.Id, Player.X, 2, 2);

        Assert.Equal(GameStatus.Won, won.Status);
        Assert.Equal(new[] { 0, 4, 8 }, won.WinningCells);
    }

    [Fact]
    public void FullBoardWithoutWinnerIsDraw()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        var moves = new (Player Player, int Row, int Column)[]
        {
            (Player.X, 0, 0), (Player.O, 0, 1), (Player.X, 0, 2),
            (Player.O, 1, 1), (Player.X, 1, 0), (Player.O, 1, 2),
            (Player.X, 2, 1), (Player.O, 2, 0), (Player.X, 2, 2)
        };

        Game final = game;
        foreach (var move in moves)
        {
            final = service.MakeMove(game.Id, move.Player, move.Row, move.Column);
        }

        Assert.Equal(GameStatus.Draw, final.Status);
        Assert.Null(final.Winner);
        Assert.Equal(1, service.GetScoreboard().Draws);
    }

    [Fact]
    public void ResetGameStartsFreshSessionAndPreservesScoreboard()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 1, 0);
        Play(service, game.Id, Player.X, 0, 1);
        Play(service, game.Id, Player.O, 1, 1);
        service.MakeMove(game.Id, Player.X, 0, 2);

        var reset = service.ResetGame(game.Id);

        Assert.NotEqual(game.Id, reset.Id);
        Assert.All(reset.Board, cell => Assert.Null(cell));
        Assert.Empty(reset.Moves);
        Assert.Equal(Player.X, reset.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, reset.Status);
        Assert.Equal(1, service.GetScoreboard().XWins);
    }

    [Fact]
    public void UndoTwoPlayerRemovesOnlyLatestMove()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 1, 1);

        var undone = service.Undo(game.Id);

        Assert.Equal(Player.X, undone.Board[0]);
        Assert.Null(undone.Board[4]);
        Assert.Equal(Player.O, undone.CurrentPlayer);
        Assert.Single(undone.Moves);
    }

    [Fact]
    public void UndoComputerRemovesHumanAndComputerPair()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.Computer);
        service.MakeMove(game.Id, Player.X, 0, 0);

        var undone = service.Undo(game.Id);

        Assert.All(undone.Board, cell => Assert.Null(cell));
        Assert.Empty(undone.Moves);
        Assert.Equal(Player.X, undone.CurrentPlayer);
    }

    [Fact]
    public void ResetScoreboardClearsAllResults()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 1, 0);
        Play(service, game.Id, Player.X, 0, 1);
        Play(service, game.Id, Player.O, 1, 1);
        service.MakeMove(game.Id, Player.X, 0, 2);

        var reset = service.ResetScoreboard();

        Assert.Equal(0, reset.XWins);
        Assert.Equal(0, reset.OWins);
        Assert.Equal(0, reset.Draws);
    }

    [Fact]
    public void ComputerUsesRequiredMovePriority()
    {
        var computer = new ComputerPlayerService();

        Assert.Equal(2, computer.GetBestMove(new Player?[] { Player.O, Player.O, null, null, null, null, null, null, null }));
        Assert.Equal(2, computer.GetBestMove(new Player?[] { Player.X, Player.X, null, null, null, null, null, null, null }));
        Assert.Equal(4, computer.GetBestMove(new Player?[] { null, null, null, null, null, null, null, null, null }));
        Assert.Equal(0, computer.GetBestMove(new Player?[] { null, null, null, null, Player.O, null, null, null, null }));
        Assert.Equal(1, computer.GetBestMove(new Player?[] { Player.X, null, Player.O, Player.O, Player.X, Player.X, Player.X, Player.O, Player.O }));
    }

    [Fact]
    public void MoveAfterCompletionIsRejected()
    {
        var service = CreateService();
        var game = service.CreateGame(GameMode.TwoPlayer);
        Play(service, game.Id, Player.X, 0, 0);
        Play(service, game.Id, Player.O, 1, 0);
        Play(service, game.Id, Player.X, 0, 1);
        Play(service, game.Id, Player.O, 1, 1);
        service.MakeMove(game.Id, Player.X, 0, 2);

        Assert.Throws<InvalidOperationException>(() => service.MakeMove(game.Id, Player.O, 2, 2));
    }

    private static GameService CreateService()
    {
        return new GameService(new ComputerPlayerService());
    }

    private static void Play(GameService service, Guid gameId, Player player, int row, int column)
    {
        service.MakeMove(gameId, player, row, column);
    }
}