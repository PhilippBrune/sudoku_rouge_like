import os
import re
import unittest

import achievement_icons as icons

SERVICE = os.path.join(icons.ROOT, 'Assets', 'Scripts', 'Meta', 'SteamAchievementService.cs')


class AchievementIconsTests(unittest.TestCase):
    def test_every_new_achievement_has_an_icon_and_every_icon_an_achievement(self):
        source = open(SERVICE, encoding='utf-8-sig').read()
        listed = re.search(r'AllAchievementIds\s*=\s*\{(.*?)\};', source, re.S).group(1)
        ids = set(re.findall(r'"(\w+)"', listed))
        new_ids = {i for i in ids if not i.startswith('harmony_')}
        self.assertEqual(new_ids, set(icons.ICONS))

    def test_an_icon_pair_is_256_square_and_the_grey_one_has_no_colour(self):
        achieved, grey = icons.make(*icons.ICONS['first_win'])
        self.assertEqual(achieved.size, (256, 256))
        self.assertEqual(grey.size, (256, 256))
        self.assertGreater(len(set(achieved.getdata())), 1000, 'a painted icon, not a flat image')
        self.assertEqual(achieved.getpixel((3, 128)), icons.GOLD, 'gold frame when achieved')
        self.assertEqual(grey.getpixel((3, 128)), (150, 150, 150), 'grey frame when not')

    def test_unknown_names_stop_with_a_message(self):
        with self.assertRaises(SystemExit):
            icons.main(['--only', 'no_such_achievement'])


if __name__ == '__main__':
    unittest.main()
