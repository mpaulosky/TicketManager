#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
BLOGS_DIR = REPO_ROOT / 'docs' / 'blogs'
OUTPUT_PATH = REPO_ROOT / 'docs' / 'data' / 'blog-posts.json'
FILE_NAME_PATTERN = re.compile(
    r'^(?P<date>\d{4}-\d{2}-\d{2})-(?P<sequence>\d{2})-(?P<slug>.+)\.md$'
)
FRONT_MATTER_PATTERN = re.compile(r'^---\r?\n(.*?)\r?\n---(?:\r?\n|$)', re.DOTALL)
FRONT_MATTER_LINE_PATTERN = re.compile(r'^(?P<key>[A-Za-z0-9_]+):\s*(?P<value>.*)$')


def parse_front_matter(content: str) -> dict[str, str]:
    match = FRONT_MATTER_PATTERN.match(content)
    if match is None:
        return {}

    front_matter = match.group(1)
    lines = front_matter.splitlines()
    values: dict[str, str] = {}
    index = 0

    while index < len(lines):
        line_match = FRONT_MATTER_LINE_PATTERN.match(lines[index])
        if line_match is None:
            index += 1
            continue

        key = line_match.group('key')
        raw_value = line_match.group('value').strip()

        if raw_value.startswith('"'):
            value = raw_value[1:]
            while True:
                if value.endswith('"') and not value.endswith('\\"'):
                    value = value[:-1]
                    break

                index += 1
                if index >= len(lines):
                    raise ValueError(
                        f'Unterminated quoted front matter value for {key}.'
                    )

                value += f'\n{lines[index]}'

            normalized_value = re.sub(r'\r?\n\s*', ' ', value).strip()
            normalized_value = (
                normalized_value.replace('\\"', '"').replace('\\\\', '\\')
            )
            values[key] = normalized_value
        else:
            values[key] = raw_value

        index += 1

    return values


def content_without_front_matter(content: str) -> str:
    return FRONT_MATTER_PATTERN.sub('', content, count=1).strip()


def get_first_heading_title(content: str) -> str | None:
    match = re.search(r'^#\s+(.+?)\s*$', content_without_front_matter(content), re.MULTILINE)
    if match is None:
        return None

    return match.group(1).strip()


def get_first_paragraph_summary(content: str) -> str | None:
    body = content_without_front_matter(content)
    for paragraph in re.split(r'\r?\n\s*\r?\n', body):
        candidate = paragraph.strip()
        if not candidate or candidate.startswith('#'):
            continue

        return re.sub(r'\r?\n\s*', ' ', candidate).strip()

    return None


def build_post(markdown_path: Path) -> dict[str, object]:
    match = FILE_NAME_PATTERN.match(markdown_path.name)
    if match is None:
        raise ValueError(f'Unexpected blog filename format: {markdown_path.name}')

    content = markdown_path.read_text(encoding='utf-8')
    front_matter = parse_front_matter(content)

    title = front_matter.get('post_title') or get_first_heading_title(content)
    summary = front_matter.get('summary') or get_first_paragraph_summary(content)

    if not title:
        raise ValueError(
            f'Missing title for {markdown_path.name}. Expected front matter '
            'post_title or a first H1 heading.'
        )

    if not summary:
        raise ValueError(
            f'Missing summary for {markdown_path.name}. Expected front matter '
            'summary or a first paragraph.'
        )

    return {
        'fileName': markdown_path.name,
        'title': title,
        'summary': summary,
        'date': match.group('date'),
        'slug': match.group('slug'),
        'sequence': int(match.group('sequence')),
        'url': (
            'https://github.com/mpaulosky/TicketManager/blob/main/'
            f'docs/blogs/{markdown_path.name}'
        ),
    }


def main() -> None:
    if not BLOGS_DIR.is_dir():
        raise FileNotFoundError(f'Blogs directory not found: {BLOGS_DIR}')

    posts = [build_post(path) for path in BLOGS_DIR.glob('*.md')]
    posts.sort(
        key=lambda post: (post['date'], post['sequence'], post['fileName']),
        reverse=True,
    )

    document = {
        'generatedAt': datetime.now(timezone.utc)
        .isoformat(timespec='microseconds')
        .replace('+00:00', 'Z'),
        'posts': posts,
    }

    OUTPUT_PATH.write_text(
        json.dumps(document, indent=2, ensure_ascii=False) + '\n',
        encoding='utf-8',
    )


if __name__ == '__main__':
    main()
