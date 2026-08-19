#!/bin/sh
# Stop hook: nudge for a field-notes entry when a session ends without touching one.
#
# docs/AGENT_FIELD_NOTES.md only accumulates if something makes it happen. A line in a
# prompt is easy to skip; this is not. It checks whether the file was modified in the
# working tree, staged, or changed in commits not yet on the upstream branch, and stays
# silent if any of those is true.
#
# Fails open: any unexpected condition exits 0 with no message rather than nagging.

set -u

NOTES="docs/AGENT_FIELD_NOTES.md"

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0
git rev-parse --git-dir >/dev/null 2>&1 || exit 0
[ -f "$NOTES" ] || exit 0

touched=0
git diff --quiet HEAD -- "$NOTES" 2>/dev/null || touched=1
git diff --quiet --cached -- "$NOTES" 2>/dev/null || touched=1

# Commits made this session but not yet pushed also count as "touched".
upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>/dev/null || true)
if [ -n "${upstream:-}" ]; then
    git diff --quiet "$upstream"..HEAD -- "$NOTES" 2>/dev/null || touched=1
fi

[ "$touched" -eq 0 ] || exit 0

cat <<'JSON'
{"systemMessage":"docs/AGENT_FIELD_NOTES.md was not touched this session. If anything was learned that a future session should not have to rediscover, append a dated entry to §5 - and update the §4 liveness ledger in the same commit if any mechanism changed live/dead status."}
JSON
exit 0
