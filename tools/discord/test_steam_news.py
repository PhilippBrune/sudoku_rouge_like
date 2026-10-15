"""Offline checks for tools/discord/steam_news.py (no network, no token).

    python -m unittest discover -s tools/discord -p "test_*.py"
"""
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))
import steam_news as sn  # noqa: E402

LAYOUT = {'guild_id': '1', 'steam_news': [
    {'key': 'rotn', 'game': 'Run of the Nine', 'app_id': 4858470, 'channel': 'rotn-news', 'ping_role': 'Patch Notes'}]}


def item(gid, date, title='Update'):
    return {'gid': gid, 'date': date, 'title': title, 'url': 'https://example/x', 'contents': 'Text',
            'feedname': 'steam_community_announcements'}


class MarkdownTests(unittest.TestCase):
    def test_bbcode_becomes_discord_markdown(self):
        src = ('[img]{STEAM_CLAN_IMAGE}/1/a.png[/img]Intro\n\n[h2]Highlights[/h2]\n[list]\n[*][b]Resume works.[/b] Yes\n'
               '[*]See [url=https://x.y]the notes[/url][/list]')
        self.assertEqual('Intro\n\n**Highlights**\n\n• **Resume works.** Yes\n• See [the notes](https://x.y)',
                         sn.to_markdown(src))
        self.assertEqual('https://clan.akamai.steamstatic.com/images/1/a.png', sn.first_image(src))

    def test_long_text_is_cut_at_a_line_with_an_ellipsis(self):
        text = '\n'.join(f'line {i:04d} ' + 'x' * 40 for i in range(100))
        short = sn.excerpt(text, 500)
        self.assertLessEqual(len(short), 502)
        self.assertTrue(short.endswith('\n…'))


class RunTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        for name, value in (('STATE_DIR', Path(tmp.name)), ('STATE', Path(tmp.name) / 's.json'),
                            ('LOG', Path(tmp.name) / 'l.log')):
            patcher = mock.patch.object(sn, name, value)
            patcher.start()
            self.addCleanup(patcher.stop)

    def test_first_run_records_without_posting_then_posts_only_new_oldest_first(self):
        posted = []
        with mock.patch.object(sn, 'fetch_news', return_value=[item('2', 20), item('1', 10)]), \
                mock.patch.object(sn, 'post', side_effect=lambda *a: posted.append(a)):
            sn.cmd_run(LAYOUT)
        self.assertEqual([], posted)
        self.assertEqual(['2', '1'], sn.load_state()['4858470']['seen'])

        fake_state = mock.Mock()
        fake_state.role.return_value = {'id': '99'}
        with mock.patch.object(sn, 'fetch_news', return_value=[item('4', 40, 'B'), item('3', 30, 'A'), item('2', 20)]), \
                mock.patch.object(sn.da, 'State', return_value=fake_state), \
                mock.patch.object(sn.da, 'guild_id', return_value='1'), \
                mock.patch.object(sn, 'announcement_url', return_value='https://example/a'), \
                mock.patch.object(sn, 'post', side_effect=lambda st, ch, msg: posted.append((ch, msg))):
            sn.cmd_run(LAYOUT)
        self.assertEqual(['A', 'B'], [m['embeds'][0]['title'] for _, m in posted])
        self.assertEqual({'rotn-news'}, {ch for ch, _ in posted})
        self.assertEqual({'parse': [], 'roles': ['99']}, posted[0][1]['allowed_mentions'])
        self.assertEqual(['2', '1', '3', '4'], sn.load_state()['4858470']['seen'])

    def test_unreleased_game_is_skipped(self):
        with mock.patch.object(sn, 'fetch_news', side_effect=sn.NotPublic('not yet')):
            sn.cmd_run(LAYOUT)
        self.assertEqual({}, sn.load_state())


if __name__ == '__main__':
    unittest.main()
