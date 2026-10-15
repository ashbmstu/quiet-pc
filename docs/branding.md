# Visual design

Quiet PC uses a cool grey surface, soft raised buttons, and an indigo accent. Keep the windows small and the text sparse.

## Interface

Use Segoe UI, 10 pt for controls and 18 pt for a settings heading. The timer can use a large countdown. Use short labels: **Save**, **Close**, **Start**, **Restore**. Explanations belong in tooltips and accessible descriptions. Errors still need enough detail to be useful.

`Brand.Canvas` is `#E8ECF2`; `Brand.Surface` is `#EBEEF4`. Text uses `#202332`, supporting text `#62697A`, and the accent `#5B4AD1`. Warning text uses `#A86600`. Do not use pale grey for important text.

`NeoButton` inherits Windows Forms Button behavior. It paints opposing soft shadows for a raised surface and an inset gradient when pressed. `Brand.StylePrimary` gives it indigo text; `StyleSecondary` gives it normal text. Leave at least 44 logical pixels of height, including six pixels on each side for shadows. Keep a visible focus ring for keyboard users. High contrast uses native button painting and system colors.

Keep checkboxes, number inputs, and shortcut menus native. Decorative depth must not be the only sign that a control is interactive. Preserve keyboard activation, tab order, visible labels, and accessible names. Keep status feedback near the relevant controls.

## Icon

Retain the crescent moon and circular background. The icon uses both shape and color:

| State | Shape | Color |
| --- | --- | --- |
| Off | Crossed moon | Slate |
| Quiet | Crescent | Indigo |
| Needs attention | Exclamation | Amber |

`Brand.CreateIcon` draws the runtime icons. Keep them flat: soft shadows are for window controls and would blur a tiny tray icon. A timer belongs in text, not a small badge over the moon. Keep enough contrast on both light and dark taskbars.

## Copy

Use the name **Quiet PC**. Use `QuietPC` for code and executable names. Describe actual behavior: suppress Windows notifications, mute playback audio, restore previous settings. Avoid promises of complete silence; apps can draw their own alerts or bypass the Windows mixer.

A timer restores the previous settings. It does not necessarily unmute a device. **No timer** leaves quiet mode active; explain that through its tooltip and accessible name. Successful expiry should not create another interruption.

## Assets

- `logo.svg`: moon mark.
- `wordmark.svg`: mark with the name.
- `banner.svg`: repository header.
- `icon-states.svg`: three states with labels.

The vectors are original, with no external image or font dependency. SVG text uses the viewer's system font. Keep a quarter of the mark's width clear around it. Do not stretch or animate the icon.

## Checks before release

Inspect real windows at 100%, 150%, and 200% display scaling. Check mouse-down appearance, keyboard focus and activation, high contrast, long errors, and disabled actions. Inspect the tray icons at their actual size on both taskbar themes.

Use screenshots of the real build. Crop to the relevant window or tray menu. Exclude usernames, device names, paths, logs, and unrelated windows. Offscreen previews can omit native combo-box text; verify the actual selected value separately.
