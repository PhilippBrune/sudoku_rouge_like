using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.UI;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class FontAssetServiceTests
    {
        private const string ProductionFontAssetPath = "Assets/Resources/Fonts/MainFont.ttf";
        private const string ProductionFontMetaPath = "Assets/Resources/Fonts/MainFont.ttf.meta";
        private const string ProductionFontNoticePath = "docs/legal/third-party/Roboto-NOTICE.md";
        private const string ProductionFontLicensePath = "docs/legal/third-party/APACHE-2.0.txt";

        private static readonly int[] RequiredGermanCodepoints =
        {
            0x00E4,
            0x00F6,
            0x00FC,
            0x00C4,
            0x00D6,
            0x00DC,
            0x00DF
        };

        [TearDown]
        public void TearDown()
        {
            FontAssetService.ResetCacheForTests();
        }

        [Test]
        public void ProductionFontPath_IsStableAndExtensionless()
        {
            Assert.AreEqual("Fonts/MainFont", FontAssetService.ProductionFontResourcePath);
            Assert.IsFalse(FontAssetService.ProductionFontResourcePath.Contains("\\"));
            Assert.IsFalse(FontAssetService.ProductionFontResourcePath.EndsWith(".ttf"));
            Assert.IsFalse(FontAssetService.ProductionFontResourcePath.EndsWith(".otf"));
        }

        [Test]
        public void FallbackFontNames_AreDeclared()
        {
            Assert.AreEqual("LegacyRuntime.ttf", FontAssetService.PrimaryFallbackFontName);
            Assert.AreEqual("Arial.ttf", FontAssetService.SecondaryFallbackFontName);
        }

        [Test]
        public void ProductionFontAsset_IsBundledWithUnityMetaAndLegalNotice()
        {
            Assert.IsTrue(File.Exists(ProductionFontAssetPath), "Expected production font at the Resources path used by FontAssetService.");
            Assert.IsTrue(File.Exists(ProductionFontMetaPath), "Expected Unity meta file for stable font asset identity.");
            Assert.Greater(new FileInfo(ProductionFontAssetPath).Length, 0, "Production font file is empty.");
            Assert.IsTrue(File.Exists(ProductionFontNoticePath), "Expected third-party font legal notice.");
            Assert.IsTrue(File.Exists(ProductionFontLicensePath), "Expected bundled Apache 2.0 license text for the production font.");
        }

        [Test]
        public void ProductionFontAsset_CoversRequiredGermanGlyphs()
        {
            var fontBytes = File.ReadAllBytes(ProductionFontAssetPath);

            foreach (var codepoint in RequiredGermanCodepoints)
            {
                Assert.IsTrue(
                    TrueTypeFontHasCodepoint(fontBytes, codepoint),
                    $"Production font is missing required German glyph U+{codepoint:X4}.");
            }
        }

        [Test]
        public void GetFont_ReturnsUsableFont()
        {
            FontAssetService.ResetCacheForTests();

            var font = FontAssetService.GetFont();

            Assert.IsNotNull(font);
        }

        private static bool TrueTypeFontHasCodepoint(byte[] fontBytes, int codepoint)
        {
            var cmapOffset = FindTableOffset(fontBytes, "cmap");
            if (cmapOffset < 0)
                return false;

            var encodingTableCount = ReadUInt16BE(fontBytes, cmapOffset + 2);
            var seenSubtables = new HashSet<uint>();

            for (var i = 0; i < encodingTableCount; i++)
            {
                var recordOffset = cmapOffset + 4 + i * 8;
                var subtableRelativeOffset = ReadUInt32BE(fontBytes, recordOffset + 4);
                if (!seenSubtables.Add(subtableRelativeOffset))
                    continue;

                var subtableOffset = cmapOffset + (int)subtableRelativeOffset;
                var format = ReadUInt16BE(fontBytes, subtableOffset);

                if (format == 4 && codepoint <= 0xFFFF && Format4HasCodepoint(fontBytes, subtableOffset, codepoint))
                    return true;

                if (format == 12 && Format12HasCodepoint(fontBytes, subtableOffset, codepoint))
                    return true;
            }

            return false;
        }

        private static int FindTableOffset(byte[] fontBytes, string tag)
        {
            if (fontBytes.Length < 12)
                return -1;

            var tableCount = ReadUInt16BE(fontBytes, 4);
            for (var i = 0; i < tableCount; i++)
            {
                var tableRecordOffset = 12 + i * 16;
                if (tableRecordOffset + 16 > fontBytes.Length)
                    return -1;

                if (fontBytes[tableRecordOffset] != tag[0]
                    || fontBytes[tableRecordOffset + 1] != tag[1]
                    || fontBytes[tableRecordOffset + 2] != tag[2]
                    || fontBytes[tableRecordOffset + 3] != tag[3])
                {
                    continue;
                }

                var offset = ReadUInt32BE(fontBytes, tableRecordOffset + 8);
                return offset <= int.MaxValue ? (int)offset : -1;
            }

            return -1;
        }

        private static bool Format4HasCodepoint(byte[] fontBytes, int subtableOffset, int codepoint)
        {
            var length = ReadUInt16BE(fontBytes, subtableOffset + 2);
            var subtableEnd = subtableOffset + length;
            var segmentCount = ReadUInt16BE(fontBytes, subtableOffset + 6) / 2;
            var endCodeOffset = subtableOffset + 14;
            var startCodeOffset = endCodeOffset + segmentCount * 2 + 2;
            var idDeltaOffset = startCodeOffset + segmentCount * 2;
            var idRangeOffsetOffset = idDeltaOffset + segmentCount * 2;

            for (var i = 0; i < segmentCount; i++)
            {
                var endCode = ReadUInt16BE(fontBytes, endCodeOffset + i * 2);
                var startCode = ReadUInt16BE(fontBytes, startCodeOffset + i * 2);

                if (codepoint < startCode || codepoint > endCode)
                    continue;

                var idDelta = ReadInt16BE(fontBytes, idDeltaOffset + i * 2);
                var idRangeOffsetAddress = idRangeOffsetOffset + i * 2;
                var idRangeOffset = ReadUInt16BE(fontBytes, idRangeOffsetAddress);

                if (idRangeOffset == 0)
                    return ((codepoint + idDelta) & 0xFFFF) != 0;

                var glyphIndexOffset = idRangeOffsetAddress + idRangeOffset + (codepoint - startCode) * 2;
                if (glyphIndexOffset + 2 > subtableEnd || glyphIndexOffset + 2 > fontBytes.Length)
                    return false;

                var glyphIndex = ReadUInt16BE(fontBytes, glyphIndexOffset);
                if (glyphIndex == 0)
                    return false;

                return ((glyphIndex + idDelta) & 0xFFFF) != 0;
            }

            return false;
        }

        private static bool Format12HasCodepoint(byte[] fontBytes, int subtableOffset, int codepoint)
        {
            var groupCount = ReadUInt32BE(fontBytes, subtableOffset + 12);
            var targetCodepoint = (uint)codepoint;

            for (var i = 0u; i < groupCount; i++)
            {
                var groupOffset = subtableOffset + 16 + (int)i * 12;
                var startCharCode = ReadUInt32BE(fontBytes, groupOffset);
                var endCharCode = ReadUInt32BE(fontBytes, groupOffset + 4);

                if (targetCodepoint >= startCharCode && targetCodepoint <= endCharCode)
                    return true;
            }

            return false;
        }

        private static ushort ReadUInt16BE(byte[] bytes, int offset)
        {
            return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
        }

        private static short ReadInt16BE(byte[] bytes, int offset)
        {
            return unchecked((short)ReadUInt16BE(bytes, offset));
        }

        private static uint ReadUInt32BE(byte[] bytes, int offset)
        {
            return ((uint)bytes[offset] << 24)
                | ((uint)bytes[offset + 1] << 16)
                | ((uint)bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }
    }
}
