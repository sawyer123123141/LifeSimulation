"""Split a long markdown document into per-section files and leave an index behind.

Why this exists
---------------
AGENT_FIELD_NOTES.md reached 1,752 lines and SESSION_HANDOFF.md 1,559, and both are files
an agent is told to read for context at the start of a session. That is thousands of lines
of reading before any work happens, most of it archival.

Splitting them by hand would mean reading them by hand, which is the cost being removed.
This does it mechanically: sections are lifted whole on their heading lines, nothing is
rewritten, and nothing is summarised - so a split cannot change what the documents say.

Usage
-----
    python tools/split_doc.py docs/AGENT_FIELD_NOTES.md docs/field-notes --keep "1." "2."
    python tools/split_doc.py docs/some-file.md docs/out --level 3

Sections whose heading starts with one of the --keep prefixes stay inline in the original.
Everything else moves to <out_dir>/<slug>.md and is replaced by a one-line link carrying the
section's first real line as a hook, so the index says what is in each file rather than only
naming it. --level picks the heading depth to split on (2 means "## ").
"""

import argparse
import io
import os
import re
import sys


def slugify(heading):
    slug = heading.lower()
    slug = re.sub(r"[`*_]", "", slug)
    slug = re.sub(r"[^a-z0-9]+", "-", slug)
    return slug.strip("-")[:60] or "section"


def first_sentence(body):
    """The section's first real line, trimmed, as a hook for the index."""
    for line in body.split("\n"):
        stripped = line.strip()
        if not stripped or stripped.startswith(("#", ">", "|", "-", "*", "`")):
            continue
        stripped = re.sub(r"[`*]", "", stripped)
        return stripped[:110].rstrip(" ,.;:") + ("..." if len(stripped) > 110 else "")
    return ""


def split(path, out_dir, keep_prefixes, level=2):
    text = io.open(path, encoding="utf-8").read()
    lines = text.split("\n")

    marker = ("#" * level) + " "
    starts = [i for i, line in enumerate(lines) if line.startswith(marker)]
    if not starts:
        print("no " + marker.strip() + " sections in " + path)
        return

    # Everything before the first heading is the preamble and always stays.
    preamble = lines[: starts[0]]
    bounds = starts + [len(lines)]
    os.makedirs(out_dir, exist_ok=True)

    kept, moved = [], []
    for index in range(len(starts)):
        heading = lines[bounds[index]][level + 1:].strip()
        body = "\n".join(lines[bounds[index]: bounds[index + 1]]).rstrip() + "\n"

        if any(heading.startswith(prefix) for prefix in keep_prefixes):
            kept.append(body)
            continue

        name = slugify(heading) + ".md"
        io.open(os.path.join(out_dir, name), "w", encoding="utf-8", newline="").write(body)
        relative = os.path.relpath(
            os.path.join(out_dir, name), os.path.dirname(path)).replace("\\", "/")
        moved.append((heading, relative, first_sentence(body), len(body.split("\n"))))

    out = "\n".join(preamble).rstrip() + "\n"
    if moved:
        out += "\n## Sections held in separate files\n\n"
        out += "Lifted whole, nothing rewritten. Read the one you need.\n\n"
        for heading, relative, hook, count in moved:
            out += "- [" + heading + "](" + relative + ") - " + str(count) + " lines"
            out += ((". " + hook) if hook else "") + "\n"
        out += "\n"
    if kept:
        out += "\n".join(kept).rstrip() + "\n"

    io.open(path, "w", encoding="utf-8", newline="").write(out)
    print(
        os.path.basename(path) + ": " + str(len(lines)) + " -> "
        + str(len(out.split("\n"))) + " lines, "
        + str(len(moved)) + " sections moved to " + out_dir)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("path")
    parser.add_argument("out_dir")
    parser.add_argument("--keep", nargs="*", default=[])
    parser.add_argument("--level", type=int, default=2)
    arguments = parser.parse_args()
    split(arguments.path, arguments.out_dir, arguments.keep, arguments.level)


if __name__ == "__main__":
    sys.exit(main())
