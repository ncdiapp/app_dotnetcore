import json, subprocess, tempfile
from pathlib import Path

base = Path(r"c:\DevAppAI\AppAIClean\AppReact\ImportDoc\ImportPLMSearchView\output")
ids = [
    23902, 24002, 24102, 25402, 28902, 30206, 30207,
    30213, 30214, 30215, 30216, 30217, 30218, 30219, 30221,
    30223, 30228, 30229, 30231, 30233,
]
for sid in ids:
    bp = json.loads((base / str(sid) / "1_PlmSearch_ImportBlueprint.json").read_text(encoding="utf-8"))
    q = "SET NOCOUNT ON;\nSELECT TOP 1 * FROM (\n" + bp["dataSet"]["queryText"] + "\n) q;"
    p = Path(tempfile.gettempdir()) / f"val_{sid}.sql"
    p.write_text(q, encoding="utf-8")
    r = subprocess.run(
        ["sqlcmd", "-S", r"PC3B\MSSQLSERVER01", "-d", "TenantDB_PLM27", "-U", "sa", "-P", "appsa", "-C", "-i", str(p)],
        capture_output=True, text=True,
    )
    out = (r.stdout or "") + (r.stderr or "")
    err = ("Msg " in out) or r.returncode != 0
    status = "FAIL" if err else "OK  "
    print(f"{status} {sid} {bp['search']['name']}")
    if err:
        print(out[:500])
