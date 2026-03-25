# TFramework

Unity向けのモジュール型ゲーム基盤フレームワークです。DI・非同期・リアクティブ・アセット管理を中核に据え、プロジェクト規模の拡大に耐える「疎結合なサービス設計」と「運用しやすい設定管理」を目的として実装しています。

> Note: 本リポジトリは個人の学習・検証用途として継続開発している成果物です。外部公開を前提としたサポートや互換性保証は行いません。

---

## 特徴（設計思想）

- **Service Oriented（モジュール単位の責務分離）**
  - UI / Audio / Network / SaveData / MasterData / Localization / Scene / Time などをサービスとして統合し、利用側はインターフェース中心で依存します。
- **DIによる依存関係管理**
  - Unityにおける「参照の増殖」「暗黙の初期化順」問題を、コンテナによる構成で抑制します。
- **非同期処理の標準化**
  - 通信、ロード、セーブなど待機が発生する処理を `async/await` ベースで統一し、呼び出し側の可読性と拡張性を確保します。
- **リアクティブなイベント設計**
  - 状態変化やイベント通知をストリームとして扱い、購読・破棄の規律を保った実装を可能にします。
- **設定（Settings）アセットの運用性**
  - `Resources` 配下の Settings アセットを前提に、検出・作成・移動を支援する EditorWindow を提供します。

---

## 技術スタック

- **Unity**: 6000.3.6f1（Unity 6 / LTS系）
- **DI**: VContainer
- **Async**: UniTask
- **Reactive**: R3
- **Asset Management**: Addressables（Resourceサービスでラップ）

---

## ディレクトリ構成（抜粋）

- `TFrameworkUnity/Assets/TFramework/`
  - [`Core/`](TFrameworkUnity/Assets/TFramework/Core/): 起動・ライフサイクル・共通インターフェース
  - [`Installer/`](TFrameworkUnity/Assets/TFramework/Installer/): DIの登録（ProjectLifetimeScope など）
  - [`Debug/`](TFrameworkUnity/Assets/TFramework/Debug/): ログ出力（File/Console など）
  - [`Pool/`](TFrameworkUnity/Assets/TFramework/Pool/): 汎用/Unityオブジェクトプール
  - [`Resource/`](TFrameworkUnity/Assets/TFramework/Resource/): アセットロード（Addressablesラップ）
  - [`UI/`](TFrameworkUnity/Assets/TFramework/UI/): Page/Dialog・アニメーション・仮想スクロール
  - [`Scene/`](TFrameworkUnity/Assets/TFramework/Scene/): シーン管理（ライフサイクル）
  - [`Audio/`](TFrameworkUnity/Assets/TFramework/Audio/): BGM/SE・AudioSourceプール・Mixer制御
  - [`SaveData/`](TFrameworkUnity/Assets/TFramework/SaveData/): 保存/暗号化/ストレージ/シリアライズ
  - [`MasterData/`](TFrameworkUnity/Assets/TFramework/MasterData/): CSV等のマスターデータ運用・コード生成
  - [`Localization/`](TFrameworkUnity/Assets/TFramework/Localization/): 多言語テーブル・プロバイダ
  - [`Network/`](TFrameworkUnity/Assets/TFramework/Network/): HTTP・シリアライズ・API生成（Generated含む）
  - [`FSM/`](TFrameworkUnity/Assets/TFramework/FSM/): 汎用有限ステートマシン（最小実装）
  - [`Time/`](TFrameworkUnity/Assets/TFramework/Time/): タイマー・時間スケール（基盤）
  - [`SDK/`](TFrameworkUnity/Assets/TFramework/SDK/): Ads/Analytics/Store の抽象インターフェース + Manager（ダミーAdsを含む）

---

## 主要モジュール概要

### Core（基盤）

- **目的**: 初期化順序・更新ループ・サービスの契約を統一
- **代表要素**
  - `IInitializable`: 非同期初期化の統一インターフェース
  - `IService`: モジュールのサービス境界
  - `Bootstrap`: 起動・ライフサイクル統合

### Resource（アセットロード）

- **目的**: Addressables/Resources 等のロード差異を吸収し、参照管理を一元化
- **設計方針**
  - 取得したアセットは「ハンドル（Handle）」として扱い、解放タイミングを明示できる設計を志向

### UI（ページ/ダイアログ + 仮想スクロール）

- **目的**: 画面遷移を Page として管理し、Dialog はスタック/キューとして扱えるようにする
- **ポイント**
  - UIの状態遷移は「画面の種類」「表示/非表示」「アニメーション」の3軸が混ざりやすいため、責務境界を分けて実装

### Network（HTTP + シリアライズ）

- **目的**: HTTP要求/応答の統一、環境切替、生成コードとの接続
- **ポイント**
  - API層の責務を `RequestBase` / `ResponseBase` に寄せ、送信・例外・リトライ等の方針を統一しやすい形を目指す

### SaveData（保存/暗号化）

- **目的**: プレイヤーデータの保存、暗号化、ストレージの差し替え
- **ポイント**
  - `IStorageProvider` / `ISaveDataSerializer` / `IEncryptionProvider` を分離し、プラットフォームや要件変更（暗号化有無、形式変更）に対応しやすくする

### FSM（有限ステートマシン）

- **目的**: 汎用的な状態遷移ロジックを小さく提供し、ゲーム進行やサブシステムの状態管理に利用可能にする
- **備考**
  - まずは最小構成（State/Transition/AnyTransition/ChangeState/Update）から導入し、必要に応じて拡張する前提

### SDK（外部SDKの抽象）

- **目的**: Ads/Analytics/Store をインターフェース化し、プロバイダ差し替えを容易にする
- **現状**
  - Ads は Editor 検証用の `DummyAdsProvider` を同梱
  - Analytics/Store はインターフェース中心（実運用には各Provider実装が必要）

---

## セットアップ

### 前提

- Unity Hub で **Unity 6000.3.6f1** をインストール
- `TFrameworkUnity/` を Unity プロジェクトとして開く
- Package 取得（manifest / lock に従って解決）

### Settings（Resourcesアセット）の整備

メニューから Settings 管理ウィンドウを開き、必要な Settings を `Assets/Resources/` に揃えます。

- `TFramework/Settings/Modules`
  - **Install**: Settingsアセットが無い場合に作成
  - **Move/Duplicate**: Resources外にある場合の移動/複製

---

## 開発方針（品質・運用）

- **小さな責務**: クラス/関数は用途単位で小さく保ち、合成して拡張する
- **依存の向き**: 可能な限りインターフェースへ依存し、実装はDIで差し替える
- **テスト可能性**: Unity依存の薄い層は NUnit で検証し、Unity依存の強い層は PlayMode で検証する

---

## AI活用について

本プロジェクトでは、生産性の向上（調査・選択肢整理・テスト観点の洗い出し等）にAIを補助的に活用することがあります。ただし、最終的な設計判断・実装の整合性確認・リスク評価は人間側で行い、コードはプロジェクトの設計方針と読みやすさを優先して整えています。

---

## License

All rights reserved.