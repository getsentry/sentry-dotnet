import re
import sys
from pathlib import Path


ACTION = re.compile(
    r"^\s*(?:-\s*)?uses:\s*(['\"]?)(github/codeql-action/[^@\s'\"]+)@([^\s'\"#]+)\1\s*(?:#.*)?$"
)


def check_versions(root):
    actions = []
    for path in sorted((root / ".github").rglob("*")):
        if not path.is_file() or path.suffix not in (".yml", ".yaml"):
            continue
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            match = ACTION.fullmatch(line)
            if match:
                actions.append((path.relative_to(root), number, match[2], match[3]))

    if not actions:
        raise ValueError("No CodeQL action references found under .github.")
    if len({ref for _, _, _, ref in actions}) != 1:
        details = "\n".join(f"  {path}:{number}: {action}@{ref}" for path, number, action, ref in actions)
        raise ValueError(f"CodeQL actions must use the same ref. Update all sub-actions together:\n{details}")
    return actions[0][3]


if __name__ == "__main__":
    try:
        ref = check_versions(Path(__file__).resolve().parent.parent)
    except ValueError as error:
        print(error, file=sys.stderr)
        sys.exit(1)
    print(f"All CodeQL actions use {ref}.")
