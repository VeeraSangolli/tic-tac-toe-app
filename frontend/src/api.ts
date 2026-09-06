import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export type Mode = 'TwoPlayer' | 'Computer';
export interface GameState { gameId: string; board: ('X'|'O'|null)[][]; currentPlayer: 'X'|'O'; mode: Mode; status: 'InProgress'|'Won'|'Draw'; winner: string|null; winningCells: {row:number;column:number}[]; moveHistory: {moveNumber:number;player:string;row:number;column:number}[]; }
export interface Scoreboard { xWins:number; oWins:number; draws:number; }
@Injectable({providedIn:'root'})
export class GameApiService {
  private http = inject(HttpClient);
  create(mode:Mode) { return this.http.post<GameState>('/api/games',{mode}); }
  get(id:string) { return this.http.get<GameState>(`/api/games/${id}`); }
  move(game:GameState,row:number,column:number) { return this.http.post<GameState>(`/api/games/${game.gameId}/moves`,{player:game.currentPlayer,row,column}); }
  undo(id:string) { return this.http.post<GameState>(`/api/games/${id}/undo`,{}); }
  reset(id:string) { return this.http.post<GameState>(`/api/games/${id}/reset`,{}); }
}
@Injectable({providedIn:'root'})
export class ScoreboardApiService {
  private http = inject(HttpClient);
  get() { return this.http.get<Scoreboard>('/api/scoreboard'); }
  reset() { return this.http.post<Scoreboard>('/api/scoreboard/reset',{}); }
}
