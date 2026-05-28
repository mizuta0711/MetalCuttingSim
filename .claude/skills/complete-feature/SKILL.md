---
name: complete-feature
description: "機能設計書の完了処理。全タスク完了後、sync-checkで設計書の整合性を確認し、completed/に移動する。"
user_invocable: true
---

# 機能設計書の完了処理

M/L フローの最後に実行する。全タスク完了後、設計書と実装の整合性を確認してから `completed/` に移動する。

## Step 1: 対象の設計書を特定

引数でファイル名が指定されている場合はそれを使用。
未指定の場合は `docs/features/` 配下の設計書一覧を表示し、ユーザーに選択を求める。

```bash
ls docs/features/*.md 2>/dev/null | grep -v TEMPLATE
```

## Step 2: タスク完了チェック

対象の設計書を読み、タスク一覧のステータスを確認:
- 🔵未実施 / 🟡実装中 のタスクが残っていたら完了処理を中断
- ✅完了 / ⏸️保留 / ❌却下 のみなら次のステップへ

## Step 3: /sync-check 実行（スコープ限定）

設計書の変更内容に関連する設計書のみを対象に整合性チェックを実行。

```bash
# 新規スクリプトが設計書に記載されているか確認
grep -rn "public class" Assets/Scripts/ --include="*.cs" | awk '{print $3}'

# シーンファイルが設計書と一致するか
find Assets/Scenes -name "*.unity" | sort
```

乖離が見つかった場合:
- 自動修正可能なもの（クラス追加・削除の反映等）は修正を実行
- 修正後、改訂履歴を更新

乖離がない場合:
- 「✅ 設計書と実装は同期済み」と報告

## Step 4: 完了処理

1. 設計書のメタ情報のステータスを更新:
   - ✅ と ❌ のみ → 🟢 完了
   - ⏸️保留 が 1 つ以上 → ⏸️ 一部保留

2. 移動:
   - 🟢 完了 → `docs/features/completed/` に移動
   - ⏸️ 一部保留 → `docs/features/pending/` に移動

3. 結果報告:
```
## 完了処理結果

### 設計書: {ファイル名}
### sync-check: ✅ 同期済み / ⚠️ N件修正
### ステータス: 🟢 完了 → completed/ に移動 / ⏸️ 一部保留 → pending/ に移動
```
