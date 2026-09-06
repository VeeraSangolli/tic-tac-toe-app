import assert from 'node:assert/strict';
const base = process.env.TTT_URL || 'http://127.0.0.1:5080';
async function api(path, body, expected = 200) {
  const response = await fetch(base + '/api' + path, body === undefined ? {} : {
    method: 'POST', headers: {'Content-Type':'application/json'}, body: JSON.stringify(body)
  });
  assert.equal(response.status, expected, path);
  return response.json();
}
const created = await fetch(base+'/api/games', {method:'POST',headers:{'Content-Type':'application/json'},body:'{"mode":"TwoPlayer"}'});
assert.equal(created.status,201); assert.ok(created.headers.get('location'));
let game = await created.json(); const id=game.gameId;
assert.equal(game.board.flat().filter(c=>c===null).length,9);
assert.equal((await api('/games/'+id+'/moves',{player:'X'},400)).code,'INVALID_REQUEST');
assert.equal((await api('/games/'+id+'/moves',{player:'O',row:0,column:0},409)).code,'WRONG_PLAYER');
await api('/scoreboard/reset',{});
for (const cell of [0,3,1,4,2]) game=await api(`/games/${id}/moves`,{player:game.currentPlayer,row:Math.floor(cell/3),column:cell%3});
assert.equal(game.status,'Won'); assert.equal(game.winner,'X'); assert.equal(game.winningCells.length,3);
assert.equal((await api(`/games/${id}/moves`,{player:'O',row:2,column:2},409)).code,'GAME_ALREADY_COMPLETED');
assert.equal((await api(`/games/${id}/undo`,{},409)).code,'UNDO_NOT_AVAILABLE');
assert.deepEqual(await api('/scoreboard'),{xWins:1,oWins:0,draws:0});
await api(`/games/${id}/reset`,{});
assert.deepEqual(await api('/scoreboard'),{xWins:1,oWins:0,draws:0});
let computer=await api('/games',{mode:'Computer'},201);
computer=await api(`/games/${computer.gameId}/moves`,{player:'X',row:0,column:0});
assert.equal(computer.board[1][1],'O'); assert.equal(computer.moveHistory.length,2);
computer=await api(`/games/${computer.gameId}/undo`,{}); assert.equal(computer.moveHistory.length,0);
assert.equal((await api('/games/00000000-0000-0000-0000-000000000000',undefined,404)).code,'GAME_NOT_FOUND');
assert.equal((await api('/games/not-a-guid',undefined,404)).code,'INVALID_REQUEST');
assert.equal((await api('/games',{mode:'wrong'},400)).code,'INVALID_GAME_MODE');
const malformed=await fetch(base+'/api/games',{method:'POST',headers:{'Content-Type':'application/json'},body:'{bad'});
assert.equal(malformed.status,400); assert.equal((await malformed.json()).code,'INVALID_REQUEST');
const noBody=await fetch(base+`/api/games/${id}/undo`,{method:'POST'}); assert.equal(noBody.status,409);
const html=await fetch(base); assert.equal(html.status,200); assert.match(await html.text(),/<app-root>/);
await api('/scoreboard/reset',{});
console.log('PASS: HTTP create/location, validation, win, scoreboard, reset, computer, undo, missing game, malformed JSON, empty body, and Angular hosting.');
