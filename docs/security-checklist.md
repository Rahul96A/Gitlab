# Security, Scaling & Monitoring Checklist

Comprehensive operational checklist for GitLabClone production readiness.

---

## 1. Authentication & Authorization

### JWT Security
- [ ] JWT secret is **>= 64 bytes**, stored in **Key Vault** (never in appsettings or env vars directly)
- [ ] Token expiry set to **8 hours** max; consider refresh tokens for long sessions
- [ ] `ClockSkew` set to **1 minute** (not the default 5)
- [ ] Tokens include `sub`, `jti` (unique ID), `iat`, `exp`, and role claims only — no PII
- [ ] Revocation strategy: maintain a short-lived deny-list in Redis for forced logouts

### Password Security
- [ ] BCrypt with **work factor 12+** (current implementation uses BCrypt)
- [ ] Enforce minimum 10-character passwords with complexity rules
- [ ] Rate-limit login attempts: **5 failures per 15 minutes per IP** (use Redis sliding window)
- [ ] Account lockout after **10 consecutive failures**; unlock via email or admin

### Git Basic Auth
- [ ] Basic Auth middleware only activates on `.git/` URL paths
- [ ] Credentials validated against DB with BCrypt — no plaintext comparison
- [ ] Failed auth returns `401` with `WWW-Authenticate: Basic` header (no error details)
- [ ] Consider supporting **Personal Access Tokens** (PATs) as Basic Auth passwords

### Entra ID (Azure AD)
- [ ] App registration uses **SPA redirect URIs** (not Web — avoids PKCE bypass)
- [ ] API scope `access_as_user` configured; no broad `User.Read.All` grants
- [ ] App roles (Admin/Maintainer/Developer/Reporter/Guest) mapped to claims
- [ ] Token validation: issuer, audience, lifetime, signing key all checked
- [ ] Multi-tenant disabled (`AzureADMyOrg` audience)

---

## 2. Input Validation & Injection Prevention

### SQL Injection
- [ ] **EF Core parameterized queries only** — no raw SQL or `FromSqlRaw` with string concatenation
- [ ] Verify no `ExecuteSqlRaw` calls with user input
- [ ] Database user has **minimal permissions** (no `db_owner`; use `db_datareader` + `db_datawriter`)

### XSS Prevention
- [ ] React auto-escapes JSX output — verify no `dangerouslySetInnerHTML` usage
- [ ] API responses use `Content-Type: application/json` (no HTML rendering)
- [ ] File content from repositories displayed in `<pre>` / `<code>` — never rendered as HTML
- [ ] CSP header: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'`

### Command Injection (Git operations)
- [ ] `GitHttpService` shells out to `git` — verify arguments are validated/sanitized
- [ ] Repository paths derived from **GUID-based directory names**, not user input
- [ ] No user-controllable input reaches `Process.Start` arguments without validation
- [ ] Git ref names validated against `GitReference` value object regex

### Path Traversal
- [ ] File browser `path` parameter sanitized — no `..` segments allowed
- [ ] Repository path resolution uses `Path.GetFullPath` and validates it stays within repo root
- [ ] Blob storage keys derived from project GUIDs, not user input

### Request Validation
- [ ] FluentValidation on all commands (string lengths, enum ranges, format checks)
- [ ] `MaximumLength` on all string fields to prevent memory abuse
- [ ] File upload size limits: **100 MB** for Git push, **10 MB** for API uploads
- [ ] Request body size limit: `app.UseMaxRequestBodySize(52_428_800)` (50 MB)

---

## 3. API Security

