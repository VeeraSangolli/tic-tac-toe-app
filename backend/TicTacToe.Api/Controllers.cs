using Microsoft.AspNetCore.Mvc;
namespace TicTacToe;
public record CreateGameRequest(string? Mode);
public record MoveRequest(string? Player, int? Row, int? Column);
[ApiController, Route("api/games")]
public sealed class GameController(GameService games) : ControllerBase
{
    [HttpPost] public IActionResult Create(CreateGameRequest request)
    {
        var game = games.Create(request.Mode);
        return CreatedAtAction(nameof(Get), new { id = game.GameId }, game);
    }
    [HttpGet("{id:guid}")] public GameState Get(Guid id) => games.Get(id);
    [HttpPost("{id:guid}/moves")] public GameState Move(Guid id, MoveRequest request) => games.MakeMove(id, request.Player, request.Row, request.Column);
    [HttpPost("{id:guid}/undo")] public GameState Undo(Guid id) => games.Undo(id);
    [HttpPost("{id:guid}/reset")] public GameState Reset(Guid id) => games.Reset(id);
}
[ApiController, Route("api/scoreboard")]
public sealed class ScoreboardController(ScoreboardService scores) : ControllerBase
{
    [HttpGet] public Scoreboard Get() => scores.Get();
    [HttpPost("reset")] public Scoreboard Reset() => scores.Reset();
}
