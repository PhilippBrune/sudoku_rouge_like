using System;
using System.Reflection;
using NUnit.Framework;
using SudokuRoguelike.Core;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class DropdownCompatibilityTests
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private LanguageOption _originalLanguage;

        [SetUp]
        public void SetUp()
        {
            _originalLanguage = LocalizationService.Current;
            LocalizationService.SetLanguage(LanguageOption.English);
        }

        [TearDown]
        public void TearDown()
        {
            LocalizationService.SetLanguage(_originalLanguage);
        }

        [Test]
        public void GeneratedOptionsDropdowns_AttachUnityDropdownItemCompatibilityMembers()
        {
            var hadEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null;
            var host = new GameObject("DropdownCompatibilityHost");

            try
            {
                host.AddComponent<MainMenuBlueprintBuilder>().Build();

                AssertDropdownCompatibility(host, "ResolutionDropdown");
                AssertDropdownCompatibility(host, "LanguageDropdown");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);

                if (!hadEventSystem)
                {
                    var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                    if (eventSystem != null)
                        UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                }
            }
        }

        private static void AssertDropdownCompatibility(GameObject host, string dropdownName)
        {
            var dropdown = FindDropdown(host, dropdownName);
            Assert.NotNull(dropdown.captionText, dropdownName);
            Assert.NotNull(dropdown.template, dropdownName);
            Assert.NotNull(dropdown.itemText, dropdownName);

            var item = dropdown.template.Find("Viewport/Content/Item") as RectTransform;
            Assert.NotNull(item, dropdownName);

            var itemType = typeof(Dropdown).GetNestedType("DropdownItem", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(itemType, dropdownName);
            Assert.IsTrue(typeof(Component).IsAssignableFrom(itemType), dropdownName);

            var compatibilityComponent = item.gameObject.GetComponent(itemType);
            Assert.NotNull(compatibilityComponent, dropdownName);

            var expectedLabel = item.Find("Item Label")?.GetComponent<Text>();
            var expectedImage = item.GetComponent<Image>();
            var expectedToggle = item.GetComponent<Toggle>();
            Assert.NotNull(expectedLabel, dropdownName);
            Assert.NotNull(expectedImage, dropdownName);
            Assert.NotNull(expectedToggle, dropdownName);

            Assert.AreSame(expectedLabel, ReadMember<Text>(compatibilityComponent, "text", "m_Text"), dropdownName);
            Assert.AreSame(expectedImage, ReadMember<Image>(compatibilityComponent, "image", "m_Image"), dropdownName);
            Assert.AreSame(expectedToggle, ReadMember<Toggle>(compatibilityComponent, "toggle", "m_Toggle"), dropdownName);
            Assert.AreSame(item, ReadMember<RectTransform>(compatibilityComponent, "rectTransform", "m_RectTransform"), dropdownName);
        }

        private static Dropdown FindDropdown(GameObject host, string name)
        {
            foreach (var dropdown in host.GetComponentsInChildren<Dropdown>(true))
            {
                if (dropdown.name == name)
                    return dropdown;
            }

            Assert.Fail($"Dropdown '{name}' was not found.");
            return null;
        }

        private static T ReadMember<T>(Component target, string propertyName, string fieldName) where T : class
        {
            var type = target.GetType();
            var property = type.GetProperty(propertyName, MemberFlags);
            if (property != null && typeof(T).IsAssignableFrom(property.PropertyType))
                return property.GetValue(target, null) as T;

            var field = type.GetField(fieldName, MemberFlags);
            if (field != null && typeof(T).IsAssignableFrom(field.FieldType))
                return field.GetValue(target) as T;

            throw new MissingMemberException(type.FullName, $"{propertyName}/{fieldName}");
        }
    }
}
