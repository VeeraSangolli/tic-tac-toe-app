namespace TicTacToe;

public record Position(int Row, int Column);
public record Move(int MoveNumber, string Player, int Row, int Column);
public record GameState(Guid GameId, string?[][] Board, string CurrentPlayer, string Mode,
    string Status, string? Winner, Position[] WinningCells, Move[] MoveHistory);
public record Scoreboard(int XWins, int OWins, int Draws);
public sealed class Game(string mode)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Mode { get; } = mode;
    public string?[] Board { get; } = new string?[9];
    public string CurrentPlayer { get; set; } = "X";
    public string Status { get; set; } = "InProgress";
    public string? Winner { get; set; }
    public Position[] WinningCells { get; set; } = [];
    public List<Move> History { get; } = [];
    public bool ResultRecorded { get; set; }
    public GameState Snapshot() => new(Id, [Board[..3], Board[3..6], Board[6..9]], CurrentPlayer,
        Mode, Status, Winner, [.. WinningCells], [.. History]);
}
public sealed class GameException(string code, string message, int status = 409) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
public static class GameRules
{
    private static readonly int[][] Lines = [[0,1,2],[3,4,5],[6,7,8],[0,3,6],[1,4,7],[2,5,8],[0,4,8],[2,4,6]];
    public static int[] WinningLine(string?[] board) => Lines.FirstOrDefault(line =>
        board[line[0]] != null && line.All(i => board[i] == board[line[0]])) ?? [];
}
public sealed class ComputerMoveStrategy
{
    public int Select(string?[] board)
    {
        foreach (var player in new[] { "O", "X" })
            for (var i = 0; i < 9; i++)
            {
                if (board[i] != null) continue;
                var simulation = (string?[])board.Clone();
                simulation[i] = player;
                if (GameRules.WinningLine(simulation).Length > 0) return i;
            }
        return new[] { 4, 0, 2, 6, 8, 1, 3, 5, 7 }.First(i => board[i] == null);
    }
}
