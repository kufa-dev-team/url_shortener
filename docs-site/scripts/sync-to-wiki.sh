#!/bin/bash

# sync-to-wiki.sh - Sync documentation to GitHub Wiki
# Usage: ./scripts/sync-to-wiki.sh [options]
#
# Options:
#   --repo-owner OWNER    GitHub repository owner (default: kufa-dev-team)
#   --repo-name NAME      GitHub repository name (default: url_shortener)
#   --docs-dir DIR        Documentation directory (default: ./docs)
#   --wiki-dir DIR        Wiki clone directory (default: ./wiki-temp)
#   --branch BRANCH       Wiki branch (default: master or main)
#   --clean               Clean wiki directory before sync
#   --dry-run             Show what would be done without making changes
#   --help                Show this help message

set -e

# Default values
REPO_OWNER="kufa-dev-team"
REPO_NAME="url_shortener"
DOCS_DIR="./docs"
WIKI_DIR="./wiki-temp"
WIKI_BRANCH=""
CLEAN_WIKI=false
DRY_RUN=false

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --repo-owner)
            REPO_OWNER="$2"
            shift 2
            ;;
        --repo-name)
            REPO_NAME="$2"
            shift 2
            ;;
        --docs-dir)
            DOCS_DIR="$2"
            shift 2
            ;;
        --wiki-dir)
            WIKI_DIR="$2"
            shift 2
            ;;
        --branch)
            WIKI_BRANCH="$2"
            shift 2
            ;;
        --clean)
            CLEAN_WIKI=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --help)
            grep "^#" "$0" | grep -E "^# (sync-to-wiki|Usage:|Options:)" | sed 's/^# //'
            grep "^#   " "$0" | sed 's/^# //'
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if required tools are installed
check_requirements() {
    if ! command -v git &> /dev/null; then
        log_error "git is not installed"
        exit 1
    fi

    if ! command -v gh &> /dev/null; then
        log_warning "GitHub CLI (gh) is not installed. Some features may not work."
        log_info "Install with: brew install gh"
    fi
}

