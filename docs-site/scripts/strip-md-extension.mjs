#!/usr/bin/env node
// 1회성 스크립트: 가이드 마크다운 링크에서 .md 확장자 제거
// Starlight 정적 빌드에서 .md 확장자가 clean URL로 변환되지 않으므로 제거 필요
// Usage: node docs-site/scripts/strip-md-extension.mjs

import { readFileSync, writeFileSync, readdirSync } from 'fs';
import { join, dirname, relative } from 'path';

const GUIDES_DIR = join(import.meta.dirname, '..', 'src', 'content', 'docs', 'guides');

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
  let content = readFileSync(filePath, 'utf8');
  let fileReplacements = 0;

  // Match markdown links ending with .md before ) or #
  // Pattern: ](path/filename.md) or ](path/filename.md#anchor) or ](path/filename.md — desc)
  // Only strip .md from relative links (starting with ./ or ../)
  content = content.replace(
    /(\]\()(\.\.[\/\\]|\.\/)((?:[A-Za-z0-9_-]+[\/\\])*[A-Za-z0-9_-]+)\.md((?:#[^)\s]*|(?:\s+—\s+[^)]*)?)?)\)/g,
    (match, prefix, relStart, pathWithoutExt, suffix) => {
      fileReplacements++;
      return `${prefix}${relStart}${pathWithoutExt}${suffix})`;
    }
  );

  if (fileReplacements > 0) {
    writeFileSync(filePath, content, 'utf8');
    const shortPath = relative(GUIDES_DIR, filePath).replace(/\\/g, '/');
    console.log(`  ${shortPath}: ${fileReplacements} .md extensions removed`);
    totalReplacements += fileReplacements;
  }
}

console.log(`\nTotal: ${totalReplacements} .md extensions removed across ${files.length} files`);
