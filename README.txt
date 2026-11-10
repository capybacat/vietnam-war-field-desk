VIETNAM WAR - FIELD DESK
Version 0.4.0 Alpha | English | Windows x64
Release by CapybaCat

Field Desk is a standalone live editor for Vietnam War. Change your character's XP, skills, rank progression, money and supplies while the game is running.

COMPATIBILITY
- Steam app 1837480: Vietnam War.
- Supported Steam build: 26541584.
- Windows 10/11, 64-bit, with .NET Framework 4.8.
- The editor checks GameAssembly.dll SHA-256 before connecting. Other builds are rejected until a compatible profile is released.
- No mod loader or Cheat Engine is required. Use Manual Download; this is a separate application.

QUICK START
1. Extract the Main File archive to a folder of your choice.
2. Launch VietnamWarLiveEditor.exe.
3. Launch Vietnam War and load a character. The main menu has no editable character data.
4. Click Connect. Choose Personnel, Armory or Supply in the field journal.
5. Select a value, enter the desired amount and apply it.
6. Use the game's normal save/autosave to preserve progress.

The EXE includes its icon and artwork. It can be moved independently of the other files. Launch game opens the Steam game URI and does not require a specific Steam library folder. Run the editor at the same privilege level as the game.

FEATURES
Personnel
- Live XP, total XP, rank and skills.
- Set XP or add 1,000 / 10,000 XP.
- Promote by setting XP to a rank threshold read from the game. Unpause the game to let it handle promotion and rewards. Demotion is not supported.
- Edit individual skills, skill cap and skill-gain chance.
- Max all positive skills and the five weapon-training lists to the current skill cap.

Armory
- Edit primary, secondary, sidearm, melee and grenade training.
- Entries use the game's internal order. Individual weapon names are not mapped in this alpha.

Supply
- Cash and bank balance; cash shortcuts add $1,000 or $10,000.
- Rations, bandages, syringes and medical glue.
- Carried construction materials: 100 without a backpack or 150 with one.
- Field-base construction materials, fuel, food, water, beer, ammunition crates and reinforcement points.
- Set quantities, add quantities, or restock the selected supply group.

Utilities
- Undo last edit, if the game has not changed the affected values since the edit.
- Export current values to TSV.
- Always on top and a field-equipment-inspired interface.
- Ctrl+1 / Ctrl+2 / Ctrl+3: switch sections. Ctrl+R: connect.

LIMITS AND SAVE BEHAVIOUR
- Safe XP limit for this build: 9,999,999. The last available rank starts at 102,400 XP.
- Cash/bank input limit: $10,000,000.
- Bandages and syringes follow the character's current caps. Rations and glue have an editor input limit of 99.
- Personal restock targets: 10 rations/glue; bandages and syringes up to their caps.
- Field-base stocks are available only after your own field base has been created or loaded. Storage capacity depends on buildings; reinforcement points are limited to 2,000. Beer restock targets 100.
- Edits change process memory. The editor does not save the game or modify game files.
- Closing the editor does not undo edits. Undo does not retract ranks, items, achievements or other rewards the game has already granted.
- If the game updates a value after an edit, undo is blocked to avoid overwriting newer progress.
- Some game menus refresh money or materials only when reopened or after a game action.

ALPHA STATUS / TEST COVERAGE
Live memory write/read/undo was tested for XP plus total XP, Aim, a weapon-training entry, cash, bank balance, rations and carried construction materials. Group restock and undo of personal supplies were also tested. Original test values were restored. Invalid item fractions, negative money, medicine-cap overflow, NaN and invalid rank XP were rejected. TSV export was checked.

The field-base stock editor was checked only for refusal when a player-owned field base was absent. Writes to an active field base and all building-capacity combinations are not yet live-tested. Full rank promotion, max-all skills and persistence of the new resource edits after restarting the game have not been tested end-to-end. This is an alpha, not a claim of full coverage.

The English release adds English labels/messages, en-US number formatting and Steam-based launching. No new memory offsets or game-build support were added.

TROUBLESHOOTING
- Version mismatch: this exact build is unsupported; wait for an updated release/profile.
- No character data: load a character and reconnect.
- Field-base stock unavailable: create/load your own field base first.
- Access denied: match the game's privilege level; administrator rights are not requested by default.
- A stale in-game number: reopen the relevant game menu.

AI DISCLOSURE AND CREDITS
Release/maintenance: CapybaCat.
The majority of code, UI, English translation and documentation was created using Codex. The app emblem was generated with OpenAI ImageGen. Page copy was also prepared with AI. Nexus tags: AI-Generated Content and AI Media.
This is an independent fan utility, not an official game or military product.

SOURCE
The separate Source archive contains the application source, embedded artwork, field profile, manifest and Build.ps1. Build using the standard Windows .NET Framework C# compiler. No game binaries, Unity source files or saves are included. Follow the reuse permissions displayed on the Nexus mod page.
