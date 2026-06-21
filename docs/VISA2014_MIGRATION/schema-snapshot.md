# VISA2014 schema snapshot (bootstrap only)

**Global table index** from one-time bootstrap after restore. Per-BO columns, FKs, and mapping detail live in **`discovery/{Entity}.yaml`** — not here.

**Status:** pending — run bootstrap SQL via `visa2014-sql-local`, then mark `order.yaml` → `bootstrapOnce` complete.

**Workflow:** [discovery/README.md](./discovery/README.md)

---

## Database confirmation

| Check | Value |
|-------|-------|
| Legacy database name | `VISA2015` (on `localhost\\SQLEXPRESS` in local dev) |
| Prod source name (TBD) | _fill when known_ |
| Restore date | _fill when restored_ |
| Total user tables | _fill from discovery SQL_ |

---

## Discovery log

_Append dated sections below after each MCP discovery session._

### Template (copy for new session)

```markdown
#### YYYY-MM-DD — Discovery session

- **MCP:** visa2014-sql-local
- **Notes:** (FK surprises, naming patterns, soft-delete columns, audit tables)
- **Tables reviewed:** (list)
- **Follow-ups:** (entity-inventory rows to add/update)
```

---

## Notable patterns (TBD)

- _XAF / PermissionPolicy / audit tables → mark `skip` in dossiers_
- _Legacy BO naming vs SQL table naming — record per dossier_
- _File/blob storage tables — plan attachment phase_

---

## Global table index

_Paste bootstrap query results here (schema, table, ~row_count). Do not add per-BO column lists._

| Schema | Table | ~Rows | Skip? | Notes |
|--------|-------|-------|-------|-------|
| dbo | _TBD_ | | | |
