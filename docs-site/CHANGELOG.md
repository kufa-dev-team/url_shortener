# Changelog

All notable changes to the Wiki Sync tools will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-03

### Added
- Initial release of Wiki Sync tools for URL Shortener documentation
- Bash script (`sync-to-wiki.sh`) for command-line wiki synchronization
- Python script (`sync_wiki.py`) with advanced configuration support
- GitHub Actions workflow for automated synchronization on push
- Configuration file support (`.wiki-sync.json`) for customization
- Automatic sidebar generation for wiki navigation
- Link conversion from documentation format to wiki format
- Dry-run mode for testing changes before applying
- Support for custom path mappings and exclusion patterns
- Comprehensive documentation and setup guide

### Features
- Clone and update GitHub Wiki repository automatically
- Convert documentation structure to wiki-compatible format
- Generate navigation sidebar (`_Sidebar.md`) 
- Transform internal links for wiki compatibility
- Add source headers to track origin files
- Support for gitflow branching strategy
- Full CI/CD integration via GitHub Actions

### Configuration
- Repository owner and name settings
- Custom documentation and wiki directories
- Exclusion and inclusion patterns
- Custom path transformations
- Commit message templates
- Git author configuration

### Documentation
- Complete setup guide (WIKI_SETUP.md)
- Scripts documentation (scripts/README.md)
- Troubleshooting guide
- Best practices and examples

### Technical Details
- Compatible with macOS, Linux, and Windows (via WSL)
- Requires Git, Bash (for shell script), Python 3.6+ (for Python script)
- GitHub Actions runner compatible
- Support for manual and automated workflows

---

[1.0.0]: https://github.com/kufa-dev-team/url_shortener/releases/tag/wiki-sync-v1.0.0
