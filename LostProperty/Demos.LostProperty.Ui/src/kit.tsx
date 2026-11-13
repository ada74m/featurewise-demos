import { useState, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { linkToAddress, service } from "./addresses";
import { styles } from "./styles";
import type { Host, Link } from "./shell";

// What every feature's handlers share: mounting a page, presenting a link, and asking for details.

// A handler shows one feature's result, or one case of it, and returns a function that clears it. The
// body is the result as the service sent it. Its shape is the feature's, so the handler's author reads it
// there, rather than copying it into types here that nothing would check against the service.
export type Handler = (element: HTMLElement, body: any, host: Host) => () => void;

// Mounts a page into the element the shell gives, with the office's styles.
export function page(element: HTMLElement, content: ReactNode): () => void {
  addStyles(element.ownerDocument);
  const root = createRoot(element);
  root.render(<div className="lp">{content}</div>);
  return () => root.unmount();
}

// A command's result that's a link to a query: say what happened, and go there.
export function toastAndFollow(host: Host, message: string, next: Link): () => void {
  host.toast(message);
  host.follow(next);
  return () => {};
}

// A link presented as an anchor: it has an href where the link has an address, so it can be opened in
// a new tab or copied, and a click follows it through the shell.
export function Anchor({ link, host, children }: { link: Link; host: Host; children: ReactNode }) {
  const address = linkToAddress(link);
  return (
    <a href={address === null ? "#" : `/${service}/${address}`}
       onClick={event => {
         if (event.metaKey || event.ctrlKey || event.shiftKey)
           return;
         event.preventDefault();
         host.follow(link);
       }}>
      {children}
    </a>
  );
}

// A link presented as a button, for doing something rather than going somewhere.
export function Button({ link, host, primary, children }: { link: Link; host: Host; primary?: boolean; children: ReactNode }) {
  return <button type="button" className={primary ? "fw-primary" : undefined} onClick={() => host.follow(link)}>{children}</button>;
}

// A link with details left to supply, presented as a form: one field for each, filled in with what the
// link already holds, the missing ones marked. Submitting follows the link with them filled in.
export function DetailsForm({ link, fields, missing, submit, host }: {
  link: Link;
  fields: { name: string; label: string }[];
  missing: string[];
  submit: string;
  host: Host;
}) {
  const [values, setValues] = useState<Record<string, string>>(
    Object.fromEntries(fields.map(field => [field.name, typeof link[field.name] === "string" ? link[field.name] as string : ""])));

  return (
    <form className="lp-form" onSubmit={event => {
      event.preventDefault();
      host.follow({ ...link, ...values });
    }}>
      {fields.map((field, index) => (
        <label key={field.name} className={missing.includes(field.name) && !values[field.name] ? "lp-missing" : undefined}>
          {field.label}
          <input name={field.name} value={values[field.name] ?? ""} autoFocus={index === 0}
                 onChange={event => setValues({ ...values, [field.name]: event.target.value })} />
        </label>
      ))}
      <div className="fw-actions">
        <button type="submit" className="fw-primary">{submit}</button>
      </div>
    </form>
  );
}

// "4 Nov", as the office writes its dates.
export const day = (iso: string) => new Date(iso).toLocaleDateString("en-GB", { day: "numeric", month: "short" });

function addStyles(document: Document): void {
  if (document.getElementById("lost-property-styles"))
    return;
  const style = document.createElement("style");
  style.id = "lost-property-styles";
  style.textContent = styles;
  document.head.append(style);
}
