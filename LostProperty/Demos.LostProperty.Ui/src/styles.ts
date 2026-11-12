// The office's look, from the shell's custom properties, so it sits with the rest of the shell.
export const styles = `
  .lp h1 { font-size: 1.5rem; margin: 0 0 .25rem; }
  .lp .lp-lede { color: var(--fw-muted); margin: 0 0 1rem; }
  .lp a { color: var(--fw-accent); }
  .lp table { width: 100%; border-collapse: collapse; background: var(--fw-surface); border-radius: 8px; overflow: hidden; }
  .lp th, .lp td { text-align: left; padding: .5rem .9rem; border-bottom: 1px solid var(--fw-border); vertical-align: top; }
  .lp th { color: var(--fw-muted); font-weight: 500; font-size: .9em; }
  .lp .lp-category { display: block; color: var(--fw-muted); font-size: .9em; }
  .lp .lp-bar { display: flex; flex-wrap: wrap; align-items: center; gap: .75rem; margin: 1rem 0; }
  .lp .lp-bar .fw-primary { margin-right: auto; }
  .lp .lp-cards { display: grid; gap: .75rem; }
  .lp .lp-card { background: var(--fw-surface); border: 1px solid var(--fw-border); border-radius: 8px; padding: .75rem 1rem; }
  .lp .lp-card h2 { font-size: 1.05rem; margin: 0; }
  .lp .lp-match { margin-top: .5rem; padding-top: .5rem; border-top: 1px dashed var(--fw-border);
                  display: flex; flex-wrap: wrap; align-items: center; gap: .75rem; }
  .lp .lp-match p { margin: 0; flex: 1; }
  .lp .lp-none { color: var(--fw-muted); margin: .5rem 0 0; }
  .lp .lp-form { display: grid; gap: .75rem; max-width: 28rem; background: var(--fw-surface); padding: 1rem;
                 border: 1px solid var(--fw-border); border-radius: 8px; }
  .lp .lp-form label { display: grid; gap: .25rem; }
  .lp .lp-form input { font: inherit; padding: .35rem .5rem; border: 1px solid var(--fw-border); border-radius: 4px; }
  .lp .lp-form .lp-missing input { border-color: var(--fw-accent); }
  .lp .lp-form .fw-actions { margin-top: 0; }
  .lp .lp-notice { background: var(--fw-surface); border-left: 4px solid var(--fw-accent); padding: .75rem 1rem; margin-bottom: 1rem; }
  .lp .lp-notice p { margin: 0 0 .5rem; }
`;
