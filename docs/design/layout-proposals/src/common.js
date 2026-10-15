// Shared data and helpers of the layout mockups (docs/design/layout-proposals.md).
const R = '../../../../Assets/Resources/';
const PARAMS = new URLSearchParams(location.search);

// --- boards -------------------------------------------------------------------------------------------------------
// 6×6 (2×3 boxes): givens, player digits, pencil marks, a killer cage and two German-whisper lines.
const BOARD6 = {
  n: 6, boxR: 2, boxC: 3,
  sol: [[1, 2, 3, 4, 5, 6], [4, 5, 6, 1, 2, 3], [2, 3, 1, 5, 6, 4], [5, 6, 4, 2, 3, 1], [3, 1, 2, 6, 4, 5], [6, 4, 5, 3, 1, 2]],
  given: [[1, 0, 1, 0, 0, 1], [0, 1, 0, 1, 0, 0], [1, 0, 0, 0, 1, 0], [0, 1, 0, 0, 0, 1], [0, 0, 1, 0, 1, 0], [1, 0, 0, 1, 0, 1]],
  player: { '0,3': 4, '1,0': 4, '2,1': 3, '3,3': 2, '4,5': 5 },
  pencil: { '1,2': [3, 6], '2,3': [5, 6], '4,1': [1, 4], '5,2': [2, 5], '0,4': [5] },
  selected: '3,2', same: 4,
  cage: { cells: ['4,3', '4,4'], sum: 10 },
  whispers: [['1,2', '2,2'], ['3,1', '4,1']]
};

// 9×9 (3×3 boxes), a valid grid with about 40 % givens.
function board9() {
  const sol = [];
  for (let r = 0; r < 9; r++) sol.push(Array.from({ length: 9 }, (_, c) => ((r * 3 + Math.floor(r / 3) + c) % 9) + 1));
  let seed = 7;
  const rnd = () => (seed = (seed * 9301 + 49297) % 233280) / 233280;
  const given = sol.map(row => row.map(() => (rnd() < 0.42 ? 1 : 0)));
  return {
    n: 9, boxR: 3, boxC: 3, sol, given,
    player: { '0,1': sol[0][1], '2,5': sol[2][5], '4,4': sol[4][4], '6,2': sol[6][2], '7,7': sol[7][7] },
    pencil: { '1,3': [2, 5, 8], '5,6': [1, 7], '8,0': [3, 6] },
    selected: '3,4', same: sol[4][4],
    cage: { cells: ['6,6', '6,7'], sum: sol[6][6] + sol[6][7] },
    whispers: []
  };
}

function cellPos(r, c, L, B) {
  return [c * (L.cell + L.gap) + Math.floor(c / B.boxC) * L.boxGap,
          r * (L.cell + L.gap) + Math.floor(r / B.boxR) * L.boxGap];
}
function boardSize(L, B) {
  return [B.n * L.cell + (B.n - 1) * L.gap + (B.n / B.boxC - 1) * L.boxGap,
          B.n * L.cell + (B.n - 1) * L.gap + (B.n / B.boxR - 1) * L.boxGap];
}

// Fills `el` (position: relative) with absolutely placed cells. Classes: given, player, pencil, selected, same, cage,
// parity (every other box).
function buildBoard(el, L, B = BOARD6) {
  const [w, h] = boardSize(L, B);
  el.style.width = w + 'px'; el.style.height = h + 'px';
  for (let r = 0; r < B.n; r++) {
    for (let c = 0; c < B.n; c++) {
      const key = `${r},${c}`;
      const d = document.createElement('div');
      d.className = 'cell';
      const [x, y] = cellPos(r, c, L, B);
      Object.assign(d.style, { position: 'absolute', left: x + 'px', top: y + 'px', width: L.cell + 'px', height: L.cell + 'px' });
      const box = Math.floor(r / B.boxR) * (B.n / B.boxC) + Math.floor(c / B.boxC);
      if (box % 2) d.classList.add('parity');
      let v = null;
      if (B.given[r][c]) { v = B.sol[r][c]; d.classList.add('given'); }
      else if (B.player[key]) { v = B.player[key]; d.classList.add('player'); }
      if (v !== null) {
        d.textContent = v;
        if (v === B.same) d.classList.add('same');
      } else if (B.pencil[key]) {
        d.classList.add('pencil');
        const p = document.createElement('div');
        p.className = 'marks';
        for (let k = 1; k <= Math.min(B.n, 9); k++) {
          const sp = document.createElement('span');
          sp.textContent = B.pencil[key].includes(k) ? k : '';
          p.appendChild(sp);
        }
        d.appendChild(p);
      }
      if (key === B.selected) d.classList.add('selected');
      if (B.cage.cells.includes(key)) d.classList.add('cage');
      el.appendChild(d);
    }
  }
}