### Rate Limiting
- [ ] Add `Microsoft.AspNetCore.RateLimiting` middleware:
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("api", o =>
      {
          o.PermitLimit = 100;
          o.Window = TimeSpan.FromMinutes(1);
          o.QueueLimit = 10;
      });
      options.AddFixedWindowLimiter("auth", o =>
      {
          o.PermitLimit = 5;
          o.Window = TimeSpan.FromMinutes(15);
      });
  });
  ```
- [ ] Apply `[EnableRateLimiting("auth")]` to login/register endpoints
- [ ] Apply `[EnableRateLimiting("api")]` globally or per-controller
- [ ] Git clone/push uses separate higher limit (50 req/min)

### CORS
- [ ] Production: `AllowedOrigins` set to **exact frontend domain** (not `*`)
- [ ] `AllowCredentials` only with explicit origins (never with `*`)
- [ ] No `AllowAnyHeader` in production — specify allowed headers explicitly

### Headers
- [ ] Add security headers middleware:
  ```
  X-Content-Type-Options: nosniff
  X-Frame-Options: DENY
  Referrer-Policy: strict-origin-when-cross-origin
  Permissions-Policy: camera=(), microphone=(), geolocation=()
  Strict-Transport-Security: max-age=31536000; includeSubDomains
  ```
- [ ] Remove `Server` header: `builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false)`

### HTTPS
- [ ] Container Apps enforce HTTPS by default — verify `ingress.allowInsecure: false`
- [ ] HSTS header set with 1-year max-age
- [ ] All internal service communication over TLS (SQL, Redis, Storage all require TLS 1.2+)

---

## 4. Data Protection

### Secrets Management
- [ ] All secrets in **Azure Key Vault** — not in config files, env vars, or source control
- [ ] Key Vault uses **RBAC authorization** (not access policies)
- [ ] Container Apps reference secrets via Key Vault references or managed identity
- [ ] Rotate JWT secret every **90 days** — coordinate with token expiry
- [ ] SQL password rotation every **90 days**

### Encryption
- [ ] SQL Server: **TDE (Transparent Data Encryption)** enabled by default on Azure SQL
- [ ] Blob Storage: **SSE (Storage Service Encryption)** enabled by default
- [ ] Redis: TLS 1.2 required, non-SSL port disabled
- [ ] Backups: repository tar.gz archives in Blob Storage use SSE

### Soft Delete & Data Retention
- [ ] Soft delete interceptor prevents permanent data loss on accidental deletes
- [ ] `IgnoreQueryFilters` used only in admin/audit contexts
- [ ] Blob Storage soft delete enabled (14-day retention)
- [ ] SQL point-in-time restore enabled (7-day retention minimum)

### PII Handling
- [ ] User emails stored but never exposed in public API responses
- [ ] Git commits contain author name/email from repository — displayed as-is (git metadata, not PII)
- [ ] No PII in application logs (Serilog configured to exclude request bodies)

---

## 5. Scaling

### Container Apps Auto-Scaling
- [ ] API: min 1, max 10 replicas; scale on HTTP concurrency (50 concurrent requests per replica)
- [ ] Frontend: min 1, max 5 replicas; scale on HTTP concurrency (100 per replica)
- [ ] Scale-to-zero disabled for API (cold start too slow for Git operations)

### Database Scaling
- [ ] Start with Azure SQL **Basic** (5 DTU) for MVP; upgrade to **Standard S1** (20 DTU) at ~100 users
- [ ] Add **read replicas** for file browser queries at ~1000 users
- [ ] Connection pooling: EF Core default pool size is 1024 — sufficient for most loads
- [ ] Index checklist:
  - `IX_Projects_Slug` (unique, filtered on IsDeleted=0) — already configured
  - `IX_Issues_ProjectId_IssueNumber` (unique, filtered) — already configured
  - `IX_Pipelines_ProjectId_CreatedAt` — add for pipeline listing
  - `IX_ProjectMembers_ProjectId_UserId` — add for member lookups

### Redis Scaling
- [ ] Start with **Basic C0** (250 MB); upgrade to **Standard C1** (1 GB) at ~500 users
- [ ] Enable Redis Cluster at ~5000 users for horizontal scaling
- [ ] Cache strategy:
  - Session: per-user, 8-hour TTL
  - File tree: per-project per-ref, 60-second TTL (invalidated on push)
  - Project list: global, 30-second TTL

### Blob Storage Scaling
- [ ] Standard LRS sufficient for MVP; upgrade to **GRS** for geo-redundancy at scale
- [ ] Repository tar.gz backups: trigger on push (with 2-second debounce) — already implemented
- [ ] Artifact storage: set lifecycle policy to **delete after 30 days** for non-release artifacts
- [ ] Large repository support: consider Azure Files (NFS) mounted volume at ~100+ repos

### Git Operations Scaling
- [ ] Ephemeral disk: repos restored from Blob on container startup — works for single replica
- [ ] Multi-replica strategy: use **Azure Files Premium** (NFS) shared volume instead of EmptyDir
- [ ] Git pack operations are CPU-intensive: allocate 1+ CPU for API container at scale
- [ ] Consider dedicated **Git worker** container for clone/push operations at ~500+ repos

---

## 6. Monitoring & Observability

### Structured Logging (Serilog)
- [ ] Log to **Azure Log Analytics** via Application Insights sink
- [ ] Structured properties: `UserId`, `ProjectSlug`, `RequestId`, `TraceId`
- [ ] Log levels:
  - `Warning`: requests > 500ms (LoggingBehavior already does this)
  - `Error`: unhandled exceptions, Git operation failures
  - `Information`: authentication events, project creation, pipeline triggers
- [ ] Exclude from logs: passwords, tokens, file content, request/response bodies

### Application Insights
- [ ] Install `Microsoft.ApplicationInsights.AspNetCore` for automatic telemetry
- [ ] Track custom metrics:
  ```csharp
  // Git operations duration
  telemetry.TrackMetric("GitCloneDuration", stopwatch.ElapsedMilliseconds);
  // Pipeline execution time
  telemetry.TrackMetric("PipelineRunDuration", (pipeline.FinishedAt - pipeline.StartedAt).TotalSeconds);
  ```
- [ ] Enable **Dependency Tracking** for SQL, Redis, Blob Storage
- [ ] Application Map shows service topology automatically

### Health Checks
- [ ] `/health` endpoint already configured with DB check
- [ ] Add additional checks:
  ```csharp
  builder.Services.AddHealthChecks()
      .AddDbContextCheck<AppDbContext>("database")
      .AddRedis(redisConnection, "redis")
      .AddAzureBlobStorage(storageConnection, "blob-storage")
      .AddCheck("git", () =>
      {
          // Verify git CLI is available
          var process = Process.Start("git", "--version");
          return process?.ExitCode == 0
              ? HealthCheckResult.Healthy()
              : HealthCheckResult.Unhealthy("git not found");
      });
  ```
- [ ] Separate `/health/ready` (full checks) vs `/health/live` (process alive)
- [ ] Container Apps liveness/readiness probes configured — already in Bicep

### Alerting
- [ ] **Azure Monitor Alert Rules**:
  | Metric | Threshold | Action |
  |--------|-----------|--------|
  | HTTP 5xx rate | > 5/min for 5 min | Email + Teams webhook |
  | Response time P95 | > 2s for 10 min | Email |
  | CPU usage | > 80% for 5 min | Auto-scale (already configured) |
  | Database DTU | > 90% for 10 min | Email (upgrade prompt) |
  | Failed health checks | > 3 consecutive | PagerDuty / email |

### Dashboard
- [ ] Create **Azure Dashboard** with:
  - Request rate and latency (P50/P95/P99)
  - Error rate by endpoint
  - Container replica count over time
  - Database DTU usage
  - Redis memory and hit/miss ratio
  - Active pipeline count
  - Git operations per hour

### Distributed Tracing
- [ ] `Activity.DefaultIdFormat = ActivityIdFormat.W3C` (default in .NET 9)
- [ ] Serilog enriches with `TraceId` and `SpanId` automatically
- [ ] Frontend: add `X-Request-Id` header in ky `beforeRequest` hook
- [ ] SignalR traces: enable in Application Insights configuration

---

## 7. Disaster Recovery

### Backup Strategy
| Resource | Method | Frequency | Retention |
|----------|--------|-----------|-----------|
| Azure SQL | Point-in-time restore | Continuous | 7 days (Basic), 35 days (Standard+) |
| Blob Storage (repos) | Soft delete + versioning | On every push | 14 days |
| Blob Storage (artifacts) | Lifecycle policy | N/A | 30 days |
| Redis | N/A (cache only) | N/A | Rebuild from DB |
| Key Vault | Soft delete + purge protection | N/A | 90 days |

### Recovery Procedures
- [ ] **Database corruption**: Restore from point-in-time backup via Azure Portal
- [ ] **Lost repository data**: Repos auto-restore from Blob on container restart
- [ ] **Compromised JWT secret**: Rotate in Key Vault → restart API containers → all tokens invalidated
- [ ] **Container failure**: Auto-restart via Container Apps health probes (already configured)
- [ ] **Region failure**: Redeploy Bicep to secondary region; restore DB from geo-backup (requires GRS)

### RTO/RPO Targets (MVP)
| Metric | Target |
|--------|--------|
| RTO (Recovery Time Objective) | 30 minutes |
| RPO (Recovery Point Objective) | 5 minutes (SQL continuous backup) |

---

## 8. Compliance & Audit

### Audit Trail
- [ ] `AuditableEntity` tracks `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` — already implemented
- [ ] `ActivityEvent` entity records project-level activities
- [ ] Enable **Azure SQL Auditing** to Log Analytics
- [ ] Enable **Key Vault diagnostic logging**

### Access Control Audit
- [ ] Log all authentication events (login, logout, failed attempts)
- [ ] Log all authorization failures (403 responses)
- [ ] Log all admin actions (project deletion, role changes)
- [ ] Monthly review of Entra ID app role assignments

---

## 9. Pre-Launch Checklist

### Before First Deploy
- [ ] Run `dotnet ef database update` to create schema
- [ ] Verify Key Vault secrets are populated
- [ ] Verify ACR images are pushed
- [ ] Verify DNS/custom domain configured (optional)
- [ ] Verify SSL certificate (Container Apps provides free managed cert)

### Before Going Public
- [ ] Penetration test or security review completed
- [ ] Rate limiting enabled and tested
- [ ] CORS locked to production domain
- [ ] Security headers verified (use securityheaders.com)
- [ ] Error pages don't leak stack traces (GlobalExceptionMiddleware handles this)
- [ ] Swagger UI disabled in production (`if (app.Environment.IsDevelopment())`)
- [ ] Default admin account created with strong password
- [ ] Monitoring dashboard and alerts configured
- [ ] Backup/restore procedure tested
- [ ] Load test: verify 100 concurrent users with acceptable latency (< 500ms P95)
