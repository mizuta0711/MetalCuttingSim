---
name: design-review
description: 設計書のレビューを実施する。モード引数でゲームデザイン（feature）と技術設計（tech）を切り替える。
argument-hint: "[feature | tech | (省略=互換モード)]"
---

# 設計レビュー

機能設計書の Stage 1 / Stage 2 に対応するレビューを実施する。

## モード

| モード | 対象 | 使用エージェント |
|-------|------|-----------------|
| `feature` | 機能・ゲームシステム設計（Stage 1） | code-reviewer + game-designer（並列実行） |
| `tech` | 技術設計（Stage 2） | code-reviewer のみ |
| (省略) | 設計書全体 | code-reviewer のみ（互換モード） |

## Step 1: 対象設計書の特定

`$ARGUMENTS` をチェック:
- `feature` → Stage 1 モード
- `tech` → Stage 2 モード
- 上記以外 or 未指定 → 互換モード

対象の機能設計書（`docs/features/yyyymmdd_*.md`）は以下で特定:
1. 直近の未コミット変更から `docs/features/` 配下のファイルを探す
2. 見つからない場合はユーザーに確認

## Step 2: 設計書の部分読み

必要なセクションのみ読む（全文読みはしない）。

```bash
# メタ情報（冒頭 30 行）
# Read(file, offset=0, limit=30)

# Stage 1 セクションの開始行を特定
grep -n "^## 3\." {設計書パス}

# Stage 2 セクションの開始行を特定
grep -n "^## 4\." {設計書パス}
```

## Step 3: エージェント起動

### feature モード（並列実行）

code-reviewer と game-designer を並列起動する:

```
# code-reviewer（技術・設計整合性）
Agent(subagent_type: "code-reviewer", model: "sonnet", prompt:
  "Stage 1 設計レビュー。対象: {設計書パス} の「3. ゲーム・システム設計」セクション。
   以下の観点で評価して:
   - 設計書間の矛盾（他の .claude/01_development_docs/ との整合）
   - MonoBehaviour 責務分離の妥当性
   - ScriptableObject 活用の妥当性
   - 依存関係の方向性（循環参照がないか）
   結果は『要修正』テーブル形式で返して。")

# game-designer（ゲームデザイン・体験）
Agent(subagent_type: "game-designer", model: "sonnet", prompt:
  "Stage 1 設計レビュー。対象: {設計書パス} の「3. ゲーム・システム設計」セクション。
   以下の観点でゲームデザイン妥当性を評価して:
   - コアループとの整合性
   - プレイヤー体験・難易度バランス
   - 代替案の検討余地
   - このゲームのコンセプト・ゲーム性との一貫性
   結果は『課題・懸念』テーブル形式（優先度: 高/中/低付き）で返して。")
```

### tech モード

code-reviewer を起動:

```
Agent(subagent_type: "code-reviewer", model: "sonnet", prompt:
  "Stage 2 設計レビュー。対象: {設計書パス} の「4. 技術設計」セクション。
   以下の観点で評価して:
   - クラス設計・依存関係の妥当性
   - Unity API の適切な使用（MonoBehaviour ライフサイクル等）
   - パフォーマンス懸念（Update() での重い処理など）
   - 命名規則・namespace 規約の遵守
   結果は『要修正』テーブル形式で返して。")
```

## Step 4: 結果統合 + ファイル保存

`docs/reviews/YYYYMMDD_HHMMSS_design-review-{mode}.md` に保存。

### feature モードの統合フォーマット

```markdown
# Stage 1 設計レビュー結果

- 実施日時: YYYY-MM-DD HH:MM
- 対象: {設計書パス}
- モード: feature (Stage 1)

## 総合判定: ✅ 承認 / ⚠️ 条件付き承認 / ❌ 差し戻し

## code-reviewer（技術・設計整合性）

| # | 重要度 | 内容 | 対象 | 対応状況 |
|---|--------|------|------|---------|

## game-designer（ゲームデザイン）

| # | 優先度 | 観点 | 指摘内容 | 対応状況 |
|---|--------|------|---------|---------|
```

## Step 5: ユーザーへの報告

- 総合判定
- 重要な指摘の件数
- レビューファイルのパス
- feature モード: 「ユーザー承認後、Stage 2 の記入に進んでください」
- tech モード: 「ユーザー承認後、実装フェーズに進んでください」
