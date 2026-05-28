---
name: build-check
description: C#スクリプトの静的チェックを実行し、結果を報告する。コミット前やフェーズ完了時に実行する。
---

# C# スクリプト 品質チェック

Unity Editor を開く前に静的チェックで問題を早期発見する。

> **制約**: Unity C# の完全なコンパイルチェックは Unity Editor（Windows 側）でのみ可能。
> このスキルは Unity Editor なしで実行できる静的解析のみを対象とする。

## パス検出

```bash
SCRIPTS_DIR="Assets/Scripts"
```

## 実行チェック

### 1. namespace チェック

```bash
grep -rL "namespace MetalCuttingSim" "$SCRIPTS_DIR" --include="*.cs"
```

### 2. クラス名とファイル名の一致チェック

```bash
find "$SCRIPTS_DIR" -name "*.cs" | while read f; do
  base=$(basename "$f" .cs)
  grep -qE "public (class|struct|enum) $base\b" "$f" || echo "MISMATCH: $f"
done
```

### 3. マジックナンバー検出

```bash
grep -rn "\b[0-9]\{2,\}\b" "$SCRIPTS_DIR" --include="*.cs" \
  | grep -v "\/\/" | grep -v "namespace\|using\|#" | head -20
```

### 4. よくある Unity 禁止パターン

```bash
# GetComponent を Update/FixedUpdate/LateUpdate 内で呼んでいないか
grep -rn "void Update\|void FixedUpdate\|void LateUpdate" "$SCRIPTS_DIR" --include="*.cs" -A 10 \
  | grep "GetComponent"

# FindObjectOfType の使用件数
grep -rn "FindObjectOfType\|FindFirstObjectByType" "$SCRIPTS_DIR" --include="*.cs" | wc -l
```

## 結果報告

```
## C# 品質チェック結果

### namespace: ✅ 全ファイル宣言済み / ⚠️ N件未宣言
（未宣言のファイル一覧）

### クラス名/ファイル名一致: ✅ 問題なし / ⚠️ N件不一致
（不一致のファイル一覧）

### マジックナンバー: ✅ 検出なし / ⚠️ 要確認 N件
（該当箇所）

### Unity 禁止パターン: ✅ 検出なし / ⚠️ 要確認
（該当箇所）

### 総合判定: ✅ コミット可 / ⚠️ 要確認 / ❌ 修正必要
```

## エラーがある場合

問題が見つかった場合は修正を提案する。
コメント内の数値など意図的なものは無視して構わない。
