# HUD (タスクバー・オーバーレイ) PoC 検証レポート

## 1. 概要
Strata Scene の非干渉型オーバーレイ HUD (Taskbar HUD) の設計、実装、および検証内容をまとめたドキュメント。

## 2. 実装方式

### 2.1 非干渉性 (No-Activate) の確保
- **拡張ウィンドウスタイル**:
  - `WS_EX_NOACTIVATE (0x08000000)`: クリックされてもフォアグラウンドにならず、背後アプリの入力フォーカスを奪わない。
  - `WS_EX_TOOLWINDOW (0x00000080)`: Alt+Tab 画面およびタスクバーのアプリアイコン一覧から除外。
  - `WS_EX_TOPMOST (0x00000008)`: 常に最前面レイヤーを維持。
- **メッセージプロシージャ制御**:
  - `WM_MOUSEACTIVATE (0x0021)` をインターセプトし、常に `MA_NOACTIVATE (0x0003)` を返却。
  - これにより、エディタやブラウザでタイピング中に HUD をクリックしてもキャレットが外れず、入力が継続される。

### 2.2 Windows 11 / Windows 10 におけるタスクバー Z-Order 対策
- **課題**: Windows 11 では、タスクバー (`Shell_TrayWnd`) をクリックした際、タスクバーが自身の TOPMOST レベルを押し上げてウィジェットが背後に隠れる場合がある。
- **対策**:
  1. `ForegroundTracker` が `EVENT_SYSTEM_FOREGROUND` を監視し、フォアグラウンドになったウィンドウがタスクバー (`Shell_TrayWnd` または `Shell_SecondaryTrayWnd`) であることを検出。
  2. 直ちに全ウィジェットに対して `SetWindowPos(HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER)` を発行し、アクティブ化せずに Z-Order の最前面へ再配置。
  3. 500ms 間隔のタイマーフォールバックでも定期的に最前面性を維持。

### 2.3 タスクバー追従と解像度変更対応
- `TaskbarTracker` が非表示ウィンドウで以下の Win32 メッセージを受信:
  - `WM_DISPLAYCHANGE (0x007E)`
  - `WM_SETTINGCHANGE (0x001A)`
  - `WM_DPICHANGED (0x02E0)`
  - `RegisterWindowMessage("TaskbarCreated")` (explorer.exe 再起動検知)
- タスクバーの座標変動や解像度変更があった場合、自動的に各ウィジェットの物理座標・DPIスケーリングを再計算して `SetWindowPos` で再配置。

## 3. 手動確認・検証チェックリスト

| 項目 | 期待動作 | 検証結果 |
| :--- | :--- | :--- |
| 入力フォーカス非干渉 | メモ帳やVS Code等で文字入力中に CurrentMode ウィジェットをクリックしても入力フォーカスが失われないこと | 正常 (MA_NOACTIVATE により背後アプリのキャレットが継続) |
| Alt+Tab 除外 | Alt+Tab ウィンドウ一覧およびタスクバー一覧にウィジェットが出現しないこと | 正常 (WS_EX_TOOLWINDOW 適用) |
| タスクバー前面維持 | タスクバーの空白部をクリックしても、ウィジェットがタスクバーの背面に隠れないこと | 正常 (Shell_TrayWnd 検知による EnsureTopmost 連動) |
| Scene 状態反映 | Scene が切り替わると、ウィジェットの色と名称が即座に連動更新されること | 正常 (CurrentModeWidget が SceneManager イベントを購読) |
| explorer 再起動追従 | タスクマネージャーからエクスプローラーを再起動してもウィジェットが再配置されること | 正常 (TaskbarCreated 受信による再配置) |
