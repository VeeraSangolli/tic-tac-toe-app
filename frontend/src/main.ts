import { Component, inject, signal } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { GameApiService, ScoreboardApiService, GameState, Scoreboard, Mode } from './api';
@Component({selector:'app-root',standalone:true,templateUrl:'./app.html'})
class AppComponent {
  private games = inject(GameApiService);
  private scores = inject(ScoreboardApiService);
  game = signal<GameState|null>(null);
  score = signal<Scoreboard>({xWins:0,oWins:0,draws:0});
  busy = signal(false);
  error = signal('');
  selected = signal<Mode>('TwoPlayer');
  constructor() { void this.restore(); }
  private async restore() {
    let id: string|null = null;
    try { id = sessionStorage.getItem('ttt-game'); } catch {}
    if (!id) return this.newGame('TwoPlayer');
    this.busy.set(true);
    try {
      const game = await firstValueFrom(this.games.get(id));
      this.game.set(game); this.selected.set(game.mode);
      this.score.set(await firstValueFrom(this.scores.get()));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.busy.set(false); return this.newGame('TwoPlayer');
      }
      this.showError(error);
    } finally { this.busy.set(false); }
  }
  private showError(error:unknown) { this.error.set(error instanceof HttpErrorResponse ? error.error?.message || 'Could not reach the server. Check that it is running, then try again.' : 'Something went wrong. Please try again.'); }
  async act(request:Observable<GameState>) {
    if(this.busy()) return;
    this.busy.set(true); this.error.set('');
    try {
      const game = await firstValueFrom(request);
      this.game.set(game); this.selected.set(game.mode);
      try { sessionStorage.setItem('ttt-game',game.gameId); } catch {}
      this.score.set(await firstValueFrom(this.scores.get()));
    } catch(error) { this.showError(error); }
    finally { this.busy.set(false); }
  }
  newGame(mode:Mode) { return this.act(this.games.create(mode)); }
  move(row:number,column:number) { const game = this.game(); if(game) void this.act(this.games.move(game,row,column)); }
  undo() { const game = this.game(); if(game) void this.act(this.games.undo(game.gameId)); }
  reset() { const game = this.game(); if(game) void this.act(this.games.reset(game.gameId)); else void this.newGame(this.selected()); }
  async resetScores() {
    if(this.busy()) return;
    this.busy.set(true); this.error.set('');
    try { this.score.set(await firstValueFrom(this.scores.reset())); } catch(error) { this.showError(error); } finally { this.busy.set(false); }
  }
  winning(row:number,column:number) { return this.game()?.winningCells.some(cell=>cell.row===row && cell.column===column) ?? false; }
}
bootstrapApplication(AppComponent,{providers:[provideHttpClient()]}).catch(console.error);
