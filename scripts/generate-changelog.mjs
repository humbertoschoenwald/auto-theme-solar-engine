import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname } from 'node:path';

const CHANGELOG_HEADER = '# Changelog';
const TAG_PATTERN = /^v?\d{2}\.\d{2}\.\d{2}$/;
const AUTO_GENERATED_SUBJECT_PATTERNS = [
  /^chore\(changelog\): update unreleased changelog\b/i,
  /^docs\(changelog\): release v?\d{2}\.\d{2}\.\d{2}\b/i
];
const TYPE_TITLES = new Map([
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

function normalizeContent(value) {
  return value.replace(/^\uFEFF/, '').replace(/\r\n/g, '\n');
}

function sanitizeSubject(value) {
  return value.replace(/\s+\[(?:skip [^\]]+)\]\s*/gi, ' ').trim();
}

function splitExistingChangelog(content) {
  const normalized = normalizeContent(content).trimStart();
  const body = normalized.startsWith(CHANGELOG_HEADER)
    ? normalized.slice(CHANGELOG_HEADER.length).trimStart()
    : normalized;

  return {
    bodyWithoutUnreleased: removeSection(body, 'Unreleased'),
    body
  };
}

function removeSection(body, headingTitle) {
  const trimmedBody = body.trimStart();
  const pattern = new RegExp(
    `^##\\s+${escapeRegex(headingTitle)}\\b[\\s\\S]*?(?=^##\\s+|\\Z)`,
    'm'
  );

  return trimmedBody.replace(pattern, '').replace(/^\s+/, '');
}

function getSection(body, headingTitle) {
  const trimmedBody = body.trimStart();
  const pattern = new RegExp(
    `^##\\s+${escapeRegex(headingTitle)}\\b[\\s\\S]*?(?=^##\\s+|\\Z)`,
    'm'
  );
  const match = trimmedBody.match(pattern);
  return match ? match[0].trimEnd() : '';
}

function buildLogEntries(rangeSpec) {
  const hasHead = tryRunGit(['rev-parse', '--verify', 'HEAD']) !== '';
  if (!hasHead) {
    return {
      buckets: new Map(),
      otherEntries: []
    };
  }

  const gitFormat = '%H%x1f%s%x1f%b%x1e';
  const rawLog = tryRunGit([
    'log',
    '--reverse',
    '--no-merges',
    `--pretty=format:${gitFormat}`,
    rangeSpec
  ]);

  const buckets = new Map();
  const otherEntries = [];

  const records = rawLog
    .split('\x1e')
    .map((value) => value.trim())
    .filter(Boolean);

  for (const record of records) {
    const [hash = '', subject = ''] = record.split('\x1f');
    const trimmedSubject = sanitizeSubject(subject.trim());

    if (!hash || !trimmedSubject) {
      continue;
    }

    if (AUTO_GENERATED_SUBJECT_PATTERNS.some((pattern) => pattern.test(trimmedSubject))) {
      continue;
    }

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

  return {
    buckets,
    otherEntries
  };
}

function buildSection(heading, logEntries) {
  const sectionLines = [`## ${heading}`, ''];
  let wroteEntries = false;

  for (const [type, title] of TYPE_TITLES) {
    const entries = logEntries.buckets.get(type);
    if (!entries?.length) {
      continue;
    }

    wroteEntries = true;
    sectionLines.push(`### ${title}`, '', ...entries, '');
  }

  if (logEntries.otherEntries.length) {
    wroteEntries = true;
    sectionLines.push('### Other', '', ...logEntries.otherEntries, '');
  }

  if (!wroteEntries) {
    sectionLines.push('- No unreleased changes.', '');
  }

  return sectionLines
    .join('\n')
    .replace(/\n{3,}/g, '\n\n')
    .trimEnd();
}

function getTagListDescending() {
  return tryRunGit(['tag', '--sort=-creatordate'])
    .split(/\r?\n/)
    .map((value) => value.trim())
    .filter((value) => TAG_PATTERN.test(value));
}

const mode = getArg('--mode', 'unreleased').trim().toLowerCase();
const currentTag = getArg('--tag') || getArg('--current-tag');
const dateOverride = getArg('--date');
const changeLogPath = getArg('--changelog', 'CHANGELOG.md');
const releaseNotesPath = getArg('--release-notes', '.github/release-notes.md');
const today = dateOverride || new Date().toISOString().slice(0, 10);

const existingContent = existsSync(changeLogPath)
  ? readFileSync(changeLogPath, 'utf8')
  : `${CHANGELOG_HEADER}\n\n`;
const { bodyWithoutUnreleased } = splitExistingChangelog(existingContent);
const tagListDescending = getTagListDescending();
const latestExistingTag = tagListDescending[0] ?? '';

let nextChangelog = '';
let releaseSection = '';

if (mode === 'unreleased') {
  const rangeSpec = latestExistingTag ? `${latestExistingTag}..HEAD` : 'HEAD';
  const unreleasedSection = buildSection('Unreleased', buildLogEntries(rangeSpec));
  const preservedBody = bodyWithoutUnreleased.trim();
  nextChangelog = `${CHANGELOG_HEADER}\n\n${unreleasedSection}\n\n${
    preservedBody ? `${preservedBody}\n` : ''
  }`;
} else if (mode === 'release') {
  if (!currentTag) {
    throw new Error('Missing required --tag argument for release mode.');
  }

  const previousTag = tagListDescending.find((value) => value !== currentTag) ?? '';
  const releaseRange = previousTag ? `${previousTag}..HEAD` : 'HEAD';
  const unreleasedRange = `${currentTag}..HEAD`;
  const existingReleaseSection = getSection(bodyWithoutUnreleased, currentTag);
  releaseSection = /\bYANKED\b/i.test(existingReleaseSection)
    ? existingReleaseSection
    : buildSection(`${currentTag} - ${today}`, buildLogEntries(releaseRange));
  const unreleasedSection = buildSection('Unreleased', buildLogEntries(unreleasedRange));
  const bodyWithoutCurrentRelease = removeSection(bodyWithoutUnreleased, currentTag).trim();

  nextChangelog = `${CHANGELOG_HEADER}\n\n${unreleasedSection}\n\n${releaseSection}\n\n${
    bodyWithoutCurrentRelease ? `${bodyWithoutCurrentRelease}\n` : ''
  }`;
} else {
  throw new Error(`Unsupported mode '${mode}'. Use 'unreleased' or 'release'.`);
}

writeFileSync(changeLogPath, nextChangelog.replace(/\n{3,}/g, '\n\n'), 'utf8');

if (mode === 'release') {
  mkdirSync(dirname(releaseNotesPath), { recursive: true });
  writeFileSync(releaseNotesPath, `${releaseSection.trim()}\n`, 'utf8');
}
