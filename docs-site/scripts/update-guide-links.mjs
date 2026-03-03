#!/usr/bin/env node
// 1회성 스크립트: guides/ 하위 폴더 구조화 후 상대 링크 업데이트
// Usage: node docs-site/scripts/update-guide-links.mjs

import { readFileSync, writeFileSync, readdirSync } from 'fs';
import { join, dirname, basename, relative, posix } from 'path';

const GUIDES_DIR = join(import.meta.dirname, '..', 'src', 'content', 'docs', 'guides');

// file basename → subfolder mapping
const FILE_TO_FOLDER = {
  '00-writing-guide.md': 'architecture',
  '01-project-structure.md': 'architecture',
  '02-solution-configuration.md': 'architecture',
  '02b-ci-cd-and-versioning.md': 'architecture',
  '03-dotnet-tools.md': 'architecture',
  '04-ddd-tactical-overview.md': 'domain',
  '05a-value-objects.md': 'domain',
  '05b-value-objects-validation.md': 'domain',
  '06a-aggregate-design.md': 'domain',
  '06b-entity-aggregate-core.md': 'domain',
  '06c-entity-aggregate-advanced.md': 'domain',
  '07-domain-events.md': 'domain',
  '08a-error-system.md': 'domain',
  '08b-error-system-domain-app.md': 'domain',
  '08c-error-system-adapter-testing.md': 'domain',
  '09-domain-services.md': 'domain',
  '10-specifications.md': 'domain',
  '11-usecases-and-cqrs.md': 'application',
  '17-dto-strategy.md': 'application',
  '12-ports.md': 'adapter',
  '13-adapters.md': 'adapter',
  '14a-adapter-pipeline-di.md': 'adapter',
  '14b-adapter-testing.md': 'adapter',
  '14c-repository-query-implementation-guide.md': 'adapter',
  '15a-unit-testing.md': 'testing',
  '15b-integration-testing.md': 'testing',
  '16-testing-library.md': 'testing',
  '18a-observability-spec.md': 'observability',
  '18b-observability-naming.md': 'observability',
  '19-observability-logging.md': 'observability',
  '20-observability-metrics.md': 'observability',
  '21-observability-tracing.md': 'observability',
  '22-crash-diagnostics.md': 'observability',
  'A01-vscode-debugging.md': 'appendix',
  'A02-git-reference.md': 'appendix',
  'A03-response-type-evolution.md': 'appendix',
  'A04-architecture-rules-coverage.md': 'appendix',
};

// Collect all .md files to process
function collectFiles(dir) {
  const results = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...collectFiles(fullPath));
    } else if (entry.name.endsWith('.md')) {
      results.push(fullPath);
    }
  }
  return results;
}

const files = collectFiles(GUIDES_DIR);
let totalReplacements = 0;

for (const filePath of files) {
  const fileDir = dirname(filePath);
  // What folder is this file in relative to GUIDES_DIR?
  const relToGuides = relative(GUIDES_DIR, fileDir).replace(/\\/g, '/');
  // '' for index.md (root), 'architecture', 'domain', etc. for subfolder files

  let content = readFileSync(filePath, 'utf8');
  let fileReplacements = 0;

  // Match markdown links like [text](./filename.md) or [text](./filename.md#anchor)
  // Also match [text](./filename.md — description)
  content = content.replace(
    /(\]\()(\.\/)([A-Za-z0-9_-]+\.md)((?:#[^)\s]*|(?:\s+—\s+[^)]*)?)?)\)/g,
    (match, prefix, dotSlash, targetFile, suffix) => {
      const targetFolder = FILE_TO_FOLDER[targetFile];
      if (!targetFolder) {
        // Not a guide file (e.g., external reference), leave unchanged
        return match;
      }

      let newPath;
      if (relToGuides === '') {
        // index.md at guides root → link to subfolder
        newPath = `./${targetFolder}/${targetFile}`;
      } else if (relToGuides === targetFolder) {
        // Same folder → keep ./filename.md
        newPath = `./${targetFile}`;
      } else {
        // Different folder → ../<targetFolder>/filename.md
        newPath = `../${targetFolder}/${targetFile}`;
      }

      fileReplacements++;
      return `${prefix}${newPath}${suffix})`;
    }
  );

  if (fileReplacements > 0) {
    writeFileSync(filePath, content, 'utf8');
    const shortPath = relative(GUIDES_DIR, filePath).replace(/\\/g, '/');
    console.log(`  ${shortPath}: ${fileReplacements} links updated`);
    totalReplacements += fileReplacements;
  }
}

console.log(`\nTotal: ${totalReplacements} links updated across ${files.length} files`);
