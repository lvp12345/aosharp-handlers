# AOSharp Handlers & Managers

A collection of AOSharp combat handlers and managers for Anarchy Online.

## Combat Handlers

- **Agent** - Agent combat handler
- **Adventurer** - Adventurer combat handler
- **Bureaucrat** - Bureaucrat combat handler
- **Doctor** - Doctor combat handler
- **Enforcer** - Enforcer combat handler
- **Engineer** - Engineer combat handler with pet management
- **Fixer** - Fixer combat handler
- **Keeper** - Keeper combat handler
- **Martial Artist** - MA combat handler
- **Meta-Physicist** - MP combat handler with pet management
- **Nano-Technician** - NT combat handler
- **Shade** - Shade combat handler
- **Soldier** - Soldier combat handler
- **Trader** - Trader combat handler
- **Generic** - Base/shared combat handler logic

## Managers

- **Loot Manager** - Automated looting from corpses with rule-based filtering
- **Assist Manager** - Assist targeting management
- **Bag Manager** - Bag/inventory management
- **Follow Manager** - Follow target management
- **GMI Manager** - Global Market Interface management
- **Help Manager** - In-game help and kit management
- **Mail Manager** - In-game mail management
- **Pet Manager** - Pet control and management
- **Research Manager** - Research management
- **Social Pet Manager** - Social pet management
- **Sync Manager** - Multi-character synchronization

## Loot Manager Features

- **Rule-based looting** - Add items by name with QL range, quantity, and exact/partial matching
- **Loot All** - Loot everything from corpses automatically
- **Delete leftovers** - Delete items not in your loot list
- **Use Any Bag** - Toggle to use any bag in inventory for storing loot, not just bags named "loot"
- **Quantity 0 (Blacklist)** - Add an item with quantity 0 to prevent it from being looted during Loot All
- **Quantity "delete"** - Add an item with quantity set to "delete" to automatically delete that item during Loot All
- **Numpad . hotkey** - Open a bag and press Numpad . to rename it to "loot" for use as a loot bag
- **Leave corpse open** - Option to leave corpses open after looting
- **Disable when full** - Auto-disable when inventory is full
- **Save/Load loot lists** - Save and load named loot configurations
- **Item autocomplete** - Autocomplete suggestions when adding items
- **Loot logging** - All loot actions logged to file
- **Global/Local scope** - Rules can be global (all characters) or local (per character)

## Key Features

- Removed PvP checks - Handlers work without PvP restrictions
- Instant Agent FP casting on zone with retry
- Pet sit kits for pet classes (Engineer, MP, Bureaucrat)
- Spirit perks on cooldown toggle
- Auto-opens LootManager settings on load
