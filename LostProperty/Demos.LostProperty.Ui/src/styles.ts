// The office's own look. Headings, links, buttons, fields and tables come styled from the shell, and
// fw-stack and fw-row lay things out; this adds only what's particular to the office, from the shell's
// custom properties, so it sits with the rest of the shell.
export const styles = `
  .lp .lp-lede { color: var(--fw-muted); margin: 0 0 var(--fw-space-4); }
  .lp .lp-category { display: block; color: var(--fw-muted); font-size: var(--fw-size-small); }
  .lp .lp-bar { margin: var(--fw-space-4) 0; }
  .lp .lp-bar > .fw-primary { margin-right: auto; }
  .lp .lp-panel { background: var(--fw-surface); border: 1px solid var(--fw-border); border-radius: var(--fw-radius);
                  padding: var(--fw-space-3) var(--fw-space-4); }
  .lp form.lp-panel { max-width: 28rem; padding: var(--fw-space-4); }
  .lp .lp-panel button[type=submit] { align-self: flex-end; }
  .lp .lp-match { margin-top: var(--fw-space-2); padding-top: var(--fw-space-2); border-top: 1px dashed var(--fw-border); }
  .lp .lp-match p { margin: 0; flex: 1; }
  .lp .lp-none { color: var(--fw-muted); margin: var(--fw-space-2) 0 0; }
  .lp .lp-missing input { border-color: var(--fw-accent); }
  .lp .lp-notice { background: var(--fw-surface); border-left: 4px solid var(--fw-accent);
                   padding: var(--fw-space-3) var(--fw-space-4); margin-bottom: var(--fw-space-4); }
  .lp .lp-notice p { margin: 0 0 var(--fw-space-2); }
`;
