/**
 * PostToolUse フック: git commit 後に設計書同期の必要性をチェック
 *
 * - git commit を含むコマンド時のみ実行
 * - Assets/Scripts/ や Assets/Prefabs/ 変更時に /update-docs の実行を促す
 */
const fs = require("fs");
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

try {
  const files = execSync("git diff --name-only HEAD~1 HEAD", {
    encoding: "utf-8",
    timeout: 5000,
  }).trim();

  if (!files) process.exit(0);

  const lines = files.split("\n");
  const docTriggers = [];

  if (lines.some((f) => f.startsWith("Assets/Scripts/")))
    docTriggers.push("Scripts");
  if (lines.some((f) => f.startsWith("Assets/Prefabs/")))
    docTriggers.push("Prefabs");
  if (lines.some((f) => f.endsWith(".unity")))
    docTriggers.push("Sceneファイル");

  if (docTriggers.length > 0) {
    console.log(
      JSON.stringify({
        systemMessage: `[doc-sync] このコミットには ${docTriggers.join("・")} の変更が含まれています。M/L 規模の作業であれば /update-docs を実行してください。`,
      })
    );
  }
} catch {
  // 初回コミット等で HEAD~1 が存在しない場合は無視
}
