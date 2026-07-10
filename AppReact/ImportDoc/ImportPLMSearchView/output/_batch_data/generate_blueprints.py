#!/usr/bin/env python3
"""Batch Phase B generator for ImportPLMSearchView — auto-selects best JOIN plan."""
from __future__ import annotations

import csv
import json
import re
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]  # output/
DATA = Path(__file__).resolve().parent
OUT = ROOT

IDS = [
    23902, 24002, 24102, 25402, 26202, 28902, 30206, 30207,
    30213, 30214, 30215, 30216, 30217, 30218, 30219, 30221,
    30223, 30224, 30228, 30229, 30231, 30233,
]

# Preferred primary table family + link targets (auto defaults)
PROFILES = {
    23902: {
        "primary": "Plm_Trim_Header",
        "preferred_tables": ["Plm_Trim_Header", "Plm_Trim_Info", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4252,
        "txn": "Tab_4252",
        "group_id": 7,
        "group_name": "06 - Trims",
        "scope": "TrimHeaderOnly",
    },
    24002: {
        "primary": "Plm_Packaging_Header",
        "preferred_tables": ["Plm_Packaging_Header", "Plm_Packaging_Info", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4249,
        "txn": "Tab_4249",
        "group_id": 6,
        "group_name": "05 - Packaging",
        "scope": "PackagingHeaderOnly",
    },
    24102: {
        "primary": "Plm_Label_Header",
        "preferred_tables": ["Plm_Label_Header", "Plm_Label_Info", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4247,
        "txn": "Tab_4247",
        "group_id": 5,
        "group_name": "03 - Labels & Hangtags",
        "scope": "LabelHeaderOnly",
    },
    25402: {
        "primary": "Plm_Style_Header",
        "preferred_tables": ["Plm_Style_Header", "Plm_Style_Summary", "Plm_Publish_to_ERP", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4251,
        "txn": "Tab_4272",
        "group_id": 1,
        "group_name": "01 - Apparel - 10 Colorways",
        "scope": "StyleHeaderOnly",
    },
    28902: {
        "primary": "Plm_Fabric_Header",
        "preferred_tables": [
            "Plm_Fabric_Header", "Plm_Fabric_Info", "Plm_Fabric_Cost",
            "Plm_Attributes", "Plm_ReferenceBasicInfo",
        ],
        "plm_tab_id": 4258,
        "txn": "Tab_4258",
        "group_id": 3,
        "group_name": "02 - Fabrics",
        "scope": "FabricHeaderOnly",
    },
    30206: {
        "primary": "Plm_Artwork_Details",
        "preferred_tables": ["Plm_Artwork_Details", "Plm_Artworks", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4228,
        "txn": "Tab_4228",
        "group_id": 4,
        "group_name": "04 - Artwork",
        "scope": "ArtworkDetailsOnly",
    },
    30207: {
        "primary": "Plm_Color_Patette_Header",
        "preferred_tables": ["Plm_Color_Patette_Header", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4254,
        "txn": "Tab_4254",
        "group_id": 8,
        "group_name": "08 - Color Palette",
        "scope": "ColorPaletteHeaderOnly",
    },
    30228: {
        "primary": "Plm_Graphic_Requests",
        "preferred_tables": ["Plm_Graphic_Requests", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4259,
        "txn": "Tab_4259",
        "group_id": 9,
        "group_name": "09 - Graphic Requests",
        "scope": "GraphicRequestsOnly",
    },
    30229: {
        "primary": "Plm_Fabric_Header",
        "preferred_tables": [
            "Plm_Fabric_Header", "Plm_Fabric_Info", "Plm_Fabric_Cost",
            "Plm_Attributes", "Plm_Costing", "Plm_ReferenceBasicInfo",
        ],
        "plm_tab_id": 4258,
        "txn": "Tab_4270",
        "group_id": 3,
        "group_name": "02 - Fabrics",
        "scope": "FabricHeaderOnly",
    },
    30231: {
        "primary": "Plm_Style_Header",
        "preferred_tables": ["Plm_Style_Header", "Plm_Style_Summary", "Plm_Fabrics___Trims", "Plm_ReferenceBasicInfo"],
        "plm_tab_id": 4251,
        "txn": "Tab_4251",
        "group_id": 1,
        "group_name": "01 - Apparel - 10 Colorways",
        "scope": "StyleHeaderOnly",
    },
}

# Style-family (same as 23702 plan B)
STYLE_PROFILE = {
    "primary": "Plm_Style_Header",
    "preferred_tables": ["Plm_Style_Header", "Plm_Style_Summary", "Plm_ReferenceBasicInfo"],
    "plm_tab_id": 4251,
    "txn": "Tab_4251",
    "group_id": 1,
    "group_name": "01 - Apparel - 10 Colorways",
    "scope": "StyleHeaderOnly",
}
for sid in (30213, 30214, 30215, 30216, 30217, 30218, 30219, 30221, 30223, 30233):
    PROFILES[sid] = STYLE_PROFILE

SKIP = {
    26202: "No imported Request Header table / SubItem 3865 not in FieldMapping",
    30224: "Trims Approval/Tracking are GridColumn-only; exclude strategy cannot cover criteria/view",
}

ALIASES = {
    "hdr": None,  # filled per primary
}


def read_tsv(path: Path):
    rows = []
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    for line in text.splitlines():
        line = line.strip("\r")
        if not line or line.startswith("---") or "rows affected" in line.lower():
            continue
        # skip sqlcmd noise
        if line.startswith("Msg ") or line.startswith("Changed database"):
            continue
        rows.append(line.split("\t"))
    return rows


def parse_null(v):
    if v is None or v == "" or v.upper() == "NULL":
        return None
    return v


def parse_int(v):
    v = parse_null(v)
    if v is None:
        return None
    try:
        return int(float(v))
    except ValueError:
        return None


def load_data():
    shells = {}
    for r in read_tsv(DATA / "shells.tsv"):
        if len(r) < 6:
            continue
        sid = parse_int(r[0])
        if sid is None:
            continue
        shells[sid] = {
            "id": sid,
            "name": r[1],
            "blQueryId": parse_int(r[2]),
            "referenceViewId": parse_int(r[3]),
            "autoExecute": parse_int(r[4]) == 1,
            "viewName": r[5] or r[1],
        }

    criteria = defaultdict(list)
    for r in read_tsv(DATA / "criteria.tsv"):
        if len(r) < 14:
            continue
        sid = parse_int(r[0])
        if sid is None:
            continue
        criteria[sid].append({
            "dcuId": parse_int(r[1]),
            "subItemId": parse_int(r[2]),
            "gridColumnId": parse_int(r[3]),
            "displayText": parse_null(r[4]) or "",
            "controlType": parse_int(r[5]),
            "entityId": parse_int(r[6]),
            "operationId": parse_int(r[7]) if parse_null(r[7]) is not None else 0,
            "row": parse_int(r[8]) or 1,
            "col": parse_int(r[9]) or 1,
            "sort": parse_int(r[10]),
            "isVisible": parse_int(r[11]) != 0,
            "isReadOnly": parse_int(r[12]) == 1,
            "defaultValue": parse_null(r[13]),
        })

    views = defaultdict(list)
    for r in read_tsv(DATA / "viewcols.tsv"):
        if len(r) < 9:
            continue
        sid = parse_int(r[0])
        if sid is None:
            continue
        views[sid].append({
            "colId": parse_int(r[1]),
            "subItemId": parse_int(r[2]),
            "gridColumnId": parse_int(r[3]),
            "displayText": parse_null(r[4]) or "",
            "controlType": parse_int(r[5]),
            "entityId": parse_int(r[6]),
            "sort": parse_int(r[7]) or 0,
            "isVisible": parse_int(r[8]) != 0,
        })

    # FieldMapping: subItem -> list of mappings (TabField preferred)
    fm_by_sub = defaultdict(list)
    for r in read_tsv(DATA / "fieldmapping.tsv"):
        if len(r) < 8:
            continue
        sub = parse_int(r[0])
        kind = parse_null(r[5]) or ""
        if kind not in ("TabField", "ReferenceField"):
            continue
        entry = {
            "subItemId": sub,
            "metaColumnId": parse_int(r[1]),
            "appTable": r[2],
            "appColumn": r[3],
            "plmTabId": parse_int(r[4]),
            "fieldKind": kind,
            "controlType": parse_int(r[6]),
            "entityId": parse_int(r[7]),
        }
        if sub is not None:
            fm_by_sub[sub].append(entry)

    return shells, criteria, views, fm_by_sub


def pick_mapping(sub_id, preferred_tables, fm_by_sub):
    """Resolve SubItem only within preferred table set (never cross-template)."""
    if sub_id is None:
        return None
    cands = fm_by_sub.get(sub_id, [])
    if not cands:
        return None
    preferred_set = set(preferred_tables)
    for t in preferred_tables:
        for c in cands:
            if c["appTable"] == t:
                return c
    # Do NOT fall back to unrelated templates (e.g. Label COO on a Fabric search)
    in_pref = [c for c in cands if c["appTable"] in preferred_set]
    return in_pref[0] if in_pref else None


def slug_key(label: str, fallback: str) -> str:
    s = re.sub(r"[^A-Za-z0-9]+", "", (label or "").strip())
    return s or fallback


def alias_for(table: str, primary: str) -> str:
    if table == primary:
        return "hdr"
    if table == "Plm_ReferenceBasicInfo":
        return "ref"
    mapping = {
        "Plm_Style_Summary": "sum",
        "Plm_Fabric_Info": "info",
        "Plm_Fabric_Cost": "cost",
        "Plm_Attributes": "attr",
        "Plm_Trim_Info": "info",
        "Plm_Label_Info": "info",
        "Plm_Packaging_Info": "info",
        "Plm_Publish_to_ERP": "pub",
        "Plm_Artworks": "art",
        "Plm_Costing": "cst",
        "Plm_Fabrics___Trims": "ft",
        "Plm_Graphic_Requests": "hdr",
        "Plm_Artwork_Details": "hdr",
        "Plm_Color_Patette_Header": "hdr",
    }
    return mapping.get(table, re.sub(r"[^a-z]", "", table.lower())[:6] or "t")


def build_blueprint(sid, shells, criteria, views, fm_by_sub, profile):
    shell = shells[sid]
    preferred = profile["preferred_tables"]
    primary = profile["primary"]

    crit_rows = criteria.get(sid, [])
    view_rows = [v for v in views.get(sid, []) if v["isVisible"]]

    used_tables = {primary, "Plm_ReferenceBasicInfo"}
    resolved_crit = []
    unmapped = []
    col_aliases = {}  # (table, column) -> output alias
    used_aliases = set()

    def ensure_alias(table, column):
        key = (table, column)
        if key in col_aliases:
            return col_aliases[key]
        # conflict if same column name already used from another table
        base = column
        alias = base
        if any(a == alias or a.endswith("." + alias) for a in used_aliases) or any(
            v == alias for v in col_aliases.values() if col_aliases and True
        ):
            # find existing
            existing = [k for k, v in col_aliases.items() if v == alias]
            if existing and existing[0] != key:
                alias = f"{column}_{sid if False else table.split('_')[-1][:6]}"
                # simpler unique: append sub-ish
                alias = f"{column}__{alias_for(table, primary)}"
        # also check collision among planned
        taken = set(col_aliases.values()) | used_aliases
        if alias in taken:
            n = 2
            while f"{column}_{n}" in taken:
                n += 1
            alias = f"{column}_{n}"
        col_aliases[key] = alias
        used_aliases.add(alias)
        return alias

    # Pre-register ReferenceId
    ensure_alias(primary, "ReferenceId")
    ensure_alias("Plm_ReferenceBasicInfo", "ReferenceCode")
    ensure_alias("Plm_ReferenceBasicInfo", "FolderId")

    for i, c in enumerate(crit_rows):
        label = c["displayText"] or f"Field_{c['subItemId'] or i}"
        if c["subItemId"] is None and not c["gridColumnId"]:
            # built-in Ref No / Created By
            if "ref" in label.lower() or label.lower() in ("ref no.", "ref no"):
                resolved_crit.append({
                    "ok": True,
                    "src": c,
                    "label": label or "Ref No.",
                    "table": "Plm_ReferenceBasicInfo" if primary != "Plm_ReferenceBasicInfo" else primary,
                    "column": "ReferenceId",
                    "alias": "ReferenceId",
                    "controlType": 20,
                    "entityId": None,
                    "builtin": True,
                })
                continue
            unmapped.append({"role": "criteria", "displayLabel": label or "Built-in", "reason": "No SubItemId / not in FieldMapping"})
            continue
        if c["gridColumnId"] and not c["subItemId"]:
            unmapped.append({"role": "criteria", "displayLabel": label, "reason": "GridColumn — gridColumnStrategy=exclude"})
            continue
        m = pick_mapping(c["subItemId"], preferred, fm_by_sub)
        if not m:
            unmapped.append({"role": "criteria", "displayLabel": label, "plmSubItemId": c["subItemId"], "reason": "Not in FieldMapping / ignored"})
            continue
        used_tables.add(m["appTable"])
        alias = ensure_alias(m["appTable"], m["appColumn"])
        ct = m["controlType"] if m["controlType"] is not None else c["controlType"]
        ent = m["entityId"] if m["entityId"] is not None else c["entityId"]
        resolved_crit.append({
            "ok": True,
            "src": c,
            "label": label,
            "table": m["appTable"],
            "column": m["appColumn"],
            "alias": alias,
            "controlType": ct,
            "entityId": ent,
            "builtin": False,
        })

    resolved_view = []
    for i, v in enumerate(view_rows):
        label = v["displayText"] or f"Col_{v['subItemId'] or i}"
        if v["gridColumnId"] and not v["subItemId"]:
            unmapped.append({"role": "view", "displayLabel": label, "reason": "GridColumn — gridColumnStrategy=exclude"})
            continue
        if v["subItemId"] is None:
            unmapped.append({"role": "view", "displayLabel": label, "reason": "No SubItemId"})
            continue
        m = pick_mapping(v["subItemId"], preferred, fm_by_sub)
        if not m:
            unmapped.append({"role": "view", "displayLabel": label, "plmSubItemId": v["subItemId"], "reason": "Not in FieldMapping / ignored"})
            continue
        used_tables.add(m["appTable"])
        alias = ensure_alias(m["appTable"], m["appColumn"])
        ct = m["controlType"] if m["controlType"] is not None else v["controlType"]
        ent = m["entityId"] if m["entityId"] is not None else v["entityId"]
        resolved_view.append({
            "ok": True,
            "src": v,
            "label": label,
            "table": m["appTable"],
            "column": m["appColumn"],
            "alias": alias,
            "controlType": ct,
            "entityId": ent,
        })

    # Coverage gate: skip if criteria mapped < 50% of those with subItem
    crit_with_sub = [c for c in crit_rows if c["subItemId"] is not None]
    mapped_crit = len(resolved_crit)
    total_crit = max(len(crit_rows), 1)
    if crit_with_sub:
        ratio = len([r for r in resolved_crit if not r.get("builtin")]) / max(len(crit_with_sub), 1)
        if ratio < 0.5 and mapped_crit < 2:
            return None, f"Criteria coverage too low ({ratio:.0%})"

    # Build SELECT / JOINs — only tables actually used + primary + ref
    join_order = [primary, "Plm_ReferenceBasicInfo"]
    for t in preferred:
        if t in used_tables and t not in join_order:
            join_order.append(t)
    for t in sorted(used_tables):
        if t not in join_order:
            join_order.append(t)

    # Fix alias collisions properly: rebuild aliases uniquely
    col_aliases = {}
    used_aliases = set()
    select_parts = []

    def add_select(table, column, force_alias=None):
        key = (table, column)
        if key in col_aliases:
            return col_aliases[key]
        a = force_alias or column
        if a in used_aliases:
            a = f"{column}__{alias_for(table, primary)}"
            n = 2
            while a in used_aliases:
                a = f"{column}__{alias_for(table, primary)}{n}"
                n += 1
        col_aliases[key] = a
        used_aliases.add(a)
        al = alias_for(table, primary)
        if a == column:
            select_parts.append(f"  [{al}].[{column}]")
        else:
            select_parts.append(f"  [{al}].[{column}] AS [{a}]")
        return a

    # Always include root keys
    add_select(primary, "ReferenceId")
    if "Plm_ReferenceBasicInfo" in join_order:
        add_select("Plm_ReferenceBasicInfo", "ReferenceCode")
        add_select("Plm_ReferenceBasicInfo", "FolderId")

    for r in resolved_crit:
        if r.get("builtin") and r["column"] == "ReferenceId":
            r["table"] = primary
            r["alias"] = add_select(primary, "ReferenceId")
        else:
            r["alias"] = add_select(r["table"], r["column"])

    for r in resolved_view:
        r["alias"] = add_select(r["table"], r["column"])

    # Deduplicate select_parts while preserving order
    seen_sel = set()
    uniq_sel = []
    for p in select_parts:
        if p not in seen_sel:
            seen_sel.add(p)
            uniq_sel.append(p)
    select_parts = uniq_sel

    joins_meta = []
    sql_joins = []
    for t in join_order:
        al = alias_for(t, primary)
        if t == primary:
            joins_meta.append({
                "alias": al,
                "appTableName": t,
                "joinType": "FROM",
                "role": "primary",
                "cardinality": "1:1",
                "plmTabId": profile["plm_tab_id"],
                "isTemplateHeaderTab": True,
            })
            continue
        joins_meta.append({
            "alias": al,
            "appTableName": t,
            "joinType": "LEFT",
            "leftTable": primary,
            "leftColumn": "ReferenceId",
            "rightTable": t,
            "rightColumn": "ReferenceId",
            "cardinality": "1:1",
        })
        sql_joins.append(
            f"LEFT OUTER JOIN [dbo].[{t}] AS [{al}]\n  ON [{al}].[ReferenceId] = [hdr].[ReferenceId]"
        )

    query_text = (
        "SELECT\n"
        + ",\n".join(select_parts)
        + f"\nFROM [dbo].[{primary}] AS [hdr]\n"
        + "\n".join(sql_joins)
    )

    # criteriaFields
    criteria_fields = []
    for idx, r in enumerate(resolved_crit):
        c = r["src"]
        key = f"criteria_{slug_key(r['label'], str(idx))}"
        field = {
            "integrationKey": key,
            "displayText": r["label"] or key,
            "sysTableFiledPath": r["alias"],
            "sysTableFiledFullPath": f"{r['table']}.{r['column']}",
            "controlType": r["controlType"] if r["controlType"] is not None else 2,
            "operationId": c.get("operationId", 0) if not r.get("builtin") else 0,
            "positionRow": c.get("row") or 1,
            "positionColumn": c.get("col") or 1,
            "isVisible": True,
            "sort": (idx + 1) * 10,
            "defaultValue": c.get("defaultValue"),
        }
        if r["entityId"] is not None:
            field["entityIntegrationId"] = str(r["entityId"])
        if c.get("isReadOnly"):
            field["isReadOnly"] = True
        if c.get("subItemId"):
            field["plmSubItemId"] = c["subItemId"]
        criteria_fields.append(field)

    # searchView fields
    view_fields = [
        {
            "displayText": "Ref No.",
            "sysTableFiledPath": "ReferenceId",
            "controlType": 20,
            "isTransRootId": True,
            "isVisible": True,
            "sort": 5,
        }
    ]
    for idx, r in enumerate(resolved_view):
        f = {
            "displayText": r["label"],
            "sysTableFiledPath": r["alias"],
            "controlType": r["controlType"] if r["controlType"] is not None else 2,
            "isVisible": True,
            "sort": (idx + 1) * 10,
        }
        if r["entityId"] is not None:
            f["entityIntegrationId"] = str(r["entityId"])
        if r["src"].get("subItemId"):
            f["plmSubItemId"] = r["src"]["subItemId"]
        view_fields.append(f)

    integ = f"Search_{sid}"
    bp = {
        "schemaVersion": 1,
        "generatedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "source": {
            "plmSearchTemplateId": sid,
            "plmSearchName": shell["name"],
            "plmReferenceViewId": shell["referenceViewId"],
            "plmReferenceViewName": shell["viewName"],
            "plmBlQueryId": shell["blQueryId"],
            "plmDatabase": "plm_live_20260602",
            "appDatabase": "TenantDB_PLM27",
            "tablePrefix": "Plm_",
            "selectedJoinPlanId": "AUTO",
            "gridColumnStrategy": "exclude",
            "primaryTableName": primary,
            "referenceScope": {
                "mode": profile["scope"],
                "notes": f"FROM {primary} drives row set; LEFT JOIN siblings on ReferenceId.",
            },
        },
        "search": {
            "name": shell["name"],
            "description": f"Imported from PLM SearchTemplate {sid}",
            "integrationId": integ,
            "usageType": "Management",
            "autoExecute": shell["autoExecute"],
            "saasApplicationId": None,
        },
        "dataSet": {
            "name": shell["name"],
            "queryMode": "SynthesizedFromFieldMapping",
            "primaryTableName": primary,
            "rootTableName": primary,
            "queryText": query_text,
            "joins": joins_meta,
            "filters": [],
            "tenantDataSourceRegisterId": None,
        },
        "joinPlan": {
            "planId": "AUTO",
            "score": None,
            "label": " + ".join(join_order),
            "semanticSummary": f"Auto-selected: primary {primary} with LEFT JOIN siblings covering mapped fields.",
            "tables": [
                {"appTableName": t, "role": "primary" if t == primary else ("reference" if t == "Plm_ReferenceBasicInfo" else "sibling"), "joinType": "FROM" if t == primary else "LEFT"}
                for t in join_order
            ],
        },
        "transactionGroup": {
            "transactionGroupId": profile["group_id"],
            "groupName": profile["group_name"],
            "primaryTransactionIntegrationId": profile["txn"],
        },
        "criteriaFields": criteria_fields,
        "searchView": {
            "name": shell["viewName"],
            "integrationId": f"{integ}_View",
            "viewType": "GridView",
            "gridOutputMode": 1,
            "fields": view_fields,
        },
        "linkTargets": [
            {
                "name": "Open",
                "actionType": "Edit",
                "transactionIntegrationId": profile["txn"],
                "linkTargetTransactionGroupId": profile["group_id"],
                "linkTargetTransactionGroupName": profile["group_name"],
                "sourceColumn": "ReferenceId",
                "sort": 1,
            },
            {
                "name": "Create",
                "actionType": "Create",
                "transactionIntegrationId": profile["txn"],
                "linkTargetTransactionGroupId": profile["group_id"],
                "linkTargetTransactionGroupName": profile["group_name"],
                "sourceColumn": "ReferenceId",
                "sort": 2,
            },
        ],
        "menu": {
            "registerInMainMenu": True,
            "menuTitle": shell["name"],
            "menuGroup": None,
        },
        "coverage": {
            "criteria": {
                "total": len(crit_rows),
                "mapped": len(resolved_crit),
                "ignored": len([u for u in unmapped if u["role"] == "criteria"]),
            },
            "view": {
                "total": len(view_rows),
                "mapped": len(resolved_view),
                "ignored": len([u for u in unmapped if u["role"] == "view"]),
            },
        },
        "unmappedPlmFields": unmapped,
    }
    return bp, None


def write_readme(folder: Path, sid: int, shell: dict, profile: dict, bp: dict, note: str = ""):
    cov = bp["coverage"]
    lines = [
        f"# SearchTemplate {sid} — {shell['name']}",
        "",
        f"**Phase B generated:** {datetime.now(timezone.utc).strftime('%Y-%m-%d')} (batch auto)",
        f"**PLM:** `plm_live_20260602`",
        f"**APP tenant:** `TenantDB_PLM27`",
        "",
        "## Auto selections",
        "",
        "| Item | Choice |",
        "|------|--------|",
        f"| JOIN plan | AUTO — `{bp['joinPlan']['label']}` |",
        "| Grid strategy | exclude |",
        f"| Reference scope | {profile['scope']} |",
        "| Missing fields | ignore |",
        f"| Link target | Group {profile['group_id']} (`{profile['group_name']}`) + `{profile['txn']}` |",
        f"| IntegrationId | `Search_{sid}` / `Search_{sid}_View` |",
        "| Main menu | yes |",
        "",
        "## Coverage",
        "",
        f"- Criteria: {cov['criteria']['mapped']}/{cov['criteria']['total']} mapped ({cov['criteria']['ignored']} ignored)",
        f"- View: {cov['view']['mapped']}/{cov['view']['total']} mapped ({cov['view']['ignored']} ignored)",
        "",
        "## Deliverable",
        "",
        "- `1_PlmSearch_ImportBlueprint.json`",
    ]
    if note:
        lines.extend(["", f"**Note:** {note}"])
    (folder / "README.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    shells, criteria, views, fm_by_sub = load_data()
    summary = {"generated": [], "skipped": [], "errors": []}

    for sid in IDS:
        if sid in SKIP:
            summary["skipped"].append({"id": sid, "reason": SKIP[sid], "name": shells.get(sid, {}).get("name")})
            print(f"SKIP {sid}: {SKIP[sid]}")
            continue
        if sid not in PROFILES:
            summary["skipped"].append({"id": sid, "reason": "No profile / no imported tables", "name": shells.get(sid, {}).get("name")})
            print(f"SKIP {sid}: no profile")
            continue
        if sid not in shells:
            summary["skipped"].append({"id": sid, "reason": "Not found in PLM", "name": None})
            print(f"SKIP {sid}: not in PLM")
            continue

        profile = PROFILES[sid]
        bp, err = build_blueprint(sid, shells, criteria, views, fm_by_sub, profile)
        if err or bp is None:
            summary["skipped"].append({"id": sid, "reason": err or "build failed", "name": shells[sid]["name"]})
            print(f"SKIP {sid}: {err}")
            continue

        folder = OUT / str(sid)
        folder.mkdir(parents=True, exist_ok=True)
        (folder / "1_PlmSearch_ImportBlueprint.json").write_text(
            json.dumps(bp, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
        )
        write_readme(folder, sid, shells[sid], profile, bp)
        summary["generated"].append({
            "id": sid,
            "name": shells[sid]["name"],
            "integrationId": f"Search_{sid}",
            "primary": profile["primary"],
            "criteria": bp["coverage"]["criteria"],
            "view": bp["coverage"]["view"],
        })
        print(
            f"OK   {sid} {shells[sid]['name']}: "
            f"crit {bp['coverage']['criteria']['mapped']}/{bp['coverage']['criteria']['total']} "
            f"view {bp['coverage']['view']['mapped']}/{bp['coverage']['view']['total']}"
        )

    (DATA / "batch_summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print("\n=== SUMMARY ===")
    print(f"Generated: {len(summary['generated'])}")
    print(f"Skipped:   {len(summary['skipped'])}")
    for s in summary["skipped"]:
        print(f"  - {s['id']}: {s['reason']}")


if __name__ == "__main__":
    main()
