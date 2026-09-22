# 自治会向け団地マネージャー（WPF / MVVM）

自治会向けの日本語アプリです。VB6 版の後継です。入居者情報は **この PC のローカル SQLite だけ** に置きます。ネットワーク送信はありません。

## 必要環境

- Windows 10 / 11
- Visual Studio 2022（17.14 以降）または .NET 10 SDK
- ワークロード: **.NET デスクトップ開発**

## 起動

1. `DanchiManager.sln` を開く
2. 構成は `Debug | Any CPU`
3. 開始（F5）

初回起動で `%AppData%\KINRIN\data\danchi.db` を作り、錦林の棟レイアウト（空室）を入れます。氏名は入りません。

## 既存の `danchi.db` を使う

ロックを外し（パスワードは従来どおり `1502`）、「DBパス」から現行の SQLite を指定します。
テーブル `danchi` / `kaihi` の列名は VB6 版と揃えてあります。

## データの場所

| 用途 | 場所 |
|---|---|
| 既定 DB | `%AppData%\KINRIN\data\danchi.db` |
| 設定 | `%AppData%\KINRIN\danchi.settings.json` |
| バックアップ | 画面の「バックアップ」（SQLite の複製） |
| CSV | 「CSV出力」（UTF-8 BOM） |

ブラウザやクラウドには保存しません。

## 構成（MVVM）

```
View          MainWindow / *Window.xaml     {Binding ...}
ViewModel     MainViewModel ほか            INotifyPropertyChanged
Model         RoomRecord / Building         業務データ
Service       DatabaseService               SQLite（ローカル）
```

部屋カードは階ごとの `ItemsControl` + `UniformGrid` で表示します。行数・列数は棟の階数と部屋数に合わせて変わります。

## 画面

- メイン: 棟コンボ、部屋カード、件数、ロック
- 部屋編集: 氏名（IME 読み取りでフリガナ）、人数、バイク、自転車、電話、備考、会員、会費
- 棟編集: 階数・部屋数、4号飛ばし、部屋番号の手編集
- 駐車場 / 全棟集計 / 会費 / 封筒印刷 / 検索

印刷は Excel コンポーネントではなく、Windows の印刷ダイアログ（FlowDocument）です。