// Whisper lines through cell centres and the cage sum, on an SVG covering the board.
function boardOverlay(svg, L, style, B = BOARD6) {
  const [w, h] = boardSize(L, B);
  const center = key => {
    const [r, c] = key.split(',').map(Number);
    const [x, y] = cellPos(r, c, L, B);
    return [x + L.cell / 2, y + L.cell / 2];
  };
  let html = '';
  for (const [a, b] of B.whispers) {
    const [x1, y1] = center(a), [x2, y2] = center(b);
    html += `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${style.line}" stroke-width="${style.width}" stroke-linecap="round" opacity="${style.opacity}"/>`;
  }
  const [cx, cy] = cellPos(...B.cage.cells[0].split(',').map(Number), L, B);
  html += `<text x="${cx + 10}" y="${cy + 24}" font-size="${style.cageSize || 20}" font-family="Main" fill="${style.cageText}">${B.cage.sum}</text>`;
  svg.setAttribute('viewBox', `0 0 ${w} ${h}`);
  Object.assign(svg.style, { position: 'absolute', left: 0, top: 0, width: w + 'px', height: h + 'px', pointerEvents: 'none', zIndex: 3 });
  svg.innerHTML = html;
}

// --- garden path --------------------------------------------------------------------------------------------------
const NODE_ICON = {
  start: R + 'class/icon_number_freak.png', puzzle: R + 'node/icon_puzzle_tile.png', elite: R + 'node/icon_elite_mask.png',
  shop: R + 'node/icon_market_stall.png', rest: R + 'node/icon_campfire_stones.png', relic: R + 'node/icon_relic_pedestal.png',
  cursed: R + 'node/icon_moss_trap.png', preboss: R + 'node/icon_preboss_gate.png', boss: R + 'boss/icon_grove_spirit.png',
  event: R + 'node/icon_event_scroll.png'
};

// The five floors (FloorThemeData, InRunController floor backgrounds, BossGateViewController boss avatars).
// Boss names are working titles taken from the avatar files; the game does not name its bosses yet.
const FLOORS = [
  { name: 'Bamboo Courtyard', bg: 'bg_garden_grove', boss: 'grove_spirit', bossName: 'Grove Spirit', count: 'one' },
  { name: 'Moss Garden', bg: 'bg_koi_garden', boss: 'koi_warden', bossName: 'Koi Warden', count: 'two' },
  { name: 'Koi Terrace', bg: 'bg_koi_terrace', boss: 'shrine_oni', bossName: 'Shrine Oni', count: 'three' },
  { name: 'Stone Lantern Walk', bg: 'bg_shrine_garden', boss: 'stone_patriarch', bossName: 'Stone Patriarch', count: 'four' },
  { name: 'Shrine Summit', bg: 'bg_demon_summit', boss: 'demon_mask', bossName: 'Summit Demon', count: 'five' }
];
function floorIndex() { return Math.min(Math.max(parseInt(PARAMS.get('floor') || '1', 10), 1), 5) - 1; }