# Clone or update wiki repository
setup_wiki_repo() {
    local wiki_url="https://github.com/${REPO_OWNER}/${REPO_NAME}.wiki.git"
    
    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would clone/update wiki from: $wiki_url"
        return
    fi

    if [ -d "$WIKI_DIR/.git" ]; then
        log_info "Updating existing wiki repository..."
        cd "$WIKI_DIR"
        git pull origin
        cd - > /dev/null
    else
        log_info "Cloning wiki repository..."
        rm -rf "$WIKI_DIR"
        git clone "$wiki_url" "$WIKI_DIR" || {
            log_error "Failed to clone wiki. Make sure the wiki is enabled in repository settings."
            exit 1
        }
    fi

    # Detect default branch if not specified
    if [ -z "$WIKI_BRANCH" ]; then
        cd "$WIKI_DIR"
        WIKI_BRANCH=$(git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@')
        cd - > /dev/null
        log_info "Detected wiki branch: $WIKI_BRANCH"
    fi
}

# Convert internal links from docs format to wiki format
convert_links() {
    local content="$1"
    # Convert relative .md links to wiki links
    # Example: [link](./file.md) -> [link](file)
    echo "$content" | sed -E 's/\[([^]]+)\]\(\.?\/([^)]+)\.md\)/[\1](\2)/g'
}

# Process and copy documentation files
sync_docs() {
    if [ ! -d "$DOCS_DIR" ]; then
        log_error "Documentation directory not found: $DOCS_DIR"
        exit 1
    fi

    log_info "Syncing documentation from $DOCS_DIR to wiki..."

    # Clean wiki directory if requested (keep .git)
    if [ "$CLEAN_WIKI" = true ]; then
        if [ "$DRY_RUN" = true ]; then
            log_info "[DRY RUN] Would clean wiki directory (except .git)"
        else
            find "$WIKI_DIR" -mindepth 1 -maxdepth 1 -not -name '.git' -exec rm -rf {} +
        fi
    fi

    # Create Home.md from intro.md or README.md if it doesn't exist
    if [ -f "$DOCS_DIR/intro.md" ]; then
        if [ "$DRY_RUN" = true ]; then
            log_info "[DRY RUN] Would create Home.md from intro.md"
        else
            cp "$DOCS_DIR/intro.md" "$WIKI_DIR/Home.md"
            log_info "Created Home.md from intro.md"
        fi
    elif [ -f "$DOCS_DIR/../README.md" ]; then
        if [ "$DRY_RUN" = true ]; then
            log_info "[DRY RUN] Would create Home.md from README.md"
        else
            cp "$DOCS_DIR/../README.md" "$WIKI_DIR/Home.md"
            log_info "Created Home.md from README.md"
        fi
    fi

    # Copy all markdown files, preserving directory structure
    while IFS= read -r -d '' file; do
        # Get relative path from docs directory
        rel_path="${file#$DOCS_DIR/}"
        
        # Convert path for wiki (replace / with -)
        wiki_name=$(echo "$rel_path" | sed 's/\//\-/g' | sed 's/\.md$//')
        wiki_file="$WIKI_DIR/${wiki_name}.md"
        
        if [ "$DRY_RUN" = true ]; then
            log_info "[DRY RUN] Would copy: $file -> $wiki_file"
        else
            # Read file content and convert links
            content=$(<"$file")
            converted_content=$(convert_links "$content")
            
            # Add header with original location
            {
                echo "<!-- Generated from: $rel_path -->"
                echo ""
                echo "$converted_content"
            } > "$wiki_file"
            
            log_info "Copied: $rel_path -> ${wiki_name}.md"
        fi
    done < <(find "$DOCS_DIR" -name "*.md" -type f -print0)

    # Create _Sidebar.md with navigation
    create_sidebar
}

# Create sidebar navigation
create_sidebar() {
    local sidebar_file="$WIKI_DIR/_Sidebar.md"
    
    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would create sidebar navigation"
        return
    fi

    {
        echo "# Navigation"
        echo ""
        echo "* [Home](Home)"
        echo ""
        echo "## Documentation"
        echo ""
        
        # Group files by directory
        local last_dir=""
        while IFS= read -r -d '' file; do
            rel_path="${file#$DOCS_DIR/}"
            dir_name=$(dirname "$rel_path")
            base_name=$(basename "$rel_path" .md)
            wiki_name=$(echo "$rel_path" | sed 's/\//\-/g' | sed 's/\.md$//')
            
            # Add directory header if changed
            if [ "$dir_name" != "." ] && [ "$dir_name" != "$last_dir" ]; then
                echo ""
                echo "### $(echo "$dir_name" | sed 's/-/ /g' | sed 's/\b\(.\)/\u\1/g')"
                last_dir="$dir_name"
            fi
            
            # Add link to file
            display_name=$(echo "$base_name" | sed 's/-/ /g' | sed 's/\b\(.\)/\u\1/g')
            echo "* [$display_name]($wiki_name)"
        done < <(find "$DOCS_DIR" -name "*.md" -type f -print0 | sort -z)
    } > "$sidebar_file"
    
    log_info "Created sidebar navigation"
}

# Commit and push changes to wiki
push_changes() {
    cd "$WIKI_DIR"
    
    # Check if there are changes
    if [ -z "$(git status --porcelain)" ]; then
        log_info "No changes to push"
        return
    fi

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would commit and push the following changes:"
        git status --short
        return
    fi

    # Commit changes
    git add -A
    git commit -m "Sync documentation from docs-site $(date '+%Y-%m-%d %H:%M:%S')" || {
        log_error "Failed to commit changes"
        exit 1
    }

    # Push to wiki
    log_info "Pushing changes to wiki..."
    git push origin "$WIKI_BRANCH" || {
        log_error "Failed to push changes. Check your permissions."
        exit 1
    }

    cd - > /dev/null
    log_info "Successfully synced documentation to wiki!"
}

# Main execution
main() {
    log_info "Starting wiki synchronization..."
    log_info "Repository: ${REPO_OWNER}/${REPO_NAME}"
    
    check_requirements
    setup_wiki_repo
    sync_docs
    push_changes
    
    log_info "Wiki synchronization complete!"
}

# Run main function
main
