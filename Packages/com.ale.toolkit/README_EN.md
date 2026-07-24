# Ale Toolkit

[简体中文](README.md) · [English](README_EN.md) · [日本語](README_JA.md)

A **general-purpose foundation library** for Unity plugin development. It carries no business-domain concepts, letting several plugins share one attribute-configuration system, list engine, editor framework and localization service.

> This package was split out of `com.ale.inventory` 1.8.0. The general-purpose capabilities that used to live inside the inventory system (the three-column editor framework, virtual scrolling lists, the custom attribute system, the trilingual editor UI) were extracted here so more plugins can reuse them.

---

## ⚠️ Installation (read this first)

**`com.ale.toolkit` must be installed before any plugin that depends on it.**

Unity's Package Manager **does not support git URLs in the `dependencies` field of `package.json`**, so a dependent plugin cannot pull this package automatically. You must install both manually, **in this order**:

`Window > Package Manager` → `+` in the top-left → `Install package from git URL...`

**Step 1 — install Toolkit first:**

```
(URL to be announced: this package will be published from its own repository)
```

**Step 2 — then install the dependent plugin**, for example the inventory system:

```
https://github.com/AleFeng/unity-ale-inventory-system.git?path=/Packages/com.ale.inventory#1.8.0
```

> If the order is reversed or this package is missing, Unity reports compile errors such as `Ale.Toolkit.* not found`. Just install this package and wait for the recompile — there is no need to reinstall the other plugin.

Requires **Unity 2022.3** or newer (developed and maintained on Unity 6000.3).

---

## Modules

| Module | Contents |
| --- | --- |
| **Attribute system** | `AttributeValue` with 20+ field types, attribute definitions (schema), custom enum types, number-format configs. Use it wherever configurable attribute entries are needed |
| **Sorting** | An element-type-agnostic sort engine: the host implements `ISortContext<TData>` to supply what comparison needs, the engine handles multi-level priorities and tiebreakers |
| **UI** | Virtual scrolling lists (grid / sequential, object pool + visible-region-only rendering), tab strips, filter bars, tooltip base classes, widget pools |
| **Editor framework** | Three-column tab base class, master list panel, entity list panel and tool window base — all generic over the database type |
| **Editor localization** | 中文 / English / 日本語 service, keyed by the Chinese source string, falling back automatically when a translation is missing |
| **Optional dependency support** | Macro toggles and adapters for TextMeshPro (`IS_TMP`), Unity Localization (`IS_LOCALIZATION`) and Addressables (`IS_ADDRESSABLE`) |

> Modules land progressively as the split proceeds; this version is still under construction. See the [CHANGELOG](CHANGELOG.md) for details.

---

## Assemblies

| Assembly Definition | Purpose | Macro constraint |
| --- | --- | --- |
| `Ale.Toolkit.Runtime` | Attribute system, sorting, asset-loading abstraction, shared serialization | — |
| `Ale.Toolkit.UI` | Virtual scrolling lists and general UI widgets | — |
| `Ale.Toolkit.UI.Localization` | Unity Localization adapter components | `IS_LOCALIZATION` |
| `Ale.Toolkit.Addressables.Runtime` | Addressables loading and handle management | `IS_ADDRESSABLE` |
| `Ale.Toolkit.Editor` | Editor framework, attribute drawers, localization service, macro toggles | — |
| `Ale.Toolkit.Addressables.Editor` | Addressables editor tooling | `IS_ADDRESSABLE` |

Dependencies flow one way: host plugin → `Ale.Toolkit.*`. This package never references a host plugin.

---

## License

[MIT](LICENSE.md)
