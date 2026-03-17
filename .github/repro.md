# Sentry SDK for .NET – Bug Report Reproduction Guide

First of all, thank you for reporting this potential bug. Nobody likes bugs, and to help us
diagnose and resolve your issue as effectively and quickly as possible, we would like to give
you a bit more information about why we ask for a reproducible example of the problem, and how
to provide one.

## What is a reproduction?

A reproduction, reproducible example or just *repro* for short is the most basic code to
demonstrate the issue that you're seeing. It's the simplest way to reproduce the issue.
Ideally, you should be able to reproduce the issue by just running the code in the project you
have provided and see the problem. If any reproduction steps are needed, either note them in the
issue or include them in the project somehow.

## Why do we ask for a reproducible example?

Depending on your project a codebase can be pretty big. The bigger your project, the more
factors that can influence a bug or issue. In order to be sure that the issue is happening in
the Sentry SDK and not something in your code (or a combination of both), it is super helpful
to have a small sample that reproduces the issue. This way we can:

- better pinpoint the source of the issue;
- fix the issue faster;
- make sure the issue is not a false positive.

It also yields an additional benefit. When you take your code apart piece-by-piece in a new
project, adding things back one by one, it will become very clear what factors are at play and
you might discover that the issue is somewhere in your own code. At the very least you will be
able to provide a very detailed description of what you're seeing. That helps us get to the
cause faster and resolve the issue quicker.

### I just want to report a bug, why do you want a reproduction?

We hear this a lot. We understand you're busy, we're all busy! A reproduction is not just about
pleasing us or you doing our work. As already mentioned above, it will help you get a better
understanding of where the issue is exactly. We've seen lots of cases where people realized,
through a reproduction, that the solution was right within their reach – regardless of whether
it was a bug in the SDK or not.

We like to see this as a team effort.

#### But I don't have time for this!

Please help us, help you! This is an open-source project under the MIT license. Provided as-is,
without any support or guarantees. We care about our project and therefore, by extension, also
about your project. But realize that when you come onto our repo, maybe frustrated because
things are not working and you just drop a one-liner without a reproduction, mentioning that you
don't have the time, is also not very motivating for us. On the other end of these GitHub
issues are real people – people that are doing their best to move this project forward.

Also consider how that comes across. If you don't have the time to report in detail what is
going on, then really how important is the issue? If this is really important and blocking you,
it would seem to make sense to prioritize getting us all the details to help resolve this
faster. We are all here to help you. But remember that we don't know your project and we don't
know any details – please help us understand and be nice.

## How to provide a reproduction project?

With a reproduction we want to rule out a couple of things:

- The issue is in the Sentry SDK, not in your code or a third-party library;
- The issue has not already been resolved in the latest version of the SDK;

Therefore we would like to propose the following steps to create a reproduction sample:

1. Check that you are using the **latest stable version** of the relevant Sentry NuGet package
   and that you can still reproduce the issue.
2. If yes, check any available preview/pre-release versions and see if you can reproduce the
   issue there too.
3. Check whether an issue is already open for this. If there is, add a 👍 reaction to the
   first post so we know how many people are impacted and, if you have additional information,
   add a comment. If there isn't, open a new issue and fill in all the fields of the template.
4. Create a minimal reproduction:
   - Start with `dotnet new console` (or the relevant project type) – a clean, minimal project.
   - Add only the Sentry NuGet package(s) needed to demonstrate the bug.
   - Reproduce the issue with as little extra code as possible.
   - Remove any code that is not needed to reproduce the issue – this is noise and makes it
     harder for us to find the root cause.
5. Put the reproduction on a **public GitHub repository** and include the link in your issue.

> [!WARNING]
> We can't accept zip files attached to the issue. If we need the code as a zip we can always
> get that from the GitHub repository. Using a repo also makes it easier to collaborate – we
> can open a PR against your repro repo if we spot something and you can easily see the diff.

## Why can't you just download my zip file reproduction?

While we've never had problems with this, it is still a potential attack vector. Even unzipping
a file could execute code, let alone loading it into an IDE. Because we value your safety and
privacy as well as our own, we want to make sure that none of this can happen.

By putting it on a GitHub repo it's also easier to collaborate. We (and our awesome community!)
can comment on code right then and there and help you further. It can even serve as a nice
example for other people!

If you don't like having lots of repos, you could create one repo (e.g. `sentry-dotnet-repros`)
where `main` holds a default new project and you create a branch for each issue.

## Big don'ts!

- **Never** put any sensitive information in your code. No DSN values, API keys, credentials,
  personal information, etc.
- **Never** include proprietary code from your employer or a third party. We are contractually
  not allowed to look at such code without NDA agreements and legal sign-off.
- **Never** submit binaries (mostly covered by using a GitHub repo).
- **Do not** reference external data sources that we can't access. Any external data needed
  should be generated synthetically inside the repro itself.
- **Always** refer to **concrete version numbers**. Avoid "the latest version" – we don't know
  whether you mean stable or pre-release. Always include the exact version number.

## That's it!

The first time might take you a bit longer to go through all this, but once you've done it
you'll see it isn't that much more work and it will benefit the process a lot.

Thank you so much for taking some of your valuable time to make the Sentry SDK for .NET better!
We really appreciate it. ❤️
