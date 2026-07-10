#!/usr/bin/env bash
#
# Verifies that CHANGELOG.md contains no manually-added entries under an
# "## Unreleased" heading.
#
# This repository generates its changelog automatically at release time via
# craft (`changelogPolicy: auto` in .craft.yml), from the pull requests merged
# since the previous release. craft only regenerates the section when it is
# empty, so a single leftover manual entry under "## Unreleased" suppresses the
# auto-generation and silently drops every other change from the release notes.
# (This is what broke the 6.7.0 release, which shipped with a single entry.)
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

# A manual changelog entry is a bullet line ("- ..." or "* ..."). This
# deliberately ignores an empty "## Unreleased" heading and blank/sub-heading
# lines, so only real entries fail the check.
entry_re='^[[:space:]]*[-*][[:space:]]+[^[:space:]]'

if printf '%s\n' "$unreleased_body" | grep -Eq "$entry_re"; then
  echo "::error file=$CHANGELOG::Manual changelog entries found under '## Unreleased'."
  echo ""
  echo "This repository generates its changelog automatically at release time"
  echo "(changelogPolicy: auto in .craft.yml). A manual entry under '## Unreleased'"
  echo "suppresses that generation and causes other changes to be dropped from the"
  echo "release notes."
  echo ""
  echo "Offending line(s):"
  printf '%s\n' "$unreleased_body" | grep -En "$entry_re" || true
  echo ""
  echo "Please remove them. Your change is added to the changelog automatically,"
  echo "based on the PR title / commit message. If it is not user-facing, add the"
  echo "'skip-changelog' label or write '#skip-changelog' in the PR description."
  exit 1
fi

echo "OK: no manual '## Unreleased' changelog entries."
