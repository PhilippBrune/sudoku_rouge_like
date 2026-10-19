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


class ParseUserIdTests(unittest.TestCase):
    def test_a_snowflake_is_accepted_with_surrounding_spaces(self):
        self.assertEqual('101708598840692736', da.parse_user_id(' 101708598840692736 '))

    def test_names_mentions_short_numbers_and_unicode_digits_are_rejected(self):
        for bad in ('Kratossos', '<@101708598840692736>', '12345', '1' * 22, '10170859884069273\u00b2'):
            with self.assertRaises(SystemExit):
                da.parse_user_id(bad)


class GiveRoleTests(unittest.TestCase):
    """cmd_give_role against a fake server state; no network."""

    class FakeState:
        def __init__(self, gid):
            self.gid = gid
            self.roles = {'Admin': {'id': 'r1', 'position': 5, 'permissions': str(da.P['ADMINISTRATOR'])},
                          'Playtester': {'id': 'r2', 'position': 3, 'permissions': '0'},
                          '@bot': {'id': 'r0', 'position': 9, 'permissions': str(da.P['ADMINISTRATOR'])}}

        def role(self, name):
            return self.roles.get(name)

        def bot_perms(self):
            return da.P['ADMINISTRATOR']

    def setUp(self):
        self.calls = []
        self.member = {'user': {'username': 'tester\x1b[31m'}, 'roles': []}
        self._patch('State', self.FakeState)
        self._patch('guild_id', lambda layout=None: 'g')
        self._patch('api', self.fake_api)

    def _patch(self, name, value):
        original = getattr(da, name)
        setattr(da, name, value)
        self.addCleanup(setattr, da, name, original)

    def fake_api(self, method, path, body=None, reason=None):
        self.calls.append((method, path))
        return self.member if method == 'GET' else None

    def test_a_normal_role_is_given_once(self):
        da.cmd_give_role({}, 'Playtester', '101708598840692736')
        self.assertIn(('PUT', '/guilds/g/members/101708598840692736/roles/r2'), self.calls)

    def test_an_administrator_role_needs_yes(self):
        with self.assertRaises(SystemExit):
            da.cmd_give_role({}, 'Admin', '101708598840692736')
        self.assertEqual([], self.calls)
        da.cmd_give_role({}, 'Admin', '101708598840692736', confirmed=True)
        self.assertIn(('PUT', '/guilds/g/members/101708598840692736/roles/r1'), self.calls)

    def test_a_member_who_has_the_role_is_left_alone(self):
        self.member['roles'] = ['r2']
        da.cmd_give_role({}, 'Playtester', '101708598840692736')
        self.assertEqual([('GET', '/guilds/g/members/101708598840692736')], self.calls)

    def test_names_lose_control_characters(self):
        self.assertEqual('tester[31m', da.printable('tester\x1b[31m'))


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
