# Contributing to This Project

Thank you for taking the time to consider contributing to our project.

The following is a set of guidelines for contributing to the project. These are
mostly guidelines, not rules, and can be changed in the future. Please submit
your suggestions with a pull-request to this document.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [What should I know before I get started](#what-should-i-know-before-i-get-started)
  - [Project Folder Structure](#project-folder-structure)
  - [Design Decisions](#design-decisions)
  - [How can I contribute](#how-can-i-contribute)
    - [Create an Issue](#create-an-issue)
    - [Respond to an Issue](#respond-to-an-issue)
    - [Write code](#write-code)
    - [Write documentation](#write-documentation)

## Welcome

Thank you for your interest in contributing! We value all contributions and strive to make this project a welcoming, inclusive space for everyone.

Below are guidelines to help you get started. If you have suggestions, please submit a pull request to this document.

## Code of Conduct

We have adopted a code of conduct from the Contributor Covenant. Contributors to
this project are expected to adhere to this code. Please report unwanted
behavior to [Project Maintainer](mailto:matthew.paulosky@outlook.com)

## Quick Start

1. Fork the repository and clone your fork.
2. Run `git config core.hooksPath .github/hooks` once, so the repo's pre-commit and pre-push hooks run.
3. Create a branch from `main`, named by the branch standard below.
4. Make your changes, following the code style and guidelines below.
5. Add or update tests as needed.
6. Commit with clear messages (see below).
7. Push your branch and open a Pull Request to `main`.
8. Ensure all checks pass and respond to review feedback.

### Branch names

The pre-push hook refuses any other name:

| Branch | Use |
| --- | --- |
| `feature/{issue}-{slug}` | New behaviour |
| `fix/{issue}-{slug}` | A bug fix |
| `hotfix/{issue}-{slug}` | An urgent fix |
| `chore/{slug}` | Work without an issue |

`{slug}` is lowercase letters and digits in words joined by `-`, such as `feature/123-add-search`.

## What should I know before I get started

This project is a Blazor web application, orchestrated with .NET Aspire, for managing GitHub Issues through GitHub Projects. See [ARCHITECTURE.md](ARCHITECTURE.md).

### Code Style & Commit Messages

- Use consistent formatting (C# conventions, .editorconfig if present).
- Write clear, descriptive commit messages:
  - Use present tense (e.g., "Add search feature")
  - Reference issues (e.g., `Fixes #123`)
- Add comments to explain complex logic.

### Project Folder Structure

This project is designed to be built and run primarily with Visual Studio,
JetBrains Rider or Visual Studio Code. The folders are configured so that they
will support editing and working in other editors and on other operating
systems. We encourage you to develop with these other environments, because we
would like to be able to support developers who use those tools as well. The
folders are configured as follows:

```bash
docs/                                   -- Documentation, architecture, release blog posts

src/                                    -- Source code
  AppHost/                              -- .NET Aspire orchestration host
  ServiceDefaults/                      -- Shared Aspire service defaults (telemetry, health, resilience)
  Web/                                  -- Blazor UI project
    Components/                         -- Blazor components
      Layout/                           -- Layout components
      Pages/                            -- Page components
      Shared/                           -- Shared components
    Security/                           -- Auth0 configuration helpers
    Services/                           -- GitHub API clients and services
    wwwroot/                            -- Static web assets (CSS, JS, etc.)
    package.json                        -- Tailwind CSS build (pnpm)

tests/                                  -- Test projects
  AppHost.Tests/                        -- Aspire AppHost tests
  Architecture.Tests/                   -- Architecture and design rules tests
  Web.Tests.Bunit/                      -- Blazor component tests
  Web.Tests.E2E/                        -- Playwright end-to-end tests
  Web.Tests.Integration/                -- Web integration tests
  Web.Tests.Unit/                       -- Web unit tests

TicketManager.slnx                      -- Solution file
codecov.yml                             -- Code coverage configuration
Directory.Packages.props                -- Central NuGet package management
global.json                             -- Global SDK version
LICENSE                                 -- License
README.md                               -- Project overview
```

See the main [README.md](../README.md) for more details.

All official versions of the project are built and delivered with GitHub Actions and linked in the main README.md and the [Releases tab](https://github.com/mpaulosky/TicketManager/releases).

### Design Decisions

Design for this project is ultimately decided by the project team lead ([maintainer name or role]). The following project tenets are adhered to when making decisions:

1. Use [UI framework] for the UI.
1. Use [database technology] for data persistence.
1. Provide both [ORM/driver options] for data access.
1. Use [cloud orchestration/tooling] for cloud-native orchestration.
1. Follow clean architecture principles with repository pattern.

If you have suggestions, please open an issue or discuss in your pull request.

### How can I contribute

We are always looking for help on this project. There are several ways that you can help:
This means one of several types of contributions:

1. [Create an Issue](#create-an-issue)
1. [Respond to an Issue](#respond-to-an-issue)
1. [Write code](#write-code)
1. [Write documentation](#write-documentation)

## Contribution Types

- **Report a Bug:** Please add the `Bug` label so we can triage and track it.
- **Suggest an Enhancement:** Add the `Enhancement` label for new features or improvements.
- **Write Code:** All code should be linked to an issue. Include or update tests for new features and bug fixes.
- **Write Documentation:** Help us improve `/docs` and keep the main [README.md](../README.md) up to date.

### Create an Issue

Create a [New Issue Here](https://github.com/mpaulosky/TicketManager/issues/new/choose).

1. If you are reporting a `Bug` that you have found. Be sure to add the `Bug` label so that we can triage and track it.
1. If you are reporting an `Enhancement` that you think would improve the project. Be sure to add the `Enhancement`
   label so we can track it.

Please provide as much detail as possible, including steps to reproduce, expected behavior, and screenshots if helpful.

### Respond to an Issue

[Fork the Repository to your account](https://github.com/mpaulosky/TicketManager/fork).

1. Create a new Branch from `main` with a reference to the existing Issue number (`feature/{issue}-{slug}` or `fix/{issue}-{slug}`).
1. Work on the issue.
1. Create Unit, Integration tests for any code that require them. We use xUnit v3, bUnit and Playwright to test our code and components.
1. When you are done Create a Pull Request from your branch to `main`.
1. Submit the Pull Request.

**Note:** Pull requests without unit tests will be delayed until tests are added. All new features and bug fixes must
include appropriate tests.

#### Running the E2E tests locally

`tests/Web.Tests.E2E` uses Playwright to drive a real browser against `Web`, hosted on a real Kestrel port. Before
running it locally (or after a fresh `dotnet build`), install the Playwright browser binaries once:

```bash
pwsh tests/Web.Tests.E2E/bin/Release/net10.0/playwright.ps1 install chromium
```

(On a machine without PowerShell, install it first, or run the equivalent `playwright install chromium` via the
Playwright CLI.) CI installs browsers automatically as part of the pipeline.

Any code that is written to support a component or new functionality are
required to be accompanied with unit tests at the time the pull request is
submitted. Pull requests without unit tests will be delayed and asked for unit
tests to prove their functionality.

### Review Process

1. All PRs are reviewed by maintainers and may require changes before merging.
2. Automated checks (build, tests, lint) must pass before review.
3. Be responsive to feedback and update your PR as needed.
4. Once approved, your PR will be merged into `main`.

### Write code

All code should have an assigned issue that matches it. This way we can prevent contributors from working on the same
feature at the same time.

Code for components' features should also include some definition in the `/docs` folder so that our users can
identify and understand which feature is supported.

See [docs/](../docs) for feature documentation guidelines.

### Write documentation

The documentation for the project is always needed. We are always looking for help to add content to the `/docs`
section of the repository with proper links back through to the main `/README.md`.

---

Thank you for helping us make this project better!
