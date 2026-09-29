#!/usr/bin/env node
// Coverage gate for Bikontrol.
//
// The backend has two test projects (unit + integration) that each produce their
// own cobertura report, so coverlet's per-project <Threshold> cannot express a
// single gate. This script merges every cobertura report (union of covered lines,
// keyed by file + line number) and fails if the merged line coverage is below the
// threshold.
//
// Usage:
//   node scripts/check-coverage.mjs [minimumLinePercent]
//   COVERAGE_MIN=70 node scripts/check-coverage.mjs
//
// Coverage files are discovered under the repo (TestResults/**/coverage.cobertura.xml).

import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const threshold = Number(process.argv[2] ?? process.env.COVERAGE_MIN ?? 70);

const files = execSync('find . -name coverage.cobertura.xml -not -path "*/node_modules/*"', {
  encoding: 'utf8',
})
  .split('\n')
  .map((line) => line.trim())
  .filter(Boolean);

if (files.length === 0) {
  console.error('No coverage.cobertura.xml files found. Did the tests run with coverage?');
  process.exit(2);
}

// key: "<filename>:<lineNumber>" -> max hits across all reports
const lines = new Map();

for (const file of files) {
  const xml = readFileSync(file, 'utf8');
  const classRe = /<class\b[^>]*filename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g;
  let classMatch;
  while ((classMatch = classRe.exec(xml)) !== null) {
    const filename = classMatch[1];
    const body = classMatch[2];
    const lineRe = /<line\b[^>]*number="(\d+)"[^>]*hits="(\d+)"/g;
    let lineMatch;
    while ((lineMatch = lineRe.exec(body)) !== null) {
      const key = `${filename}:${lineMatch[1]}`;
      const hits = Number(lineMatch[2]);
      lines.set(key, Math.max(lines.get(key) ?? 0, hits));
    }
  }
}

let covered = 0;
for (const hits of lines.values()) if (hits > 0) covered += 1;
const valid = lines.size;
const percent = valid === 0 ? 0 : (covered / valid) * 100;

console.log(
  `Merged line coverage: ${percent.toFixed(2)}% (${covered}/${valid}) from ${files.length} report(s)`,
);
console.log(`Minimum required: ${threshold}%`);

if (percent < threshold) {
  console.error(`::error::Coverage ${percent.toFixed(2)}% is below the ${threshold}% threshold.`);
  process.exit(1);
}
console.log('Coverage gate passed.');
