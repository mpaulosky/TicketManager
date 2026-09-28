"""Tests for .github/scripts/docs_readme.py.

Usage: python3 -m unittest discover -s .github/scripts/tests -p 'test_docs_readme.py'
"""

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from docs_readme import rewrite_links  # noqa: E402


class RewriteLinksTests(unittest.TestCase):
    def test_docs_prefix_is_dropped(self):
        self.assertEqual(
            rewrite_links("| [Post](docs/blogs/2026-09-28-pr-1-x.md) |\n"),
            "| [Post](blogs/2026-09-28-pr-1-x.md) |\n",
        )

    def test_other_relative_paths_go_up_one_level(self):
        self.assertEqual(rewrite_links("[License](LICENSE) and [web](./src/Web)\n"), "[License](../LICENSE) and [web](../src/Web)\n")

    def test_absolute_anchor_and_root_relative_links_are_kept(self):
        text = "[a](https://example.com/docs/x.md) [b](#usage) [c](/docs/x) [d](mailto:me@example.com)\n"
        self.assertEqual(rewrite_links(text), text)

    def test_image_title_and_angle_brackets_are_kept(self):
        self.assertEqual(rewrite_links('![logo](docs/logo.png "Logo")\n'), '![logo](logo.png "Logo")\n')
        self.assertEqual(rewrite_links("[x](<docs/a b.md>)\n"), "[x](<a b.md>)\n")

    def test_reference_definitions_and_html_attributes(self):
        self.assertEqual(rewrite_links("[post]: docs/blogs/README.md\n"), "[post]: blogs/README.md\n")
        self.assertEqual(rewrite_links('<img src="docs/banner.png" alt="">\n'), '<img src="banner.png" alt="">\n')
        self.assertEqual(rewrite_links("<a href='CONTRIBUTING.md'>c</a>\n"), "<a href='../CONTRIBUTING.md'>c</a>\n")

    def test_the_docs_folder_itself(self):
        self.assertEqual(rewrite_links("[docs](docs/)\n"), "[docs](./)\n")

    def test_fenced_code_is_left_alone(self):
        text = "```md\n[x](docs/a.md)\n```\n[y](docs/b.md)\n"
        self.assertEqual(rewrite_links(text), "```md\n[x](docs/a.md)\n```\n[y](b.md)\n")


if __name__ == "__main__":
    unittest.main()
