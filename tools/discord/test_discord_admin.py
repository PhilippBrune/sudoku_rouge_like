"""Tests for the offline parts of discord_admin.py (message splitting and image attachments). No network.

    python -m unittest tools/discord/test_discord_admin.py
"""
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import discord_admin as da  # noqa: E402


class SplitMessageTests(unittest.TestCase):
    def test_a_dash_line_starts_a_new_message(self):
        self.assertEqual(['one', 'two'], da.split_message('one\n---\ntwo'))

    def test_long_text_is_split_between_paragraphs_never_inside(self):
        para = 'x' * 1500
        chunks = da.split_message(para + '\n\n' + para)
        self.assertEqual([para, para], chunks)


class TakeImagesTests(unittest.TestCase):
    def test_image_lines_become_attachments_and_leave_the_text(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            (base / 'roadmap.png').write_bytes(b'\x89PNG')
            text, files = da.take_images('# Roadmap\n![The roadmap](roadmap.png)\nPlans, not promises.', base)
        self.assertEqual('# Roadmap\n\nPlans, not promises.', text)
        self.assertEqual(['roadmap.png'], [f.name for f in files])

    def test_a_missing_image_stops_before_anything_is_posted(self):
        with tempfile.TemporaryDirectory() as tmp, self.assertRaises(SystemExit):
            da.take_images('![x](nope.png)', Path(tmp))

    def test_only_image_files_can_be_attached(self):
        with tempfile.TemporaryDirectory() as tmp, self.assertRaises(SystemExit):
            base = Path(tmp)
            (base / '.env').write_text('DISCORD_BOT_TOKEN=x', encoding='utf-8')
            da.take_images('![x](.env)', base)

    def test_inline_image_syntax_inside_a_sentence_is_left_alone(self):
        text, files = da.take_images('See ![x](a.png) here', Path('.'))
        self.assertEqual('See ![x](a.png) here', text)
        self.assertEqual([], files)


if __name__ == '__main__':
    unittest.main()
