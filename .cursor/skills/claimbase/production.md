# Production-ready generation

Read from [SKILL.md](SKILL.md) when implementing a slice. Generate as if Production and `ng build` will run before the slice is finished. Development conveniences must not leak into Production artifacts.

## Non-negotiables

1. No placeholders (`TODO`, `FIXME`, `NotImplementedException` stubs).
2. No committed Production secrets. Development JWT and database password stay in `appsettings.Development.json`. `appsettings.json` ships empty secrets. Development and Testing json are `CopyToPublishDirectory=Never`.
3. Production fails fast via `StartupConfiguration.Validate`: Development JWT key, development database password, and `Include Error Detail` refuse to boot. JWT key length ≥ 64 in Production. `Portal:BaseUrl` must be absolute `https`. `Email:Host` and `Email:FromAddress` are required.
4. New env vars use `__` nesting and are documented in [reference.md](reference.md) and [README.md](../../../README.md) in the same slice.
5. `dotnet test ClaimBase.sln` and the Portal production build must succeed for slices that contain those projects.
6. Honest empties. Do not seed fake claims or hide unfinished buttons that call missing APIs.

## Backend

- **Secrets:** never log passwords, reset tokens, reset URLs, JWTs, or raw biometric workbook bytes. Redact those property names in Serilog.
- **Auth:** maximum length on login password and on the reset token (DoS bound). Unknown-email login still runs a dummy password verify so timing does not enumerate users. Forgot-password returns one message for an unknown address, a shared address, and a real account. The request only looks up the email and queues the same follow-up for every result, so writing the link and talking to SMTP cannot enumerate users. Never return or log the raw reset token in Production. Rate-limit login, forgot-password, and reset-password together, and do not apply that limit to `/api/auth/me`.
- **Tenancy:** every business table has `TenantId` and the global query filter. Handlers read the tenant from JWT. Cross-tenant ids → 404.
- **Roles:** authorize on the controller. HoD queries add `DepartmentId` from the token. Do not trust a department id from the body.
- **Uploads:** the biometric endpoint accepts `.xlsx` only, with a size cap (16 MB). Other endpoints keep a small JSON body cap.
- **Host:** Production gets HSTS, HTTPS redirection, security headers (`nosniff`, `DENY` frame, `no-store` on API responses), anonymous `GET /health`. OpenAPI in Development only.
- **Hangfire:** dashboard is authenticated and limited to `TenantAdmin` in Development. Disable the dashboard in Production unless it sits behind the same JWT policy. Jobs are tenant-scoped by an explicit `TenantId` argument, not by the current HTTP user (there is no HTTP user on the worker).
- **CORS:** empty origins in Production mean same-origin Portal. `UseCors` runs before exception handling so error JSON still has `Access-Control-Allow-Origin`.
- **Logging:** console in Production. File sink in Development only.
- **Errors:** unhandled exceptions return 500 without internals.
- **Migrations:** generate from EF config. Development may migrate at boot. Production applies migrations in the release job, not on process start.
- **PDF:** render from claim lines in the database. Do not re-resolve live rates while rendering an approved claim.

## Portal

- `environment.ts`: `production: false`, `apiUrl: 'http://localhost:5190'`.
- `environment.production.ts`: `production: true`, `apiUrl: ''`. Feature `*.api.ts` files use the environment. Never hard-code `localhost` there.
- Expired JWT signs the user out. A 401 clears the session.
- Forms: `*_FIELD_LIMITS`, validators, `[attr.maxlength]`, trim and email lowercase on submit, course code uppercase on submit.
- Production build hashes output files.

## MobileApp

- API base URL comes from configuration, not a hard-coded production host in source. The ViewModel reads it through a service.
- Tokens live in secure storage, not in a world-readable text file, and not in a public ViewModel property that the binding would display.
- Offline logs stay on the device until sync succeeds. A failed sync does not delete the local row.

## Slice checklist

```
- [ ] No Development secrets in appsettings.json or the Portal production bundle
- [ ] New settings fail fast in Production
- [ ] TenantId + query filter on new tables
- [ ] Role checks match reference.md
- [ ] Presence jobs cannot write claim amounts
- [ ] Portal production apiUrl is same-origin
- [ ] Tests and production Portal build pass
- [ ] New public C# and exported TypeScript include XML/JSDoc; product-rule branches have an inline comment ([documentation.md](documentation.md))
- [ ] Hot path is paged, indexed, and async; Portal lists are OnPush and lazy-loaded ([performance.md](performance.md))
```
