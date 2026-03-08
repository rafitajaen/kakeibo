#!/usr/bin/env bun
/**
 * Monorepo quality check runner.
 *
 * Executes all registered quality checks sequentially.
 * Add new projects to the CHECKS array when they are created (mandatory.md Rule 6).
 *
 * Usage:
 *   bun run scripts/quality-check.ts          # all checks
 *   bun run scripts/quality-check.ts api      # api checks only
 *   bun run scripts/quality-check.ts email    # email checks only
 *   bun run scripts/quality-check.ts app      # app checks only
 */

import { spawnSync } from "child_process";

// ANSI color codes
const GREEN = "\x1b[32m";
const RED = "\x1b[31m";
const YELLOW = "\x1b[33m";
const CYAN = "\x1b[36m";
const RESET = "\x1b[0m";
const BOLD = "\x1b[1m";

// Detect Docker availability by attempting to contact the Docker daemon.
function hasDocker(): boolean {
  const result = spawnSync("docker", ["info"], {
    stdio: "pipe",
    timeout: 5000,
  });
  return result.status === 0;
}

interface Check {
  name: string;
  project: string;
  cmd: string[];
  /** Optional prerequisite function. When it returns false the check is skipped. */
  prerequisite?: () => boolean;
  skipReason?: string;
}

// ─── CHECKS ─────────────────────────────────────────────────────────────────
// Executed sequentially (never in parallel) per CLAUDE.md sequential execution rule.
// Format checks are intentionally excluded here — CI validates formatting via `dotnet format --verify-no-changes` (api:format:check).
const CHECKS: Check[] = [
  // ── API (backend) ─────────────────────────────────────────────────────────
  {
    name: "build",
    project: "api",
    cmd: ["dotnet", "build", "Kakeibo.slnx", "--configuration", "Release"],
  },
  {
    name: "test",
    project: "api",
    cmd: [
      "dotnet",
      "test",
      "tests/Kakeibo.Tests/",
      "--configuration",
      "Release",
      "--no-build",
    ],
    prerequisite: hasDocker,
    skipReason: "Docker not available — Testcontainers tests require Docker",
  },

  // ── Email service ─────────────────────────────────────────────────────────
  {
    name: "typecheck",
    project: "email",
    cmd: ["bun", "run", "email:typecheck"],
  },
  {
    name: "lint",
    project: "email",
    cmd: ["bun", "run", "email:lint"],
  },
  {
    name: "format:check",
    project: "email",
    cmd: ["bun", "run", "email:format:check"],
  },

  // ── App (frontend) ────────────────────────────────────────────────────────
  {
    name: "lint:check",
    project: "app",
    cmd: ["bun", "run", "app:lint:check"],
  },
  {
    name: "typecheck",
    project: "app",
    cmd: ["bun", "run", "app:typecheck"],
  },
  {
    name: "test:unit",
    project: "app",
    cmd: ["bun", "run", "app:test:unit"],
  },
  {
    name: "build",
    project: "app",
    cmd: ["bun", "run", "app:build"],
  },
];
// ─────────────────────────────────────────────────────────────────────────────

// Filter by project argument if provided (e.g. "bun run scripts/quality-check.ts api")
const filterProject = process.argv[2]?.toLowerCase();
const checks = filterProject
  ? CHECKS.filter((c) => c.project === filterProject)
  : CHECKS;

if (filterProject && checks.length === 0) {
  console.error(`${RED}No checks registered for project "${filterProject}"${RESET}`);
  process.exit(1);
}

interface Result {
  check: Check;
  status: "pass" | "fail" | "skip";
  durationMs: number;
}

const results: Result[] = [];

console.log(
  `\n${BOLD}${CYAN}Kakeibo quality checks${RESET}${
    filterProject ? ` — ${filterProject}` : ""
  }\n`
);

for (const check of checks) {
  const label = `${check.project}:${check.name}`;
  process.stdout.write(`  ${CYAN}▶${RESET}  ${label.padEnd(32)}`);

  // Check prerequisite
  if (check.prerequisite && !check.prerequisite()) {
    results.push({ check, status: "skip", durationMs: 0 });
    console.log(`${YELLOW}SKIP${RESET}  (${check.skipReason ?? "prerequisite not met"})`);
    continue;
  }

  const start = Date.now();
  const result = spawnSync(check.cmd[0], check.cmd.slice(1), {
    stdio: "pipe",
    encoding: "utf-8",
    cwd: process.cwd(),
  });
  const durationMs = Date.now() - start;

  if (result.status === 0) {
    results.push({ check, status: "pass", durationMs });
    console.log(`${GREEN}PASS${RESET}  (${durationMs}ms)`);
  } else {
    results.push({ check, status: "fail", durationMs });
    console.log(`${RED}FAIL${RESET}  (${durationMs}ms)`);

    // Print stdout/stderr on failure
    if (result.stdout?.trim()) {
      console.log(`\n${result.stdout.trim()}\n`);
    }
    if (result.stderr?.trim()) {
      console.error(`\n${result.stderr.trim()}\n`);
    }
  }
}

// Summary
const passed = results.filter((r) => r.status === "pass").length;
const failed = results.filter((r) => r.status === "fail").length;
const skipped = results.filter((r) => r.status === "skip").length;

console.log(`\n${BOLD}Summary:${RESET}  ${GREEN}${passed} passed${RESET}  ${RED}${failed} failed${RESET}  ${YELLOW}${skipped} skipped${RESET}\n`);

if (failed > 0) {
  process.exit(1);
}
