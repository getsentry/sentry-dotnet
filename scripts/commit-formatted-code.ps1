# Requires at least PowerShell 5.1 or PowerShell Core

param (
    [string]$GITHUB_BRANCH
)

$gitStatus = git status

if ($gitStatus -match "nothing to commit") {
    Write-Host "Nothing to commit. All code formatted correctly."
} else {
    Write-Host "Formatted some code. Going to push the changes."
    git config --global user.name 'Sentry Github Bot'
    git config --global user.email 'bot+github-bot@sentry.io'
    git fetch
    git checkout $GITHUB_BRANCH
    git commit -am "Format code"
    git push --set-upstream origin $GITHUB_BRANCH
}