function pathData(floor = 0) {
  const calm = [['start', 'puzzle', 'puzzle', 'shop', 'puzzle', 'elite', 'puzzle', 'rest', 'preboss'],
                ['start', 'puzzle', 'rest', 'puzzle', 'event', 'puzzle', 'shop', 'puzzle', 'preboss'],
                ['start', 'puzzle', 'shop', 'puzzle', 'puzzle', 'relic', 'elite', 'rest', 'preboss'],
                ['start', 'puzzle', 'puzzle', 'event', 'elite', 'puzzle', 'shop', 'rest', 'preboss'],
                ['start', 'puzzle', 'rest', 'elite', 'puzzle', 'shop', 'puzzle', 'rest', 'preboss']];
  const risk = [['start', 'puzzle', 'cursed', 'puzzle', 'elite', 'relic', 'puzzle', 'event', 'preboss'],
                ['start', 'cursed', 'puzzle', 'elite', 'puzzle', 'relic', 'puzzle', 'elite', 'preboss'],
                ['start', 'puzzle', 'elite', 'cursed', 'puzzle', 'puzzle', 'relic', 'event', 'preboss'],
                ['start', 'elite', 'puzzle', 'cursed', 'relic', 'puzzle', 'elite', 'puzzle', 'preboss'],
                ['start', 'cursed', 'elite', 'puzzle', 'relic', 'elite', 'puzzle', 'cursed', 'preboss']];
  const sizes = [['6×6 ★', '6×6 ★★', '7×7 ★'], ['6×6 ★★', '7×7 ★★', '7×7 ★★★'], ['7×7 ★★', '8×8 ★★', '7×7 ★★★'],
                 ['8×8 ★★', '8×8 ★★★', '9×9 ★★'], ['9×9 ★★★', '8×8 ★★★★', '9×9 ★★★★']];
  return {
    calm: calm[floor], risk: risk[floor],
    labels: { puzzle: 'Puzzle', elite: 'Elite', shop: 'Shop', rest: 'Rest', relic: 'Relic', cursed: 'Cursed',
              preboss: 'Gate', boss: FLOORS[floor].bossName, event: 'Event', start: '' },
    puzzleInfo: sizes[floor]
  };
}

// --- boss screens -------------------------------------------------------------------------------------------------
// Floor 3: pick 3 of 4 modifiers (two chosen so far, one not discovered yet), highest intensity, the floor's debuff.
const GATE = {
  floor: 2, pick: 3, chosen: 2,
  intensity: { level: 4, name: 'Very High' },
  debuff: { name: 'Row Wipe', rule: 'A wrong digit erases all your digits in its row.', icon: R + 'debuff/icon_row_wipe.png' },
  options: [
    { name: 'Greater/Less Than', rule: 'Chevrons between neighbours show which digit is larger.', icon: R + 'modifier/icon_greater_less.png', chosen: true },
    { name: 'Nonconsecutive', rule: 'No two side-by-side cells may hold consecutive digits.', icon: R + 'modifier/icon_nonconsecutive.png', chosen: true },
    { name: 'Palindrome', rule: 'Lines read the same forwards and backwards.', icon: R + 'modifier/icon_palindrome_line.png', chosen: false },
    { name: '???', rule: 'Not discovered yet. Choose it to learn its rule.', icon: R + 'ui/icon_petal_orb.png', chosen: false, unknown: true }]
};
// After the Floor 3 boss: the run's numbers and the three relics offered next.
const DEFEAT = {
  floor: 2,
  stats: [['Mistakes', '1'], ['HP lost', '2'], ['Items used', '1'], ['Pencil marks', '14']],
  relics: [
    { name: 'Sakura Seal', rule: 'Perfect-puzzle streak: +1 HP at 2, +3 HP at 5.', icon: R + 'relic/icon_sakura_seal.png' },
    { name: 'Spirit Lantern', rule: 'Shop prices reduced by 20%.', icon: R + 'relic/icon_spirit_lantern.png' },
    { name: 'Wisteria Branch', rule: 'Heal 2 HP at the start of each floor.', icon: R + 'relic/icon_wisteria_branch.png' }]
};
