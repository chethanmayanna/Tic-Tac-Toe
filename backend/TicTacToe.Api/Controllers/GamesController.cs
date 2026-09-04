using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly GameService _gameService;

    public GamesController(GameService gameService)
    {
        _gameService = gameService;
    }

    // ============================================================
    // POST /api/games
    // Create a new game
    // ============================================================

    [HttpPost]
    public ActionResult<Game> CreateGame(
        [FromBody] CreateGameRequest request)
    {
        var game = _gameService.CreateGame(request.Mode);

        return Ok(game);
    }

    // ============================================================
    // GET /api/games/{id}
    // Get current game state
    // ============================================================

    [HttpGet("{id:guid}")]
    public ActionResult<Game> GetGame(Guid id)
    {
        try
        {
            return Ok(_gameService.GetGame(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ============================================================
    // POST /api/games/{id}/moves
    // Make a move
    // ============================================================

    [HttpPost("{id:guid}/moves")]
    public ActionResult<Game> MakeMove(
        Guid id,
        [FromBody] MakeMoveRequest request)
    {
        try
        {
            var game = _gameService.MakeMove(
                id,
                request.Player,
                request.Row,
                request.Column);

            return Ok(game);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // POST /api/games/{id}/undo
    // Undo move
    // ============================================================

    [HttpPost("{id:guid}/undo")]
    public ActionResult<Game> Undo(Guid id)
    {
        try
        {
            return Ok(_gameService.Undo(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // POST /api/games/{id}/reset
    // Reset game
    // ============================================================

    [HttpPost("{id:guid}/reset")]
    public ActionResult<Game> Reset(Guid id)
    {
        try
        {
            return Ok(_gameService.ResetGame(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}


// ================================================================
// REQUEST MODELS
// ================================================================

public class CreateGameRequest
{
    public GameMode Mode { get; set; } = GameMode.TwoPlayer;
}

public class MakeMoveRequest
{
    public Player Player { get; set; }

    public int Row { get; set; }

    public int Column { get; set; }
}