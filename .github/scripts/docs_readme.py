"""Write docs/README.md from README.md, with relative links rebased for docs/.

The root README links to files by their path from the repository root, such
as docs/blogs/post.md or src/Web. Copied verbatim into docs/, those links
resolve one directory too deep (docs/docs/blogs/post.md). This rewrites every
relative link target in Markdown links, reference definitions and HTML
src/href attributes: a docs/ prefix is dropped, and anything else gets ../.
Absolute URLs, anchors and root-relative paths are left alone, as is
everything inside fenced code blocks.

Usage: python3 .github/scripts/docs_readme.py [README.md] [docs/README.md]
"""

import re
import sys
from pathlib import Path

# ](target) or ](<target with spaces>), optionally followed by a "title".
INLINE_LINK = re.compile(r"(\]\()(?:(<)([^>\n]+)(>)|()([^)\s<>]+)())((?:\s+\"[^\"]*\")?\))")
REFERENCE = re.compile(r"^(\s{0,3}\[[^\]]+\]:\s*)(<?)(\S+?)(>?)(?=\s|$)", re.MULTILINE)
HTML_ATTRIBUTE = re.compile(r"""(\b(?:src|href)=)(["'])([^"']+)\2""")
FENCE = re.compile(r"^(\s*)(```|~~~)")


def rebase(target):
    """A link target as seen from docs/ rather than the repository root."""
    if re.match(r"^[a-z][a-z0-9+.-]*:", target, flags=re.IGNORECASE) or target.startswith(("#", "/", "../")):
        return target
    path = target[2:] if target.startswith("./") else target
    if path == "docs" or path.startswith(("docs/", "docs#", "docs?")):
        rest = path[len("docs"):].lstrip("/")
        return rest or "./"
    return "../" + path


def rewrite_links(markdown):
    """The README text with every relative link rebased for docs/README.md."""
    out = []
    in_fence = None
    for line in markdown.splitlines(keepends=True):
        fence = FENCE.match(line)
        if fence:
            if in_fence is None:
                in_fence = fence.group(2)
            elif fence.group(2) == in_fence:
                in_fence = None
            out.append(line)
            continue
        if in_fence is not None:
            out.append(line)
            continue
        line = INLINE_LINK.sub(
            lambda m: m.group(1)
            + (m.group(2) or m.group(5))
            + rebase(m.group(3) or m.group(6))
            + (m.group(4) or m.group(7))
            + m.group(8),
            line,
        )
        line = REFERENCE.sub(lambda m: m.group(1) + m.group(2) + rebase(m.group(3)) + m.group(4), line)
        line = HTML_ATTRIBUTE.sub(lambda m: m.group(1) + m.group(2) + rebase(m.group(3)) + m.group(2), line)
        out.append(line)
    return "".join(out)


def main(argv):
    source = Path(argv[1] if len(argv) > 1 else "README.md")
    target = Path(argv[2] if len(argv) > 2 else "docs/README.md")
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(rewrite_links(source.read_text(encoding="utf-8")), encoding="utf-8")


if __name__ == "__main__":
    main(sys.argv)
