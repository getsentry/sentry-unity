// e.g. "feat" if PR title is "Feat : add more useful stuff"
// or  "ci" if PR branch is "ci/update-danger"
function getPrFlavor() {
  if (danger.github && danger.github.pr) {
    var separator = undefined;
    if (danger.github.pr.title) {
      const parts = danger.github.pr.title.split(":");
      if (parts.length > 1) {
        return parts[0].toLowerCase().trim();
      }
    }
    if (danger.github.pr.head && danger.github.pr.head.ref) {
      const parts = danger.github.pr.head.ref.split("/");
      if (parts.length > 1) {
        return parts[0].toLowerCase();
      }
    }
  }
  return "";
}

async function checkDocs() {
  if (getPrFlavor().startsWith("feat")) {
    message(
      'Do not forget to update <a href="https://github.com/getsentry/sentry-docs">Sentry-docs</a> with your feature once the pull request gets approved.'
    );
  }
}

async function checkChangelog() {
  const changelogFile = "CHANGELOG.md";

  // Check if skipped
  if (danger.github && danger.github.pr) {
    if (
      ["ci", "chore(deps)"].includes(getPrFlavor()) ||
      (danger.github.pr.body + "").includes("#skip-changelog")
    ) {
      return;
    }
  }

  // Check if current PR has an entry in changelog
  const changelogContents = await danger.github.utils.fileContents(
    changelogFile
  );

  const hasChangelogEntry = RegExp(`#${danger.github.pr.number}\\b`).test(
    changelogContents
  );

  if (hasChangelogEntry) {
    return;
  }

  // Report missing changelog entry
  fail(
    "Please consider adding a changelog entry for the next release.",
    changelogFile
  );

  const prTitleFormatted = danger.github.pr.title
    .split(": ")
    .slice(-1)[0]
    .trim()
    .replace(/\.+$/, "");

  markdown(
    `
### Instructions and example for changelog

Please add an entry to \`CHANGELOG.md\` to the "Unreleased" section. Make sure the entry includes this PR's number.

Example:

\`\`\`markdown
## Unreleased

- ${prTitleFormatted} ([#${danger.github.pr.number}](${danger.github.pr.html_url}))
\`\`\`

If none of the above apply, you can opt out of this check by adding \`#skip-changelog\` to the PR description.`.trim()
  );
}

async function checkAll() {
  await checkDocs();
  await checkChangelog();
}

schedule(checkAll);
