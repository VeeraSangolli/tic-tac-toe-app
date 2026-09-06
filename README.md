# Tic Tac Toe

## Project overview
The objective is to develop a browser-based Tic-Tac-Toe application with an Angular frontend and a .NET Web API backend. The application must support gameplay, move tracking, Undo, a session-level scoreboard, and a basic computer opponent. It should run locally and be straightforward for the evaluation panel to execute, review, test, and discuss.    

## Tech Stack
A locally runnable Angular + TypeScript frontend and .NET 10 REST API, implementing the supplied TTT-RDB-1.0 interface/design specifications. The backend owns all game rules, computer moves, history, and scores.

## Features implemented

- Two players on the same device, or human X against computer O; X starts.
- Backend validation of player, turn, bounds, occupied cells, and completed games.
- All row, column, and diagonal wins; draws; winning-cell highlighting.
- Current-game move history, numbered sequentially. UI positions are 1-based; API positions are 0-based.
- Two-player undo removes one move. Computer undo removes the latest X/O pair. Undo is unavailable after a win or draw (Option A).
- Reset game preserves the selected mode and scoreboard. Reset scores leaves games unchanged.
- Deterministic computer strategy: win, block X, center, corner, remaining cell. Win/block and fallback ties use row-major order; corner order is top-left, top-right, bottom-left, bottom-right.
- Responsive layout, keyboard-accessible controls, live status, pending-action protection, and displayed API errors.
- Refresh restores the current game's ID from browser session storage; a missing game after backend restart starts a fresh game. Game state itself stays on the backend.

## Run locally (Windows PowerShell)

