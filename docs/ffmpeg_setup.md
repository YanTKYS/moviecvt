# FFmpeg 配置手順

## 概要

このツールは動画変換に `ffmpeg.exe` を使用します。
FFmpeg 本体はリポジトリに含まれておらず、管理者が別途入手して配置する必要があります。

> **重要:** `ffmpeg.exe` 単体だけでは動作しないビルドがあります。
> 配布元のZIPに含まれるDLLファイル一式を、`ffmpeg.exe` と同じフォルダに配置してください。

---

## 1. 配置先とフォルダ構成

```
MovieConverter.exe と同じフォルダ内の bin/ffmpeg/ フォルダ
```

具体的なパス（例）:

```
C:\Tools\MovieConverter\
├── MovieConverter.exe
└── bin\
    └── ffmpeg\
        ├── ffmpeg.exe        ← 必須（ファイル名は ffmpeg.exe 固定）
        ├── avcodec-*.dll     ┐
        ├── avformat-*.dll    │
        ├── avutil-*.dll      │ 配布元ZIPに含まれるDLL一式を
        ├── swresample-*.dll  │ ffmpeg.exe と同じフォルダに配置
        ├── swscale-*.dll     │
        └── その他のDLL       ┘
```

**注意:**

- ファイル名は必ず `ffmpeg.exe` とすること（`ffmpeg-win64.exe` などは不可）
- `ffprobe.exe` や `ffplay.exe` は必須ではないが、同梱しても問題ない
- DLL同梱型のffmpegを使う場合は、**配布元のZIPのフォルダ構成を崩さずに** `bin/ffmpeg/` へコピーすること
- DLLを欠いた状態で実行すると、終了コード `-1073741515`（DLL不足）が返ることがある

---

## 2. FFmpeg の入手方法

### 公式・推奨ビルド配布元

FFmpeg の公式サイト（https://ffmpeg.org/download.html）から、
Windows 向けビルドを提供しているサードパーティサイトを参照してください。

代表的な配布元（インターネット接続が可能な端末から事前に入手）:

- **BtbN（GitHub）**: `ffmpeg-master-latest-win64-gpl.zip` または `lgpl` 版
- **Gyan.dev**: `ffmpeg-release-essentials.zip`

**閉域環境での手順:**

1. インターネットに接続できる端末でFFmpegのWindows版ZIPをダウンロード
2. ZIPを展開し、`bin/` フォルダ内の `ffmpeg.exe` と同梱DLLを確認する
3. `ffmpeg.exe` および同梱DLL一式を庁内ネットワーク経由または記録媒体でコピー
4. 上記の配置先（`bin\ffmpeg\`）に配置する

---

## 3. 動作確認方法

配置後、**コマンドプロンプト**または**PowerShell**で以下を実行して動作確認してください。

```bat
bin\ffmpeg\ffmpeg.exe -version
```

（MovieConverter.exeと同じフォルダで実行する場合）

または絶対パスで:

```bat
C:\Tools\MovieConverter\bin\ffmpeg\ffmpeg.exe -version
```

バージョン情報が表示されれば、`ffmpeg.exe` と必要DLLの配置は正常です。
**配布前に必ずこの確認を行ってください。**

---

## 4. バージョンの目安

- **最低要件**: FFmpeg 4.0 以上
- **推奨**: FFmpeg 6.0 以上（最新安定版を推奨）
- libx264 と aac エンコーダが含まれているビルドを選択すること

---

## 5. ライセンスに関する確認事項

FFmpegには GPL版と LGPL版があります。
利用目的に応じたビルドを選択し、ライセンスを必ず確認してください。
配布元・同梱ファイルの扱いについても、管理者が責任を持って確認してください。

詳細は `docs/license_notice.md` を参照してください。

---

## 6. トラブルシューティング

### アプリ起動時・変換実行時に「ffmpeg.exeが見つかりません」と表示される場合

1. `bin\ffmpeg\ffmpeg.exe` が存在するか確認する
2. フォルダ名・ファイル名のスペルが正しいか確認する（大文字・小文字は区別しない）
3. `ffmpeg.exe` のプロパティで「ブロックの解除」が必要な場合がある（ダウンロードしたファイルはWindowsがブロックすることがある）

### 変換実行時に「終了コード: -1073741515」が表示される場合

`ffmpeg.exe` は見つかっているが、必要なDLLが不足している可能性があります。

- `ffmpeg.exe` と同じフォルダ（`bin\ffmpeg\`）に、配布元ZIPに含まれるDLL一式を配置してください
- `bin\ffmpeg\ffmpeg.exe -version` を実行してバージョンが表示されるか確認してください
- 表示されない場合はDLL配置が不完全です。配布元のZIPを再確認してください

### 「プログラムの実行にエラーが発生しました」と表示される場合

- ffmpeg.exe が破損していないか、別の正常なビルドで試す
- 管理者権限が必要な場合は、IT担当者に相談する

---

*作成: 2026-05-23  更新: 2026-05-24  バージョン: v0.1.3*
