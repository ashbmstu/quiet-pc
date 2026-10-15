# Design

Compact neumorphic windows. Short labels. Explanations in tooltips. Errors must remain specific.

## Controls

- Segoe UI: 10 pt controls, 34 pt countdown.
- Native checkboxes, number inputs and menus.
- `NeoButton`: raised surface, pressed inset, visible keyboard focus.
- Minimum button height: 44 logical pixels, including shadow padding.
- High contrast: native painting and system colours.

## Colours

| Use | Hex |
|---|---|
| Canvas | `#E8ECF2` |
| Surface | `#EBEEF4` |
| Text | `#202332` |
| Secondary text | `#62697A` |
| Accent | `#5B4AD1` |
| Warning | `#A86600` |

## Icons

| State | Shape | Colour |
|---|---|---|
| Off | Crossed moon | Slate |
| On | Crescent | Indigo |
| Error | Exclamation | Amber |

`Brand.CreateIcon` draws runtime icons. Keep tray icons flat and legible on both taskbar themes. Show timer information in text.

SVG assets: `logo`, `wordmark`, `banner`, `icon-states`. Original vectors; system fonts only.

## Checks

Check actual windows at 100%, 150% and 200% scaling: focus, keyboard activation, high contrast, errors and disabled controls. Offscreen renders may omit native combo-box text.

Screenshots should contain only the relevant app window. Exclude personal paths, device names and logs.
