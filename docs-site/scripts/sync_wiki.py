#!/usr/bin/env python3
"""
sync_wiki.py - Advanced GitHub Wiki Synchronization Script
Syncs documentation from a docs directory to GitHub Wiki with configuration support.
"""

import os
import sys
import json
import argparse
import subprocess
import shutil
import re
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional, Any
import logging

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class WikiSync:
    """Main class for synchronizing documentation to GitHub Wiki."""
    
    def __init__(self, config_file: str = ".wiki-sync.json", dry_run: bool = False):
        """Initialize WikiSync with configuration."""
        self.dry_run = dry_run
        self.config = self.load_config(config_file)
        self.wiki_dir = Path(self.config['sync']['wikiDirectory'])
        self.docs_dir = Path(self.config['sync']['docsDirectory'])
        
    def load_config(self, config_file: str) -> Dict[str, Any]:
        """Load configuration from JSON file."""
        config_path = Path(config_file)
        if not config_path.exists():
            logger.warning(f"Config file {config_file} not found. Using defaults.")
            return self.get_default_config()
        
        with open(config_path, 'r') as f:
            return json.load(f)
    
    def get_default_config(self) -> Dict[str, Any]:
        """Return default configuration."""
        return {
            "repository": {
                "owner": "kufa-dev-team",
                "name": "url_shortener"
            },
            "sync": {
                "docsDirectory": "./docs",
                "wikiDirectory": "./wiki-temp",
                "cleanBeforeSync": False
            },
            "mapping": {
                "homePageSource": "intro.md",
                "createSidebar": True
            },
            "content": {
                "convertRelativeLinks": True,
                "addSourceHeader": True
            },
            "github": {
                "commitMessage": "Sync documentation",
                "authorName": "github-actions[bot]",
                "authorEmail": "github-actions[bot]@users.noreply.github.com"
            }
        }
    
    def setup_wiki_repo(self) -> bool:
        """Clone or update the wiki repository."""
        owner = self.config['repository']['owner']
        name = self.config['repository']['name']
        wiki_url = f"https://github.com/{owner}/{name}.wiki.git"
        
        logger.info(f"Setting up wiki repository from {wiki_url}")
        
        if self.dry_run:
            logger.info("[DRY RUN] Would clone/update wiki repository")
            return True
        
        try:
            if self.wiki_dir.exists() and (self.wiki_dir / '.git').exists():
                # Update existing repository
                logger.info("Updating existing wiki repository...")
                result = subprocess.run(
                    ['git', 'pull', 'origin'],
                    cwd=self.wiki_dir,
                    capture_output=True,
                    text=True
                )
                if result.returncode != 0:
                    logger.error(f"Failed to update wiki: {result.stderr}")
                    return False
            else:
                # Clone new repository
                logger.info("Cloning wiki repository...")
                if self.wiki_dir.exists():
                    shutil.rmtree(self.wiki_dir)
                
                result = subprocess.run(
                    ['git', 'clone', wiki_url, str(self.wiki_dir)],
                    capture_output=True,
                    text=True
                )
                if result.returncode != 0:
                    logger.error(f"Failed to clone wiki: {result.stderr}")
                    logger.error("Make sure the wiki is enabled in repository settings")
                    return False
            
            return True
        except Exception as e:
            logger.error(f"Error setting up wiki repository: {e}")
            return False
    
    def should_exclude(self, file_path: Path) -> bool:
        """Check if a file should be excluded based on patterns."""
        exclude_patterns = self.config.get('sync', {}).get('excludePatterns', [])
        
        for pattern in exclude_patterns:
            if file_path.match(pattern):
                return True
        return False
    
    def transform_path(self, rel_path: str) -> str:
        """Transform file path according to configuration."""
        mapping_config = self.config.get('mapping', {})
        
        # Check for custom mappings first
        custom_mappings = mapping_config.get('customMappings', [])
        for mapping in custom_mappings:
            if mapping['source'] == rel_path:
                return mapping['target'].replace('.md', '')
        
        # Apply path transformation
        transform = mapping_config.get('pathTransform', {})
        if transform.get('type') == 'flatten':
            separator = transform.get('separator', '-')
            transformed = rel_path.replace('/', separator).replace('.md', '')
            return transformed
        
        return rel_path.replace('.md', '')
    
    def convert_links(self, content: str) -> str:
        """Convert relative links in markdown content."""
        if not self.config.get('content', {}).get('convertRelativeLinks', True):
            return content
        
        # Convert [text](./path/file.md) to [text](path-file)
        def replace_link(match):
            text = match.group(1)
            path = match.group(2)
            # Remove ./ prefix and .md extension
            path = path.lstrip('./')
            if path.endswith('.md'):
                path = path[:-3]
            # Transform path
            path = self.transform_path(path)
            return f"[{text}]({path})"
        
        pattern = r'\[([^\]]+)\]\(([^)]+\.md)\)'
        return re.sub(pattern, replace_link, content)
    
    def add_header(self, content: str, source_file: str) -> str:
        """Add header to content if configured."""
        if not self.config.get('content', {}).get('addSourceHeader', True):
            return content
        
        header_config = self.config.get('content', {}).get('customHeaders', {})
        if header_config.get('enabled', True):
            template = header_config.get('template', '<!-- Generated from: {source} -->')
            header = template.format(
                source=source_file,
                date=datetime.now().strftime('%Y-%m-%d %H:%M:%S')
            )
            return f"{header}\n\n{content}"
        
        return content
    
    def sync_file(self, source_file: Path) -> bool:
        """Sync a single documentation file to wiki."""
        rel_path = source_file.relative_to(self.docs_dir)
        
        # Check if should be excluded
        if self.should_exclude(source_file):
            logger.debug(f"Excluding {rel_path}")
            return True
        
        # Transform path for wiki
        wiki_name = self.transform_path(str(rel_path))
        wiki_file = self.wiki_dir / f"{wiki_name}.md"
        
        logger.info(f"Syncing {rel_path} -> {wiki_name}.md")
        
        if self.dry_run:
            logger.info(f"[DRY RUN] Would copy {source_file} to {wiki_file}")
            return True
        
        try:
            # Read and process content
            content = source_file.read_text(encoding='utf-8')
            content = self.convert_links(content)
            content = self.add_header(content, str(rel_path))
            
            # Write to wiki
            wiki_file.parent.mkdir(parents=True, exist_ok=True)
            wiki_file.write_text(content, encoding='utf-8')
            
            return True
        except Exception as e:
            logger.error(f"Failed to sync {source_file}: {e}")
            return False
    
    def create_home_page(self) -> bool:
        """Create Home.md from configured source."""
        home_source = self.config.get('mapping', {}).get('homePageSource', 'intro.md')
        source_file = self.docs_dir / home_source
        
        if not source_file.exists():
            # Try README.md as fallback
            source_file = self.docs_dir.parent / 'README.md'
            if not source_file.exists():
                logger.warning("No source file found for Home.md")
                return False
        
        logger.info(f"Creating Home.md from {source_file.name}")
        
        if self.dry_run:
            logger.info("[DRY RUN] Would create Home.md")
            return True
        
        try:
            content = source_file.read_text(encoding='utf-8')
            content = self.convert_links(content)
            
            home_file = self.wiki_dir / 'Home.md'
            home_file.write_text(content, encoding='utf-8')
            return True
        except Exception as e:
            logger.error(f"Failed to create Home.md: {e}")
            return False
    
    def create_sidebar(self) -> bool:
        """Create _Sidebar.md with navigation."""
        if not self.config.get('mapping', {}).get('createSidebar', True):
            return True
        
        logger.info("Creating sidebar navigation")
        
        if self.dry_run:
            logger.info("[DRY RUN] Would create _Sidebar.md")
            return True
        
        try:
            sidebar_content = []
            sidebar_title = self.config.get('mapping', {}).get('sidebarTitle', 'Navigation')
            
            sidebar_content.append(f"# {sidebar_title}\n")
            sidebar_content.append("* [Home](Home)\n")
            sidebar_content.append("\n## Documentation\n")
            
            # Group files by directory
            files_by_dir = {}
            for md_file in sorted(self.docs_dir.rglob('*.md')):
                if self.should_exclude(md_file):
                    continue
                
                rel_path = md_file.relative_to(self.docs_dir)
                dir_name = rel_path.parent.name if rel_path.parent.name else 'root'
                
                if dir_name not in files_by_dir:
                    files_by_dir[dir_name] = []
                
                wiki_name = self.transform_path(str(rel_path))
                display_name = md_file.stem.replace('-', ' ').title()
                files_by_dir[dir_name].append((display_name, wiki_name))
            
            # Write grouped files
            for dir_name, files in files_by_dir.items():
                if dir_name != 'root':
                    sidebar_content.append(f"\n### {dir_name.replace('-', ' ').title()}\n")
                
                for display_name, wiki_name in files:
                    sidebar_content.append(f"* [{display_name}]({wiki_name})\n")
            
            sidebar_file = self.wiki_dir / '_Sidebar.md'
            sidebar_file.write_text(''.join(sidebar_content), encoding='utf-8')
            
            return True
        except Exception as e:
            logger.error(f"Failed to create sidebar: {e}")
            return False
    
    def sync_docs(self) -> bool:
        """Sync all documentation files to wiki."""
        if not self.docs_dir.exists():
            logger.error(f"Documentation directory not found: {self.docs_dir}")
            return False
        
        # Clean wiki directory if configured
        if self.config.get('sync', {}).get('cleanBeforeSync', False):
            if self.dry_run:
                logger.info("[DRY RUN] Would clean wiki directory")
            else:
                logger.info("Cleaning wiki directory...")
                for item in self.wiki_dir.iterdir():
                    if item.name != '.git':
                        if item.is_dir():
                            shutil.rmtree(item)
                        else:
                            item.unlink()
        
        # Create home page
        self.create_home_page()
        
        # Sync all markdown files
        success = True
        for md_file in self.docs_dir.rglob('*.md'):
            if not self.sync_file(md_file):
                success = False
        
        # Create sidebar
        self.create_sidebar()
        
        return success
    
    def push_changes(self) -> bool:
        """Commit and push changes to wiki."""
        if self.dry_run:
            logger.info("[DRY RUN] Would commit and push changes")
            return True
        
        try:
            # Configure git
            git_config = self.config.get('github', {})
            subprocess.run([
                'git', 'config', 'user.name', git_config.get('authorName', 'github-actions[bot]')
            ], cwd=self.wiki_dir, check=True)
            subprocess.run([
                'git', 'config', 'user.email', git_config.get('authorEmail', 'github-actions[bot]@users.noreply.github.com')
            ], cwd=self.wiki_dir, check=True)
            
            # Check for changes
            result = subprocess.run(
                ['git', 'status', '--porcelain'],
                cwd=self.wiki_dir,
                capture_output=True,
                text=True
            )
            
            if not result.stdout.strip():
                logger.info("No changes to push")
                return True
            
            # Add all changes
            subprocess.run(['git', 'add', '-A'], cwd=self.wiki_dir, check=True)
            
            # Commit
            commit_msg = git_config.get('commitMessage', 'Sync documentation')
            commit_msg = f"{commit_msg} - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
            subprocess.run(
                ['git', 'commit', '-m', commit_msg],
                cwd=self.wiki_dir,
                check=True
            )
            
            # Push
            logger.info("Pushing changes to wiki...")
            result = subprocess.run(
                ['git', 'push', 'origin'],
                cwd=self.wiki_dir,
                capture_output=True,
                text=True
            )
            
            if result.returncode != 0:
                logger.error(f"Failed to push: {result.stderr}")
                return False
            
            logger.info("Successfully pushed changes to wiki!")
            return True
            
        except subprocess.CalledProcessError as e:
            logger.error(f"Git operation failed: {e}")
            return False
        except Exception as e:
            logger.error(f"Failed to push changes: {e}")
            return False
    
    def run(self) -> bool:
        """Run the complete synchronization process."""
        logger.info("Starting wiki synchronization...")
        logger.info(f"Repository: {self.config['repository']['owner']}/{self.config['repository']['name']}")
        
        if self.dry_run:
            logger.info("Running in DRY RUN mode - no changes will be made")
        
        # Setup wiki repository
        if not self.setup_wiki_repo():
            return False
        
        # Sync documentation
        if not self.sync_docs():
            return False
        
        # Push changes
        if not self.push_changes():
            return False
        
        logger.info("Wiki synchronization complete!")
        return True


def main():
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(
        description='Sync documentation to GitHub Wiki',
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    parser.add_argument(
        '--config',
        default='.wiki-sync.json',
        help='Path to configuration file (default: .wiki-sync.json)'
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Perform dry run without making changes'
    )
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Enable verbose logging'
    )
    
    args = parser.parse_args()
    
    if args.verbose:
        logging.getLogger().setLevel(logging.DEBUG)
    
    # Run synchronization
    sync = WikiSync(config_file=args.config, dry_run=args.dry_run)
    success = sync.run()
    
    sys.exit(0 if success else 1)


if __name__ == '__main__':
    main()
