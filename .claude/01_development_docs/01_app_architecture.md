# アプリケーションアーキテクチャ設計

> このドキュメントは MetalCuttingSim のアーキテクチャに合わせて更新してください。

プロジェクト全体のアーキテクチャ方針とシステム間の依存関係を定義する。

---

## 全体構成

```
GameManager（シングルトン）
├── CuttingSystem        # 切削シミュレーションコア
├── MachineSystem        # 工作機械制御
├── MaterialSystem       # 素材・工具管理
├── UISystem             # HUD・メニュー管理
└── AudioSystem          # BGM・SE 管理
```

> プロジェクトに合わせてシステム構成を変更してください。

## レイヤー構造

```
UI Layer        HUD, メニュー, 各画面
   ↕ イベント
Game Layer      GameManager, 各 System
   ↕ 参照
Data Layer      ScriptableObject（パラメータ定義）
```

## 依存関係の原則

- **GameManager は各 System を保持する**（逆は禁止）
- **System 間の直接参照は禁止**（GameManager 経由またはイベントで通信）
- **ScriptableObject はどの System からも参照可能**（純粋なデータ）
- **UI Layer は Game Layer を参照できるが、逆は禁止**

## ScriptableObject 設計方針

| SO 名 | 用途 |
|-------|------|
| （例）CuttingSettingsSO | 切削パラメータ（送り速度・切込み量・回転数等） |
| （例）MaterialSettingsSO | 素材特性（硬度・切削抵抗等） |
| （例）ToolSettingsSO | 工具パラメータ（工具径・刃数・耐摩耗性等） |

> 実際の ScriptableObject 一覧はプロジェクトに合わせて記入してください。

## 命名規則

| 種別 | 命名パターン | 例 |
|------|------------|-----|
| MonoBehaviour | PascalCase | `CuttingManager`, `MachineController` |
| ScriptableObject | PascalCase + SO | `CuttingSettingsSO` |
| Interface | 先頭に I | `ICuttable`, `ISimulatable` |
| イベント | On + 動詞過去形 | `OnCuttingCompleted`, `OnToolWorn` |

---

## 改訂履歴

| 版数 | 日付 | コミット | 内容 |
|------|------|---------|------|
| 1.0 | 2026-05-28 | (初版) | 初版作成 |
