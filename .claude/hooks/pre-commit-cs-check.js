/**
 * PreToolUse フック: git commit 前の C# スクリプト検証
 *
 * - git commit を含むコマンド時のみ実行
 * - ステージングされた Assets/ 以下の .cs ファイルを対象にチェック
 *   1. ファイル名とクラス名の一致（MonoBehaviour 等）
 *   2. namespace MetalCuttingSim の宣言有無
 */
const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

let input = "";
try {
  input = fs.readFileSync(0, "utf-8");
} catch {
  process.exit(0);
}

let payload = {};
try {
  payload = JSON.parse(input || "{}");
} catch {
  process.exit(0);
}

const command = payload?.tool_input?.command || "";
if (!/\bgit\s+commit\b/.test(command)) {
  process.exit(0);
}

let stagedFiles = [];
try {
  const out = execSync("git diff --cached --name-only --diff-filter=ACM", {
    encoding: "utf-8",
    timeout: 5000,
  });
  stagedFiles = out
    .trim()
    .split("\n")
    .filter((f) => f.endsWith(".cs") && f.startsWith("Assets/Scripts/"));
} catch {
  process.exit(0);
}

if (stagedFiles.length === 0) {
  process.exit(0);
}

const errors = [];
const warnings = [];

for (const file of stagedFiles) {
  if (!fs.existsSync(file)) continue;
  const content = fs.readFileSync(file, "utf-8");
  const baseName = path.basename(file, ".cs");

  // namespace チェック
  if (!/\bnamespace\s+MetalCuttingSim/.test(content)) {
    warnings.push(`${file}: namespace MetalCuttingSim が宣言されていません`);
  }

  // ファイル名とクラス名の一致チェック
  const classMatch = content.match(/\bpublic\s+(?:class|struct|enum)\s+(\w+)/);
  if (classMatch && classMatch[1] !== baseName) {
    errors.push(
      `${file}: クラス名 "${classMatch[1]}" がファイル名 "${baseName}" と一致しません`
    );
  }
}

if (errors.length > 0) {
  console.log(
    JSON.stringify({
      continue: false,
      stopReason:
        "C# スクリプトにエラーがあります。コミット前に修正してください:\n" +
        errors.join("\n"),
    })
  );
} else {
  const msg =
    warnings.length > 0
      ? `C# check: 警告 ${warnings.length} 件\n` + warnings.join("\n")
      : `C# check passed. (${stagedFiles.length} file(s))`;
  console.log(JSON.stringify({ systemMessage: msg }));
}
