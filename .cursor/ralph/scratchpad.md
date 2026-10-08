---
iteration: 1
max_iterations: 0
completion_promise: "REVIEWS CLEAN"
---

run bugbot, security review, and UI/UX review subagents on this slice and then plan and implement all issues that arise in the review and then run bugbot, security review, and UI/UX review again and plan and implement all issues that arise in the review. Do this continuously in a ralph loop until there are no issues

You are running an autonomous Ralph loop on the current feature slice (the uncommitted/working-tree changes, or the named branch/PR/diff if one is specified).

### Review suite (run every iteration)
1. **Bugbot** – logic bugs, edge cases, correctness.
2. **Security Review** – secrets, authz, injection, dependency risk, dangerous patterns.
3. **Senior Developer Review** (you perform this yourself or via an equivalent high-rigor pass):
   - Infrastructure & operability: deployment model, config management, observability (logs/metrics/traces), failure modes, rollback safety, resource limits.
   - Performance: algorithmic complexity, hot paths, unnecessary allocations/IO, caching, concurrency, database/query patterns, memory/CPU characteristics under expected load.
   - Industry best practices: idiomatic use of the language/framework, SOLID / clean architecture where relevant, error handling & resilience, testing strategy (unit/integration/contract), API design, security-by-default, maintainability, and avoidance of common anti-patterns.
4. **UI/UX Review** (you perform this yourself or via an equivalent high-rigor UI/UX pass):
   - Accessibility: WCAG conformance, semantic HTML/ARIA, keyboard navigation, focus management, screen-reader labels, contrast, reduced motion, zoom/text scaling.
   - Responsive & adaptive layout: breakpoints, mobile/tablet/desktop behavior, overflow, touch targets, safe areas, orientation.
   - Interaction design: affordances, hover/active/focus/disabled/loading/error states, form validation, feedback, confirmation, undo, empty states.
   - Visual & design-system consistency: spacing, typography, color tokens, component reuse, iconography, dark mode, theming, brand alignment.
   - Usability & content: user flows, cognitive load, error prevention/recovery, microcopy, localization/i18n, RTL, date/number/currency formats.
   - Cross-browser/device compatibility: supported browsers, graceful degradation, progressive enhancement.
   - Front-end performance perception: layout shift, jank, skeleton/loading UX, optimistic UI where appropriate.
   - Analytics/observability for UX: funnel/error tracking, privacy-safe instrumentation, feature flags.

### Loop rules (strict)
1. Run the full review suite above (Bugbot, Security, Senior Developer, UI/UX) on the current slice.
2. Treat every finding as a candidate. Verify each one against the actual code + nearest tests + UI/UX evidence (screenshots, DOM, accessibility tree, design spec, analytics if available). Do not accept or dismiss blindly.
3. Produce a short prioritized plan:
   - Severity (Critical / High / Medium / Low)
   - Exact file:line (or clear location)
   - Root cause
   - Smallest correct fix that also improves (or at least does not regress) infrastructure, performance, UI/UX, accessibility, or best-practice alignment.
4. Implement **only the verified issues**. Prefer minimal, correct, production-grade changes. Add or update tests when the finding warrants it. For UI/UX findings, add/update visual regression, accessibility, or interaction tests where practical.
5. After the implementation (commit if appropriate), re-run the full review suite on the updated slice.
6. Repeat until **all four** reviews report zero actionable findings (or only clearly invalid/false-positive findings that you have documented and dismissed with evidence).

### Final response (required when the loop terminates)
Once the loop is clean, end with a clear summary that explains:
- Every change that was made (file + concise description of the diff).
- Why each change was necessary (link it back to the specific finding, the risk it mitigated, and how it improved correctness, security, infrastructure, performance, UI/UX, accessibility, or best-practice alignment).
- Any findings that were reviewed and deliberately dismissed, with the evidence used to dismiss them.
- Residual notes (if any) that a human should still be aware of.

### Guardrails
- Keep the loop tight: one coherent slice at a time. If the diff is large, first split it into smaller intermediate commits and review each in turn.
- Never expand scope beyond the verified findings.
- After each successful iteration, update a short `REVIEW_LOG.md` with: iteration number, findings fixed, findings dismissed + reason, remaining open items, and any residual performance/infra/UI/UX/accessibility notes.
- Stop only when Bugbot, Security Review, Senior Developer Review, and UI/UX Review are all clean. If a finding cannot be fixed without external product/architecture/design decisions, surface it clearly and pause.

Begin now with the current slice.
