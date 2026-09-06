using System.Collections.Concurrent;
namespace TicTacToe;

// One lock makes game mutations, snapshots, and score updates a single atomic operation.
// Lock ordering stays simple for this deliberately small, process-local application.
public sealed class GameStore
{
    public object Sync { get; } = new();
    public ConcurrentDictionary<Guid, Game> Games { get; } = new();
    public Scoreboard Score { get; set; } = new(0, 0, 0);
}
public sealed class ScoreboardService(GameStore store)
{
    public Scoreboard Get() { lock (store.Sync) return store.Score; }
    public Scoreboard Reset() { lock (store.Sync) return store.Score = new(0, 0, 0); }
    public void Record(Game game)
    {
        lock (store.Sync)
        {
            if (game.ResultRecorded || game.Status == "InProgress") return;
            var score = store.Score;
            store.Score = new(score.XWins + (game.Winner == "X" ? 1 : 0),
                score.OWins + (game.Winner == "O" ? 1 : 0), score.Draws + (game.Status == "Draw" ? 1 : 0));
            game.ResultRecorded = true;
        }
    }
}
public sealed class GameService(GameStore store, ScoreboardService scores, ComputerMoveStrategy computer)
{
    private Game Find(Guid id) => store.Games.TryGetValue(id, out var game) ? game :
        throw new GameException("GAME_NOT_FOUND", "This game is no longer available. Start a new game.", 404);
    public GameState Create(string? mode)
    {
        if (mode is not ("TwoPlayer" or "Computer"))
            throw new GameException("INVALID_GAME_MODE", "Choose TwoPlayer or Computer.", 400);
        lock (store.Sync)
        {
            var game = new Game(mode);
            store.Games[game.Id] = game;
            return game.Snapshot();
        }
    }
    public GameState Get(Guid id) { lock (store.Sync) return Find(id).Snapshot(); }
    public GameState MakeMove(Guid id, string? player, int? row, int? column)
    {
        lock (store.Sync)
        {
            var game = Find(id);
            if (game.Status != "InProgress") throw new GameException("GAME_ALREADY_COMPLETED", "The game has ended. Reset the game to play again.");
            if (player is not ("X" or "O")) throw new GameException("INVALID_REQUEST", "Player must be X or O.", 400);
            if (player != game.CurrentPlayer || (game.Mode == "Computer" && player != "X"))
                throw new GameException("WRONG_PLAYER", "It is not that player's turn.");
            if (row is null || column is null) throw new GameException("INVALID_REQUEST", "Row and column are required.", 400);
            if (row < 0 || row > 2 || column < 0 || column > 2)
                throw new GameException("INVALID_POSITION", "Row and column must be between 0 and 2.", 400);
            var index = row.Value * 3 + column.Value;
            if (game.Board[index] != null) throw new GameException("CELL_OCCUPIED", "The selected cell is already occupied.");
            Apply(game, index);
            if (game.Mode == "Computer" && game.Status == "InProgress") Apply(game, computer.Select(game.Board));
            return game.Snapshot();
        }
    }
    private void Apply(Game game, int index)
    {
        var player = game.CurrentPlayer;
        game.Board[index] = player;
        game.History.Add(new(game.History.Count + 1, player, index / 3, index % 3));
        var line = GameRules.WinningLine(game.Board);
        if (line.Length > 0)
        {
            game.Status = "Won";
            game.Winner = player;
            game.WinningCells = line.Select(i => new Position(i / 3, i % 3)).ToArray();
        }
        else if (game.Board.All(cell => cell != null)) game.Status = "Draw";
        else game.CurrentPlayer = player == "X" ? "O" : "X";
        scores.Record(game);
    }
    public GameState Undo(Guid id)
    {
        lock (store.Sync)
        {
            var game = Find(id);
            if (game.Status != "InProgress" || game.History.Count == 0)
                throw new GameException("UNDO_NOT_AVAILABLE", "Undo is available only during a game with moves.");
            var count = game.Mode == "Computer" ? 2 : 1;
            for (var i = 0; i < count; i++)
            {
                var move = game.History[^1];
                game.Board[move.Row * 3 + move.Column] = null;
                game.History.RemoveAt(game.History.Count - 1);
                game.CurrentPlayer = move.Player;
            }
            return game.Snapshot();
        }
    }
    public GameState Reset(Guid id)
    {
        lock (store.Sync)
        {
            var game = Find(id);
            Array.Clear(game.Board);
            game.History.Clear();
            game.CurrentPlayer = "X";
            game.Status = "InProgress";
            game.Winner = null;
            game.WinningCells = [];
            game.ResultRecorded = false;
            return game.Snapshot();
        }
    }
}
