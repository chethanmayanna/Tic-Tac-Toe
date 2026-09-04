using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public class ScoreboardController : ControllerBase
{
    private readonly GameService _gameService;

    public ScoreboardController(GameService gameService)
    {
        _gameService = gameService;
    }

    // ============================================================
    // GET /api/scoreboard
    // ============================================================

    [HttpGet]
    public ActionResult<Scoreboard> GetScoreboard()
    {
        return Ok(_gameService.GetScoreboard());
    }

    // ============================================================
    // POST /api/scoreboard/reset
    // ============================================================

    [HttpPost("reset")]
    public ActionResult<Scoreboard> ResetScoreboard()
    {
        return Ok(_gameService.ResetScoreboard());
    }
}