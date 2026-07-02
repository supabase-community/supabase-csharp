$version: "2"

namespace io.supabase

/// Common string list shape reused across services.
list StringList {
  member: String
}

/// Generic string-to-string map — used for arbitrary query parameter collections
/// (e.g. PostgREST filter params, RPC GET arguments).
map StringMap {
  key: String
  value: String
}
