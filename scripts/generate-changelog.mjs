import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname } from 'node:path';

function getArg(name, fallback = '') {
  const index = process.argv.indexOf(name);
  return index >= 0 && index + 1 < process.argv.length ? process.argv[index + 1] : fallback;
}

function tryRunGit(args) {
  try {
    return execFileSync('git', args, {
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe']
    }).trim();
  } catch {
    return '';
  }
}

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

const currentTag = getArg('--tag') || getArg('--current-tag');
const changeLogPath = getArg('--changelog', 'CHANGELOG.md');
const releaseNotesPath = getArg('--release-notes', '.github/release-notes.md');

if (!currentTag) {
  throw new Error('Missing required --tag argument.');
}

const hasHead = tryRunGit(['rev-parse', '--verify', 'HEAD']) !== '';
const allTags = hasHead
  ? tryRunGit(['tag', '--sort=-creatordate'])
      .split(/\r?\n/)
      .map((value) => value.trim())
      .filter((value) => /^\d{2}\.\d{2}\.\d{2}$/.test(value))
  : [];

const previousTag = allTags.find((value) => value !== currentTag) ?? '';
const range = previousTag ? `${previousTag}..HEAD` : 'HEAD';
const gitFormat = '%H%x09%s%x09%b';
const rawLog = hasHead
  ? tryRunGit(['log', '--reverse', '--no-merges', `--pretty=format:${gitFormat}`, range])
  : '';

const typeTitles = new Map([
  ['feat', 'Features'],
  ['fix', 'Fixes'],
  ['perf', 'Performance'],
  ['refactor', 'Refactoring'],
  ['docs', 'Documentation'],
  ['test', 'Tests'],
  ['ci', 'CI'],
  ['build', 'Build'],
  ['chore', 'Chores'],
  ['revert', 'Reverts']
]);

const buckets = new Map();
const otherEntries = [];

for (const line of rawLog.split(/\r?\n/).filter(Boolean)) {
  const [hash, subject] = line.split('\t');
  const trimmedSubject = subject.trim();
  const match =
    /^(?<type>[a-z]+)(?:\((?<scope>[^)]+)\))?(?<breaking>!)?:\s+(?<description>.+)$/.exec(
      trimmedSubject
    );
  const shortHash = hash.slice(0, 7);

  if (!match?.groups) {
    otherEntries.push(`- ${trimmedSubject} (${shortHash})`);
    continue;
  }

  const type = match.groups.type;
  const scope = match.groups.scope?.trim();
  const description = match.groups.description.trim();
  const breaking = match.groups.breaking ? ' **BREAKING**' : '';
  const prefix = scope ? `**${scope}:** ` : '';
  const entry = `- ${prefix}${description}${breaking} (${shortHash})`;

  if (!buckets.has(type)) {
    buckets.set(type, []);
  }

  buckets.get(type).push(entry);
}

const today = new Date().toISOString().slice(0, 10);
const sectionLines = [`## ${currentTag} - ${today}`, ''];
let wroteAnySection = false;

for (const [type, title] of typeTitles) {
  const entries = buckets.get(type);
  if (!entries?.length) {
    continue;
  }

  wroteAnySection = true;
  sectionLines.push(`### ${title}`, '');
  sectionLines.push(...entries, '');
}

if (otherEntries.length) {
  wroteAnySection = true;
  sectionLines.push('### Other', '', ...otherEntries, '');
}

if (!wroteAnySection) {
  sectionLines.push('- No changes recorded.', '');
}

if (sectionLines.at(-1) !== '') {
  sectionLines.push('');
}

const releaseSection = sectionLines.join('\n');
const existing = existsSync(changeLogPath)
  ? readFileSync(changeLogPath, 'utf8').replace(/^\uFEFF/, '')
  : '# Changelog\n\n';
const normalizedExisting = existing.trimStart();
const existingBody = normalizedExisting.startsWith('# Changelog')
  ? normalizedExisting.replace(/^# Changelog\s*/m, '').trimStart()
  : normalizedExisting;
const initialPlaceholder =
  'This file is generated from Conventional Commits using `conventional-changelog`.';
const body = existingBody.trim() === initialPlaceholder ? '' : existingBody;
const nextChangelog = `# Changelog\n\n${releaseSection}${body ? `${body}\n` : ''}`;

writeFileSync(changeLogPath, nextChangelog.replace(/\n{3,}/g, '\n\n'), 'utf8');
mkdirSync(dirname(releaseNotesPath), { recursive: true });
writeFileSync(releaseNotesPath, `${releaseSection.trim()}\n`, 'utf8');

const currentTagPattern = new RegExp(`^##\\s+${escapeRegex(currentTag)}\\b`, 'm');
if (!currentTagPattern.test(readFileSync(changeLogPath, 'utf8'))) {
  throw new Error(`Generated changelog does not contain section for ${currentTag}.`);
}
