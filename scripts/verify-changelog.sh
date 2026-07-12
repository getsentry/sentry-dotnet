#!/usr/bin/env bash
#
# Verifies that CHANGELOG.md has no content under an "## Unreleased" heading.
#
# This repository generates its changelog automatically at release time via
# craft (`changelogPolicy: auto` in .craft.yml), from the pull requests merged
# since the previous release. Craft only regenerates the section when it is
# empty, so a single leftover manual entry under "## Unreleased" suppresses the
# auto-generation and silently drops every other change from the release notes.
#
# Usage: scripts/verify-changelog.sh [path-to-changelog]
set -euo pipefail

CHANGELOG="${1:-CHANGELOG.md}"

if [[ ! -f "$CHANGELOG" ]]; then
  echo "Changelog file not found: $CHANGELOG" >&2
  exit 2
fi

# Extract the body of the "## Unreleased" section: everything between the
# "## Unreleased" heading and the next "## " (h2) heading. "### " sub-headings
# are not treated as section boundaries.
unreleased_body="$(awk '
  /^## / {
    if (in_section) exit
    if (tolower($0) ~ /^## +unreleased/) { in_section = 1; next }
  }
  in_section { print }
' "$CHANGELOG")"

# craft trims the section body and skips auto-generation whenever it is
# non-empty (`if (!changeset.body)` after `.trim()`), so ANY non-whitespace
# content under "## Unreleased" -- a bullet, a stray "### Features" sub-heading,
# or loose text -- suppresses generation. Match that exactly: fail on any
# non-blank line. A bare/empty "## Unreleased" heading is fine (craft
# regenerates it).
if printf '%s\n' "$unreleased_body" | grep -Eq '[^[:space:]]'; then
  echo "::error file=$CHANGELOG::The '## Unreleased' section is not empty."
  echo ""
  echo "This repository generates its changelog automatically at release time"
  echo "(changelogPolicy: auto in .craft.yml). craft only regenerates the"
  echo "'## Unreleased' section when it is empty, so ANY leftover content there"
  echo "(entries or even a bare sub-heading) suppresses generation and causes the"
  echo "rest of the release notes to be dropped."
  echo ""
  echo "Offending line(s):"
  printf '%s\n' "$unreleased_body" | grep -En '[^[:space:]]' || true
  echo ""
  echo "Please remove them. Your change is added to the changelog automatically,"
  echo "based on the PR title / commit message -- or a '### Changelog Entry' section"
  echo "in the PR description if you want a more detailed entry. If it is not"
  echo "user-facing, add the 'skip-changelog' label or write '#skip-changelog' in the"
  echo "PR description."
  exit 1
fi

echo "OK: '## Unreleased' section is empty."
