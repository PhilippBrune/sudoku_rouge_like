// The player modifiers once the whole modifier plan is built (docs/design/modifier-catalog-plan.md): 71 in six
// families, with the game's icons. "new" marks modifiers added after the first 30. Used by the Custom Puzzle mockups.
const MOD_FAMILIES = [
  { id: 'lines', name: 'Lines', mods: [
    ['German Whispers', 'german_whisper'], ['Dutch Whispers', 'dutch_whisper'], ['Parity Lines', 'parity_line'],
    ['Renban Lines', 'renban_chain'], ['Palindrome', 'palindrome_line'], ['Thermometer', 'thermo_line'],
    ['Between Lines', 'between_line'], ['Consecutive Line', 'consecutive_line'], ['Slow Thermo', 'slow_thermo'],
    ['Unique Set Line', 'unique_set_line'], ['Nonconsecutive Line', 'nonconsec_line', 1], ['High-Low Line', 'high_low_alternating', 1],
    ['Nabner Line', 'nabner_line', 1], ['Entropic Line', 'entropic_line', 1], ['Modular Line', 'modular_line', 1],
    ['Zipper Line', 'zipper_line', 1], ['Fast Thermo', 'fast_thermo', 1], ['Ambiguous Thermo', 'ambiguous_thermo', 1],
    ['Region Sum Line', 'region_sum_line', 1], ['Whisper (any K)', 'whisper_generalized', 1], ['N-Line', 'n_line', 1],
    ['Sum Line', 'sum_line', 1] ] },
  { id: 'dots', name: 'Dots & pairs', mods: [
    ['Difference Kropki', 'white_dot'], ['Ratio Kropki', 'black_dot'], ['Full Kropki', 'full_kropki'], ['Sum Dot', 'sum_dot'],
    ['Greater/Less Than', 'greater_less'], ['XV Pairs', 'xv_pairs'], ['Full White Kropki', 'full_white_kropki', 1],
    ['Full Black Kropki', 'full_black_kropki', 1], ['Difference ≥ 3', 'difference_k', 1], ['Ratio N', 'ratio_n', 1],
    ['Distance Pair', 'distance_pair', 1] ] },
  { id: 'cages', name: 'Cages & arrows', mods: [
    ['Killer Cages', 'killer_cage'], ['Hidden Killer', 'killer_hidden_sum', 1], ['Product Cage', 'cage_product', 1],
    ['Difference Cage', 'cage_difference', 1], ['Ratio Cage', 'cage_ratio', 1], ['Renban Cage', 'renban_cage', 1],
    ['Arrow Sums', 'arrow_sum'], ['Arrow Average', 'arrow_average', 1], ['Double Arrow', 'double_arrow', 1],
    ['Pill Arrow', 'pill_arrow', 1] ] },
  { id: 'cells', name: 'Cells', mods: [
    ['Even/Odd', 'even_odd'], ['Prime Cells', 'prime_cells'], ['Fortress Cells', 'fortress_cells'], ['Fog of War', 'fog_cloud'],
    ['Multiples of 3', 'multiple_cells', 1], ['Mirror Cells', 'mirror_cells', 1], ['Opposite Sum', 'opposite_cells_sum', 1],
    ['Counting Circles', 'counting_circles', 1], ['Distance Cell', 'distance_cell', 1] ] },
  { id: 'global', name: 'Whole board', mods: [
    ['Nonconsecutive', 'nonconsecutive'], ['Anti-Knight', 'antiknight'], ['Anti-King', 'antiking'], ['Anti-Bishop', 'antibishop'],
    ['Nonconsec. Diagonal', 'nonconsec_diagonal'], ['Distance ≥ 2', 'distance_ge2'], ['Entropy', 'tricolor_regions'],
    ['Modular Regions', 'modular_regions'], ['X-Sudoku', 'xsudoku', 1], ['Hyper Sudoku', 'hypersudoku', 1],
    ['Extra Region', 'extra_region', 1], ['Disjoint Groups', 'disjoint_groups', 1], ['Magic Square', 'magic_square', 1],
    ['Inverted Zone', 'constraint_negation', 1] ] },
  { id: 'outside', name: 'Outside clues', mods: [
    ['Quadruples', 'quadruples', 1], ['Skyscrapers', 'skyscrapers', 1], ['Sandwich', 'sandwich', 1], ['X-Sums', 'xsums', 1],
    ['Little Killer', 'little_killer', 1] ] },
];

// The example selection the mockups show (three of five).
const MOD_CHOSEN = ['Dutch Whispers', 'Killer Cages', 'Skyscrapers'];

function modIcon(icon) { return R + 'modifier/icon_' + icon + '.png'; }
