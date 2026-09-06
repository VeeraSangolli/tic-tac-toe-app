using System.Text.Json;
using TicTacToe;
using Xunit;
namespace TicTacToe.Tests;
public class GameTests
{
    private readonly GameStore store = new();
    private readonly GameService games;
    private readonly ScoreboardService scores;
    public GameTests() { scores = new(store); games = new(store, scores, new()); }
    private GameState Play(params int[] cells)
    {
        var game = games.Create("TwoPlayer");
        foreach (var cell in cells) game = games.MakeMove(game.GameId, game.CurrentPlayer, cell / 3, cell % 3);
        return game;
    }
    [Fact] public void NewGameHasNineEmptyCellsAndXStarts()
    {
        var game = games.Create("TwoPlayer");
        Assert.Equal(9, game.Board.SelectMany(r => r).Count(c => c == null));
        Assert.Equal("X", game.CurrentPlayer); Assert.Equal("InProgress", game.Status);
        Assert.Empty(game.MoveHistory); Assert.Null(game.Winner); Assert.Empty(game.WinningCells);
    }
    [Fact] public void ValidMovesSwitchTurnsAndRecordPositions()
    {
        var game = Play(1, 4);
        Assert.Equal("X", game.Board[0][1]); Assert.Equal("O", game.Board[1][1]);
        Assert.Equal("X", game.CurrentPlayer); Assert.Equal(new Move(2,"O",1,1), game.MoveHistory[1]);
    }
    [Theory]
    [InlineData("X",0,0,"CELL_OCCUPIED")]
    [InlineData("O",1,1,"WRONG_PLAYER")]
    [InlineData("Q",1,1,"INVALID_REQUEST")]
    [InlineData("X",-1,1,"INVALID_POSITION")]
    [InlineData("X",1,3,"INVALID_POSITION")]
    [InlineData("X",null,1,"INVALID_REQUEST")]
    [InlineData("X",1,null,"INVALID_REQUEST")]
    public void InvalidMoveChangesNothing(string player, int? row, int? column, string code)
    {
        var game = Play(0,4); var before = JsonSerializer.Serialize(game);
        Assert.Equal(code,Assert.Throws<GameException>(() => games.MakeMove(game.GameId,player,row,column)).Code);
        Assert.Equal(before,JsonSerializer.Serialize(games.Get(game.GameId)));
        Assert.Equal(new Scoreboard(0,0,0),scores.Get());
    }
    [Theory]
    [InlineData(0,3,1,4,2)] [InlineData(3,0,4,1,5)] [InlineData(6,0,7,1,8)]
    [InlineData(0,1,3,2,6)] [InlineData(1,0,4,2,7)] [InlineData(2,0,5,1,8)]
    [InlineData(0,1,4,2,8)] [InlineData(2,0,4,1,6)]
    public void DetectsAllEightWinningLines(int a,int b,int c,int d,int e)
    {
        var game=Play(a,b,c,d,e);
        Assert.Equal("Won",game.Status); Assert.Equal("X",game.Winner);
        Assert.Equal(new[]{a,c,e}.Order(),game.WinningCells.Select(p=>p.Row*3+p.Column).Order());
        Assert.Equal(new Scoreboard(1,0,0),scores.Get());
    }
    [Fact] public void OWinsAreRecorded() { var game=Play(0,3,1,4,8,5); Assert.Equal("O",game.Winner); Assert.Equal(new Scoreboard(0,1,0),scores.Get()); }
    [Fact] public void DrawIsRecordedExactlyOnceAndTerminalUndoIsRejected()
    {
        var game=Play(0,1,2,4,3,5,7,6,8);
        Assert.Equal("Draw",game.Status); Assert.Null(game.Winner); Assert.Empty(game.WinningCells);
        Assert.Throws<GameException>(()=>games.Undo(game.GameId));
        Assert.Throws<GameException>(()=>games.MakeMove(game.GameId,"X",0,0));
        games.Get(game.GameId); Assert.Equal(new Scoreboard(0,0,1),scores.Get());
    }
    [Fact] public void WonGameRejectsMovesAndUndoWithoutDoubleCounting()
    {
        var game=Play(0,3,1,4,2);
        Assert.Equal("GAME_ALREADY_COMPLETED",Assert.Throws<GameException>(()=>games.MakeMove(game.GameId,"O",2,2)).Code);
        Assert.Equal("UNDO_NOT_AVAILABLE",Assert.Throws<GameException>(()=>games.Undo(game.GameId)).Code);
        games.Get(game.GameId); Assert.Equal(new Scoreboard(1,0,0),scores.Get());
    }
    [Fact] public void ResetPreservesModeAndScoresAndAllowsAnotherScore()
    {
        var game=Play(0,3,1,4,2); var reset=games.Reset(game.GameId);
        Assert.Equal(game.GameId,reset.GameId); Assert.Equal(game.Mode,reset.Mode);
        Assert.Equal("X",reset.CurrentPlayer); Assert.Equal("InProgress",reset.Status);
        Assert.Null(reset.Winner); Assert.Empty(reset.WinningCells); Assert.Empty(reset.MoveHistory);
        Assert.All(reset.Board.SelectMany(r=>r), cell=>Assert.Null(cell));
        Assert.Equal(new Scoreboard(1,0,0),scores.Get());
        foreach(var cell in new[]{0,3,1,4,2}) reset=games.MakeMove(reset.GameId,reset.CurrentPlayer,cell/3,cell%3);
        Assert.Equal(new Scoreboard(2,0,0),scores.Get());
    }
    [Fact] public void ResetScoresLeavesActiveAndCompletedGamesUnchanged()
    {
        var completed=Play(0,3,1,4,2); var active=Play(4);
        scores.Reset();
        Assert.Equal(JsonSerializer.Serialize(active),JsonSerializer.Serialize(games.Get(active.GameId)));
        scores.Record(store.Games[completed.GameId]); Assert.Equal(new Scoreboard(0,0,0),scores.Get());
    }
    [Fact] public void UndoTwoPlayerRestoresTurnAndSequentialHistory()
    {
        var game=Play(0,4,1); game=games.Undo(game.GameId);
        Assert.Null(game.Board[0][1]); Assert.Equal("X",game.CurrentPlayer); Assert.Equal(2,game.MoveHistory.Length);
        game=games.MakeMove(game.GameId,"X",2,2); Assert.Equal(3,game.MoveHistory[^1].MoveNumber);
    }
    [Fact] public void EmptyUndoAndMissingGamesAreRejected()
    {
        Assert.Equal("UNDO_NOT_AVAILABLE",Assert.Throws<GameException>(()=>games.Undo(games.Create("TwoPlayer").GameId)).Code);
        Assert.Equal(404,Assert.Throws<GameException>(()=>games.Get(Guid.NewGuid())).Status);
        Assert.Equal("INVALID_GAME_MODE",Assert.Throws<GameException>(()=>games.Create("Other")).Code);
    }
    [Fact] public void ComputerMovesAutomaticallyAndUndoRemovesPair()
    {
        var game=games.Create("Computer"); game=games.MakeMove(game.GameId,"X",0,0);
        Assert.Equal("O",game.Board[1][1]); Assert.Equal("X",game.CurrentPlayer); Assert.Equal(2,game.MoveHistory.Length);
        Assert.Throws<GameException>(()=>games.MakeMove(game.GameId,"O",0,1));
        game=games.Undo(game.GameId); Assert.Empty(game.MoveHistory); Assert.Equal("X",game.CurrentPlayer);
        Assert.All(game.Board.SelectMany(r=>r),cell=>Assert.Null(cell));
        Assert.Equal("Computer",games.Reset(game.GameId).Mode);
    }
    [Theory]
    [InlineData("OO.XX....",2)] // Win takes priority over blocking.
    [InlineData("XX..O....",2)] // Block before center/corner.
    [InlineData("X........",4)] // Center.
    [InlineData("....X....",0)] // First corner.
    [InlineData("X.OOXXX.O",1)] // Row-major fallback, no immediate threat.
    public void ComputerUsesRequiredPriorityWithoutMutatingBoard(string pattern,int expected)
    {
        var board=pattern.Select(c=>c=='.'?null:c.ToString()).ToArray(); var copy=(string?[])board.Clone();
        Assert.Equal(expected,new ComputerMoveStrategy().Select(board)); Assert.Equal(copy,board);
    }
    [Fact] public void ComputerNeverMovesAfterHumanWins()
    {
        // A valid reachable position where the prescribed basic strategy permits an X fork.
        var game=games.Create("Computer");
        foreach(var cell in new[]{0,7,6,8}) game=games.MakeMove(game.GameId,"X",cell/3,cell%3);
        Assert.Equal("Won",game.Status); Assert.Equal("X",game.Winner);
        Assert.Equal("X",game.MoveHistory[^1].Player); Assert.Equal(7,game.MoveHistory.Length);
    }
    [Fact] public void ReturnedSnapshotsCannotModifyStoredBoard()
    {
        var game=games.Create("TwoPlayer"); game.Board[0][0]="O";
        Assert.Null(games.Get(game.GameId).Board[0][0]);
    }
    [Fact] public void ConcurrentDuplicateRequestsApplyOnlyOnce()
    {
        var game=games.Create("Computer");
        Parallel.For(0,20,_=>{try{games.MakeMove(game.GameId,"X",0,0);}catch(GameException){}});
        Assert.Equal(2,games.Get(game.GameId).MoveHistory.Length);
        Assert.Equal(new Scoreboard(0,0,0),scores.Get());
    }
}
