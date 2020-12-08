var PR_NUMBER;
var PR_AUTHOR;
var PR_URL;
var PR_LINK;

const CHANGELOG_SUMMARY_TITLE = `Instructions and example for changelog`;
const CHANGELOG_BODY = `Please add an entry to \`CHANGELOG.md\` to the "Unreleased" section under the following heading:
 1. **Feat**: For new user-visible functionality.
 2. **Fix**: For user-visible bug fixes.
 3. **Ref**: For features, refactors and bug fixes in internal operation.

To the changelog entry, please add a link to this PR (consider a more descriptive message):`;

const CHANGELOG_END_BODY = `If none of the above apply, you can opt out by adding _#skip-changelog_ to the PR description.`;

function getCleanTitleWithPrLink() {
  const title = danger.github.pr.title;
  return title.split(": ").slice(-1)[0].trim().replace(/\.+$/, "") + PR_LINK;
}

function getChangelogDetailsHtml() {
  return `
<details>
<summary><b>\`${CHANGELOG_SUMMARY_TITLE}\`$</b></summary>

\`${CHANGELOG_BODY}\`

\`\`\`md
- ${getCleanTitleWithPrLink()}
\`\`\`

\`${CHANGELOG_END_BODY}\`
</details>
`;
}

function getChangelogDetailsTxt() {
	return CHANGELOG_SUMMARY_TITLE + '\n' +
		   CHANGELOG_BODY + '\n' +
		   getCleanTitleWithPrLink() + '\n' +
		   CHANGELOG_END_BODY;
}

async function containsChangelog(path) {
  const contents = await danger.github.utils.fileContents(path);
  return contents.includes(PR_LINK);
}

async function checkChangelog() {
  console.log("A");
  const skipChangelog =
    danger.github && (danger.github.pr.body + "").includes("#skip-changelog");
  console.log("B");
  if (skipChangelog) {
    return;
  }

  const hasChangelog = await containsChangelog("CHANGELOG.md");

  console.log("C");
  if (!hasChangelog) {
    fail("Please consider adding a changelog entry for the next release.");
	try
	{
		console.log("D");
		markdown(getChangelogDetailsHtml());
	}
	catch(error)
	{
		//Fallback
		console.log("E");
		fail(getChangelogDetailsTxt());
	}
  }
}

async function checkIfFeature() {
   console.log("CheckIfFeature");
   const title = danger.github.pr.title;
   console.log("check");
   if(title.startsWith('feat:')){
	 try{
		 console.log("EEE");
		 message('Do not forget to update <a href="https://github.com/getsentry/sentry-docs">Sentry-docs</a> with your feature once the pull request gets approved.');
	 }
	 catch(error)
	 {
		 console.log("III");
	 }
   }  
}

async function checkAll() {
   PR_NUMBER = danger.github.pr.number;
   console.log(PR_NUMBER);
   PR_AUTHOR   = danger.github.pr.user.login;
   console.log(PR_AUTHOR);
   PR_URL = danger.github.pr.html_url;
   console.log(PR_URL);
   PR_LINK = `. (#${PR_NUMBER}) @${PR_AUTHOR}`;
   console.log(PR_LINK);

  // See: https://spectrum.chat/danger/javascript/support-for-github-draft-prs~82948576-ce84-40e7-a043-7675e5bf5690
  const isDraft = danger.github.pr.mergeable_state === "draft";

  if (isDraft) {
    return;
  }

  await checkIfFeature();
  await checkChangelog();
}

schedule(checkAll);
