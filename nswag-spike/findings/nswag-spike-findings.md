# NSwag Spike — Auth (GoTrue) OpenAPI → C# Client

> Concrete follow-up to `codegen-investigation-findings.md` §8/§11 (Gate 2). Goal: stop
> hypothesizing and run a real toolchain against the team's **hand-maintained** Auth spec
> (`~/Downloads/openapi.yaml`, OpenAPI 3.0.3, 3928 lines) to see what NSwag actually emits,
> what's usable, and where the boundary falls.

Reproduction lives in `_nswag-spike/` (spec preprocessor, generated `.cs`, build harness).

## TL;DR

- NSwag **cannot ingest the spec as-shipped** — two blockers, one a tool bug, one a spec bug.
- After a ~30-line preprocessor, it generates a **clean-compiling** 11k-line client
  (System.Text.Json, injected `HttpClient`) — so "it runs" is a yes.
- But the output quality **splits hard along the models/operations line**, exactly as the A/B
  framework predicted:
  - **Models: usable-ish** (15 named schemas → real classes; fixable naming/nullability warts).
  - **Operations: unusable** — garbage method names, invented request/response types, one giant
    method where we hand-write five, and *zero* of the ergonomic/stateful behavior.
- Root cause is **not NSwag**. It's the spec: **0 `operationId`s** and only **15 named schemas
  against 103 inline `type: object` bodies**. Codegen quality is capped by spec richness, and
  this spec is model-rich / operation-poor.

**Verdict for the RFC:** this is direct evidence for the **"A for models, B for operations"**
hybrid. A whole-client generator applied to *this* spec produces something no maintainer would
own.

---

## 1. Getting the spec through the tool at all (2 blockers)

Environment: `dotnet 10.0.300`, `NSwag.ConsoleCore 14.7.1` (NJsonSchema 11.6.1).

### Blocker A — NSwag bug on reusable `responses` components (spec is valid)
The spec uses standard reusable responses: every operation does
`$ref: "#/components/responses/BadRequestResponse"` etc. NSwag 14.7.1 throws:

```
System.InvalidCastException: Unable to cast object of type 'NSwag.OpenApiResponse'
to type 'NJsonSchema.JsonSchema'.
```

This is a **known NSwag/NJsonSchema limitation**, not a spec error — `$ref` into
`#/components/responses/*` is textbook OpenAPI 3.0. 101 such refs in the spec.

### Blocker B — genuine spec bug (malformed `$ref`)
`/user/identities/{identityId}` DELETE puts a **response component in a schema slot**:

```yaml
401:
  content:
    application/json:
      schema:
        $ref: "#/components/responses/UnauthorizedResponse"   # <-- should be schemas/ErrorSchema
```

A response component is not a schema. This is a latent bug in the hand-maintained spec that a
codegen pipeline surfaces immediately (2 occurrences: 401 + 403 on that op).

### Workaround
`_nswag-spike/inline-responses.js` (~30 LOC): inline the 101 `responses` refs into each
operation and rewrite the 2 malformed schema-refs to `ErrorSchema`. This is a **pre-processing
step the pipeline would have to own and maintain** — first tax item.

---

## 2. Does it compile? Yes — with the right knobs

First pass (`/classname` single name, default op-grouping) **did not compile**: NSwag grouped
operations by tag into 3 `partial` fragments of the same class and emitted its helper region
(`ProcessResponse`, `ReadObjectResponseAsync`, `ConvertToString`, …) **three times** → 99
duplicate-member errors. Also needed a `System.ComponentModel.Annotations` reference on
netstandard.

Fix: `/operationGenerationMode:SingleClientFromOperationId`. Result:
**11,085 lines, one client class, builds clean (0 warnings/errors)** on `netstandard2.1`.

Generation settings used (all "idiomatic" per the assignment):
`/jsonLibrary:SystemTextJson /injectHttpClient:true /generateClientInterfaces:true
/generateNullableReferenceTypes:true /generateOptionalParameters:true /useBaseUrl:false`.

So: **injectable `HttpClient` ✔, System.Text.Json ✔, interface ✔.** The transport plumbing
NSwag emits is competent (`SendAsync` + `HttpCompletionOption.ResponseHeadersRead`, per-status
deserialization, `PrepareRequest`/`ProcessResponse` partial seams).

---

## 3. Output quality — split along models vs operations

### 3a. Models — usable, with warts (the 15 named schemas)
`UserSchema`, `AccessTokenResponseSchema`, `MFAFactorSchema`, `IdentitySchema`, etc. render as
real, individually-named classes. Problems, all fixable via templates/spec edits:

| Issue | Generated | Hand-written today |
|---|---|---|
| **Naming** | `Email_confirmed_at`, `Is_anonymous`, `New_email` (pascalizes one token, keeps snake underscores) | `EmailConfirmedAt`, `IsAnonymous` (proper) |
| **Nullability of optional timestamps** | `DateTimeOffset Email_confirmed_at` — **non-nullable**; absent → `0001-01-01`, loses null | `DateTime? EmailConfirmedAt` |
| **Untyped bags** | `object App_metadata` | `Dictionary<string,object> AppMetadata` |
| **Type name** | `UserSchema` (component name leaks) | `User` |
| **Extras** | `[JsonExtensionData] AdditionalProperties` on every model | n/a |

The nullability one is a **data-fidelity bug**, and it traces to the spec (fields not marked
`nullable: true`). Verdict: **models are the part codegen does adequately** — deterministic,
low-behavior, and where cross-SDK field drift actually bites. Warts are template/spec work, not
fundamental.

