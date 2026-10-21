using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // Dropdown item wiring across Unity versions (split from OptionsUiFactory.cs).
    public static partial class OptionsUiFactory
    {
        private static void AttachDropdownItemCompat(GameObject itemGo, RectTransform itemRect,
            Text itemLabel, Image itemBg, Toggle itemTgl)
        {
            var compat = DropdownItemCompatResolver.Instance;
            if (!compat.IsAvailable)
            {
                Debug.LogWarning($"[OptionsUiFactory] DropdownItem compatibility unavailable: {compat.FailureReason}. Dropdowns may not work.");
                return;
            }

            var di = itemGo.GetComponent(compat.ItemType) ?? itemGo.AddComponent(compat.ItemType);
            if (di == null || !compat.Assign(di, itemRect, itemLabel, itemBg, itemTgl))
            {
                Debug.LogWarning("[OptionsUiFactory] Failed to attach DropdownItem compatibility component. Dropdowns may not work.");
            }
        }

        private sealed class DropdownItemCompatResolver
        {
            private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private static DropdownItemCompatResolver _instance;

            private readonly MemberSetter _text;
            private readonly MemberSetter _image;
            private readonly MemberSetter _toggle;
            private readonly MemberSetter _rectTransform;

            private DropdownItemCompatResolver(
                Type itemType,
                MemberSetter text,
                MemberSetter image,
                MemberSetter toggle,
                MemberSetter rectTransform,
                string failureReason)
            {
                ItemType = itemType;
                _text = text;
                _image = image;
                _toggle = toggle;
                _rectTransform = rectTransform;
                FailureReason = failureReason;
            }

            public static DropdownItemCompatResolver Instance
            {
                get
                {
                    if (_instance == null)
                        _instance = Create();
                    return _instance;
                }
            }

            public Type ItemType { get; }
            public string FailureReason { get; }
            public bool IsAvailable => ItemType != null
                && _text.IsAvailable
                && _image.IsAvailable
                && _toggle.IsAvailable
                && _rectTransform.IsAvailable;

            public bool Assign(Component target, RectTransform itemRect, Text itemLabel, Image itemBg, Toggle itemTgl)
            {
                if (!IsAvailable || target == null)
                    return false;

                _text.Set(target, itemLabel);
                _image.Set(target, itemBg);
                _toggle.Set(target, itemTgl);
                _rectTransform.Set(target, itemRect);
                return true;
            }

            private static DropdownItemCompatResolver Create()
            {
                var itemType = typeof(Dropdown).GetNestedType("DropdownItem",
                    BindingFlags.Public | BindingFlags.NonPublic);
                if (itemType == null)
                    return Unavailable("UnityEngine.UI.Dropdown.DropdownItem was not found");

                if (!typeof(Component).IsAssignableFrom(itemType))
                    return Unavailable("UnityEngine.UI.Dropdown.DropdownItem is not a Component");

                var text = MemberSetter.Resolve(itemType, "text", "m_Text", typeof(Text));
                var image = MemberSetter.Resolve(itemType, "image", "m_Image", typeof(Image));
                var toggle = MemberSetter.Resolve(itemType, "toggle", "m_Toggle", typeof(Toggle));
                var rectTransform = MemberSetter.Resolve(itemType, "rectTransform", "m_RectTransform", typeof(RectTransform));

                var failure = BuildFailureReason(text, image, toggle, rectTransform);
                return new DropdownItemCompatResolver(itemType, text, image, toggle, rectTransform, failure);
            }

            private static DropdownItemCompatResolver Unavailable(string reason)
            {
                return new DropdownItemCompatResolver(null, default, default, default, default, reason);
            }

            private static string BuildFailureReason(
                MemberSetter text,
                MemberSetter image,
                MemberSetter toggle,
                MemberSetter rectTransform)
            {
                if (text.IsAvailable && image.IsAvailable && toggle.IsAvailable && rectTransform.IsAvailable)
                    return null;

                var missing = new List<string>();
                if (!text.IsAvailable) missing.Add("text/m_Text");
                if (!image.IsAvailable) missing.Add("image/m_Image");
                if (!toggle.IsAvailable) missing.Add("toggle/m_Toggle");
                if (!rectTransform.IsAvailable) missing.Add("rectTransform/m_RectTransform");
                return "missing writable member(s): " + string.Join(", ", missing);
            }

            private readonly struct MemberSetter
            {
                private readonly PropertyInfo _property;
                private readonly FieldInfo _field;

                private MemberSetter(PropertyInfo property, FieldInfo field)
                {
                    _property = property;
                    _field = field;
                }

                public bool IsAvailable => _property != null || _field != null;

                public static MemberSetter Resolve(Type ownerType, string propertyName, string fieldName, Type valueType)
                {
                    var property = ownerType.GetProperty(propertyName, MemberFlags);
                    if (property != null
                        && property.CanWrite
                        && property.GetIndexParameters().Length == 0
                        && property.PropertyType.IsAssignableFrom(valueType))
                    {
                        return new MemberSetter(property, null);
                    }

                    var field = ownerType.GetField(fieldName, MemberFlags);
                    if (field != null && field.FieldType.IsAssignableFrom(valueType))
                        return new MemberSetter(null, field);

                    return default;
                }

                public void Set(Component target, object value)
                {
                    if (_property != null)
                    {
                        _property.SetValue(target, value, null);
                        return;
                    }

                    _field?.SetValue(target, value);
                }
            }
        }
    }
}
