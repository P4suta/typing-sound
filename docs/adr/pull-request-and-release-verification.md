---
date: 2026-10-21
---

# Pull-request and release verification

## Status

Accepted.
Supersedes the automatic nightly distribution in [ADR-0001](0001-automated-versioning-with-release-please.md) and [ADR-0004](0004-nightly-supply-chain-parity.md).

## Context

Pull requests need compiler, test, vulnerability, and static-analysis checks before merging.
Separate workflows repeat Windows setup and compilation for the same commit.
Daily unsigned distributions add another build without a requested release candidate.

## Decision

Use one Windows verification job for the build, coverage tests, NuGet audit, and C# CodeQL extraction.
Keep syntax, spelling, formatting, workflow analysis, and release approval checks in one Linux job.
Require both selected jobs through the existing `required` status.
Recheck code when labels change so approval changes cannot replace failed or unfinished verification.
Skip Windows verification only for changes outside its declared inputs.

Remove daily distributions, duplicate post-merge checks, Scorecard reporting, and the tool-version issue workflow.
Renovate proposes dependency and tool updates through pull requests.
Keep the weekly Linux scan of published release SBOMs because new advisories can affect already distributed binaries.
Keep stable release signing, attestations, and human approval gates.

## Consequences

Normal code changes use one Windows job and two Linux jobs.
Contributors keep the same `just verify` command and coverage threshold.
Unsigned daily downloads and automatic Scorecard reports end.
Release consumers keep signed, attested distributions and notice of newly disclosed dependency vulnerabilities.

Running every former check on demand would preserve unused workflows and duplicate maintenance.
Removing the shipped-release scan would miss advisories for binaries whose source has not changed.
Both alternatives are rejected.
