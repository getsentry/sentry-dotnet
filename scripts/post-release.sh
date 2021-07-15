# Called by craft after a release

# Add a new unreleased entry in the changelog
sed -i "" 's/# Changelog/# Changelog\n\n## Unreleased/' CHANGELOG.md
