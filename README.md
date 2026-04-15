# TFramework

Unity向けのモジュール型ゲーム基盤フレームワーク。DI、非同期処理、リアクティブプログラミング、アセット管理を中核に据え、プロジェクト規模の拡大に耐えうる「疎結合なサービス設計」と「運用しやすい設定管理」を目的として実装している。

> Note: 本リポジトリは個人の学習・検証用途として継続開発している成果物である。外部公開を前提としたサポートや後方互換性の保証は行わない。


## 特徴（設計思想）

- **Service Oriented（モジュール単位の責務分離）**
  - UI、Audio、Network、SaveData、MasterData、Localization、Scene、Time などをサービスとして統合。利用側はインターフェースを介して依存する。
- **DIによる依存関係管理**
  - Unityにおける「参照の増殖」や「暗黙的な初期化順」の問題を、DIコンテナによる構成で抑制する。
- **非同期処理の標準化**
  - 通信、ロード、セーブなど待機の発生する処理を `async/await` ベースで統一。呼び出し側の可読性と拡張性を確保する。
- **リアクティブなイベント設計**
  - 状態変化やイベント通知をストリームとして扱う。購読・破棄のライフサイクルを明確に管理できる実装を提供する。
- **設定（Settings）アセットの運用性**
  - `Resources` 配下の Settings アセットを前提に、検出・作成・移動を支援する EditorWindow を提供する。


## 技術スタック

- **Unity**: 6000.3.6f1（Unity 6 / LTS系）
- **DI**: VContainer
- **Async**: UniTask
- **Reactive**: R3
- **Asset Management**: Addressables（Resourceサービスでラップ）


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


## アーキテクチャの特長

本フレームワークは、中〜大規模なプロジェクトでの運用とパフォーマンスを前提に技術的な意思決定を行っている。

- **プレハブとメモリの最適化 (Pool / Audio)**
  - 頻繁に生成される `GameObject` や C# クラスインスタンスに対して統合的なプーリング（`GameObjectPool` / `GenericPool`）を提供し、GCスパイクやInstantiateコストを最小化する。
  - Audio再生においても、Addressablesと連動した非同期クリップキャッシュと `AudioSource` のプール機構を導入し、再生時のオーバーヘッドを抑制する。
- **高パフォーマンスで拡張性の高いUI設計 (UI)**
  - 画面遷移（`Page`）とポップアップ（`Dialog`）でライフサイクルの責務を完全に分離する。
  - 大量要素のリスト表示においてボトルネックとならないよう、仮想スクロール（`VirtualScroll`）を標準機能として組み込む。
  - アニメーションやロード待機などのフローは `UniTask` によってキャンセラブルな非同期処理として扱い、堅牢なUX管理を実現する。
- **クリーンアーキテクチャ的アプローチ (SDK / Network)**
  - 広告、分析、課金などの機能は `IAdsService` などの抽象インターフェースとして定義。ビジネスロジックは実装の詳細に依存せずDI（VContainer）経由で注入され、環境ごとの実装やダミー実装（`DummyAdsProvider` 等）への切り替えを容易にする。
  - HTTP通信ではシリアライズからエラーハンドリングまでを一元化し、自動生成APIコードとの連携を想定した設計を持つ。
- **型安全・条件駆動の有限ステートマシン (FSM)**
  - ジェネリクスによる型安全な状態管理（`StateMachine<TOwner>`）を実装。不要な外部参照を防ぎつつ、条件ベースの自動状態遷移（`Transition` / `AnyTransition`）をサポートし、複雑なゲーム進行を宣言的に記述できる。

## 主要モジュール概要

### Core（基盤）

- **目的**: 初期化順序、更新ループ、サービスの契約の統一。
- **代表要素**:
  - `IInitializable`: 非同期初期化の統一インターフェース
  - `IService`: モジュールのサービス境界
  - `Bootstrap`: 起動・ライフサイクルの統合

### Resource（アセットロード）

- **目的**: AddressablesやResources等のロード処理の差異を吸収し、参照管理を一元化する。
- **設計方針**:
  - 取得したアセットは「ハンドル (Handle)」として扱い、解放タイミングを明示できる設計としている。

### UI（ページ/ダイアログ + 仮想スクロール）

- **目的**: 画面遷移を Page として管理し、Dialog はスタック/キューとして扱えるようにする。
- **ポイント**:
  - UIの状態遷移は「画面の種類」「表示・非表示」「アニメーション」の3軸が混ざりやすいため、それぞれの責務境界を明確に分けて実装している。

### Network（HTTP + シリアライズ）

- **目的**: HTTPリクエスト/レスポンスの統一、環境の切り替え、生成コードとの統合。
- **ポイント**:
  - API層の責務を `RequestBase` / `ResponseBase` に集約し、送信・例外処理・リトライ等の方針を一元管理しやすい設計を目指している。

### SaveData（保存/暗号化）

- **目的**: プレイヤーデータの保存、暗号化、ストレージ実装の抽象化。
- **ポイント**:
  - `IStorageProvider` / `ISaveDataSerializer` / `IEncryptionProvider` を分離し、プラットフォームや要件変更（暗号化の有無、形式変更など）に柔軟に対応できるようにしている。

### FSM（有限ステートマシン）

- **目的**: 汎用的な状態遷移ロジックをシンプルに提供し、ゲーム進行やサブシステムの状態管理に利用する。
- **備考**:
  - 最小構成（State / Transition / AnyTransition / ChangeState / Update）から導入し、必要に応じて拡張する方針。

### SDK（外部SDKの抽象化）

- **目的**: Ads/Analytics/Store をインターフェース化し、プロバイダの差し替えを容易にする。
- **現状**:
  - Ads: Editor 検証用の `DummyAdsProvider` を同梱。
  - Analytics/Store: インターフェースのみ提供（実運用には各Providerの実装が必要）。

## セットアップ

### 前提

- Unity Hub で **Unity 6000.3.6f1** をインストールする。
- `TFrameworkUnity/` を Unity プロジェクトとして開く。
- Package を取得する（manifest / lock に従って解決される）。

### パッケージ定義

本リポジトリは UPM パッケージ定義として `package.json` を含んでいる。

- `TFrameworkUnity/Assets/TFramework/package.json`

### Settings（Resourcesアセット）の整備

メニューから Settings 管理ウィンドウを開き、必要な Settings アセットを `Assets/Resources/` に配置する。

- `TFramework/Settings/Modules`
  - **Install**: Settingsアセットが存在しない場合に新規作成する。
  - **Move/Duplicate**: Assets/Resources 外にある場合の移動や複製を行う。


## 開発方針（品質・運用）

- **小さな責務**: クラスやメソッドは単一責任を意識して小さく保ち、コンポジションによって機能を拡張する。
- **依存の方向**: 可能な限りインターフェースに依存させ、具象クラスはDIによって注入・差し替えを行う。
- **テスト容易性**: Unity非依存のロジックは EditMode (NUnit) で高速に検証し、依存の強い機能は PlayMode で検証する。

## AIの活用

開発作業の補助（アーキテクチャの調査、選択肢の整理、テスト観点の抽出など）としてAIツールを活用している。
ただし、最終的な設計判断、実装の妥当性確認、リスク評価はすべて人間が行い、自動生成されたコードもプロジェクトの設計思想や可読性の基準を満たすように修正したうえで採用している。


## License

All rights reserved.
