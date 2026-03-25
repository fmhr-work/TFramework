## Debug

`TFramework.Debug` は、開発・検証時に必要なログ出力を統一的に扱うためのモジュールです。出力先（Unity Console / ファイル等）を差し替え可能にし、運用時の診断や不具合調査を行いやすい形を目指します。

---

## 概要

- **責務**: ログの記録・出力の抽象化（出力先の差し替え）
- **対象**: Unity Console、ファイル出力など

---

## 設計目標

- **出力先の分離**: 呼び出し側は「記録する」だけに集中し、出力先は `ILogOutput` で切り替える
- **運用指向**: 後から追えるログ（カテゴリ/レベル/相関）に寄せる（今後の深化）
- **低コスト**: 本番での過剰な文字列生成を抑える

---

## 構成（抜粋）

- `Logger/`
  - `TLogger`: ロガーの窓口
- `Interfaces/`
  - `ILogOutput`: 出力先の契約
- `Outputs/`
  - `UnityConsoleLogOutput`: Console出力
  - `FileLogOutput`: ファイル出力

---

## 処理フロー（ログの出力）

  ```mermaid
  flowchart TD
    caller[Caller] --> logger[TLogger]
    logger --> format[FormatMessage]
    format --> outputs[ILogOutput]
    outputs --> console[UnityConsole]
    outputs --> file[LogFile]
  ```

---

## 使い方（最小）

- **原則**: アプリ側は `TLogger` に出力し、出力先は環境に応じて `ILogOutput` を差し替える
- **推奨**: Network/SaveData 等のモジュールは、将来的にカテゴリを統一して追跡可能にする

---

## 未実装 / 今後

- `ROADMAP.md` の **フェーズ1** を参照
- カテゴリ/レベル/相関ID（通信トレース）を含む運用ログの体系化

