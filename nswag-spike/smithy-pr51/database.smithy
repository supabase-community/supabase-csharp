$version: "2"

namespace io.supabase.database

use aws.protocols#restJson1
use io.supabase#StringMap

/// PostgREST-backed database API.
///
/// Base URL: https://{project-ref}.supabase.co/rest/v1
///
/// Known limitations:
///   1. Write operations return 204 (no body) by default and 200 with a body when
///      Prefer: return=representation — the model uses 200 throughout so generators
///      always produce body-parsing code; clients must tolerate empty bodies.
///   2. RPC GET arguments are function-specific; they are expressed via the same
///      @httpQueryParams map as row filters, with function-defined keys.
@restJson1
@title("Supabase Database API")
service DatabaseService {
  version: "1.0"
  operations: [
    SelectRows
    InsertRows
    UpdateRows
    UpsertRows
    DeleteRows
    CallRpcPost
    CallRpcGet
  ]
  errors: [DatabaseError]
}

// ─── Filter Operators ────────────────────────────────────────────────────────
//
// PostgREST horizontal filters are expressed as query parameters:
//   ?{column}={operator}.{value}   e.g. ?id=eq.5&name=like.foo*
//
// Use @httpQueryParams on input structures to pass a StringMap where each entry
// is a column=operator.value pair. Construct values using FilterOperator:
//   filters["id"]   = FilterOperator.EQ + "." + "5"      → ?id=eq.5
//   filters["name"] = FilterOperator.LIKE + "." + "foo*"  → ?name=like.foo*
//
// Negation: prefix the operator with "not.":
//   filters["id"] = "not.eq.5"  → ?id=not.eq.5
//
// Logical grouping: use the special keys "or" / "and" with a bracketed list:
//   filters["or"] = "(id.eq.1,id.eq.2)"  → ?or=(id.eq.1,id.eq.2)
//
// Named @httpQuery members (select, order, limit, offset, on_conflict) take
// precedence; do not put those keys in the filters map.

/// All filter operators supported by PostgREST.
/// Format a filter value as "{operator}.{value}", e.g. FilterOperator.EQ + ".5".
/// Prefix with "not." to negate: "not." + FilterOperator.EQ + ".5".
enum FilterOperator {
  /// = (equal)
  EQ = "eq"
  /// <> (not equal)
  NEQ = "neq"
  /// < (less than)
  LT = "lt"
  /// <= (less than or equal)
  LTE = "lte"
  /// > (greater than)
  GT = "gt"
  /// >= (greater than or equal)
  GTE = "gte"
  /// LIKE — case-sensitive pattern match, use * as wildcard
  LIKE = "like"
  /// ILIKE — case-insensitive pattern match, use * as wildcard
  ILIKE = "ilike"
  /// ~ — case-sensitive regex match
  MATCH = "match"
  /// ~* — case-insensitive regex match
  IMATCH = "imatch"
  /// IS — for null, true, false, unknown
  IS = "is"
  /// IS DISTINCT FROM
  IS_DISTINCT = "isdistinct"
  /// IN — value is a parenthesised list: "in.(1,2,3)"
  IN = "in"
  /// @> (contains — arrays, ranges, jsonb)
  CS = "cs"
  /// <@ (contained by — arrays, ranges, jsonb)
  CD = "cd"
  /// && (overlap — arrays, ranges)
  OV = "ov"
  /// << (strictly left of — ranges)
  SL = "sl"
  /// >> (strictly right of — ranges)
  SR = "sr"
  /// &< (does not extend to the left of — ranges)
  NXL = "nxl"
  /// &> (does not extend to the right of — ranges)
  NXR = "nxr"
  /// -|- (adjacent — ranges)
  ADJ = "adj"
  /// @@ to_tsquery (full-text search)
  FTS = "fts"
  /// @@ plainto_tsquery
  PLFTS = "plfts"
  /// @@ phraseto_tsquery
  PHFTS = "phfts"
  /// @@ websearch_to_tsquery
  WFTS = "wfts"
}

// ─── Row Operations ──────────────────────────────────────────────────────────

@http(method: "GET", uri: "/{table}", code: 200)
@readonly
operation SelectRows {
  input: SelectRowsInput
  output: RowsOutput
  errors: [DatabaseError]
}

structure SelectRowsInput {
  @required
  @httpLabel
  table: String

  /// Column selection — comma-separated, supports aliasing, casting, embedded
  /// resources, and JSON operators. e.g. "id,name,orders(total,status)".
  @httpQuery("select")
  select: String

  /// Ordering — e.g. "name.asc,age.desc.nullslast"
  @httpQuery("order")
  order: String

  /// Maximum number of rows to return.
  @httpQuery("limit")
  limit: Integer

  /// Row offset for pagination.
  @httpQuery("offset")
  offset: Integer

  /// Counting mode. e.g. "count=exact", "count=planned", "count=estimated".
  @httpHeader("Prefer")
  prefer: String

  /// Range-based pagination — e.g. "0-9" (ten rows starting at 0).
  @httpHeader("Range")
  range: String

  /// Unit for the Range header. Defaults to "items".
  @httpHeader("Range-Unit")
  rangeUnit: String

  /// Response format. e.g. "application/json" (default), "text/csv",
  /// "application/vnd.pgrst.object+json" (singular-row mode).
  @httpHeader("Accept")
  accept: String

  /// Target a non-default schema exposed by PostgREST.
  @httpHeader("Accept-Profile")
  acceptProfile: String

  /// Horizontal filters — each entry becomes a query parameter.
  /// Key: column name (or "or"/"and" for logical groups).
  /// Value: "{operator}.{value}" e.g. {"id": "eq.5", "name": "like.foo*"}.
  /// See FilterOperator for the full list of operators.
  @httpQueryParams
  filters: StringMap
}

