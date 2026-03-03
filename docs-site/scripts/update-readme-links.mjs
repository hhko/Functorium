import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

const DOCS_DIR = join(import.meta.dirname, '..', 'src', 'content', 'docs');

async function* walkMd(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      yield* walkMd(full);
    } else if (entry.isFile() && entry.name.endsWith('.md')) {
      yield full;
    }
  }
}

// Replace README.md in markdown link targets: [text](path/README.md) -> [text](path/)
// Only match inside parentheses of markdown links, not in code blocks or prose
const LINK_PATTERN = /(\]\()([^)]*?)README\.md(\))/g;

let filesChanged = 0;
let totalReplacements = 0;

for await (const filePath of walkMd(DOCS_DIR)) {
  const content = await readFile(filePath, 'utf-8');

  // Split into code-block and non-code-block segments
  // to avoid changing README.md references inside code blocks
  const segments = content.split(/(```[\s\S]*?```|`[^`]+`)/g);
  let changed = false;
  let fileReplacements = 0;

  const newSegments = segments.map((segment, i) => {
    // Odd indices are code block content - skip
    if (i % 2 === 1) return segment;

    // Replace README.md in markdown link targets
    return segment.replace(LINK_PATTERN, (match, prefix, path, suffix) => {
      changed = true;
      fileReplacements++;
      // path/README.md -> path/
      // README.md -> ./
      const newPath = path === '' ? './' : path;
      return `${prefix}${newPath}${suffix}`;
    });
  });

  if (changed) {
    const newContent = newSegments.join('');
    await writeFile(filePath, newContent, 'utf-8');
    filesChanged++;
    totalReplacements += fileReplacements;
    console.log(`[OK] ${filePath} (${fileReplacements} replacements)`);
  }
}

console.log(`\nDone: ${filesChanged} files changed, ${totalReplacements} total replacements`);
