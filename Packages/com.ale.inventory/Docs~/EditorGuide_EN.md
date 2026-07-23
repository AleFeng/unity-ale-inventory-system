# Editor Configuration Guide (Split into Each Subsystem's Docs)

<p align="center">
  🌍
  <a href="./EditorGuide.md">中文</a> |
  English |
  <a href="./EditorGuide_JA.md">日本語</a>
</p>

- Back to [documentation](../README_EN.md)

This document originally covered the editor configuration of items + warehouses together. It has now been split by subsystem; please go to the corresponding document:

| Topic | Document |
|------|------|
| Creating / opening a data file, system tabs overview | [Documentation – Quick Start](../README_EN.md#quick-start) |
| Enum types / function tags / item templates / items / item Inspector | [Item System](ItemSystem_EN.md) |
| Warehouse templates / warehouses / sorting / runtime API / save data | [Warehouse System](WarehouseSystem_EN.md) |
| Shop templates / shops / product groups / refresh / trade API | [Shop System](ShopSystem_EN.md) |
| Group tags / blueprint templates / blueprints / crafting API | [Crafting System](CraftingSystem_EN.md) |
| Group tags / equipment-group templates / slot lists / item limits / attribute bonuses / equip API | [Equipment System](EquipmentSystem_EN.md) |
| Group tags / skill templates / skills / item skill references / four sources / learned-skill API | [Skill System](SkillSystem_EN.md) |
| Attribute field type reference, retrieval / display / sort comparison | [Attribute System](AttributeSystem_EN.md) |
| UI components and prefab authoring | [UI Component Guide](UIComponentGuide_EN.md) |
| Architecture and design | [Architecture](Architecture_EN.md) |

---

## Common Entry-List Operations (Middle Column of All Systems)

The "entry list" in the middle column of the six system tabs (item / warehouse / shop / blueprint / equipment group / skill) has a unified structure and consistent operations:

- **Two-row structure**: each entry occupies two rows, a "column header row + value row", with consistent row height; the header shows each column's field name — after the ID come **name / description** in turn (reading the entry's `displayNameText` / `descriptionText` plain-text fallback), then each system's specific columns (type / primary group / slot group…).
- **Template filter + search**: the template filter tag bar and search box at the top narrow the list.
- **Drag-to-reorder**: press and hold the `≡` handle on the left to drag and adjust entry order.
- **Up/down keyboard selection switching**: after selecting an entry, press `↑` / `↓` to switch selection row by row among the currently visible (filtered) entries; when the newly selected item is out of the visible area, the list auto-scrolls one row to bring it back into view. It doesn't hijack the arrow keys while editing the search box / the right-side Inspector text box.
- **Add / delete**: "Add from Template" / "Quick Add" add entries; the `✕` at the end of a row deletes it.
