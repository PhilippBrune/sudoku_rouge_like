// Shared data and helpers of the layout mockups. Board: a valid 6×6 Sudoku (2×3 boxes) with givens, player
// digits, pencil marks, a killer cage and two German-whisper lines (adjacent digits differ by 5 or more).
const R = '../../../../Assets/Resources/';
const SOL = [
  [1, 2, 3, 4, 5, 6], [4, 5, 6, 1, 2, 3], [2, 3, 1, 5, 6, 4],
  [5, 6, 4, 2, 3, 1], [3, 1, 2, 6, 4, 5], [6, 4, 5, 3, 1, 2]];
const GIVEN = [
  [1, 0, 1, 0, 0, 1], [0, 1, 0, 1, 0, 0], [1, 0, 0, 0, 1, 0],
  [0, 1, 0, 0, 0, 1], [0, 0, 1, 0, 1, 0], [1, 0, 0, 1, 0, 1]];
const PLAYER = { '0,3': 4, '1,0': 4, '2,1': 3, '3,3': 2, '4,5': 5 };
const PENCIL = { '1,2': [3, 6], '2,3': [5, 6], '4,1': [1, 4], '5,2': [2, 5], '0,4': [5] };
const SELECTED = '3,2';
const CAGE = { cells: ['4,3', '4,4'], sum: 10 };
const WHISPERS = [['1,2', '2,2'], ['3,1', '4,1']];
const BOX = (r, c) => Math.floor(r / 2) * 2 + Math.floor(c / 3);

// Board geometry: L = {cell, gap, boxGap}; extra space after column 2 and after rows 1 and 3 separates the 2x3 boxes.
function cellPos(r, c, L) {
  return [c * (L.cell + L.gap) + (c >= 3 ? L.boxGap : 0),
          r * (L.cell + L.gap) + (r >= 2 ? L.boxGap : 0) + (r >= 4 ? L.boxGap : 0)];
}
function boardSize(L) { return [6 * L.cell + 5 * L.gap + L.boxGap, 6 * L.cell + 5 * L.gap + 2 * L.boxGap]; }

// Fills `el` (position: relative) with 36 absolutely placed cells. Classes: given, player, pencil, selected, same,
// cage, parity (every other box).
function buildBoard(el, L) {
  const [w, h] = boardSize(L);
  el.style.width = w + 'px'; el.style.height = h + 'px';
  for (let r = 0; r < 6; r++) {
    for (let c = 0; c < 6; c++) {
      const key = `${r},${c}`;
      const d = document.createElement('div');
      d.className = 'cell';
      const [x, y] = cellPos(r, c, L);
      Object.assign(d.style, { position: 'absolute', left: x + 'px', top: y + 'px', width: L.cell + 'px', height: L.cell + 'px' });
      if (BOX(r, c) % 2) d.classList.add('parity');
      let v = null;
      if (GIVEN[r][c]) { v = SOL[r][c]; d.classList.add('given'); }
      else if (PLAYER[key]) { v = PLAYER[key]; d.classList.add('player'); }
      if (v !== null) {
        d.textContent = v;
        if (v === 4) d.classList.add('same');
      } else if (PENCIL[key]) {
        d.classList.add('pencil');
        const p = document.createElement('div');
        p.className = 'marks';
        for (let n = 1; n <= 6; n++) {
          const sp = document.createElement('span');
          sp.textContent = PENCIL[key].includes(n) ? n : '';
          p.appendChild(sp);
        }
        d.appendChild(p);
      }
      if (key === SELECTED) d.classList.add('selected');
      if (CAGE.cells.includes(key)) d.classList.add('cage');
      el.appendChild(d);
    }
  }
}

// Whisper lines through cell centres and the cage sum, on an SVG covering the board.
function boardOverlay(svg, L, style) {
  const [w, h] = boardSize(L);
  const center = key => {
    const [r, c] = key.split(',').map(Number);
    const [x, y] = cellPos(r, c, L);
    return [x + L.cell / 2, y + L.cell / 2];
  };
  let html = '';
  for (const [a, b] of WHISPERS) {
    const [x1, y1] = center(a), [x2, y2] = center(b);
    html += `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${style.line}" stroke-width="${style.width}" stroke-linecap="round" opacity="${style.opacity}"/>`;
  }
  const [cx, cy] = cellPos(...CAGE.cells[0].split(',').map(Number), L);
  html += `<text x="${cx + 12}" y="${cy + 26}" font-size="${style.cageSize || 20}" font-family="Main" fill="${style.cageText}">${CAGE.sum}</text>`;
  svg.setAttribute('viewBox', `0 0 ${w} ${h}`);
  Object.assign(svg.style, { position: 'absolute', left: 0, top: 0, width: w + 'px', height: h + 'px', pointerEvents: 'none', zIndex: 3 });
  svg.innerHTML = html;
}

// Garden path: nodes {id, x, y, type, label, state}, edges [[a, b, kind]]. Node icons by type.
const NODE_ICON = {
  start: R + 'class/icon_number_freak.png', puzzle: R + 'node/icon_puzzle_tile.png', elite: R + 'node/icon_elite_mask.png',
  shop: R + 'node/icon_market_stall.png', rest: R + 'node/icon_campfire_stones.png', relic: R + 'node/icon_relic_pedestal.png',
  cursed: R + 'node/icon_moss_trap.png', preboss: R + 'node/icon_preboss_gate.png', boss: R + 'boss/icon_grove_spirit.png',
  event: R + 'node/icon_event_scroll.png'
};
function pathData() {
  return {
    calm: ['start', 'puzzle', 'puzzle', 'shop', 'puzzle', 'elite', 'puzzle', 'rest', 'preboss'],
    risk: ['start', 'puzzle', 'cursed', 'puzzle', 'elite', 'relic', 'puzzle', 'event', 'preboss'],
    labels: { puzzle: 'Puzzle', elite: 'Elite', shop: 'Shop', rest: 'Rest', relic: 'Relic', cursed: 'Cursed',
              preboss: 'Gate', boss: 'Grove Spirit', event: 'Event', start: '' },
    puzzleInfo: ['6×6 ★', '6×6 ★★', '7×7 ★', '6×6 ★★', '7×7 ★★', '6×6 ★★★']
  };
}
