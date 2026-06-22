#!/usr/bin/env bash
# Usage: watch-upstream.sh <upstream-repo> <upstream-path> <local-path>
#
# Checks whether the given path in an upstream GitHub repo has a new commit
# since the last time we created a tracking issue. If so, opens a GitHub issue
# in this repo (identified by GH_REPO or inferred by gh from git context).
#
# Required env vars:
#   GH_TOKEN   — GitHub token (set automatically in Actions; use `gh auth token` locally)
#
# Optional env vars (set automatically in GitHub Actions):
#   GH_REPO           — target repo for issue creation, e.g. getsentry/sentry-dotnet
#   GITHUB_SERVER_URL — e.g. https://github.com (defaults to https://github.com)
#   GITHUB_RUN_ID     — included in the issue footer when present

set -euo pipefail

if [ $# -ne 3 ]; then
  echo "Usage: $0 <upstream-repo> <upstream-path> <local-path>" >&2
  exit 1
fi

UPSTREAM_REPO="$1"
UPSTREAM_PATH="$2"
LOCAL_PATH="$3"
GITHUB_SERVER_URL="${GITHUB_SERVER_URL:-https://github.com}"

echo "Checking upstream: ${UPSTREAM_REPO}/${UPSTREAM_PATH}"

# Resolve the upstream repo's default branch so URLs are correct even for repos
# that use a branch name other than 'main'.
DEFAULT_BRANCH=$(gh api "repos/${UPSTREAM_REPO}" --jq '.default_branch')
UPSTREAM_URL="${GITHUB_SERVER_URL}/${UPSTREAM_REPO}/tree/${DEFAULT_BRANCH}/${UPSTREAM_PATH}"

# Get the latest commit SHA affecting the tracked path.
LATEST_SHA=$(gh api "repos/${UPSTREAM_REPO}/commits?path=${UPSTREAM_PATH}&per_page=1" \
  --jq '.[0].sha')
if [ -z "${LATEST_SHA}" ] || [ "${LATEST_SHA}" = "null" ]; then
  echo "No commits found for path '${UPSTREAM_PATH}' in ${UPSTREAM_REPO}. Check the path is correct." >&2
  exit 1
fi
LATEST_SHORT="${LATEST_SHA:0:7}"
echo "Latest upstream commit: ${LATEST_SHA} (${LATEST_SHORT})"

# Avoid creating duplicate issues: skip if any issue (open or closed) already
# tracks this exact upstream commit SHA. The SHA in the title makes it unique.
ISSUE_LABEL="upstream-watch"
EXISTING_ISSUE=$(gh issue list \
  --label "$ISSUE_LABEL" \
  --state all \
  --search "\"${UPSTREAM_REPO} ${UPSTREAM_PATH} @ ${LATEST_SHORT}\"" \
  --json number,title \
  --jq '.[0].number // empty')

if [ -n "$EXISTING_ISSUE" ]; then
  echo "An issue (#${EXISTING_ISSUE}) already tracks upstream commit ${LATEST_SHORT} for ${UPSTREAM_REPO}/${UPSTREAM_PATH}. Skipping."
  exit 0
fi

echo "No existing issue found for commit ${LATEST_SHORT}. Creating one..."

# Ensure the label exists (idempotent).
gh label create "$ISSUE_LABEL" \
  --description "Upstream vendored code has changed — review required" \
  --color "E4E669" 2>/dev/null || true

COMMIT_URL="${GITHUB_SERVER_URL}/${UPSTREAM_REPO}/commit/${LATEST_SHA}"
HISTORY_URL="${GITHUB_SERVER_URL}/${UPSTREAM_REPO}/commits/${DEFAULT_BRANCH}/${UPSTREAM_PATH}"

if [ -n "${GITHUB_RUN_ID:-}" ] && [ -n "${GH_REPO:-}" ]; then
  FOOTER="> _Automatically opened by the [Watch Upstream Changes](${GITHUB_SERVER_URL}/${GH_REPO}/actions/runs/${GITHUB_RUN_ID}) workflow._"
else
  FOOTER="> _Manually triggered via watch-upstream.sh._"
fi

gh issue create \
  --title "Upstream change detected: ${UPSTREAM_REPO} ${UPSTREAM_PATH} @ ${LATEST_SHORT}" \
  --label "$ISSUE_LABEL" \
  --body "## Upstream Change Detected

The code at [\`${UPSTREAM_REPO}/${UPSTREAM_PATH}\`](${UPSTREAM_URL}) has a new commit since our last review.

| | |
|---|---|
| **Latest commit** | [\`${LATEST_SHORT}\`](${COMMIT_URL}) |
| **Path history** | [View history](${HISTORY_URL}) |

Our vendored copy lives in \`${LOCAL_PATH}\`. We modified the upstream code significantly,
so a direct merge is unlikely to be appropriate — but the commit above may reveal logic
changes worth porting.

### What to do

1. Review the [upstream commit](${COMMIT_URL}) and [path history](${HISTORY_URL}).
2. If no action is needed, close this issue with a note explaining why.
3. If changes should be ported, create a follow-up task and close this issue once the work is tracked.

${FOOTER}"

echo "Issue created successfully."