structure RowsOutput {
  @httpPayload
  body: Blob

  /// Pagination info — e.g. "0-9/200" (range/total) or "0-9/*" (unknown count).
  @httpHeader("Content-Range")
  contentRange: String
}

@http(method: "POST", uri: "/{table}", code: 201)
operation InsertRows {
  input: InsertRowsInput
  output: RowsOutput
  errors: [DatabaseError]
}

structure InsertRowsInput {
  @required
  @httpLabel
  table: String

  /// JSON object or array of objects to insert.
  @httpPayload
  @required
  body: Blob

  /// Return behavior and conflict handling.
  /// e.g. "return=representation", "return=minimal" (default),
  ///      "return=headers-only", "resolution=merge-duplicates".
  @httpHeader("Prefer")
  prefer: String

  /// Columns to select in the returned representation (requires return=representation).
  @httpQuery("select")
  select: String

  /// Restrict which columns may be populated (useful with CSV uploads).
  @httpQuery("columns")
  columns: String

  /// Target a non-default schema for the write.
  @httpHeader("Content-Profile")
  contentProfile: String
}

@http(method: "PATCH", uri: "/{table}", code: 200)
operation UpdateRows {
  input: UpdateRowsInput
  output: RowsOutput
  errors: [DatabaseError]
}

structure UpdateRowsInput {
  @required
  @httpLabel
  table: String

  /// Partial JSON object with fields to update.
  @httpPayload
  @required
  body: Blob

  @httpHeader("Prefer")
  prefer: String

  @httpQuery("select")
  select: String

  @httpHeader("Content-Profile")
  contentProfile: String

  /// Horizontal filters — rows matching these filters will be updated.
  /// Key: column name. Value: "{operator}.{value}" e.g. {"id": "eq.5"}.
  @httpQueryParams
  filters: StringMap
}

@http(method: "PUT", uri: "/{table}", code: 200)
@idempotent
operation UpsertRows {
  input: UpsertRowsInput
  output: RowsOutput
  errors: [DatabaseError]
}

structure UpsertRowsInput {
  @required
  @httpLabel
  table: String

  /// JSON object or array of objects to upsert.
  @httpPayload
  @required
  body: Blob

  /// e.g. "return=representation", "resolution=merge-duplicates",
  ///      "resolution=ignore-duplicates".
  @httpHeader("Prefer")
  prefer: String

  @httpQuery("select")
  select: String

  /// Columns to match for conflict detection (if not the primary key).
  @httpQuery("on_conflict")
  onConflict: String

  @httpHeader("Content-Profile")
  contentProfile: String

  /// Horizontal filters — rows matching these filters will be upserted.
  @httpQueryParams
  filters: StringMap
}

@http(method: "DELETE", uri: "/{table}", code: 200)
@idempotent
operation DeleteRows {
  input: DeleteRowsInput
  output: RowsOutput
  errors: [DatabaseError]
}

structure DeleteRowsInput {
  @required
  @httpLabel
  table: String

  @httpHeader("Prefer")
  prefer: String

  @httpQuery("select")
  select: String

  @httpHeader("Content-Profile")
  contentProfile: String

  /// Horizontal filters — rows matching these filters will be deleted.
  @httpQueryParams
  filters: StringMap
}

// ─── RPC Operations ──────────────────────────────────────────────────────────
//
// Calls a PostgreSQL function via /rpc/{functionName}.
// POST: function receives named parameters as a JSON object in the body.
// GET:  read-only (STABLE/IMMUTABLE) functions only; parameters are query params.
//       Argument names are function-specific — pass them via the args map.

@http(method: "POST", uri: "/rpc/{functionName}", code: 200)
operation CallRpcPost {
  input: CallRpcPostInput
  output: RpcOutput
  errors: [DatabaseError]
}

structure CallRpcPostInput {
  @required
  @httpLabel
  functionName: String

  /// Named parameters as a JSON object, or a single argument when combined
  /// with Prefer: params=single-object.
  @httpPayload
  body: Blob

  /// e.g. "params=single-object" — treat the entire body as a single parameter.
  @httpHeader("Prefer")
  prefer: String

  @httpQuery("select")
  select: String

  @httpHeader("Content-Profile")
  contentProfile: String
}

@http(method: "GET", uri: "/rpc/{functionName}", code: 200)
@readonly
operation CallRpcGet {
  input: CallRpcGetInput
  output: RpcOutput
  errors: [DatabaseError]
}

structure CallRpcGetInput {
  @required
  @httpLabel
  functionName: String

  @httpQuery("select")
  select: String

  @httpHeader("Accept-Profile")
  acceptProfile: String

  /// Function arguments — each entry becomes a query parameter.
  /// Keys and value formats are defined by the PostgreSQL function signature.
  @httpQueryParams
  args: StringMap
}

structure RpcOutput {
  @httpPayload
  body: Blob

  @httpHeader("Content-Range")
  contentRange: String
}

// ─── Errors ──────────────────────────────────────────────────────────────────

@error("client")
structure DatabaseError {
  /// PostgreSQL error code (e.g. "23505") or PostgREST error code (e.g. "PGRST301").
  code: String

  /// Human-readable error message.
  message: String

  /// Extra context — constraint name, offending column, etc.
  details: String

  /// Hint from PostgreSQL.
  hint: String
}
