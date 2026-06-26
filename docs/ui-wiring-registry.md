# Generated UI Wiring Registry

Generated menu UI is rebuilt at runtime, most visibly after language changes.
The registry records required generated controls and validates them after each
main menu build.

Current scope:

- Main menu primary/system buttons.
- Class select confirm/back buttons.
- Options language apply/cancel, options navigation, keybinding, accessibility,
  tutorial, and back buttons.
- Game modes buttons and Harmony controls.
- Profile load/delete/back buttons for the three local slots.

The registry tracks the intended action ID for each required control because
Unity does not expose runtime `Button.onClick` listener counts publicly. Register
required controls only after their runtime listener has been attached.

This is a guardrail, not full UI automation. It does not invoke controls and it
does not replace PlayMode traversal tests. Future UI work should extend the
required-control list before adding new release-critical menu actions.
