using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public class ComputerPlayerService
{
    public int GetBestMove(Player?[] board)
    {
        // 1. Winning move for O
        int winningMove = FindWinningMove(board, Player.O);

        if (winningMove != -1)
        {
            return winningMove;
        }

        // 2. Block X from winning
        int blockingMove = FindWinningMove(board, Player.X);

        if (blockingMove != -1)
        {
            return blockingMove;
        }

        // 3. Center
        if (board[4] == null)
        {
            return 4;
        }

        // 4. Corner
        int[] corners = { 0, 2, 6, 8 };

        foreach (int corner in corners)
        {
            if (board[corner] == null)
            {
                return corner;
            }
        }

        // 5. Any available cell
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindWinningMove(
        Player?[] board,
        Player player)
    {
        int[][] winningCombinations =
        {
            new[] { 0, 1, 2 },
            new[] { 3, 4, 5 },
            new[] { 6, 7, 8 },

            new[] { 0, 3, 6 },
            new[] { 1, 4, 7 },
            new[] { 2, 5, 8 },

            new[] { 0, 4, 8 },
            new[] { 2, 4, 6 }
        };

        foreach (var combination in winningCombinations)
        {
            int playerCount = 0;
            int emptyCell = -1;

            foreach (int cell in combination)
            {
                if (board[cell] == player)
                {
                    playerCount++;
                }
                else if (board[cell] == null)
                {
                    emptyCell = cell;
                }
            }

            if (playerCount == 2 && emptyCell != -1)
            {
                return emptyCell;
            }
        }

        return -1;
    }
}