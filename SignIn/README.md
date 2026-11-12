# The demo sign-in

An identity provider for the demos, not a featurewise service. It has no features and no links: it's one Lambda serving OpenID Connect's endpoints, so that a visitor can become any of the demos' personas without a password. It stands where Cognito stands in an environment with real users, and the UI shell and every demo's UI backend trust it as they would Cognito.

```
GET  /.well-known/openid-configuration   discovery, which API Gateway's JWT authorizers read
GET  /jwks.json                          the public key tokens are verified with
GET  /authorize                          the code flow with PKCE; a page of personas without a hint
POST /token                              trades a code for tokens
GET  /logout                             sends the browser back to the shell
```

- **Personas.** The environment passes every demo's id and personas in `DEMOS`. The shell names a persona in `login_hint`, and the sign-in signs it in at once; without a hint, it shows them all, grouped by demo. A demo's rules see the persona's id as the caller's name and its groups as roles.
- **Sandboxes.** On a visitor's first visit, the sign-in sets a cookie naming a new sandbox, and copies it into every token's `sandbox` claim, so every persona the visitor becomes works in the same sandbox. Only the demos' facilities read it.
- **It stores nothing.** Tokens are RS256, signed by a KMS key whose private half never leaves KMS. A code is itself a signed token, good for two minutes and only with the PKCE verifier.

`Infrastructure/` is its Terraform module: the key, the function and an HTTP API. Its `endpoints` output is the shell's `sign_in` input, and its `issuer` and `client_id` are what each UI backend's JWT authorizer trusts.
