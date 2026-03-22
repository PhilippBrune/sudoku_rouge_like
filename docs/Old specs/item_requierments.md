Game Design Document: Sudoku Garden Consumables (Finalized)
This section finalizes the item system logic, economy, and late-game scaling.
1. Item Economy & Scaling
Price Scaling: Item costs are calculated as Base_Price * (1 + (Floor_Index * 0.5)). A Normal item costing 20g on Floor 1 will cost 60g on Floor 5.
Sell Value: Disabled. Items are permanent additions to the inventory until consumed.
Item Penalty: None. Using items is considered a tactical choice and does not reduce the final XP or Star rating at the end of a run.
2. Late-Game Epic Frequency
Unlock Threshold: Epic items (Koi Dragon Scale, Kintsugi Jar) only enter the shop pool at Level 15.
Scaling Probability: The spawn rate of Epic items increases linearly but slowly from Level 15 to Level 40.
Example: 2% chance at Level 15, rising to 7% at Level 40. This ensures they remain rare rewards even for master players.
3. Visual Feedback (Clarity)
To prevent confusion when multiple items or relics are active, the board will use a Layered Color Palette:
Garden Rake: Soft Yellow Highlight.
Zen Sand Sifter: Pale Blue Highlight.
Temple Incense: White Pulse (Inner Glow).
Mossy Lantern (Relic): Soft Green Glow.
Stone Lantern (Relic): Solid Gold Border.
4. Final Item Table (Consolidated)
Rarity	Item Name	Mechanical Effect	UI Border
Normal	Garden Rake	Highlights cells with only 2 candidates (Row/Col).	Silver
Normal	Offering Bowl	Spend 5 HP to reveal the correct number for one cell.	Silver
Normal	Pruning Shears	Removes 1 'impossible' candidate from a 3x3 box.	Silver
Normal	Zen Sand Sifter	Highlights 'Hidden Pairs' in current row.	Silver
Rare	Ginkgo Leaf	Highlights chosen number constraints until placed.	Gold
Rare	Rice Paper Umbrella	Protects Zen (HP) from the next 2 mistakes.	Gold
Rare	Temple Incense	Correct cells for a number pulse for 5 moves.	Gold
Epic	Koi Dragon Scale	Instantly completes the most-filled line or box.	Cyan Glow
Epic	Kintsugi Jar	Highlights all current board mistakes in red.	Purple Glow
