using SudokuRoguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-034] [REQ: INPUT-PROFILE-002] Select Profile · Three ema (owner's pick, RM-217): each profile is a
    // wooden prayer plaque on a beam in the lantern garden: the class last walked with its level, runs and wins, bosses,
    // last played and a seal for a saved run; the active one framed in vermilion with its lantern lit; an empty plaque
    // invites a new garden. Delete is a quiet link that asks first. MainMenuController.RefreshProfileSelectCards fills it.
    public sealed partial class MainMenuBlueprintBuilder
    {
        private static readonly float[] EmaLeft = { 0.1615f, 0.3958f, 0.6302f };
        private const float EmaWidth = 0.2083f, EmaBottom = 0.2685f, EmaTop = 0.75f;

        private GameObject BuildProfileSelectPanel(RectTransform root, MainMenuController mc)
        {
            var pr = EnsureRect("ProfileSelectPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ScrollUiKit.DestroyChildrenNow(pr);
            var backdrop = EnsureOrGetImage(pr.gameObject, BgColor);
            if (ApplyPanelBackground(backdrop, "background/bg_menu_profile"))
                backdrop.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            var content = ScrollUiKit.Area(pr, "Content", Vector2.zero, Vector2.one);   // the controller finds the cards here

            ScrollUiKit.Banner(content, "ProfileBanner", new Vector2(0.318f, 0.844f), new Vector2(0.682f, 0.957f),
                "ProfileEyebrow", T("Profiles").ToUpperInvariant(), "Title", T("ProfileSelect.Title"), 46);
            var beam = ScrollUiKit.Fill(content, "ProfileBeam", new Vector2(0.130f, 0.770f), new Vector2(0.870f, 0.791f),
                new Color(0.55f, 0.36f, 0.20f, 1f), ScrollUiKit.Rounded(10f));
            beam.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);

            var loads = new Button[3];
            var deletes = new Button[3];
            for (var i = 0; i < 3; i++)
                (loads[i], deletes[i]) = BuildSlotCard(content, mc, i, EmaLeft[i], EmaLeft[i] + EmaWidth);

            var back = ScrollUiKit.RimButton(content, "BtnProfileBack", new Vector2(0.469f, 0.150f), new Vector2(0.531f, 0.203f),
                T("Back"), 22, ScrollUiKit.Gold, new Color(0.45f, 0.33f, 0.14f, 0.95f), ScrollUiKit.PaperText);
            back.onClick.AddListener(mc.BackFromPanel);   // GAP-084: back to the screen before
            for (var i = 0; i < 3; i++)
            {
                loads[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit, selectOnLeft = i > 0 ? loads[i - 1] : null, selectOnRight = deletes[i],
                    selectOnDown = back, selectOnUp = loads[i].transform.parent.Find($"BtnRename_{i}")?.GetComponent<Button>(),
                };
                deletes[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit, selectOnLeft = loads[i], selectOnRight = i < 2 ? loads[i + 1] : null,
                    selectOnDown = back,
                };
            }
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = loads[1] };

            var hint = ScrollUiKit.Label(content, "ProfileKeyHint", string.Empty, 18, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.79f, 0.66f, 1f), new Vector2(0.30f, 0.090f), new Vector2(0.70f, 0.125f), 11);
            DeviceHint.Attach(hint, "ProfileSelect.KeyHint");   // INPUT-HINT-001

            pr.gameObject.SetActive(false);
            return pr.gameObject;
        }

        // One ema: the plaque, its cord, a glow and a frame for the active profile, the class disc, the name, the record, the
        // saved run's seal, Play (or Create) and Delete.
        private (Button load, Button delete) BuildSlotCard(RectTransform parent, MainMenuController mc, int slotIndex,
            float xMin, float xMax)
        {
            var glow = ScrollUiKit.Spot(parent, $"SlotGlow_{slotIndex}", new Vector2((xMin + xMax) * 0.5f, 0.72f),
                new Vector2(420f, 320f), new Color(0.96f, 0.64f, 0.35f, 0.55f), ScrollUiKit.Glow());
            glow.raycastTarget = false;
            ScrollUiKit.Fill(parent, $"SlotCord_{slotIndex}", new Vector2((xMin + xMax) * 0.5f - 0.0006f, EmaTop),
                new Vector2((xMin + xMax) * 0.5f + 0.0006f, 0.781f), ScrollUiKit.Gold).raycastTarget = false;
            var frame = ScrollUiKit.Fill(parent, $"SlotFrame_{slotIndex}", new Vector2(xMin - 0.004f, EmaBottom - 0.007f),
                new Vector2(xMax + 0.004f, EmaTop + 0.007f), new Color(1f, 0.35f, 0.25f, 1f), ScrollUiKit.Ema());
            frame.raycastTarget = false;
            var card = ScrollUiKit.Fill(parent, $"SlotCard_{slotIndex}", new Vector2(xMin, EmaBottom), new Vector2(xMax, EmaTop),
                Color.white, ScrollUiKit.Ema());   // the ema sprite brings its wood
            card.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            var disc = ScrollUiKit.Spot(card.transform, "ClassDisc", new Vector2(0.5f, 0.735f), new Vector2(112f, 112f),
                new Color(0.16f, 0.10f, 0.06f, 1f), ScrollUiKit.Disc());
            disc.raycastTarget = false;
            ScrollUiKit.Icon(disc.transform, "ClassIcon", null, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Color.white);
            ScrollUiKit.Label(disc.transform, "Plus", "+", 64, TextAnchor.MiddleCenter, new Color(0.92f, 0.86f, 0.72f, 1f),
                Vector2.zero, Vector2.one);
            ScrollUiKit.Label(card.transform, $"SlotLabel_{slotIndex}",
                LocalizationService.Format("ProfileSelect.SlotLabel", "Profile {0}", slotIndex + 1), 34, TextAnchor.MiddleCenter,
                ScrollUiKit.Ink, new Vector2(0.05f, 0.53f), new Vector2(0.95f, 0.615f), 18);
            ScrollUiKit.Label(card.transform, "Who", string.Empty, 20, TextAnchor.MiddleCenter, ScrollUiKit.InkSoft,
                new Vector2(0.05f, 0.47f), new Vector2(0.95f, 0.53f), 12);
            ScrollUiKit.Label(card.transform, $"SlotInfo_{slotIndex}", string.Empty, 20, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.46f), 12).lineSpacing = 1.25f;
            ScrollUiKit.Label(card.transform, "Numbers", string.Empty, 20, TextAnchor.UpperRight, ScrollUiKit.Vermilion,
                new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.46f), 12).lineSpacing = 1.25f;
            var seal = ScrollUiKit.Fill(card.transform, "RunSeal", new Vector2(0.18f, 0.155f), new Vector2(0.82f, 0.205f), ScrollUiKit.Vermilion);
            seal.raycastTarget = false;
            ScrollUiKit.Label(seal.transform, "Text", string.Empty, 14, TextAnchor.MiddleCenter, new Color(0.98f, 0.93f, 0.88f, 1f),
                Vector2.zero, Vector2.one, 8);

            var btnLoad = ScrollUiKit.PillButton(card.transform, $"BtnLoad_{slotIndex}", new Vector2(0.24f, 0.035f),
                new Vector2(0.56f, 0.125f), T("ProfileSelect.Play"), 20, ScrollUiKit.Ink, ScrollUiKit.PaperText, 22f);
            var capturedSlot = slotIndex;
            btnLoad.onClick.AddListener(() => mc.SelectProfileSlot(capturedSlot));
            var btnRename = ScrollUiKit.PillButton(card.transform, $"BtnRename_{slotIndex}", new Vector2(0.30f, 0.135f),
                new Vector2(0.70f, 0.205f), T("ProfileSelect.Rename"), 17, new Color(0.89f, 0.80f, 0.61f, 1f), ScrollUiKit.InkSoft, 14f);   // [REQ: PROFILE-NAME-001]
            btnRename.onClick.AddListener(() => mc.BeginRenameProfile(capturedSlot));
            btnRename.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = btnLoad };
            var btnDelete = ScrollUiKit.PillButton(card.transform, $"BtnDelete_{slotIndex}", new Vector2(0.60f, 0.045f),
                new Vector2(0.84f, 0.115f), T("Delete"), 17, new Color(0.89f, 0.80f, 0.61f, 1f), ScrollUiKit.InkSoft, 14f);
            btnDelete.onClick.AddListener(() => mc.DeleteProfileSlot(capturedSlot));

            var centre = (xMin + xMax) * 0.5f;
            var active = ScrollUiKit.Fill(parent, $"SlotActive_{slotIndex}", new Vector2(centre - 0.024f, 0.697f),
                new Vector2(centre + 0.024f, 0.722f), ScrollUiKit.Ink);
            active.raycastTarget = false;
            ScrollUiKit.Label(active.transform, "Text", T("ProfileSelect.Active").ToUpperInvariant(), 14, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.87f, 0.65f, 1f), Vector2.zero, Vector2.one, 8);
            return (btnLoad, btnDelete);
        }
    }
}