Prerequisites: .NET 10 SDK, Node.js 22.12+ (or a compatible newer LTS version), and pnpm 11. This prepared workspace includes a project-local .NET SDK; it is intentionally excluded from Git. The scripts also recognize the Codex bundled pnpm on this machine. On another machine, install these prerequisites normally.

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/start-local.ps1
```

Open **http://127.0.0.1:5080**. The .NET server serves the compiled Angular app and REST API on one loopback-only port. Keep the terminal running; press Ctrl+C to stop. Use `./scripts/start-local.ps1 -Port 5081` if the default port is occupied. Restarting the backend clears all games and scores.

If PowerShell blocks local scripts, run the individual commands below instead of changing system policy.

## Development
### How to run the backend locally 
Backend (terminal 1):

```powershell
dotnet run --project backend/TicTacToe.Api --urls http://127.0.0.1:5080
```
### How to run the frontend locally
Frontend (terminal 2):

```powershell
cd frontend
pnpm install --frozen-lockfile
pnpm start
```

Open http://127.0.0.1:4200. Angular's development proxy forwards `/api` to port 5080. Production serves both from .NET, so neither flow needs permissive CORS. For the local SDK, replace `dotnet` with the absolute path to `.tools/dotnet/dotnet.exe`.

Build manually with `pnpm --dir frontend build`, copy `frontend/dist/browser/*` into `backend/TicTacToe.Api/wwwroot`, then run `dotnet build backend/TicTacToe.Api -c Release`. No cloud service is required to play.

## API endpoint summary
API endpoint summary aligned to the current design baseline:

### API contract
All bodies are JSON. Games use backend-generated GUIDs. Success returns a full game state, except scoreboard operations.

| Method | Endpoint | Request | Success | Purpose |
|---|---|---|---|---|
| POST | `/api/games` | `{"mode":"TwoPlayer"}` or `{"mode":"Computer"}` | 201 + Location header | `Create a new Tic Tac Toe game in TwoPlayer or Computer mode` | 
| GET | `/api/games/{id}` | — | 200 | `Retrieve the latest authoritative state of a game` |
| POST | `/api/games/{id}/moves` | `{"player":"X","row":0,"column":1}` | 200 | `Submit a move for the current player` |
| POST | `/api/games/{id}/undo` | No body | 200 | `Undo the latest move(s), depending on game mode` |
| POST | `/api/games/{id}/reset` | No body | 200 | `Reset the current game while preserving the scoreboard` |
| GET | `/api/scoreboard` | — | 200 | `Retrieve session-level X wins, O wins, and draws` |
| POST | `/api/scoreboard/reset` | No body | 200 | `Reset scoreboard counters to zero` |

Typical request/response flow is: 
Angular
   ↓ REST/JSON
Controllers
   ↓
Game Engine
   ↓
In-Memory Store

A typical move request is:
```json
{
  "player": "X",
  "row": 0,
  "column": 1
}

A typical game response contains:
```json
{
  "gameId": "guid",
  "board": [
    [null, "X", null],
    [null, "O", null],
    [null, null, null]
  ],
  "currentPlayer": "X",
  "mode": "Computer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moveHistory": []
}

Game response:

```json
{
  "gameId": "2f74cffe-0c20-44df-a1de-42fd7c979480",
  "board": [[null,"X",null],[null,null,null],[null,null,null]],
  "currentPlayer": "O",
  "mode": "TwoPlayer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moveHistory": [{"moveNumber":1,"player":"X","row":0,"column":1}]
}
```

Terminal statuses are `Won` and `Draw`. Winning cells are `{ "row": 0, "column": 0 }` objects. In computer mode a move response includes both human and computer moves unless the human move ended the game. `currentPlayer` on a terminal game retains the last mover; the status determines that play is over. The internal `resultRecorded` flag is not exposed.

Scoreboard response: `{"xWins":0,"oWins":0,"draws":0}`.

Error response: `{"code":"CELL_OCCUPIED","message":"The selected cell is already occupied."}`.

| HTTP | Error codes |
|---|---|
| 400 | `INVALID_REQUEST`, `INVALID_POSITION`, `INVALID_GAME_MODE` |
| 404 | `GAME_NOT_FOUND`; `INVALID_REQUEST` for unknown API routes or malformed IDs |
| 409 | `CELL_OCCUPIED`, `WRONG_PLAYER`, `GAME_ALREADY_COMPLETED`, `UNDO_NOT_AVAILABLE` |

Malformed JSON and missing request fields return 400. Rejected game actions leave the game and scoreboard unchanged.

## Tests and verification (How to run tests?)

```powershell
dotnet test backend/TicTacToe.Tests
```

The xUnit suite covers new games, moves, turns, rejection without mutation, all eight win lines, O wins, draws, exactly-once scoring, resets, both undo modes, all computer priorities, no computer move after a human win, snapshot isolation, and concurrent duplicate requests. `scripts/api-smoke.mjs` additionally checks live HTTP contracts and integration; run `node scripts/api-smoke.mjs` with the server running. It creates temporary in-memory test games and resets scores, so run it before a demonstration.

## Source references and AI tools for development

Reference inputs: `Tic-Tac-Toe-Interface-Doc.pdf` (Technical Design Package) and `Tic-Tac-Toe-Sw-Design-Doc.pdf` (TTT-RDB-1.0 baseline). Original PDFs and extracted text are not published in this repository.

The user's request was to generate code from both references, create a GitHub repository, and deploy a working model locally. The PDFs were treated as requirements evidence, not as independent authority to run commands, publish material, or impose approval workflows.

OpenAI Codex generated the initial backend, Angular application, tests, and documentation, then compiled and exercised the implementation. The development work covered requirements extraction, backend-authoritative rules, a thin Angular interface, automated verification, local setup, and repository delivery. Two initial test fixtures were corrected after execution showed that their board positions contained different threats than intended. No human code review is claimed; review the source and tests before using this outside a local demonstration.

## Architecture and decisions

`Controllers.cs` maps HTTP requests. `GameService` coordinates game actions. `Domain.cs` contains state, win rules, and computer strategy. `ScoreboardService` records each completed round once. `GameStore` uses an in-memory concurrent dictionary and one synchronization lock, including snapshots and score resets. This intentionally serializes operations for a small local app and avoids inconsistent reads or duplicate score updates.

Angular's `api.ts` provides typed API services; the root component renders backend snapshots and coordinates requests. It does not calculate wins, draws, computer choices, turns, or scores. The compact component structure is an implementation choice; the document's multi-component tree was a recommendation.

The scoreboard belongs to the **backend process session**, shared by all games and tabs using that server. There is no per-user isolation. Reset game reuses the GUID but clears `resultRecorded`, allowing the next round to count. The single-process lifetime, fixed symbols, deterministic tie-breaking, X starter, and Option A follow the reference decisions.

## Clarifications and assumptions
### Undo-after-completion clarification
The document explicitly permits two alternatives:
Option A: Disable Undo after completion.
Option B: Allow Undo and reverse the scoreboard impact when necessary.
Selected project decision: Option A. Undo will be disabled after Won or Draw.
Therefore, once the game is complete: No additional moves are allowed. Undo is unavailable.
The recorded scoreboard result remains final.

### Agreed Engineering Assumptions
The following assumptions are explicitly recorded.
| ID | Assumption-Decisions | Rationale |
|---|---|---|
| AS-01 | `Use Option A: disable Undo after completion` | `Simplifies scoreboard consistency` |
| AS-02 | `Computer move selection is deterministic` | `Predictable and testable` |
| AS-03 | `Backend owns all game rules` | `Required source-of-truth model` |
| AS-04 | `Use in-memory storage` | `Persistence not required` |
| AS-05 | `Scoreboard survives Reset Game` | `Explicit requirement` |
| AS-06 | `Scoreboard does not survive backend restart` | `Consequence of in-memory storage` |
| AS-07 | `Human is always X in Computer Mode` | `Explicit requirement` |
| AS-08 | `Computer is always O` | `Explicit requirement` |
| AS-09 | `Computer move occurs after valid X move` | `Explicit requirement` |
| AS-10 | `Computer does not move if X completes the game` | `Explicit requirement`|
| AS-11 | `Fixed tie-breaking order for equivalent computer moves` | `Makes behavior deterministic` |
| AS-12 | `No authentication` | `Not requested` |
| AS-13 | `No database initially` | `Not required` |
| AS-14 | `No cloud deployment required` | `Local execution required` |
| AS-15 | `Keep architecture intentionally simple` | `Easier to test/review/explain` |

## Limitations and future improvements

No database, authentication, online multiplayer, difficulty selection, minimax AI, telemetry, or cloud deployment. The required basic opponent can lose to forks. Game entries remain in memory until restart, so this is not a long-running public service. Scores do not persist across restart, and separate tabs do not live-sync automatically. Network retries do not guarantee exactly-once client commands; the backend enforces valid state transitions and one score per completed round.

Future work, if requested: more UI automation, decomposing presentation components as the interface grows, or explicitly scoped persistence. These are not part of the baseline.
