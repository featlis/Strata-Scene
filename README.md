# Strata Scene (v0.1.0 プロトタイプ)

**Windows 11 特化 タスクバー統合型ワークスペース・シーンスイッチャー**

Strata Scene は、Windows 11 のタスクバー領域に自然に溶け込む常駐型オーバーレイ HUD と、作業状況（Work / Game / Focus など）に応じたアプリ起動・ウィンドウ整理を行うシーン切り替えユーティリティです。

作業中のフォーカスを一切奪わない非侵入型（No-Activate）設計と、アイドル時メモリ消費 15MB 前後の極めて軽量な動作を特徴としています。

---

## 🌟 主な機能

### 1. タスクバー統合型 HUD ウィジェット
- **現在のモード表示 (`widget_mode`)**: 現在アクティブなシーン名とテーマカラーを表示。クリックでランチャー呼び出し。
- **ポモドーロ集中タイマー (`widget_pomodoro`)**: 作業25分／休憩5分のカウントダウンタイマー。左クリックで開始/一時停止、右クリックでリセット、完了時はバルーン通知。
- **スクラッチパッド (`widget_scratchpad`)**: タスクバーから即座にメモを確認・記録できる1行メモ。Enter で保存、Esc で破棄。メモ入力時以外は完全非アクティブ。

### 2. 非侵入型（No-Activate）ウィンドウ制御
- `WS_EX_NOACTIVATE` / `WS_EX_TOOLWINDOW` / `WS_EX_TOPMOST` を適用し、クリックしてもアクティブウィンドウのフォーカスを奪いません。
- Win11 タスクバー操作時にもタスクバーの前面に自動再浮上（Z-Order 管理）。
- ゲームや動画視聴などのフルスクリーンアプリ検出時にウィジェットが自動退避（非表示）し、解除時に自動復帰。

### 3. 超高速シーンランチャー
- 既定ショートカット: `Alt+Space`
- 事前生成インスタンスにより呼び出しレイテンシ 25ms 未満。
- 日本語・英数字対応のファジー検索（頭文字・部分一致・あいまいスコアリング）。
- カーソル位置またはプライマリモニターの中央へ自動センタリング。

### 4. シーン切り替えと直前復元
- ワンキーで関連アプリの起動、不要プロセスの最小化/終了を実行。
- **直前シーン復元 (`Ctrl+Alt+Back`)**: シーン切り替え前の各ウィンドウ位置・最小化状態を正確に復元。

### 5. ウィジェット配置編集モード (`Ctrl+Shift+E`)
- ウィジェットをドラッグ＆ドロップでタスクバー上の任意の位置へ直感的に移動。
- 10 DIP 単位のグリッドスナップとタスクバー（左・中央・右）への自動アライメント。

### 6. Fluent デザイン設定 GUI
- タスクバートレイアイコンまたはランチャーから設定ウィンドウを開くことが可能。
- 物理キーボード入力を直接検出するホットキーキャプチャボックス。
- 設定変更は再起動不要で即座に常駐環境に反映（ライブリロード）。

---

## 💻 動作要件
- **OS**: Windows 11 (64-bit, x64)
- **ランタイム**: .NET 10 LTS Desktop Runtime (x64)
  - インストーラー起動時に未導入の場合は自動ダウンロード・インストールされます。

---

## 🚀 インストール

1. `dist/StrataScene-Setup-0.1.0.exe` を実行します。
2. 管理者権限不要の Per-User インストール（ユーザーのローカル AppData 配下）が行われます。
3. インストール完了後、タスクバーのシステムトレイに常駐します。

> アンインストール時には、`%APPDATA%\StrataScene` のユーザー設定・ログデータを保持するか削除するかを選択できます。

---

## 🛠️ 開発とビルド

### 必要環境
- [.NET 10 SDK](https://dotnet.microsoft.com/) (10.0.401 以降)
- PowerShell 7 または Windows PowerShell
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (インストーラー作成時)

### ビルドとテスト
```powershell
# ソリューション全体のビルド (Release)
dotnet build -c Release

# 単体テストの実行 (42 tests)
dotnet test -c Release --no-build
```

### パッケージングとインストーラー作成
```powershell
# 自動発行および Inno Setup インストーラービルド
powershell -ExecutionPolicy Bypass -File .\tools\publish.ps1
```
作成されたインストーラーは `dist/StrataScene-Setup-0.1.0.exe` に出力されます。

### パフォーマンス測定
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\measure-perf.ps1 -WaitSeconds 6
```

---

## 📁 設定ファイル構成

ユーザー設定は `%APPDATA%\StrataScene` に JSON 形式で保存されます。
- `config.json`: 全体設定、シーン定義、ウィジェット設定
- `schema.json`: JSON Schema（VS Code 等での自動補完用）
- `state.json`: 前回のシーン状態、スクラッチパッドのメモ内容
- `logs/`: アプリケーションログファイル

---

## 📄 ライセンス
MIT License - 詳細は [LICENSE](LICENSE) を参照してください。