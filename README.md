# Tic-Tac-Toe

## Overview

This repository contains a browser-based Tic-Tac-Toe application built for the Round 2 exercise. The Angular frontend renders the game and calls the .NET REST API, which owns game sessions, move validation, game rules, move history, and the session scoreboard.

## Tech Stack

- Frontend: Angular 20, TypeScript, standalone components
- Backend: ASP.NET Core Web API targeting .NET 6
- API style: REST
- Storage: in-memory session state
- Testing: xUnit backend unit tests

## Features

- Two-player mode with X and O turn validation
- Play against a basic computer opponent
- Row, column, diagonal, and draw detection
- Winning-cell highlighting
- Move history with row and column positions
- Undo for two-player and computer modes
- Session scoreboard for X wins, O wins, and draws
- Separate game and scoreboard reset actions
- Responsive Angular interface
- Swagger API documentation in Development

## Run Locally

### Backend

From the repository root:

```powershell
dotnet run --project .\backend\TicTacToe.Api\TicTacToe.Api.csproj --launch-profile TicTacToe.Api
```

The default Development profile exposes:

- HTTP API: `http://localhost:5039`
- HTTPS API: `https://localhost:7122`
- Swagger: `https://localhost:7122/swagger`

The frontend uses the HTTP API during local development so a self-signed HTTPS certificate is not required in the browser.

### Frontend

In a second terminal, from the repository root:

```powershell
npm --prefix .\frontend install
npm --prefix .\frontend run start -- --host=127.0.0.1 --port=4200
```

Open `http://localhost:4200`.

## API Contract

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/games` | Create a game with mode `0` (TwoPlayer) or `1` (Computer) |
| GET | `/api/games/{id}` | Get the current game state |
| POST | `/api/games/{id}/moves` | Submit `player`, `row`, and `column` |
| POST | `/api/games/{id}/undo` | Undo the latest move or mode-specific move pair |
| POST | `/api/games/{id}/reset` | Start a fresh game session |
| GET | `/api/scoreboard` | Get the session scoreboard |
| POST | `/api/scoreboard/reset` | Reset the scoreboard |

Example create request:

```json
{
	"mode": 0
}
```

Example move request:

```json
{
	"player": 0,
	"row": 0,
	"column": 0
}
```

Enum values are numeric in the current .NET JSON contract:

- Player: `0` = X, `1` = O
- Mode: `0` = TwoPlayer, `1` = Computer
- Status: `0` = InProgress, `1` = Won, `2` = Draw

The game response includes the game ID, board, current player, mode, status, winner, winning cells, move history, and scoreboard-update state. The scoreboard itself is available from `/api/scoreboard`.

## Testing

Run backend unit tests:

```powershell
dotnet test .\backend\TicTacToe.sln --no-restore
```

The backend tests cover valid and invalid moves, turn switching, row/column/diagonal wins, draws, reset, both undo modes, scoreboard updates and reset, computer move priority, and moves after completion.

Build the frontend:

```powershell
npm --prefix .\frontend run build
```

For a manual acceptance pass, verify two-player turns, occupied-cell rejection, all win directions, draw handling, move history, both undo modes, computer responses, reset-game scoreboard preservation, and scoreboard reset.

## Design Decisions

- The backend is the source of truth. The frontend renders each latest API response rather than calculating game outcomes locally.
- Game and scoreboard data are intentionally in memory because persistence is not required for this exercise.
- Undo uses clarification Option A: it is disabled after a game is won or drawn, keeping the completed scoreboard result final.
- In TwoPlayer mode, undo removes one move. In Computer mode, it removes the human move and the immediately following computer move together.
- The computer follows the required priority: win, block, center, corner, then first available cell.
- Reset Game creates a new game ID and preserves the scoreboard.

## AI-Assisted Development Notes

AI assistance was used to help scaffold the Angular application, connect the typed API service, draft the UI, and generate initial backend test cases. The implementation was reviewed against the requirements document, validated with backend tests and the Angular production build, and manually exercised through the browser. The important reviewed choices were backend-owned state, Option A undo behavior, numeric enum compatibility, local HTTP CORS configuration, and in-memory storage.

## Assumptions and Known Limitations

- The application is intended for local single-process use; restarting the backend clears all games and the scoreboard.
- The frontend expects the default backend HTTP port `5039` and Angular port `4200`.
- CORS is limited to `http://localhost:4200` for the local exercise setup.
- The computer player is intentionally basic and is not a minimax opponent.
- Resetting a game creates a new session ID; the frontend uses the returned ID for subsequent actions.

## Future Improvements

- Add persistent storage for games and scoreboard data.
- Move the frontend API URL into Angular environment configuration.
- Add API integration tests and Angular component tests.
- Add stronger API validation and typed DTOs for public responses.
- Improve the computer player with a full strategy engine.