# Wiki Synchronization Scripts

This directory contains scripts for synchronizing documentation to your GitHub Wiki.

## Available Scripts

### 1. `sync-to-wiki.sh` (Bash Script)
A comprehensive bash script for syncing documentation to GitHub Wiki.

**Features:**
- Clone/update wiki repository
- Convert documentation structure for wiki
- Create navigation sidebar
- Convert relative links
- Dry-run mode for testing

**Usage:**
```bash
# Basic usage
./scripts/sync-to-wiki.sh

# Dry run to see what would be changed
./scripts/sync-to-wiki.sh --dry-run

# Clean wiki before sync
./scripts/sync-to-wiki.sh --clean

# Custom repository
./scripts/sync-to-wiki.sh --repo-owner myorg --repo-name myrepo

# Help
./scripts/sync-to-wiki.sh --help
```

### 2. `sync_wiki.py` (Python Script)
An advanced Python script with configuration file support.

**Features:**
- JSON configuration file
- Custom path mappings
- Pattern-based exclusions
- Advanced link conversion
- Verbose logging

**Usage:**
```bash
# Basic usage with default config
python3 scripts/sync_wiki.py

# Use custom config file
python3 scripts/sync_wiki.py --config my-config.json

# Dry run mode
python3 scripts/sync_wiki.py --dry-run

# Verbose output
python3 scripts/sync_wiki.py --verbose
```

## Configuration

### `.wiki-sync.json`
The Python script uses a JSON configuration file for customization:

```json
{
  "repository": {
    "owner": "kufa-dev-team",
    "name": "url_shortener"
  },
  "sync": {
    "docsDirectory": "./docs",
    "wikiDirectory": "./wiki-temp",
    "cleanBeforeSync": false,
    "excludePatterns": ["**/test/**"]
  },
  "mapping": {
    "homePageSource": "intro.md",
    "createSidebar": true,
    "customMappings": [
      {
        "source": "getting-started.md",
        "target": "Quick-Start.md"
      }
    ]
  }
}
```

## GitHub Actions

### Automatic Wiki Sync
The `.github/workflows/sync-wiki.yml` workflow automatically syncs documentation when:
- Changes are pushed to `docs/` directory
- README.md is updated
- Manual trigger via GitHub Actions UI

**Manual Trigger Options:**
- **clean**: Clean wiki before sync
- **dry_run**: Test mode without making changes

## Prerequisites

### Required Tools
- Git (for all scripts)
- Bash (for shell script)
- Python 3.6+ (for Python script)
- GitHub CLI `gh` (optional, for enhanced features)

### Repository Setup
1. **Enable Wiki**: Go to Settings → Features → Enable Wiki
2. **Initialize Wiki**: Create at least one page manually first
3. **Permissions**: Ensure you have write access to the repository

## How It Works

1. **Clone Wiki Repository**: The scripts clone `{repo}.wiki.git`
2. **Process Documentation**: 
   - Convert file paths (e.g., `docs/api/endpoints.md` → `api-endpoints.md`)
   - Fix internal links
   - Add source headers
3. **Create Navigation**: Generate `_Sidebar.md` with organized links
4. **Push Changes**: Commit and push to wiki repository

## File Mapping

| Source File | Wiki Page |
|------------|-----------|
| `docs/intro.md` | `Home.md` |
| `docs/getting-started.md` | `getting-started.md` |
| `docs/api/endpoints.md` | `api-endpoints.md` |
| `docs/architecture/clean-architecture.md` | `architecture-clean-architecture.md` |

## Troubleshooting

### Wiki Not Found Error
```
Failed to clone wiki. Make sure the wiki is enabled in repository settings.
```
**Solution**: Enable wiki in GitHub repository settings and create an initial page.

### Permission Denied
```
Failed to push changes. Check your permissions.
```
**Solution**: 
- Ensure you have write access to the repository
- Check GitHub token permissions in Actions
- Verify git credentials are configured

### No Changes Detected
The scripts check for actual content changes before pushing. If you see "No changes to push", the wiki is already up-to-date.

## Best Practices

1. **Test First**: Always run with `--dry-run` first
2. **Backup**: Keep a backup of important wiki content
3. **Consistent Structure**: Maintain consistent documentation structure
4. **Link Format**: Use relative markdown links in docs
5. **Automation**: Use GitHub Actions for automatic syncing

## Examples

### Manual Sync
```bash
# Navigate to docs-site directory
cd /path/to/docs-site

# Run dry-run first
./scripts/sync-to-wiki.sh --dry-run

# If everything looks good, run actual sync
./scripts/sync-to-wiki.sh
```

### Scheduled Sync
Add to GitHub Actions for daily sync:
```yaml
on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM UTC
```

### Custom Repository
```bash
# For a different repository
./scripts/sync-to-wiki.sh \
  --repo-owner myusername \
  --repo-name myproject \
  --docs-dir ./documentation
```

## Contributing

Feel free to enhance these scripts:
- Add new features
- Improve error handling
- Support additional VCS platforms
- Add more configuration options

## License

These scripts are part of the url_shortener project and follow the same license.