### 3b. Operations — unusable as emitted
**0 `operationId`s in the spec** → NSwag falls back to `{lastPathSegment}{METHOD}` naming:

```
VerifyGETAsync / VerifyPOSTAsync / VerifyPOST2Async
ClientsGETAsync / ClientsGET2Async / ClientsDELETEAsync
Authorize2Async / Authorize3Async / CallbackGETAsync
```

**Only 15 named schemas vs 103 inline `type: object` bodies** → NSwag invents type names for
every request/response envelope:

```
Body, Body2 … Body23          (request bodies)
Response, Response2 … Response15   (response envelopes)
Anonymous, Anonymous2, Weak_password2   (nested inline objects)
Task<object> RecoverAsync(...)     (bodies too thin to type at all)
```

No maintainer accepts `Task<Response11> Token2Async(Grant_types2 grant_type, Body11 body)`.

### 3c. The flagship case: `/token`
Spec has **one** `POST /token` operation whose body is a single `type: object` merging the
fields of **five different grant flows** (password / refresh_token / id_token / pkce / web3).
NSwag faithfully emits **one** method:

```csharp
Task<AccessTokenResponseSchema> TokenAsync(Grant_type grant_type, Body? body = null, ...)
```

`Body` has every field of every flow, all optional (`Refresh_token`, `Password`, `Email`,
`Id_token`, `Auth_code`, `Code_verifier`, `Message`, `Signature`, `Chain`, …). The caller gets
zero guidance on which combination is legal.

Compare the hand-written SDK, which splits the same endpoint into **five ergonomic methods**:
`SignInWithEmail`, `SignInWithPhone`, `SignInWithIdToken`, `ExchangeCodeForSession`,
`RefreshAccessToken`. A generator **cannot** produce that split — the shape isn't in the spec.

### 3d. What's missing entirely (the hand-written value, §3/§7b of prior doc)
None of this is in the generated client, and none of it *can* be from this spec:
- **PKCE**: `SignInWithOtp`/`ResetPasswordForEmail` generate the code challenge/verifier and
  return a `PasswordlessSignInState`/`ResetPasswordForEmailState`. Generated `OtpAsync` returns
  `Response2` and does none of it.
- **CAPTCHA** `gotrue_meta_security` wrapping, **provider validation**
  (`SignInWithIdToken` throws on unsupported providers), **SSO XOR** (providerId vs domain),
  **dual-shape parsing** (`SignUpWithEmail` returns `Session` *or* `User`).
- **Auth/headers**: no apikey/`Authorization: Bearer`/`X-Client-Info`/dynamic-header merging.
  You'd wire it via the `PrepareRequest` partial or a `DelegatingHandler` — i.e. hand-written.
- **Error model**: generated code `throw`s `ApiException<ErrorSchema>` per status. SDK maps to
  `GotrueException` + `FailureHint` reason codes. Different contract.

### 3e. Bonus: codegen faithfully reproduces spec bugs
The spec swaps 401/403 on `/token` (401→ForbiddenResponse, 403→UnauthorizedResponse). The
generated code dutifully throws `"HTTP Forbidden response."` on 401 and vice-versa. Codegen
launders spec errors into shipped code — an argument for models-only generation where the blast
radius is contained.

---

## 4. Maintainability read

- **Regen seam works for models**: `partial` classes + `PrepareRequest`/`ProcessResponse` hooks
  are a real seam (NSwag's strength per §7c). You could own auth/error behavior in the partial
  half and regenerate the model half.
- **But operations fight the seam**: to make `TokenAsync` into `SignInWithEmail` you're not
  extending a partial, you're **hiding/wrapping** generated garbage behind a second hand-written
  facade — i.e. you maintain *both* the generated op layer and the ergonomic layer. That's worse
  than today's single hand-written `Api.cs`.
- **Pre-processing tax is permanent**: the response-inlining + malformed-ref fixes must run on
  every spec update, and spec bugs (§1B, §3e) become C# bugs unless caught upstream.

---

## 5. How this resolves the framework's Gate 2

> Gate 2: are specs rich enough to render idiomatic operations, or only structure?

**Answer for Auth: only structure, and only partially.** The spec is **model-rich / operation-
poor** (15 named schemas, but 0 operationIds and 103 anonymous bodies). Per the framework, "mostly
behavioral / spec can't express it → B strictly dominates on operations." Confirmed empirically.

**Recommended boundary (matches prior doc §11 hybrid):**
- **Models → A (NSwag or any deterministic tool).** Real value, deterministic, catches upstream
  field drift. Needs template polish (naming, nullability) + spec hygiene.
- **Operations + transport → B (hand-written on owned `Core`).** Where behavior lives; the spike
  shows generation here is a net negative.

**Cheapest high-value config, if we adopt any of this:** run NSwag/NJsonSchema in **schema-only
mode** (`openapi2csclient` → DTOs, or NJsonSchema directly) to emit *just* the 15 models, and
leave `Api.cs` hand-written against `Core`.

## 6. Prerequisites before codegen is even fair to evaluate again
These are **spec fixes**, independent of tool choice, and would benefit every language SDK:
1. Add `operationId` to all ~60 operations (kills the `VerifyPOST2Async` naming).
2. Promote the 103 inline `type: object` bodies to **named component schemas** (kills
   `Body11`/`Response15`).
3. Mark optional fields `nullable: true` (fixes the timestamp data-fidelity bug).
4. Fix the malformed `$ref` (§1B) and the 401/403 swap (§3e).

Without (1)+(2), *no* toolchain (NSwag, Kiota, TypeSpec-emitter) can produce idiomatic
operations — the information simply isn't in the document.
