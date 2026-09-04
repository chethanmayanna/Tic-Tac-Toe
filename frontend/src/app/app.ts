import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { GameApiService, GameMode, GameState, Player, Scoreboard } from './game-api.service';

@Component({
  selector: 'app-root',
  imports: [DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly gameApi = inject(GameApiService);

  protected readonly game = signal<GameState | null>(null);
  protected readonly scoreboard = signal<Scoreboard>({ xWins: 0, oWins: 0, draws: 0 });
  protected readonly selectedMode = signal<GameMode>(0);
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.startGame(0);
    this.refreshScoreboard();
  }

  protected startGame(mode: GameMode): void {
    this.selectedMode.set(mode);
    this.busy.set(true);
    this.error.set('');

    this.gameApi.createGame(mode).subscribe({
      next: (game) => {
        this.game.set(game);
        this.busy.set(false);
      },
      error: () => this.handleError('The game could not be started. Is the API running on port 5039?'),
    });
  }

  protected playCell(index: number): void {
    const game = this.game();
    if (!game || !this.canPlay(index)) {
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.gameApi.makeMove(game.id, game.currentPlayer, Math.floor(index / 3), index % 3).subscribe({
      next: (updatedGame) => {
        this.game.set(updatedGame);
        this.busy.set(false);
        if (updatedGame.status !== 0) {
          this.refreshScoreboard();
        }
      },
      error: (response) => this.handleError(this.readApiError(response)),
    });
  }

  protected undo(): void {
    const game = this.game();
    if (!game || game.moves.length === 0 || game.status !== 0) {
      return;
    }

    this.busy.set(true);
    this.gameApi.undo(game.id).subscribe({
      next: (updatedGame) => {
        this.game.set(updatedGame);
        this.busy.set(false);
      },
      error: (response) => this.handleError(this.readApiError(response)),
    });
  }

  protected resetGame(): void {
    const game = this.game();
    if (!game) {
      return;
    }

    this.busy.set(true);
    this.gameApi.resetGame(game.id).subscribe({
      next: (updatedGame) => {
        this.game.set(updatedGame);
        this.selectedMode.set(updatedGame.mode);
        this.busy.set(false);
      },
      error: (response) => this.handleError(this.readApiError(response)),
    });
  }

  protected resetScoreboard(): void {
    this.busy.set(true);
    this.gameApi.resetScoreboard().subscribe({
      next: (scoreboard) => {
        this.scoreboard.set(scoreboard);
        this.busy.set(false);
      },
      error: (response) => this.handleError(this.readApiError(response)),
    });
  }

  protected refreshScoreboard(): void {
    this.gameApi.getScoreboard().subscribe({
      next: (scoreboard) => this.scoreboard.set(scoreboard),
      error: () => this.error.set('The scoreboard is unavailable until the API is running.'),
    });
  }

  protected canPlay(index: number): boolean {
    const game = this.game();
    if (!game || this.busy() || game.status !== 0 || game.board[index] !== null) {
      return false;
    }

    return game.mode === 0 || game.currentPlayer === 0;
  }

  protected playerName(player: Player | null): string {
    return player === 0 ? 'X' : player === 1 ? 'O' : '';
  }

  protected modeName(mode: GameMode): string {
    return mode === 0 ? 'Two player' : 'Against computer';
  }

  protected statusMessage(): string {
    const game = this.game();
    if (!game) {
      return 'Connecting to the game server';
    }
    if (game.status === 1) {
      return `${this.playerName(game.winner)} wins`;
    }
    if (game.status === 2) {
      return 'Draw game';
    }
    return `${this.playerName(game.currentPlayer)} to move`;
  }

  protected isWinningCell(index: number): boolean {
    return this.game()?.winningCells.includes(index) ?? false;
  }

  protected cellLabel(index: number, player: Player | null): string {
    const row = Math.floor(index / 3) + 1;
    const column = (index % 3) + 1;
    return player === null
      ? `Empty row ${row}, column ${column}`
      : `Player ${this.playerName(player)}, row ${row}, column ${column}`;
  }

  private handleError(message: string): void {
    this.busy.set(false);
    this.error.set(message);
  }

  private readApiError(response: { error?: { message?: string } }): string {
    return response.error?.message ?? 'The request could not be completed.';
  }
}
