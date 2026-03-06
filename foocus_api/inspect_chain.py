import requests

cfg = requests.get("http://127.0.0.1:7865/config", timeout=30).json()
comps = {c["id"]: c for c in cfg["components"] if "id" in c}

gen_id = [c["id"] for c in cfg["components"] if c.get("props", {}).get("elem_id") == "generate_button"][0]
print("generate_button_id", gen_id)

for i, dep in enumerate(cfg["dependencies"]):
    if gen_id not in dep.get("targets", []):
        continue
    print(i, "trigger=", dep.get("trigger"), "inputs=", len(dep.get("inputs", [])), "outputs=", len(dep.get("outputs", [])), "queue=", dep.get("queue"))
    print("  in0..6", [(x, comps.get(x, {}).get("type"), comps.get(x, {}).get("props", {}).get("elem_id"), comps.get(x, {}).get("props", {}).get("label")) for x in dep.get("inputs", [])[:7]])
    print("  out", [(x, comps.get(x, {}).get("type"), comps.get(x, {}).get("props", {}).get("elem_id"), comps.get(x, {}).get("props", {}).get("label")) for x in dep.get("outputs", [])])
    print("---")
