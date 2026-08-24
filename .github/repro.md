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

## How to provide a reproduction project?

With a reproduction we want to rule out a couple of things:

- The issue is in the Sentry SDK, not in your code or a third-party library;
- The issue has not already been resolved in the latest version of the SDK;

Therefore we would like to propose the following steps to create a reproduction sample:

1. Check that you are using the **latest stable version** of the relevant Sentry NuGet package
   and that you can still reproduce the issue.
2. Check whether an issue is already open for this. If there is, add a 👍 reaction to the
   first post so we know how many people are impacted and, if you have additional information,
   add a comment. If there isn't, open a new issue and fill in all the fields of the template.
3. Create a minimal reproduction:
   - Start with `dotnet new console` (or the relevant project type) – a clean, minimal project.
   - Add only the Sentry NuGet package(s) needed to demonstrate the bug.
   - Reproduce the issue with as little extra code as possible.
   - Remove any code that is not needed to reproduce the issue – this is noise and makes it
     harder for us to find the root cause.
4. Put the reproduction on a **public GitHub repository** and include the link in your issue.

> [!WARNING]
> We can't accept zip files attached to the issue. If we need the code as a zip we can always
> get that from the GitHub repository. Using a repo also makes it easier to collaborate – we
> can open a PR against your repro repo if we spot something and you can easily see the diff.
>
> While we've never had problems with this, it is still a potential attack vector. Even unzipping
> a file could execute code, let alone loading it into an IDE. Because we value your safety and
> privacy as well as our own, we want to make sure that none of this can happen.

## Tips for good repros

- **Never** put any sensitive information in your code. No DSN values, API keys, credentials,
  personal information, etc.
- **Never** include proprietary code from your employer or a third party. We are contractually
  not allowed to look at such code without NDA agreements and legal sign-off.
- **Never** submit binaries (mostly covered by using a GitHub repo).
- **Do not** reference external data sources that we can't access. Any external data needed
  should be generated synthetically inside the repro itself.
- **Always** refer to **concrete version numbers**. Avoid "the latest version" – we don't know
  whether you mean stable or pre-release. Always include the exact version number.

Thank you so much for taking some of your valuable time to make the Sentry SDK for .NET better!
We really appreciate it. ❤️
